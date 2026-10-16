using System.Text.RegularExpressions;
using com.mc2k.MineCity2000;
using com.mc2k.MinecraftEditor;
using com.mc2k.SimCityReader;

namespace MineCity2000.Tests;

public class BuildingModelTests
{
  // File names are "<code><variant>.mca", e.g. "10.mca", "198a.mca", "221r.mca".
  public static IEnumerable<object[]> modelFiles()
  {
    foreach (string file in Directory.GetFiles(TestPaths.buildingsDir(), "*.mca").OrderBy(f => f, StringComparer.Ordinal))
    {
      Match m = Regex.Match(Path.GetFileName(file), @"^(\d+)(\D*)\.mca$");
      Assert.True(m.Success, "Unexpected building file name: " + file);
      yield return new object[] { byte.Parse(m.Groups[1].Value), m.Groups[2].Value };
    }
  }

  // Pins the content of every building model. The hash is representation independent: for each (cx, cz, sy) in
  // ascending nested order, every non-null section contributes 4096 block bytes (index x + 16 * z + 256 * y)
  // followed by 2048 packed data bytes (low nibble for even indexes, high nibble for odd ones).
  // Models can change on purpose; regenerate with
  // MC2K_UPDATE_BASELINE=1 dotnet test MineCity2000.Tests --filter FullyQualifiedName~allModels_matchBaselineHashes
  // (the filter matters: without it the golden worlds are regenerated too).
  [Fact]
  public void allModels_matchBaselineHashes()
  {
    string dir = TestPaths.buildingsDir();
    var actual = new List<string>();

    foreach (string file in Directory.GetFiles(dir, "*.mca").OrderBy(f => f, StringComparer.Ordinal))
    {
      string name = Path.GetFileName(file);
      Match m = Regex.Match(name, @"^(\d+)(\D*)\.mca$");
      Assert.True(m.Success, "Unexpected building file name: " + file);
      byte code = byte.Parse(m.Groups[1].Value);
      string variant = m.Groups[2].Value;

      Assert.True(BuildingModel.modelFileExists(dir, code, variant), "missing " + name);
      var model = new BuildingModel(dir, code, variant);
      actual.Add(name + "\t" + hashModel(model, BuildingType.getByCode(code).getSize()));
    }

    string baselineName = "building_model_hashes.txt";
    if (Environment.GetEnvironmentVariable("MC2K_UPDATE_BASELINE") == "1")
    {
      File.WriteAllLines(Path.Combine(TestPaths.repoRoot(), "MineCity2000.Tests", "Fixtures", baselineName), actual);
    }
    else
    {
      string[] expected = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", baselineName));
      Dictionary<string, string> expectedHashes = toDictionary(expected);
      Dictionary<string, string> actualHashes = toDictionary(actual);

      var problems = new List<string>();
      foreach (string name in actualHashes.Keys.Where(n => !expectedHashes.ContainsKey(n)))
      {
        problems.Add("added: " + name);
      }
      foreach (string name in expectedHashes.Keys.Where(n => !actualHashes.ContainsKey(n)))
      {
        problems.Add("removed: " + name);
      }
      foreach (string name in actualHashes.Keys.Where(n => expectedHashes.TryGetValue(n, out string? h) && h != actualHashes[n]))
      {
        problems.Add("changed: " + name);
      }

      Assert.True(problems.Count == 0, "Building model hashes differ from the baseline:\n" + string.Join("\n", problems));
    }
  }

  private static Dictionary<string, string> toDictionary(IEnumerable<string> lines)
  {
    var result = new Dictionary<string, string>();
    foreach (string line in lines)
    {
      string[] parts = line.Split('\t');
      result[parts[0]] = parts[1];
    }
    return result;
  }

  private static string hashModel(BuildingModel model, int size)
  {
    using var sha = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
    for (int cx = 0; cx < size; cx++)
    {
      for (int cz = 0; cz < size; cz++)
      {
        for (int sy = 0; sy < 16; sy++)
        {
          Section? section = model.getSection(cx, cz, sy);
          if (section == null)
          {
            continue;
          }

          sha.AppendData(section.getBlocks());
          sha.AppendData(section.getData());
        }
      }
    }
    return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
  }

  [Fact]
  public void buildingsDir_holdsTheModelFiles()
  {
    Assert.True(modelFiles().Count() > 100, "Expected the repository to hold the building models");
  }

  [Fact]
  public void modelFileExists_unknownModel_isFalse()
  {
    Assert.False(BuildingModel.modelFileExists(TestPaths.buildingsDir(), 255, "zz"));
  }
}
