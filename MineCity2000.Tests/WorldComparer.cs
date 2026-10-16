using System.IO.Compression;
using System.Text;
using com.mc2k.MinecraftEditor;
using Xunit.Sdk;

namespace MineCity2000.Tests;

public static class WorldComparer
{
  // Compares a world folder against a baseline zip. Region files are compared chunk by chunk on the
  // decompressed NBT (not raw bytes), so the timestamp table and the compressor may change freely;
  // level.dat is compared after gunzip; every other file is compared byte for byte.
  public static void assertWorldsEqual(string expectedZip, string actualDir)
  {
    using ZipArchive zip = ZipFile.OpenRead(expectedZip);
    var expected = new SortedDictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
    foreach (ZipArchiveEntry entry in zip.Entries)
    {
      if (entry.FullName.EndsWith("/"))
      {
        continue;
      }
      expected[entry.FullName.Replace('\\', '/')] = entry;
    }

    var actual = new SortedDictionary<string, string>(StringComparer.Ordinal);
    foreach (string path in Directory.GetFiles(actualDir, "*", SearchOption.AllDirectories))
    {
      actual[Path.GetRelativePath(actualDir, path).Replace('\\', '/')] = path;
    }

    string[] missing = expected.Keys.Where(name => !actual.ContainsKey(name)).ToArray();
    string[] extra = actual.Keys.Where(name => !expected.ContainsKey(name)).ToArray();
    if (missing.Length > 0 || extra.Length > 0)
    {
      throw new XunitException("File sets differ. Missing from output: [" + string.Join(", ", missing)
        + "]. Unexpected in output: [" + string.Join(", ", extra) + "].");
    }

    foreach (var pair in expected)
    {
      compareFile(pair.Key, readEntry(pair.Value), File.ReadAllBytes(actual[pair.Key]));
    }
  }

  private static void compareFile(string name, byte[] expected, byte[] actual)
  {
    if (name.StartsWith("region/") && name.EndsWith(".mca"))
    {
      compareRegion(name, expected, actual);
    }
    else if (name == "level.dat")
    {
      compareBytes(name, gunzip(expected), gunzip(actual));
    }
    else
    {
      compareBytes(name, expected, actual);
    }
  }

  private static void compareRegion(string name, byte[] expectedBytes, byte[] actualBytes)
  {
    Dictionary<(int, int), byte[]> expected = chunkNbt(expectedBytes);
    Dictionary<(int, int), byte[]> actual = chunkNbt(actualBytes);

    string[] missing = expected.Keys.Where(key => !actual.ContainsKey(key)).OrderBy(key => key).Select(coords).ToArray();
    string[] extra = actual.Keys.Where(key => !expected.ContainsKey(key)).OrderBy(key => key).Select(coords).ToArray();
    if (missing.Length > 0 || extra.Length > 0)
    {
      throw new XunitException(name + " chunk set differs (expected " + expected.Count + " chunks, actual "
        + actual.Count + "). Missing chunks: [" + string.Join(", ", missing)
        + "]. Unexpected chunks: [" + string.Join(", ", extra) + "].");
    }

    foreach (var key in expected.Keys.OrderBy(key => key))
    {
      compareBytes(name + " chunk " + coords(key), expected[key], actual[key], "NBT");
    }
  }

  private static Dictionary<(int, int), byte[]> chunkNbt(byte[] regionBytes)
  {
    var result = new Dictionary<(int, int), byte[]>();
    foreach (Chunk chunk in MCAReader.read(regionBytes))
    {
      result[(chunk.getXPos(), chunk.getZPos())] = chunk.NBTTag.toBytesWithHeader();
    }
    return result;
  }

  private static string coords((int x, int z) key)
  {
    return "(" + key.x + "," + key.z + ")";
  }

  private static void compareBytes(string what, byte[] expected, byte[] actual, string kind = "content")
  {
    int common = Math.Min(expected.Length, actual.Length);
    for (int i = 0; i < common; i++)
    {
      if (expected[i] != actual[i])
      {
        throw new XunitException(what + ": " + kind + " differs at byte " + i
          + " (expected 0x" + expected[i].ToString("x2") + ", actual 0x" + actual[i].ToString("x2") + ")");
      }
    }
    if (expected.Length != actual.Length)
    {
      throw new XunitException(what + ": " + kind + " differs at byte " + common
        + " (expected length " + expected.Length + ", actual length " + actual.Length + ")");
    }
  }

  private static byte[] readEntry(ZipArchiveEntry entry)
  {
    using Stream stream = entry.Open();
    using var buffer = new MemoryStream();
    stream.CopyTo(buffer);
    return buffer.ToArray();
  }

  private static byte[] gunzip(byte[] bytes)
  {
    using var input = new MemoryStream(bytes);
    using var gzip = new GZipStream(input, CompressionMode.Decompress);
    using var output = new MemoryStream();
    gzip.CopyTo(output);
    return output.ToArray();
  }
}
