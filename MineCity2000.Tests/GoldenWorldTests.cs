using System.IO.Compression;
using com.mc2k.MineCity2000;
using com.mc2k.MinecraftEditor;
using Xunit.Abstractions;

namespace MineCity2000.Tests;

public class GoldenWorldTests
{
  private static readonly string FixtureCity = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.sc2");
  private static readonly string BuildingsDir = TestPaths.buildingsDir();

  private readonly ITestOutputHelper output;

  public GoldenWorldTests(ITestOutputHelper output)
  {
    this.output = output;
  }

  [Theory]
  [InlineData(false, "default.zip")]
  [Trait("Category", "Golden")]
  public void defaultConversion_matchesBaseline(bool fill, string baselineName) => run(fill, baselineName);

  [Theory]
  [InlineData(true, "fill.zip")]
  [Trait("Category", "Golden"), Trait("Category", "Slow")]
  public void fillUndergroundConversion_matchesBaseline(bool fill, string baselineName) => run(fill, baselineName);

  // Guards against a baseline that only holds terrain: the city centre region must contain building blocks.
  [Fact]
  [Trait("Category", "Golden")]
  public void defaultBaseline_cityCentreContainsBuildingBlocks()
  {
    string baseline = Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline", "default.zip");
    using ZipArchive zip = ZipFile.OpenRead(baseline);
    using var stream = zip.GetEntry("region/r.1.1.mca")!.Open();
    using var buffer = new MemoryStream();
    stream.CopyTo(buffer);

    long buildingBlocks = 0;
    foreach (Chunk chunk in MCAReader.read(buffer.ToArray()))
    {
      foreach (Section section in chunk.getSections())
      {
        // 0 = air, 9 = water, 24 = sandstone: the only ids terrain generation produces.
        buildingBlocks += section.getBlocks().Count(b => b != 0 && b != 9 && b != 24);
      }
    }

    output.WriteLine("non-terrain blocks in r.1.1.mca: " + buildingBlocks);
    Assert.True(buildingBlocks > 100000, "only " + buildingBlocks + " non-terrain blocks in r.1.1.mca");
  }

  private static void run(bool fill, string baselineName)
  {
    string outDir = Path.Combine(Path.GetTempPath(), "mc2k-golden-" + Guid.NewGuid());
    var options = new MapperOptions(fill, generateTerrain: false, timestamp: 1700000000, randomSeed: 12345);
    new SCMapper(BuildingsDir, null).makeMap(FixtureCity, outDir, options);

    string baseline = Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline", baselineName);
    if (Environment.GetEnvironmentVariable("MC2K_UPDATE_BASELINE") == "1")
    {
      string repoBaseline = Path.Combine(TestPaths.repoRoot(), "MineCity2000.Tests", "Fixtures", "baseline", baselineName);
      File.Delete(repoBaseline);
      ZipFile.CreateFromDirectory(outDir, repoBaseline, CompressionLevel.Optimal, includeBaseDirectory: false);
      Directory.Delete(outDir, true);
      return;
    }
    WorldComparer.assertWorldsEqual(baseline, outDir);
    Directory.Delete(outDir, true);
  }
}
