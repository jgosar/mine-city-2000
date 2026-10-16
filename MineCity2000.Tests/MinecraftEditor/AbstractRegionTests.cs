using com.mc2k.AnvilFile.Tags;
using com.mc2k.MinecraftEditor;

namespace MineCity2000.Tests;

public class AbstractRegionTests
{
  [Fact]
  public void putBlock_thenGetBlock_returnsSameValue()
  {
    var r = new AbstractRegion(0, 0);
    r.putBlock(17, 40, 40, 24);
    Assert.Equal(24, r.getBlock(17, 40, 40));
  }

  [Fact]
  public void putBlock_thenGetBlock_readsTheSameIndexWhenZAndYDiffer()
  {
    // Asymmetric on purpose: z and y differ within the section, so a read that swapped them would miss.
    var r = new AbstractRegion(0, 0);
    r.putBlock(1, 2, 3, 24);
    Assert.Equal(24, r.getBlock(1, 2, 3));
    Assert.Equal(0, r.getBlock(1, 3, 2));
  }

  [Fact]
  public void getBlock_onUntouchedPosition_isAir()
  {
    Assert.Equal(0, new AbstractRegion(0, 0).getBlock(5, 5, 5));
  }

  [Fact]
  public void putBlock_aboveWorldHeight_isIgnored()
  {
    var r = new AbstractRegion(0, 0);
    r.putBlock(0, 0, 256, 24);
    Assert.Equal(0, r.getBlock(0, 0, 256));
    Assert.Equal(0, r.getBlock(0, 0, 255));
  }

  [Fact]
  public void getBlock_onUntouchedSection_allocatesNothing()
  {
    var r = new AbstractRegion(0, 0);
    Assert.Equal(0, r.getBlock(5, 5, 5));
    Assert.Equal(0, r.getData(5, 5, 5));
    Assert.Null(r.getSection(0, 0, 0));
  }

  [Fact]
  public void putBlock_resetsDataNibble()
  {
    var r = new AbstractRegion(0, 0);
    r.putBlock(1, 1, 1, 35, 14);
    r.putBlock(1, 1, 1, 35);
    Assert.Equal(35, r.getBlock(1, 1, 1));
    Assert.Equal(0, r.getData(1, 1, 1));
  }

  [Fact]
  public void putBlockWithData_storesDataNibble()
  {
    var r = new AbstractRegion(0, 0);
    r.putBlock(3, 5, 5, 35, 14);
    Assert.Equal(35, r.getBlock(3, 5, 5));
    Assert.Equal(14, r.getData(3, 5, 5));
  }

  [Fact]
  public void fillSection_fillsAllSixteenCubedBlocksOfThatSectionOnly()
  {
    var r = new AbstractRegion(0, 0);
    r.fillSection(1, 2, 3, 24);
    Assert.Equal(24, r.getBlock(16, 32, 48));
    Assert.Equal(24, r.getBlock(31, 47, 63));
    Assert.Equal(0, r.getBlock(15, 32, 48));
    Assert.Equal(0, r.getBlock(16, 32, 47));
    Assert.Equal(0, r.getBlock(16, 32, 64));
  }

  [Fact]
  public void putBlock_intoFilledSection_overridesOnlyThatBlock()
  {
    var r = new AbstractRegion(0, 0);
    r.fillSection(0, 0, 0, 24);
    r.putBlock(3, 4, 5, 9);
    Assert.Equal(9, r.getBlock(3, 4, 5));
    Assert.Equal(24, r.getBlock(3, 5, 4));
  }

  [Fact]
  public void saveToFolder_writesOneRegionFileWithOnlyTouchedChunks()
  {
    string dir = Path.Combine(Path.GetTempPath(), "mc2k-test-" + Guid.NewGuid());
    Directory.CreateDirectory(dir);
    try
    {
      var r = new AbstractRegion(0, 0);
      r.putBlock(0, 0, 0, 24);
      r.putBlock(511, 511, 0, 24);
      r.saveToFolder(dir, 0);

      string[] files = Directory.GetFiles(dir);
      Assert.Equal(new[] { Path.Combine(dir, "r.0.0.mca") }, files);

      List<Chunk> chunks = MCAReader.readFile(files[0]);
      Assert.Equal(2, chunks.Count);
      Assert.Contains(chunks, c => c.getXPos() == 0 && c.getZPos() == 0);
      Assert.Contains(chunks, c => c.getXPos() == 31 && c.getZPos() == 31);

      foreach (Chunk chunk in chunks)
      {
        Section section = Assert.Single(chunk.getSections());
        Assert.Equal(0, section.getY());
        Assert.Equal(24, chunk.getXPos() == 0 ? section.getBlocks()[0] : section.getBlocks()[15 + 16 * 15]);
      }
    }
    finally
    {
      TempDirs.deleteBestEffort(dir);
    }
  }

  [Fact]
  public void saveToFolder_usesRegionCoordinatesInFileNameAndChunkPositions()
  {
    string dir = Path.Combine(Path.GetTempPath(), "mc2k-test-" + Guid.NewGuid());
    Directory.CreateDirectory(dir);
    try
    {
      var r = new AbstractRegion(2, 3);
      r.putBlock(16, 0, 0, 1);
      r.saveToFolder(dir, 0);

      string file = Path.Combine(dir, "r.2.3.mca");
      Assert.True(File.Exists(file));
      Chunk chunk = Assert.Single(MCAReader.readFile(file));
      Assert.Equal(2 * 32 + 1, chunk.getXPos());
      Assert.Equal(3 * 32, chunk.getZPos());
    }
    finally
    {
      TempDirs.deleteBestEffort(dir);
    }
  }
}
