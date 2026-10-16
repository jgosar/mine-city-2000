using com.mc2k.AnvilFile.Tags;
using com.mc2k.MinecraftEditor;

namespace MineCity2000.Tests;

public class SectionTests
{
  private const byte Sandstone = 24;

  [Fact]
  public void newSection_isUniformAir_withoutArrays()
  {
    var s = new Section(3);
    Assert.True(s.isEmpty);
    Assert.False(s.isMaterialised);
    Assert.Equal(3, s.getY());
    Assert.Equal(0, s.getBlock(15, 15, 15));
    Assert.Equal(0, s.getData(15, 15, 15));
    Assert.False(s.isMaterialised); // reading allocates nothing
  }

  [Fact]
  public void uniformSection_readsItsBlockEverywhere_andMaterialisesToThatBlock()
  {
    var s = new Section(0, Sandstone);
    Assert.False(s.isEmpty);
    Assert.Equal(Sandstone, s.getBlock(0, 0, 0));
    Assert.Equal(Sandstone, s.getBlock(15, 15, 15));
    Assert.Equal(Sandstone, s.getBlock(3, 9, 12));
    Assert.Equal(0, s.getData(3, 9, 12));
    Assert.False(s.isMaterialised);

    byte[] blocks = s.getBlocks();
    Assert.True(s.isMaterialised);
    Assert.Equal(4096, blocks.Length);
    Assert.All(blocks, b => Assert.Equal(Sandstone, b));
    Assert.Equal(new byte[2048], s.getData());
  }

  [Fact]
  public void writingAirIntoUniformSandstone_materialisesAndKeepsTheRest()
  {
    var s = new Section(0, Sandstone);
    s.putBlock(4, 5, 6, 0, 0);

    Assert.True(s.isMaterialised);
    Assert.False(s.isEmpty);
    Assert.Equal(0, s.getBlock(4, 5, 6));
    int index = 4 + 16 * 6 + 256 * 5;
    byte[] blocks = s.getBlocks();
    for (int i = 0; i < blocks.Length; i++)
    {
      Assert.Equal(i == index ? 0 : Sandstone, blocks[i]);
    }
  }

  [Fact]
  public void writingTheUniformValue_doesNotMaterialise()
  {
    var air = new Section(0);
    air.putBlock(1, 2, 3, 0, 0);
    Assert.False(air.isMaterialised);
    Assert.True(air.isEmpty);

    var sand = new Section(0, Sandstone);
    sand.putBlock(1, 2, 3, Sandstone, 0);
    Assert.False(sand.isMaterialised);
  }

  [Fact]
  public void writingNonAirIntoAir_materialises_andIsNoLongerEmpty()
  {
    var s = new Section(0);
    s.putBlock(1, 2, 3, 35, 0);
    Assert.True(s.isMaterialised);
    Assert.False(s.isEmpty);
    Assert.Equal(35, s.getBlock(1, 2, 3));
    Assert.Equal(0, s.getBlock(1, 3, 2));
  }

  [Fact]
  public void writingDataIntoUniformSection_materialises()
  {
    var s = new Section(0);
    s.putBlock(0, 0, 0, 0, 5); // same block as the uniform fill, but non-zero data
    Assert.True(s.isMaterialised);
    Assert.Equal(5, s.getData(0, 0, 0));
  }

  [Fact]
  public void blockIndex_isXPlus16ZPlus256Y()
  {
    var s = new Section(0);
    s.putBlock(1, 2, 3, 99, 0);
    Assert.Equal(99, s.getBlocks()[1 + 16 * 3 + 256 * 2]);
    Assert.Equal(1, s.getBlocks().Count(b => b != 0));
  }

  [Fact]
  public void dataNibble_oddIndexIsHighNibble()
  {
    var s = new Section(0);
    s.putBlock(1, 0, 0, 5, 0xF);
    Assert.Equal(0xF0, s.getData()[0]);
  }

  [Fact]
  public void dataNibble_evenIndexIsLowNibble_andBothNibblesCoexist()
  {
    var s = new Section(0);
    s.putBlock(1, 0, 0, 5, 0xF);
    s.putBlock(0, 0, 0, 5, 0xF);
    Assert.Equal(0xFF, s.getData()[0]);
    Assert.Equal(0xF, s.getData(0, 0, 0));
    Assert.Equal(0xF, s.getData(1, 0, 0));
  }

  [Fact]
  public void dataNibble_overwritingOneNibbleLeavesItsNeighbour()
  {
    var s = new Section(0);
    s.putBlock(0, 0, 0, 5, 0xA);
    s.putBlock(1, 0, 0, 5, 0xB);
    s.putBlock(0, 0, 0, 5, 0x1);
    Assert.Equal(0xB1, s.getData()[0]);
    s.putBlock(1, 0, 0, 5, 0x0);
    Assert.Equal(0x01, s.getData()[0]);
  }

  [Fact]
  public void nbtTag_ofUniformSection_equalsTheTagOfAFullyAllocatedAirSection()
  {
    // Serialised form must not depend on whether the arrays were allocated lazily: lighting arrays are
    // present, sky light is 255, blocks and data are zero.
    CompoundNBTTag tag = new Section(2).NBTTag;
    byte[] sky = (byte[])tag.getObjectOnPath("/SkyLight");
    Assert.All(sky, b => Assert.Equal(255, b));
    Assert.Equal(new byte[4096], (byte[])tag.getObjectOnPath("/Blocks"));
    Assert.Equal(new byte[2048], (byte[])tag.getObjectOnPath("/Data"));
    Assert.Equal(new byte[2048], (byte[])tag.getObjectOnPath("/BlockLight"));
    Assert.Equal((byte)2, tag.getObjectOnPath("/Y"));
  }

  [Fact]
  public void nbtTag_roundTripsThroughTheParameterlessConstructor()
  {
    var s = new Section(1, Sandstone);
    s.putBlock(2, 3, 4, 35, 7);

    var copy = new Section { NBTTag = s.NBTTag };

    Assert.Equal(1, copy.getY());
    Assert.Equal(35, copy.getBlock(2, 3, 4));
    Assert.Equal(7, copy.getData(2, 3, 4));
    Assert.Equal(Sandstone, copy.getBlock(0, 0, 0));
  }

  [Theory]
  [InlineData((byte)0)]
  [InlineData(Sandstone)]
  public void nbtTag_ofUniformSection_doesNotMaterialise_andEqualsTheMaterialisedBytes(byte uniformBlock)
  {
    var uniform = new Section(5, uniformBlock);
    byte[] fromUniform = uniform.NBTTag.toBytesWithHeader();
    Assert.False(uniform.isMaterialised);
    Assert.Equal(uniformBlock == 0, uniform.isEmpty);

    var materialised = new Section(5, uniformBlock);
    materialised.getBlocks();
    Assert.True(materialised.isMaterialised);
    byte[] fromMaterialised = materialised.NBTTag.toBytesWithHeader();

    Assert.Equal(fromMaterialised.Length, fromUniform.Length);
    Assert.True(fromMaterialised.AsSpan().SequenceEqual(fromUniform), "NBT bytes differ");
  }
}
