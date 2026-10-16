using com.mc2k.AnvilFile.Tags;

namespace MineCity2000.Tests;

public class ByteConverterTests
{
  // readNumber rejects reads that end exactly at the end of the array, so give it one spare byte.
  private static byte[] padded(byte[] bytes)
  {
    return bytes.Concat(new byte[] { 0 }).ToArray();
  }

  [Fact]
  public void intBytes_isBigEndian()
  {
    Assert.Equal(new byte[] { 1, 2, 3, 4 }, ByteConverter.intBytes(0x01020304));
  }

  [Fact]
  public void shortBytes_isBigEndian_andKeepsTwosComplement()
  {
    Assert.Equal(new byte[] { 0x01, 0x2C }, ByteConverter.shortBytes(300));
    Assert.Equal(new byte[] { 0xFF, 0xFE }, ByteConverter.shortBytes(-2));
  }

  [Fact]
  public void longBytes_isBigEndian()
  {
    Assert.Equal(new byte[] { 0, 0, 1, 0, 0, 0, 0, 0 }, ByteConverter.longBytes(1L << 40));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(-1)]
  [InlineData(255)]
  [InlineData(256)]
  [InlineData(int.MaxValue)]
  [InlineData(int.MinValue)]
  public void int_roundTrips(int value)
  {
    byte[] bytes = padded(ByteConverter.intBytes(value));
    Assert.Equal(value, (int)ByteConverter.readNumber(bytes, 0, 4));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(300)]
  [InlineData(-300)]
  [InlineData(short.MaxValue)]
  [InlineData(short.MinValue)]
  public void short_roundTrips(int value)
  {
    byte[] bytes = padded(ByteConverter.shortBytes((short)value));
    Assert.Equal((short)value, (short)ByteConverter.readNumber(bytes, 0, 2));
  }

  [Theory]
  [InlineData(0L)]
  [InlineData(-1L)]
  [InlineData(1L << 40)]
  [InlineData(long.MaxValue)]
  [InlineData(long.MinValue)]
  public void long_roundTrips(long value)
  {
    byte[] bytes = padded(ByteConverter.longBytes(value));
    Assert.Equal(value, ByteConverter.readNumber(bytes, 0, 8));
  }

  [Theory]
  [InlineData(0.0f)]
  [InlineData(135f)]
  [InlineData(-0.1f)]
  [InlineData(float.MaxValue)]
  public void float_roundTrips(float value)
  {
    Assert.Equal(value, ByteConverter.readFloat(ByteConverter.floatBytes(value), 0));
  }

  [Fact]
  public void floatBytes_isBigEndian()
  {
    Assert.Equal(new byte[] { 0x3F, 0x80, 0, 0 }, ByteConverter.floatBytes(1.0f));
  }

  [Theory]
  [InlineData(0.0)]
  [InlineData(128.5)]
  [InlineData(-3.25e100)]
  [InlineData(double.MaxValue)]
  public void double_roundTrips(double value)
  {
    Assert.Equal(value, ByteConverter.readDouble(ByteConverter.doubleBytes(value), 0));
  }

  [Fact]
  public void readNumber_respectsOffset()
  {
    byte[] bytes = new byte[] { 9, 9, 0, 0, 1, 0, 9 };
    Assert.Equal(256L, ByteConverter.readNumber(bytes, 2, 4));
  }

  [Fact]
  public void readNumber_readEndingAtEndOfArray_isRejected()
  {
    // Characterises the existing bounds check: offset + len must be strictly less than the array length.
    Assert.Throws<Exception>(() => ByteConverter.readNumber(new byte[] { 0, 0, 0, 1 }, 0, 4));
  }

  [Fact]
  public void stringBytes_and_readString_roundTripLatin1()
  {
    byte[] bytes = ByteConverter.stringBytes("héllo");
    Assert.Equal(new byte[] { 0x68, 0xE9, 0x6C, 0x6C, 0x6F }, bytes);
    Assert.Equal("héllo", ByteConverter.readString(bytes, 0, bytes.Length));
  }
}
