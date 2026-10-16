using System.IO.Compression;
using com.mc2k.AnvilFile.Tags;
using com.mc2k.MinecraftEditor;

namespace MineCity2000.Tests;

public class LevelDatTests
{
  private static CompoundNBTTag parse(byte[] gzipped)
  {
    using var input = new GZipStream(new MemoryStream(gzipped), CompressionMode.Decompress);
    using var output = new MemoryStream();
    input.CopyTo(output);
    return (CompoundNBTTag)AnyNBTTag.parseTag(output.ToArray(), 0);
  }

  private static CompoundNBTTag levelDat(bool generateTerrain)
  {
    return parse(new LevelDat("city", new double[] { 1, 2, 3 }, generateTerrain, 1000, 42).toBytes());
  }

  [Fact]
  public void toBytes_encodesTimestampSeedAndVersion()
  {
    CompoundNBTTag tag = levelDat(false);
    Assert.Equal(1000000L, (long)tag.getObjectOnPath("/Data/LastPlayed"));
    Assert.Equal(42L, (long)tag.getObjectOnPath("/Data/RandomSeed"));
    Assert.Equal("flat", (string)tag.getObjectOnPath("/Data/generatorName"));
    Assert.Equal(19133, (int)tag.getObjectOnPath("/Data/version"));
  }

  [Fact]
  public void toBytes_encodesLevelNameAndSpawn()
  {
    CompoundNBTTag tag = levelDat(false);
    Assert.Equal("city", (string)tag.getObjectOnPath("/Data/LevelName"));
    Assert.Equal(1, (int)tag.getObjectOnPath("/Data/SpawnX"));
    Assert.Equal(2, (int)tag.getObjectOnPath("/Data/SpawnY"));
    Assert.Equal(3, (int)tag.getObjectOnPath("/Data/SpawnZ"));
  }

  [Fact]
  public void toBytes_withGenerateTerrain_usesDefaultGenerator()
  {
    Assert.Equal("default", (string)levelDat(true).getObjectOnPath("/Data/generatorName"));
  }

  [Fact]
  public void toBytes_calledTwice_isByteIdentical()
  {
    var ld = new LevelDat("city", new double[] { 1, 2, 3 }, false, 1000, 42);
    Assert.Equal(ld.toBytes(), ld.toBytes());
  }
}
