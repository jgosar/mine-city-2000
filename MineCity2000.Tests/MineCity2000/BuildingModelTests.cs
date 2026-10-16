using System.Text.RegularExpressions;
using com.mc2k.MineCity2000;

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

  [Theory]
  [MemberData(nameof(modelFiles))]
  public void allModels_loadWithoutError(byte code, string variant)
  {
    string dir = TestPaths.buildingsDir();

    Assert.True(BuildingModel.modelFileExists(dir, code, variant), "missing " + code + variant);
    var model = new BuildingModel(dir, code, variant);
    Assert.Equal(16, model.getChunkData(0, 0).Length);
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
