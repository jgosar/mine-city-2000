using System.IO.Compression;
using com.mc2k.MinecraftEditor;
using Xunit.Sdk;

namespace MineCity2000.Tests;

public class WorldComparerTests : IDisposable
{
  private readonly string tempRoot = Path.Combine(Path.GetTempPath(), "mc2k-comparer-" + Guid.NewGuid());

  public WorldComparerTests()
  {
    Directory.CreateDirectory(tempRoot);
  }

  public void Dispose()
  {
    TempDirs.deleteBestEffort(tempRoot);
  }

  private static List<Chunk> chunks(byte secondChunkBlock)
  {
    var first = new Chunk(0, 0);
    first.putBlock(1, 2, 3, 24);
    var second = new Chunk(1, 0);
    second.putBlock(0, 0, 0, secondChunkBlock);
    return new List<Chunk> { first, second };
  }

  private static byte[] levelDat()
  {
    return new LevelDat("city", new double[] { 1, 2, 3 }, false, 1000, 42).toBytes();
  }

  // Writes a world folder with level.dat and, per entry, region/<name> holding the given bytes.
  private string writeWorld(string name, Dictionary<string, byte[]> regions)
  {
    string dir = Path.Combine(tempRoot, name);
    Directory.CreateDirectory(Path.Combine(dir, "region"));
    File.WriteAllBytes(Path.Combine(dir, "level.dat"), levelDat());
    foreach (var region in regions)
    {
      File.WriteAllBytes(Path.Combine(dir, "region", region.Key), region.Value);
    }
    return dir;
  }

  private string zipOf(string worldDir)
  {
    string zip = worldDir + ".zip";
    ZipFile.CreateFromDirectory(worldDir, zip, CompressionLevel.Optimal, includeBaseDirectory: false);
    return zip;
  }

  private static Dictionary<string, byte[]> oneRegion(byte[] bytes)
  {
    return new Dictionary<string, byte[]> { { "r.0.0.mca", bytes } };
  }

  [Fact]
  public void assertWorldsEqual_identicalWorlds_passes()
  {
    string expected = zipOf(writeWorld("expected", oneRegion(MCAWriter.toBytes(chunks(7), 123))));
    string actual = writeWorld("actual", oneRegion(MCAWriter.toBytes(chunks(7), 123)));

    WorldComparer.assertWorldsEqual(expected, actual);
  }

  [Fact]
  public void assertWorldsEqual_oneBlockDiffers_failsNamingFileAndChunk()
  {
    string expected = zipOf(writeWorld("expected", oneRegion(MCAWriter.toBytes(chunks(7), 123))));
    string actual = writeWorld("actual", oneRegion(MCAWriter.toBytes(chunks(8), 123)));

    var ex = Assert.Throws<XunitException>(() => WorldComparer.assertWorldsEqual(expected, actual));

    Assert.Contains("r.0.0.mca", ex.Message);
    Assert.Contains("chunk (1,0)", ex.Message);
    Assert.Contains("differs at byte", ex.Message);
    Assert.DoesNotContain("chunk (0,0)", ex.Message);
  }

  [Fact]
  public void assertWorldsEqual_missingRegionFile_failsNamingFile()
  {
    string expected = zipOf(writeWorld("expected", oneRegion(MCAWriter.toBytes(chunks(7), 123))));
    string actual = writeWorld("actual", new Dictionary<string, byte[]>());

    var ex = Assert.Throws<XunitException>(() => WorldComparer.assertWorldsEqual(expected, actual));

    Assert.Contains("region/r.0.0.mca", ex.Message);
  }

  [Fact]
  public void assertWorldsEqual_extraFile_failsNamingFile()
  {
    string expected = zipOf(writeWorld("expected", new Dictionary<string, byte[]>()));
    string actual = writeWorld("actual", oneRegion(MCAWriter.toBytes(chunks(7), 123)));

    var ex = Assert.Throws<XunitException>(() => WorldComparer.assertWorldsEqual(expected, actual));

    Assert.Contains("region/r.0.0.mca", ex.Message);
  }

  [Fact]
  public void assertWorldsEqual_missingChunk_failsNamingChunk()
  {
    string expected = zipOf(writeWorld("expected", oneRegion(MCAWriter.toBytes(chunks(7), 123))));
    string actual = writeWorld("actual", oneRegion(MCAWriter.toBytes(chunks(7).Take(1).ToList(), 123)));

    var ex = Assert.Throws<XunitException>(() => WorldComparer.assertWorldsEqual(expected, actual));

    Assert.Contains("r.0.0.mca", ex.Message);
    Assert.Contains("(1,0)", ex.Message);
  }

  [Fact]
  public void assertWorldsEqual_differentTimestampTables_stillPasses()
  {
    byte[] early = MCAWriter.toBytes(chunks(7), 123);
    byte[] late = MCAWriter.toBytes(chunks(7), 999999);
    Assert.NotEqual(early.Skip(4096).Take(4096), late.Skip(4096).Take(4096));
    string expected = zipOf(writeWorld("expected", oneRegion(early)));
    string actual = writeWorld("actual", oneRegion(late));

    WorldComparer.assertWorldsEqual(expected, actual);
  }

  [Fact]
  public void assertWorldsEqual_levelDatDiffers_fails()
  {
    string expected = zipOf(writeWorld("expected", oneRegion(MCAWriter.toBytes(chunks(7), 123))));
    string actual = writeWorld("actual", oneRegion(MCAWriter.toBytes(chunks(7), 123)));
    File.WriteAllBytes(Path.Combine(actual, "level.dat"), new LevelDat("other", new double[] { 1, 2, 3 }, false, 1000, 42).toBytes());

    var ex = Assert.Throws<XunitException>(() => WorldComparer.assertWorldsEqual(expected, actual));

    Assert.Contains("level.dat", ex.Message);
  }

  [Fact]
  public void assertWorldsEqual_otherFileDiffers_failsWithByteOffset()
  {
    string expectedDir = writeWorld("expected", new Dictionary<string, byte[]>());
    File.WriteAllBytes(Path.Combine(expectedDir, "notes.txt"), new byte[] { 1, 2, 3 });
    string expected = zipOf(expectedDir);
    string actual = writeWorld("actual", new Dictionary<string, byte[]>());
    File.WriteAllBytes(Path.Combine(actual, "notes.txt"), new byte[] { 1, 9, 3 });

    var ex = Assert.Throws<XunitException>(() => WorldComparer.assertWorldsEqual(expected, actual));

    Assert.Contains("notes.txt", ex.Message);
    Assert.Contains("byte 1", ex.Message);
  }
}
