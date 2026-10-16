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
        Assert.Contains(archive.Entries, e => e.FullName == "level.dat");
        Assert.Contains(archive.Entries, e => e.FullName == "region/r.1.1.mca");
        archive.ExtractToDirectory(extracted);
      }
      WorldComparer.assertWorldsEqual(Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline", "default.zip"), extracted);
    }
    finally
    {
      TempDirs.deleteBestEffort(extracted);
      TempDirs.deleteBestEffort(workDir);
    }
  }

  [Fact]
  public void convert_rejectsInvalidFile()
  {
    var options = new MapperOptions(false, false, timestamp: 1700000000, randomSeed: 12345);
    byte[] notACity = System.Text.Encoding.ASCII.GetBytes("This is definitely not a SimCity 2000 file, just some text.");

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
}
