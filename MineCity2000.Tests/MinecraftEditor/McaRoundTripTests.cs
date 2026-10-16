using com.mc2k.AnvilFile.Tags;
using com.mc2k.MinecraftEditor;

namespace MineCity2000.Tests;

public class McaRoundTripTests
{
  private static List<Chunk> twoChunks()
  {
    var first = new Chunk(0, 0);
    first.putBlock(1, 2, 3, 24);
    first.putBlock(4, 20, 5, 35); // second section
    var second = new Chunk(1, 0);
    second.putBlock(0, 0, 0, 7);
    return new List<Chunk> { first, second };
  }

  [Fact]
  public void chunks_roundTripThroughMcaBytes()
  {
    List<Chunk> chunks = twoChunks();
    byte[] bytes = MCAWriter.toBytes(chunks, 123);

    List<Chunk> read = MCAReader.read(bytes);

    Assert.Equal(chunks.Count, read.Count);
    for (int i = 0; i < chunks.Count; i++)
    {
      Assert.Equal(chunks[i].getXPos(), read[i].getXPos());
      Assert.Equal(chunks[i].getZPos(), read[i].getZPos());
      Assert.Equal(chunks[i].NBTTag.toBytesWithHeader(), read[i].NBTTag.toBytesWithHeader());
    }
  }

  [Fact]
  public void toBytes_writesTimestampForFirstChunk()
  {
    byte[] bytes = MCAWriter.toBytes(twoChunks(), 123);
    Assert.Equal(new byte[] { 0, 0, 0, 123 }, bytes.Skip(4096).Take(4).ToArray());
  }

  [Fact]
  public void toBytes_fileLengthIsMultipleOfSectorSize()
  {
    byte[] bytes = MCAWriter.toBytes(twoChunks(), 123);
    Assert.Equal(0, bytes.Length % 4096);
    Assert.True(bytes.Length >= 8192 + 2 * 4096);
  }

  [Fact]
  public void toBytes_locationTableMatchesChunkPositions()
  {
    byte[] bytes = MCAWriter.toBytes(twoChunks(), 123);
    // Chunk (0,0) starts at sector 2, chunk (1,0) follows it; slot offset is 4 * (x + 32 * z).
    int firstSector = (int)ByteConverter.readNumber(bytes, 0, 3);
    int secondSector = (int)ByteConverter.readNumber(bytes, 4, 3);
    int firstLength = bytes[3];
    Assert.Equal(2, firstSector);
    Assert.Equal(firstSector + firstLength, secondSector);
  }

  [Fact]
  public void toBytes_sameInputTwice_isByteIdentical()
  {
    Assert.Equal(MCAWriter.toBytes(twoChunks(), 123), MCAWriter.toBytes(twoChunks(), 123));
  }

  [Fact]
  public void toBytes_noChunks_isTwoEmptySectorTables()
  {
    byte[] bytes = MCAWriter.toBytes(new List<Chunk>(), 123);
    Assert.Equal(8192, bytes.Length);
    Assert.All(bytes, b => Assert.Equal(0, b));
    Assert.Empty(MCAReader.read(bytes));
  }
}
