using System.IO.Compression;
using com.mc2k.MineCity2000;

namespace MineCity2000.Tests;

public class BuildingSourceTests
{
  private static readonly string FixtureCity = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.sc2");

  private static byte[] zipOf(params (string name, byte[] content)[] entries)
  {
    using var buffer = new MemoryStream();
    using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
    {
      foreach (var (name, content) in entries)
      {
        ZipArchiveEntry entry = zip.CreateEntry(name);
        using Stream stream = entry.Open();
        stream.Write(content, 0, content.Length);
      }
    }
    return buffer.ToArray();
  }

  [Fact]
  public void zipSource_readsEntryWithOrWithoutFolderPrefix()
  {
    byte[] plain = { 1, 2, 3 };
    byte[] prefixed = { 4, 5, 6, 7 };
    var source = new ZipBuildingSource(zipOf(("10.mca", plain), ("buildings/198a.mca", prefixed)));

    Assert.Equal(plain, source.read("10.mca"));
    Assert.Equal(prefixed, source.read("198a.mca"));
    Assert.Null(source.read("999.mca"));

    // reads are repeatable and return independent copies
    byte[]? first = source.read("10.mca");
    byte[]? second = source.read("10.mca");
    Assert.Equal(plain, second);
    Assert.NotSame(first, second);
  }

  [Fact]
  public void directorySource_matchesFileSystem()
  {
    string dir = TestPaths.buildingsDir();
    var source = new DirectoryBuildingSource(dir);

    foreach (string file in Directory.GetFiles(dir, "*.mca"))
    {
      Assert.Equal(File.ReadAllBytes(file), source.read(Path.GetFileName(file)));
    }
    Assert.Null(source.read("999.mca"));
  }

  [Fact]
  public void mapper_withZipSource_producesSameWorldAsDirectorySource()
  {
    byte[] zip;
    using (var buffer = new MemoryStream())
    {
      using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
      {
        foreach (string file in Directory.GetFiles(TestPaths.buildingsDir(), "*.mca"))
        {
          archive.CreateEntryFromFile(file, Path.GetFileName(file));
        }
      }
      zip = buffer.ToArray();
    }

    string outDir = Path.Combine(Path.GetTempPath(), "mc2k-zipsource-" + Guid.NewGuid());
    try
    {
      var options = new MapperOptions(false, false, timestamp: 1700000000, randomSeed: 12345);
      new SCMapper(new ZipBuildingSource(zip), null).makeMap(FixtureCity, outDir, options);

      WorldComparer.assertWorldsEqual(Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline", "default.zip"), outDir);
    }
    finally
    {
      TempDirs.deleteBestEffort(outDir);
    }
  }
}
