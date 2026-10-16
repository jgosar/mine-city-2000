using com.mc2k.AnvilFile.Tags;

namespace MineCity2000.Tests;

public class NbtTagTests
{
  private static CompoundNBTTag sampleTag()
  {
    return new CompoundNBTTag("", new List<AnyNBTTag> {
      new ByteNBTTag("b", 7), new IntNBTTag("i", -5), new LongNBTTag("l", 1L << 40),
      new StringNBTTag("s", "héllo"), new ByteArrayNBTTag("ba", new byte[] { 1, 2, 3 }),
      new IntArrayNBTTag("ia", new int[] { 4, 5 }),
      new CompoundNBTTag("inner", new List<AnyNBTTag> { new ShortNBTTag("sh", 300) }) });
  }

  [Fact]
  public void compoundTag_roundTripsThroughBytes()
  {
    var tag = sampleTag();
    byte[] bytes = tag.toBytesWithHeader();
    var parsed = (CompoundNBTTag)AnyNBTTag.parseTag(bytes, 0);
    Assert.Equal(bytes, parsed.toBytesWithHeader());
    Assert.Equal(-5, (int)parsed.getObjectOnPath("/i"));
    Assert.Equal((short)300, (short)parsed.getObjectOnPath("/inner/sh"));
  }

  [Fact]
  public void parsedTag_exposesEveryValueByPath()
  {
    var parsed = (CompoundNBTTag)AnyNBTTag.parseTag(sampleTag().toBytesWithHeader(), 0);
    Assert.Equal((byte)7, (byte)parsed.getObjectOnPath("/b"));
    Assert.Equal(1L << 40, (long)parsed.getObjectOnPath("/l"));
    Assert.Equal("héllo", (string)parsed.getObjectOnPath("/s"));
    Assert.Equal(new byte[] { 1, 2, 3 }, (byte[])parsed.getObjectOnPath("/ba"));
    Assert.Equal(new int[] { 4, 5 }, (int[])parsed.getObjectOnPath("/ia"));
  }

  [Fact]
  public void getObjectOnPath_unknownName_returnsNull()
  {
    Assert.Null(sampleTag().getObjectOnPath("/missing"));
  }

  [Fact]
  public void toBytesWithHeader_writesTypeNameAndPayload()
  {
    byte[] bytes = new IntNBTTag("n", 258).toBytesWithHeader();
    Assert.Equal(new byte[] { 3, 0, 1, (byte)'n', 0, 0, 1, 2 }, bytes);
  }

  [Fact]
  public void listTag_roundTripsThroughBytes()
  {
    var tag = new CompoundNBTTag("", new List<AnyNBTTag> {
      new ListNBTTag<double>("Pos", new double[] { 1.5, 64, -3 }),
      new ListNBTTag<float>("Rotation", new float[] { 135f, 60f }),
      new ListNBTTag<byte>("Empty", new byte[0]) });
    byte[] bytes = tag.toBytesWithHeader();
    var parsed = (CompoundNBTTag)AnyNBTTag.parseTag(bytes, 0);
    Assert.Equal(bytes, parsed.toBytesWithHeader());
    Assert.Equal(new double[] { 1.5, 64, -3 }, (double[])parsed.getObjectOnPath("/Pos"));
  }

  [Fact]
  public void parseTag_undefinedType_throws()
  {
    Assert.Throws<Exception>(() => AnyNBTTag.parseTag(new byte[] { 99, 0, 0, 0 }, 0));
  }
}
