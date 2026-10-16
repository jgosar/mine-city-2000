using com.mc2k.SimCityReader;

namespace MineCity2000.Tests;

public class ReaderTests
{
  [Fact]
  public void rle_decodesBackToOriginal()
  {
    byte[] original = new byte[] { 0, 0, 0, 5, 7, 7, 1, 0, 0 };
    var segment = new Segment("XBLD", IffBuilder.rle(original));
    Assert.Equal(original, segment.getContent());
  }

  [Fact]
  public void emptyCity_hasNoBuildings()
  {
    CityData city = Reader.read(new IffBuilder().build(), "x");
    Assert.Equal(0, city.getSquare(0, 0).buildingType);
    Assert.Empty(city.getBuildings());
  }

  [Fact]
  public void waterLevel_isReadFromMisc()
  {
    CityData city = Reader.read(new IffBuilder().waterLevel(5).build(), "x");
    Assert.Equal(5, city.getWaterLevel());
  }

  [Fact]
  public void altitude_isReadFromAltmSecondByteModulo32()
  {
    CityData city = Reader.read(new IffBuilder().altitude(3, 4, 7).altitude(5, 6, 32 + 9).build(), "x");
    Assert.Equal(7, city.getSquare(3, 4).altitude);
    Assert.Equal(9, city.getSquare(5, 6).altitude);
    Assert.Equal(0, city.getSquare(4, 3).altitude);
  }

  [Fact]
  public void terrainCodes48to69_markWater()
  {
    CityData city = Reader.read(new IffBuilder().terrain(1, 2, 48).terrain(1, 3, 69).terrain(1, 4, 47).terrain(1, 5, 70).build(), "x");
    Assert.True(city.getSquare(1, 2).hasWater);
    Assert.True(city.getSquare(1, 3).hasWater);
    Assert.False(city.getSquare(1, 4).hasWater);
    Assert.False(city.getSquare(1, 5).hasWater);
  }

  [Fact]
  public void squaresOneBelowWaterLevel_areMarkedAsCoastalWater()
  {
    CityData city = Reader.read(new IffBuilder().waterLevel(5).altitude(10, 10, 4).altitude(11, 11, 3).build(), "x");
    Assert.True(city.getSquare(10, 10).hasWater);
    Assert.False(city.getSquare(11, 11).hasWater);
  }

  [Fact]
  public void building_isPlacedWithOffsetsAcrossItsFootprint()
  {
    // Code 201 is the 4x4 gas power plant (see BuildingType.getByCode).
    CityData city = Reader.read(new IffBuilder().building(10, 20, 201).build(), "x");

    Building building = Assert.Single(city.getBuildings());
    Assert.Equal(10, building.getXPos());
    Assert.Equal(20, building.getZPos());
    Assert.Equal(4, building.getSize());

    SquareData corner = city.getSquare(10, 20);
    Assert.Equal(201, corner.buildingType);
    Assert.Equal(0, corner.buildingOffsetX);
    Assert.Equal(0, corner.buildingOffsetZ);

    SquareData farCorner = city.getSquare(13, 23);
    Assert.Equal(201, farCorner.buildingType);
    Assert.Equal(3, farCorner.buildingOffsetX);
    Assert.Equal(3, farCorner.buildingOffsetZ);

    Assert.Equal(0, city.getSquare(14, 20).buildingType);
  }

  [Fact]
  public void missingSegment_throws()
  {
    // FORM header + SCDH with no segments at all: ALTM is required.
    byte[] bytes = new byte[] { (byte)'F', (byte)'O', (byte)'R', (byte)'M', 0, 0, 0, 4, (byte)'S', (byte)'C', (byte)'D', (byte)'H' };
    var ex = Assert.Throws<InvalidDataException>(() => Reader.read(bytes, "x"));
    Assert.Contains("truncated", ex.Message);
  }

  [Fact]
  public void shortSegment_throwsInvalidData()
  {
    byte[] bytes = new IffBuilder().altmLength(100).build();
    var ex = Assert.Throws<InvalidDataException>(() => Reader.read(bytes, "x"));
    Assert.Contains("truncated or corrupt", ex.Message);
    Assert.NotNull(ex.InnerException);
  }
}
