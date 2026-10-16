using com.mc2k.SimCityReader;

namespace MineCity2000.Tests;

public class CityDataTests
{
  [Fact]
  public void cityName_fallsBackToFileName()
  {
    CityData city = Reader.read(new IffBuilder().build(), "myCity");
    Assert.Equal("myCity", city.getCityName());
  }

  [Fact]
  public void cityName_isReadFromCnam()
  {
    CityData city = Reader.read(new IffBuilder().cityName("Test").build(), "myCity");
    Assert.Equal("Test", city.getCityName());
  }

  [Fact]
  public void cityName_stopsAtFirstZeroByte()
  {
    byte[] cnam = new byte[] { 8, (byte)'A', (byte)'b', 0, (byte)'X', (byte)'Y', 0, 0, 0 };
    CityData city = Reader.read(new IffBuilder().rawCnam(cnam).build(), "myCity");
    Assert.Equal("Ab", city.getCityName());
  }

  [Fact]
  public void cityName_emptyCnam_isCurrentlyEmptyNotFileName()
  {
    // Characterises current behaviour: a CNAM segment that exists but holds an empty name does NOT fall back
    // to the file name (CityData only falls back when the CNAM segment is absent). See the PR 3 report.
    CityData city = Reader.read(new IffBuilder().rawCnam(new byte[] { 0, 0, 0, 0 }).build(), "myCity");
    Assert.Equal("", city.getCityName());
  }

  [Fact]
  public void readFile_usesFileNameWithoutExtensionAsFallback()
  {
    string dir = Path.Combine(Path.GetTempPath(), "mc2k-test-" + Guid.NewGuid());
    Directory.CreateDirectory(dir);
    try
    {
      string file = Path.Combine(dir, "Springfield.sc2");
      File.WriteAllBytes(file, new IffBuilder().build());
      Assert.Equal("Springfield", Reader.readFile(file).getCityName());
    }
    finally
    {
      Directory.Delete(dir, true);
    }
  }
}
