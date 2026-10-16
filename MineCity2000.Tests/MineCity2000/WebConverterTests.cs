using System.IO.Compression;
using com.mc2k.MineCity2000;

namespace MineCity2000.Tests;

public class WebConverterTests
{
  private static readonly string FixtureCity = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.sc2");

  private static string newWorkDir()
  {
    return Path.Combine(Path.GetTempPath(), "mc2k-web-" + Guid.NewGuid());
  }

  [Fact]
  public void convertToZip_matchesGoldenBaseline()
  {
    string workDir = newWorkDir();
    string extracted = Path.Combine(Path.GetTempPath(), "mc2k-web-out-" + Guid.NewGuid());
    try
    {
      var options = new MapperOptions(false, false, timestamp: 1700000000, randomSeed: 12345);
      var progress = new List<int>();

      byte[] zip = WebConverter.convertToZip(File.ReadAllBytes(FixtureCity), "test", new DirectoryBuildingSource(TestPaths.buildingsDir()), options, progress.Add, workDir);

      Assert.False(Directory.Exists(workDir), "workDir should be deleted");
      Assert.NotEmpty(progress);

      using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
      {
        Assert.Contains(archive.Entries, e => e.FullName == "test/level.dat");
        Assert.Contains(archive.Entries, e => e.FullName == "test/region/r.1.1.mca");
        Assert.All(archive.Entries, e => Assert.StartsWith("test/", e.FullName));
        archive.ExtractToDirectory(extracted);
      }
      WorldComparer.assertWorldsEqual(Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline", "default.zip"), Path.Combine(extracted, "test"));
    }
    finally
    {
      TempDirs.deleteBestEffort(extracted);
      TempDirs.deleteBestEffort(workDir);
    }
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void convert_rejectsInvalidFile(bool tiny)
  {
    var options = new MapperOptions(false, false, timestamp: 1700000000, randomSeed: 12345);
    byte[] notACity = tiny
      ? new byte[] { 1, 2, 3 }
      : System.Text.Encoding.ASCII.GetBytes("This is definitely not a SimCity 2000 file, just some text.");

    var ex = Assert.Throws<InvalidDataException>(() =>
      WebConverter.convertToZip(notACity, "bad", new DirectoryBuildingSource(TestPaths.buildingsDir()), options, null, newWorkDir()));

    Assert.Contains("SimCity 2000", ex.Message);
  }

  [Fact]
  public void convert_rejectsTruncatedFile()
  {
    var options = new MapperOptions(false, false, timestamp: 1700000000, randomSeed: 12345);
    byte[] truncated = File.ReadAllBytes(FixtureCity).Take(5000).ToArray();
    string workDir = newWorkDir();

    Assert.Throws<InvalidDataException>(() =>
      WebConverter.convertToZip(truncated, "cut", new DirectoryBuildingSource(TestPaths.buildingsDir()), options, null, workDir));

    Assert.False(Directory.Exists(workDir), "workDir should be deleted even when conversion fails");
  }

  [Theory]
  [InlineData("../evil/..", "city")]
  [InlineData("", "city")]
  [InlineData("a\\b", "a_b")]
  public void convertToZip_sanitisesCityName(string cityName, string expectedFolder)
  {
    string parent = Path.Combine(Path.GetTempPath(), "mc2k-web-parent-" + Guid.NewGuid());
    string workDir = Path.Combine(parent, "work");
    try
    {
      Directory.CreateDirectory(parent);
      var options = new MapperOptions(false, false, timestamp: 1700000000, randomSeed: 12345);

      byte[] zip = WebConverter.convertToZip(File.ReadAllBytes(FixtureCity), cityName, new DirectoryBuildingSource(TestPaths.buildingsDir()), options, null, workDir);

      Assert.NotEmpty(zip);
      using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
      {
        Assert.All(archive.Entries, e => Assert.StartsWith(expectedFolder + "/", e.FullName));
        Assert.Contains(archive.Entries, e => e.FullName == expectedFolder + "/level.dat");
      }
      Assert.False(Directory.Exists(workDir), "workDir should be deleted");
      Assert.False(Directory.Exists(Path.Combine(parent, "evil")));
      Assert.Empty(Directory.GetFileSystemEntries(parent));
    }
    finally
    {
      TempDirs.deleteBestEffort(parent);
    }
  }

  [Fact]
  public void convert_rejectsFileTruncatedOnSegmentBoundary()
  {
    var options = new MapperOptions(false, false, timestamp: 1700000000, randomSeed: 12345);
    byte[] city = File.ReadAllBytes(FixtureCity);

    // walk the IFF header: 12 bytes, then name(4) + length(4) + body per segment
    int offset = 12;
    var boundaries = new List<int>();
    while (offset < city.Length && boundaries.Count < 2)
    {
      int length = (city[offset + 4] << 24) | (city[offset + 5] << 16) | (city[offset + 6] << 8) | city[offset + 7];
      offset += 8 + length;
      boundaries.Add(offset);
    }

    foreach (int boundary in boundaries)
    {
      byte[] cut = city.Take(boundary).ToArray();
      string workDir = newWorkDir();

      var ex = Assert.Throws<InvalidDataException>(() =>
        WebConverter.convertToZip(cut, "cut", new DirectoryBuildingSource(TestPaths.buildingsDir()), options, null, workDir));

      Assert.Contains("truncated", ex.Message);
      Assert.False(Directory.Exists(workDir));
    }
  }
}
