using com.mc2k.MinecraftEditor;

namespace MineCity2000.Tests;

public class ZLibUtilsTests
{
  [Fact]
  public void compress_thenDecompress_roundTrips()
  {
    byte[] input = Enumerable.Range(0, 10000).Select(i => (byte)(i % 7)).ToArray();

    ZLibUtils.CompressData(input, out byte[] compressed);
    ZLibUtils.DecompressData(compressed, out byte[] decompressed);

    Assert.Equal(input, decompressed);
    Assert.True(compressed.Length < input.Length);
  }

  [Fact]
  public void compressed_isZlibFormat()
  {
    // RFC1950 header; Minecraft requires zlib (type 2), not raw deflate.
    ZLibUtils.CompressData(new byte[100], out byte[] compressed);

    Assert.Equal(0x78, compressed[0]);
  }

  [Fact]
  public void decompress_readsAllBuildingModelChunks()
  {
    // Exercises decompression of real chunk data from every building model file.
    foreach (string file in Directory.GetFiles(TestPaths.buildingsDir(), "*.mca").OrderBy(f => f, StringComparer.Ordinal))
    {
      List<Chunk> chunks = MCAReader.readFile(file);

      Assert.True(chunks.Count > 0, "no chunks in " + Path.GetFileName(file));
    }
  }
}
