using com.mc2k.MineCity2000;
using com.mc2k.SimCityReader;

namespace MineCity2000.Tests;

// Heights are in blocks: a SimCity altitude step is 16 blocks (SECTION_HEIGHT), a square is 16x16 blocks (SQUARE_SIZE).
public class TerrainTests
{
  [Fact]
  public void interpolateTriangles_equalHeights_isFlatAtHeightTimesSixteen()
  {
    int[][] field = SCMapper.interpolateTriangles(3, 3, 3, 3);

    Assert.Equal(16, field.Length);
    Assert.All(field, row =>
    {
      Assert.Equal(16, row.Length);
      Assert.All(row, h => Assert.Equal(48, h));
    });
  }

  [Fact]
  public void interpolateTriangles_slopeAlongSecondAxis_risesOneBlockPerStep()
  {
    int[][] field = SCMapper.interpolateTriangles(0, 0, 1, 1);

    for (int i = 0; i < 16; i++)
    {
      for (int j = 0; j < 16; j++)
      {
        Assert.Equal(j, field[i][j]);
      }
    }
  }

  [Fact]
  public void interpolateTriangles_slopeAlongFirstAxis_risesOneBlockPerStep()
  {
    int[][] field = SCMapper.interpolateTriangles(0, 1, 0, 1);

    for (int i = 0; i < 16; i++)
    {
      for (int j = 0; j < 16; j++)
      {
        Assert.Equal(i, field[i][j]);
      }
    }
  }

  [Fact]
  public void interpolateTriangles_diagonalSplit_keepsFlatTriangleAtZero_currentBehaviour()
  {
    // (0,0,0,16): p2 == p3, so the square is split into an upper-left triangle (flat at 0)
    // and a lower-right triangle that climbs towards p4. Characterises current behaviour (_currentBehaviour):
    // the far corner reaches 224 rather than 256 because the formula steps by diff (16) per block starting
    // from (i + j - 16). That looks like a seam quirk that a future fix may change.
    int[][] field = SCMapper.interpolateTriangles(0, 0, 0, 16);

    Assert.Equal(0, field[0][0]);
    Assert.Equal(0, field[8][8]);
    Assert.Equal(224, field[15][15]);
  }

  [Fact]
  public void interpolateTriangles_irregularSquare_hitsCornerHeightsAtCorners()
  {
    int[][] field = SCMapper.interpolateTriangles(0, 1, 2, 4);

    Assert.Equal(0, field[0][0]);
    Assert.Equal(16, field[15][0]);
    Assert.Equal(32, field[0][15]);
    Assert.Equal(64, field[15][15]);
  }

  [Fact]
  public void getLowestTerrainLevel_returnsMinimum()
  {
    Assert.Equal(1, SCMapper.getLowestTerrainLevel(new int[][] { new[] { 5, 3 }, new[] { 9, 1 } }));
  }

  [Fact]
  public void readCityHeightmap_raisesAllFourCornersOfASquare()
  {
    CityData city = Reader.read(new IffBuilder().altitude(3, 4, 7).build(), "x");

    byte[][] heightmap = SCMapper.readCityHeightmap(city);

    Assert.Equal(129, heightmap.Length);
    Assert.All(heightmap, row => Assert.Equal(129, row.Length));
    Assert.Equal(7, heightmap[3][4]);
    Assert.Equal(7, heightmap[4][4]);
    Assert.Equal(7, heightmap[3][5]);
    Assert.Equal(7, heightmap[4][5]);
    Assert.Equal(0, heightmap[2][4]);
    Assert.Equal(0, heightmap[5][5]);
  }

  [Fact]
  public void readCityHeightmap_cornerTakesHighestNeighbouringSquare()
  {
    CityData city = Reader.read(new IffBuilder().altitude(3, 4, 7).altitude(4, 5, 9).build(), "x");

    byte[][] heightmap = SCMapper.readCityHeightmap(city);

    Assert.Equal(9, heightmap[4][5]);
    Assert.Equal(7, heightmap[4][4]);
  }

  [Fact]
  public void reticulateSplines_producesFullResolutionHeightGrid()
  {
    CityData city = Reader.read(new IffBuilder().build(), "x");

    int[][] terrain = SCMapper.reticulateSplines(city);

    Assert.Equal(128 * 16 + 1, terrain.Length);
    Assert.All(terrain, row => Assert.Equal(128 * 16 + 1, row.Length));
  }

  [Fact]
  public void reticulateSplines_raisedSquareFallsOffIntoNeighbour()
  {
    // One square at altitude 2 in the north-west corner: its own 16x16 blocks are flat at 32,
    // the neighbouring square along the first axis slopes down from 32 by 2 per block.
    CityData city = Reader.read(new IffBuilder().altitude(0, 0, 2).build(), "x");

    int[][] terrain = SCMapper.reticulateSplines(city);

    Assert.Equal(32, terrain[0][0]);
    Assert.Equal(32, terrain[15][15]);
    Assert.Equal(32, terrain[16][0]);
    Assert.Equal(2, terrain[31][0]);
    Assert.Equal(0, terrain[100][100]);
  }
}
