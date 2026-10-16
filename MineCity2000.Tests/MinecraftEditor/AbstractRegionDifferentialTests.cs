using com.mc2k.MinecraftEditor;

namespace MineCity2000.Tests;

// Guards the flat-section rewrite of AbstractRegion against LegacyAbstractRegion, a verbatim snapshot of the old
// jagged implementation. Reads are NOT compared with the legacy class (its getBlockData read the y and z
// coordinates swapped within a section); they are compared with a dictionary that records what was last written
// at each position. What the legacy class serialises is correct, so saveToFolder output is compared byte for byte.
public class AbstractRegionDifferentialTests
{
  private const int Timestamp = 7;

  [Fact]
  public void randomWrites_matchLegacyImplementation()
  {
    // The legacy class costs ~200 KB per touched section, so the dense part of the workload is confined to a
    // 128 x 128 column window (about 1,300 sections); a few hundred writes anywhere else cover the whole region.
    runDifferential(seed: 2024, writes: 200_000, reads: 200_000, windowX: 100, windowZ: 100, windowSize: 128, scatteredWrites: 300);
  }

  [Fact]
  public void denseOverwrites_matchLegacyImplementation()
  {
    // A tiny window hit by many writes: exercises overwriting, data-nibble resets and neighbouring-nibble isolation.
    runDifferential(seed: 7, writes: 60_000, reads: 60_000, windowX: 30, windowZ: 45, windowSize: 20, scatteredWrites: 0);
  }

  [Fact]
  public void airOnlyWrites_stillProduceASavedSection_likeLegacy()
  {
    // The legacy class allocated a section on any write, air included, and saveToFolder wrote every allocated
    // section. Building models write air this way (tunnel entrances, hydro power), so the output must stay identical.
    var legacy = new LegacyAbstractRegion(0, 0);
    var fresh = new AbstractRegion(0, 0);
    legacy.putBlock(5, 6, 7, 0);
    fresh.putBlock(5, 6, 7, 0);
    legacy.putBlockData(100, 100, 40, new byte[] { 0, 0 });
    fresh.putBlock(100, 100, 40, 0, 0);

    assertSameSavedBytes(legacy, fresh, 0, 0);
  }

  private static void runDifferential(int seed, int writes, int reads, int windowX, int windowZ, int windowSize, int scatteredWrites)
  {
    var rng = new Random(seed);
    var legacy = new LegacyAbstractRegion(1, 2);
    var fresh = new AbstractRegion(1, 2);
    var oracle = new Dictionary<(int x, int z, int y), (byte block, byte data)>();
    var written = new List<(int x, int z, int y)>();

    for (int i = 0; i < writes + scatteredWrites; i++)
    {
      bool scattered = i >= writes;
      int x = scattered ? rng.Next(512) : windowX + rng.Next(windowSize);
      int z = scattered ? rng.Next(512) : windowZ + rng.Next(windowSize);
      int y = rng.Next(270); // includes y >= 256, which must be ignored
      byte block = (byte)rng.Next(256);
      byte data = (byte)rng.Next(16);
      bool plain = rng.Next(4) == 0;

      if (plain)
      {
        legacy.putBlock(x, z, y, block);
        fresh.putBlock(x, z, y, block);
        data = 0;
      }
      else
      {
        legacy.putBlockData(x, z, y, new byte[] { block, data });
        fresh.putBlock(x, z, y, block, data);
      }

      if (y < 256)
      {
        oracle[(x, z, y)] = (block, data);
        written.Add((x, z, y));
      }
    }

    for (int i = 0; i < reads; i++)
    {
      // Half the reads revisit written positions, the other half probe arbitrary ones (mostly never written).
      (int x, int z, int y) pos = rng.Next(2) == 0 && written.Count > 0
        ? written[rng.Next(written.Count)]
        : (windowX + rng.Next(windowSize), windowZ + rng.Next(windowSize), rng.Next(270));
      (byte block, byte data) expected = oracle.TryGetValue(pos, out var value) ? value : ((byte)0, (byte)0);

      Assert.Equal(expected.block, fresh.getBlock(pos.x, pos.z, pos.y));
      Assert.Equal(expected.data, fresh.getData(pos.x, pos.z, pos.y));
    }

    assertSameSavedBytes(legacy, fresh, 1, 2);
  }

  private static void assertSameSavedBytes(LegacyAbstractRegion legacy, AbstractRegion fresh, int regionX, int regionZ)
  {
    string a = Path.Combine(Path.GetTempPath(), "mc2k-legacy-" + Guid.NewGuid());
    string b = Path.Combine(Path.GetTempPath(), "mc2k-fresh-" + Guid.NewGuid());
    Directory.CreateDirectory(a);
    Directory.CreateDirectory(b);
    try
    {
      legacy.saveToFolder(a, Timestamp);
      fresh.saveToFolder(b, Timestamp);
      string file = "r." + regionX + "." + regionZ + ".mca";
      // Compare as arrays so a mismatch is reported once, without dumping the file contents.
      byte[] expected = File.ReadAllBytes(Path.Combine(a, file));
      byte[] actual = File.ReadAllBytes(Path.Combine(b, file));
      Assert.Equal(expected.Length, actual.Length);
      Assert.True(expected.AsSpan().SequenceEqual(actual), "saveToFolder output differs from the legacy implementation");
    }
    finally
    {
      TempDirs.deleteBestEffort(a);
      TempDirs.deleteBestEffort(b);
    }
  }
}
