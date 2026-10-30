using com.mc2k.SimCityReader;

namespace MineCity2000.Tests;

// Building codes: 81 = suspension bridge part, 221 = runway, 223 = pier, 224 = crane, 10 = a house (no direction).
// Model files without a suffix run along x, the "r" variants along z, so Orientation.AlongZ is what selects an "r" model.
public class OrientationTests
{
  private const byte BRIDGE = 81;
  private const byte RUNWAY = 221;
  private const byte PIER = 223;
  private const byte CRANE = 224;
  private const byte HOUSE = 10;

  [Fact]
  public void xbit_rotateBitIsRead_otherBitsAreIgnored()
  {
    CityData city = Reader.read(new IffBuilder()
      .xbit(1, 1, IffBuilder.XBIT_ROTATE)
      .xbit(1, 2, 0xFF ^ IffBuilder.XBIT_ROTATE)
      .xbit(1, 3, 0xFF)
      .build(), "x");
    Assert.True(city.getSquare(1, 1).rotateFlag);
    Assert.False(city.getSquare(1, 2).rotateFlag);
    Assert.True(city.getSquare(1, 3).rotateFlag);
    Assert.False(city.getSquare(2, 1).rotateFlag);
  }

  [Fact]
  public void compass_isReadFromMiscOffset8()
  {
    Assert.Equal(0, Reader.read(new IffBuilder().build(), "x").getCompass());
    Assert.Equal(3, Reader.read(new IffBuilder().compass(3).build(), "x").getCompass());
  }

  [Fact]
  public void withoutXbitSegment_flagsAreClear_andOrientationsAreStillComputed()
  {
    CityData city = Reader.read(new IffBuilder().building(5, 5, BRIDGE).build(), "x");
    Assert.False(city.getSquare(5, 5).rotateFlag);
    Assert.Equal(Orientation.AlongZ, city.getSquare(5, 5).orientation);
  }

  [Fact]
  public void buildingWithoutDirection_hasNoOrientation()
  {
    CityData city = Reader.read(new IffBuilder().building(5, 5, HOUSE).xbit(5, 5, IffBuilder.XBIT_ROTATE).build(), "x");
    Assert.Equal(Orientation.None, city.getSquare(5, 5).orientation);
    Assert.Equal(Orientation.None, city.getSquare(6, 6).orientation);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  public void bridge_followsTheRotateFlag_regardlessOfCompass(int compass)
  {
    CityData city = Reader.read(new IffBuilder().compass(compass)
      .building(5, 5, BRIDGE).xbit(5, 5, IffBuilder.XBIT_ROTATE)
      .building(7, 7, BRIDGE)
      .build(), "x");
    Assert.Equal(Orientation.AlongX, city.getSquare(5, 5).orientation);
    Assert.Equal(Orientation.AlongZ, city.getSquare(7, 7).orientation);
  }

  [Theory]
  [InlineData(0, true, Orientation.AlongX)]
  [InlineData(0, false, Orientation.AlongZ)]
  [InlineData(1, true, Orientation.AlongZ)]
  [InlineData(1, false, Orientation.AlongX)]
  [InlineData(2, true, Orientation.AlongX)]
  [InlineData(2, false, Orientation.AlongZ)]
  [InlineData(3, true, Orientation.AlongZ)]
  [InlineData(3, false, Orientation.AlongX)]
  public void runway_flagMeaningFlipsWithAnOddCompass(int compass, bool flag, Orientation expected)
  {
    var builder = new IffBuilder().compass(compass).building(5, 5, RUNWAY).xbit(5, 5, flag ? IffBuilder.XBIT_ROTATE : (byte)0);
    CityData city = Reader.read(builder.build(), "x");
    Assert.Equal(expected, city.getSquare(5, 5).orientation);
  }

  [Fact]
  public void pier_runsTowardsItsCrane_regardlessOfTheFlag()
  {
    // Pier strip along x ending in a crane at x = 13, and one along z ending in a crane at z = 20
    var builder = new IffBuilder();
    for (int x = 10; x <= 12; x++) builder.building(x, 5, PIER).xbit(x, 5, 0);
    builder.building(13, 5, CRANE);
    for (int z = 17; z <= 19; z++) builder.building(30, z, PIER).xbit(30, z, IffBuilder.XBIT_ROTATE);
    builder.building(30, 20, CRANE);

    CityData city = Reader.read(builder.build(), "x");
    for (int x = 10; x <= 12; x++) Assert.Equal(Orientation.AlongX, city.getSquare(x, 5).orientation);
    for (int z = 17; z <= 19; z++) Assert.Equal(Orientation.AlongZ, city.getSquare(30, z).orientation);
    Assert.Equal(Orientation.None, city.getSquare(13, 5).orientation);
  }

  [Fact]
  public void pier_craneBehindTheStartOfTheStrip_alsoCounts()
  {
    CityData city = Reader.read(new IffBuilder().building(10, 5, CRANE).building(11, 5, PIER).building(12, 5, PIER).build(), "x");
    Assert.Equal(Orientation.AlongX, city.getSquare(12, 5).orientation);
  }

  [Fact]
  public void pier_withCranesOnBothAxesOrNone_fallsBackToFlagAndCompass()
  {
    var builder = new IffBuilder().compass(1)
      // cranes in both directions
      .building(10, 10, PIER).xbit(10, 10, IffBuilder.XBIT_ROTATE).building(11, 10, CRANE).building(10, 11, CRANE)
      // no crane at all
      .building(20, 20, PIER).xbit(20, 20, 0);
    CityData city = Reader.read(builder.build(), "x");
    Assert.Equal(Orientation.AlongZ, city.getSquare(10, 10).orientation); // flag set, odd compass
    Assert.Equal(Orientation.AlongX, city.getSquare(20, 20).orientation); // flag clear, odd compass
  }

  [Fact]
  public void pier_walkStopsAtANonPierTile()
  {
    CityData city = Reader.read(new IffBuilder().building(10, 5, PIER).building(11, 5, HOUSE).building(12, 5, CRANE).build(), "x");
    Assert.Equal(Orientation.AlongZ, city.getSquare(10, 5).orientation); // no crane found: flag clear, compass 0
  }

  [Fact]
  public void pier_walkStopsAtTheMapEdge()
  {
    CityData city = Reader.read(new IffBuilder().building(127, 127, PIER).building(0, 0, PIER).build(), "x");
    Assert.Equal(Orientation.AlongZ, city.getSquare(127, 127).orientation);
    Assert.Equal(Orientation.AlongZ, city.getSquare(0, 0).orientation);
  }

  [Fact]
  public void testCity_runwaysPiersAndBridgesFollowTheirStrips()
  {
    CityData city = Reader.readFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.sc2"));
    Assert.Equal(0, city.getCompass());
    for (int x = 52; x <= 56; x++) Assert.Equal(Orientation.AlongX, city.getSquare(x, 69).orientation); // runway along x
    for (int z = 4; z <= 8; z++) Assert.Equal(Orientation.AlongZ, city.getSquare(77, z).orientation);   // runways along z
    for (int z = 2; z <= 6; z++) Assert.Equal(Orientation.AlongZ, city.getSquare(78, z).orientation);
    for (int z = 67; z <= 70; z++) Assert.Equal(Orientation.AlongZ, city.getSquare(96, z).orientation); // piers along z
    for (int z = 70; z <= 76; z++) Assert.Equal(Orientation.AlongZ, city.getSquare(0, z).orientation);  // road bridge along z
    for (int z = 87; z <= 94; z++) Assert.Equal(Orientation.AlongZ, city.getSquare(43, z).orientation); // raised wires along z
  }
}
