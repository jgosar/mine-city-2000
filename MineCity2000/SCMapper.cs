using System;
using com.mc2k.SimCityReader;
using System.IO;
using System.ComponentModel;
using static com.mc2k.SimCityReader.BuildingType;
using com.mc2k.MinecraftEditor;

namespace com.mc2k.MineCity2000
{
  public class SCMapper
  {
    private ProgressCallback? _progressCallback;
    private int _progress;
    private const int CITY_WIDTH_IN_REGIONS = 4;//How many Minecraft regions are in a SimCity city
    private const int REGION_SIZE = 512;//How many pixels/blocks are in a Minecraft region
    private const int SQUARE_SIZE = 16;//How many pixels/blocks are in a SimCity square
    private const int SQUARES_IN_REGION = REGION_SIZE / SQUARE_SIZE;//How many SimCity squares are in a Minecraft region
    private const int SECTIONS_IN_CHUNK = 16;//How many Minecraft sections are in a Minecraft chunk (by height)
    private const int SECTION_HEIGHT = 16;//How many pixels/blocks are in a Minecraft section (by height)
    private const int SQUARES_IN_CITY = 128; // How many squares wide and long is a SimCity 2000 city
    private const int AIR_BLOCK = 0;
    private const int WATER_BLOCK = 9;
    private const int SANDSTONE_BLOCK = 24;
    private const int OBSIDIAN_BLOCK = 49;

    private String _buildingsDir;
    BuildingModel[] _buildingModels = new BuildingModel[256];
    BuildingModel[] _rotatedModels = new BuildingModel[256];//bridges 81-89, rail bridges 90-91, raised power lines 92, runway 221, pier 223

    public delegate void ProgressCallback(int progress);

    public SCMapper(String buildingsDir, ProgressCallback? progressCallback)
    {
      _buildingsDir = buildingsDir;
      _progressCallback = progressCallback;
    }

    public void makeMap(String inputFile, String outputDir, MapperOptions options)
    {
      _progress = 0;
      reportProgress();

      CityData data = Reader.readFile(inputFile);

      _progress = 4;
      reportProgress();

      int[][] terrainHeights = reticulateSplines(data);
      subtractLowestTerrainLevel(terrainHeights);

      _progress = 4;
      reportProgress();

      prepareDir(outputDir);

      _progress = 8;
      reportProgress();

      double[] playerPosition = new double[] { 128.0, terrainHeights[128][128] + 20, 128.0 };
      createLevelDat(outputDir, data.getCityName(), playerPosition, options);

      _progress = 16;
      reportProgress();

      //map
      createCityRegions(outputDir, data, terrainHeights, options);

      if (!options.generateTerrain)
      {
        createBorderRegions(outputDir);
      }

      _progress = 100;
      reportProgress();
    }

    private void createCityRegions(String outputDir, CityData data, int[][] terrainHeights, MapperOptions options)
    {
      for (int regionX = 0; regionX < CITY_WIDTH_IN_REGIONS; regionX++)
      {
        for (int regionZ = 0; regionZ < CITY_WIDTH_IN_REGIONS; regionZ++)
        {
          int[][] terrainHeightsForRegion = getTerrainHeightsForRegion(terrainHeights, regionX, regionZ);
          AbstractRegion newRegion = mapRegion(data, terrainHeightsForRegion, regionX, regionZ, options);

          saveRegion(newRegion, outputDir);

          _progress += 4;
          reportProgress();
        }
      }
    }

    private int[][] getTerrainHeightsForRegion(int[][] terrainHeights, int regionX, int regionZ)
    {
      int[][] terrainHeightsForRegion = Util.new2DArray<int>(REGION_SIZE, REGION_SIZE);
      int regionXOffset = REGION_SIZE * regionX;
      int regionZOffset = REGION_SIZE * regionZ;

      for (int x = 0; x < REGION_SIZE; x++)
      {
        for (int z = 0; z < REGION_SIZE; z++)
        {
          terrainHeightsForRegion[x][z] = terrainHeights[x + regionXOffset][z + regionZOffset];
        }
      }

      return terrainHeightsForRegion;
    }

    private void createBorderRegions(String outputDir)
    {
      for (int i = 0; i < CITY_WIDTH_IN_REGIONS; i++)
      {
        int j1 = -1;
        int j2 = CITY_WIDTH_IN_REGIONS;

        AbstractRegion borderRegionNorth = new AbstractRegion(i, j1);
        AbstractRegion borderRegionSouth = new AbstractRegion(i, j2);
        AbstractRegion borderRegionEast = new AbstractRegion(j1, i);
        AbstractRegion borderRegionWest = new AbstractRegion(j2, i);

        for (int sectionNum = 0; sectionNum < REGION_SIZE; sectionNum++)
        {
          buildWallSection(borderRegionNorth, sectionNum, REGION_SIZE - 1);
          buildWallSection(borderRegionSouth, sectionNum, 0);
          buildWallSection(borderRegionEast, REGION_SIZE - 1, sectionNum);
          buildWallSection(borderRegionWest, 0, sectionNum);
        }

        saveRegion(borderRegionNorth, outputDir);
        saveRegion(borderRegionSouth, outputDir);
        saveRegion(borderRegionEast, outputDir);
        saveRegion(borderRegionWest, outputDir);

        _progress += 4;
        reportProgress();
      }
    }

    private static void buildWallSection(AbstractRegion borderRegion, int x, int z)
    {
      for (int y = 0; y < 256; y++)
      {
        borderRegion.putBlock(x, z, y, OBSIDIAN_BLOCK);
      }
    }

    private AbstractRegion mapRegion(CityData data, int[][] terrainHeightsForRegion, int regionX, int regionZ, MapperOptions options)
    {
      AbstractRegion newRegion;
      byte[][][][][] buildingChunkData;

      byte waterLevel = data.getWaterLevel();
      newRegion = new AbstractRegion(regionX, regionZ);

      mapRegionLandscape(newRegion, terrainHeightsForRegion, waterLevel, options.fillUnderground);

      int regionXOffset = regionX * SQUARES_IN_REGION;
      int regionZOffset = regionZ * SQUARES_IN_REGION;

      for (int squareXIndex = 0; squareXIndex < SQUARES_IN_REGION; squareXIndex++)
      {
        for (int squareZIndex = 0; squareZIndex < SQUARES_IN_REGION; squareZIndex++)
        {
          int squareX = squareXIndex + regionXOffset;
          int squareZ = squareZIndex + regionZOffset;
          SquareData thisSquare = data.getSquare(squareX, squareZ);
          SquareData? xMinusSquare = squareX > 0 ? data.getSquare(squareX - 1, squareZ) : null;
          SquareData? xPlusSquare = squareX < 127 ? data.getSquare(squareX + 1, squareZ) : null;
          SquareData? zMinusSquare = squareZ > 0 ? data.getSquare(squareX, squareZ - 1) : null;
          SquareData? zPlusSquare = squareZ < 127 ? data.getSquare(squareX, squareZ + 1) : null;

          if (thisSquare.hasWater)
          {
            bool waterXMinus = xMinusSquare != null && xMinusSquare.hasWater;
            bool waterXPlus = xPlusSquare != null && xPlusSquare.hasWater;
            bool waterZMinus = zMinusSquare != null && zMinusSquare.hasWater;
            bool waterZPlus = zPlusSquare != null && zPlusSquare.hasWater;
            fillSquareWithWater(terrainHeightsForRegion, newRegion, waterLevel, squareXIndex, squareZIndex, waterXMinus, waterXPlus, waterZMinus, waterZPlus);
          }

          if (thisSquare.buildingType != 0)
          {
            fixBridgeSlopes(terrainHeightsForRegion, thisSquare, xMinusSquare, xPlusSquare, zMinusSquare, zPlusSquare, squareXIndex, squareZIndex);

            bool useRotatedModel = shouldUseRotatedModel(thisSquare, zMinusSquare, zPlusSquare);
            bool squareIsTunnelEntrance = isTunnelEntrance(thisSquare.buildingType);
            bool overridesSlope = squareIsTunnelEntrance || isHydroPower(thisSquare.buildingType);

            BuildingModel buildingModel = loadBuildingModel(thisSquare.buildingType, useRotatedModel);

            if (buildingModel != null)
            {
              buildingChunkData = buildingModel.getChunkData(thisSquare.buildingOffsetX, thisSquare.buildingOffsetZ);
              for (int section = 0; section < SECTIONS_IN_CHUNK; section++)
              {
                if (buildingChunkData[section] != null)
                {
                  for (int sx = 0; sx < SQUARE_SIZE; sx++)
                  {
                    for (int sz = 0; sz < SQUARE_SIZE; sz++)
                    {
                      for (int sy = 0; sy < SECTION_HEIGHT; sy++)
                      {
                        int blockX = squareXIndex * SQUARE_SIZE + sx;
                        int blockZ = squareZIndex * SQUARE_SIZE + sz;
                        int blockY = section * SECTION_HEIGHT + sy + 2 - SECTION_HEIGHT;

                        byte[] buildingBlock = buildingChunkData[section][sx][sz][sy];

                        if (overridesSlope)
                        {
                          // Structures that override slopes, such as hydro power and tunnel entrances should not conform to the landscape like sloped roads do
                          // Also their air blocks should override whatever terrain was defined in the same place previously
                          newRegion.putBlockData(blockX, blockZ, blockY + (thisSquare.altitude * SECTION_HEIGHT), buildingBlock);
                        }
                        else if (buildingBlock[0] != AIR_BLOCK) // For all other structures ignore air blocks from the building model
                        {
                          bool isAboveWaterLevel = terrainHeightsForRegion[blockX][blockZ] >= (waterLevel * SECTION_HEIGHT);

                          if (isAboveWaterLevel) // Adjust placement of blocks to height of terrain below the building
                          {
                            newRegion.putBlockData(blockX, blockZ, blockY + terrainHeightsForRegion[blockX][blockZ], buildingBlock);
                          }
                          else // This structure is built on water (e.g. bridge or marina), place it on the water's surface
                          {
                            newRegion.putBlockData(blockX, blockZ, blockY + (waterLevel * SECTION_HEIGHT), buildingBlock);
                          }
                        }


                        if (squareIsTunnelEntrance)//tunnel entrances - include air, repeat along x or z axis
                        {
                          if (section == 0 || sy < SECTION_HEIGHT - 2) // Omit the top two levels of blocks above the underground tunnel sections in order not to erase the ground above
                          {
                            if (thisSquare.buildingType == 63)
                            {
                              repeatBlockUntilTunnelEndsX(data, newRegion, buildingBlock, squareX, squareZ, blockX, blockZ, blockY + (thisSquare.altitude * SECTION_HEIGHT));
                            }
                            if (thisSquare.buildingType == 64)
                            {
                              repeatBlockUntilTunnelEndsZ(data, newRegion, buildingBlock, squareX, squareZ, blockX, blockZ, blockY + (thisSquare.altitude * SECTION_HEIGHT));
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return newRegion;
    }

    private void mapRegionLandscape(AbstractRegion newRegion, int[][] terrainHeightsForRegion, byte waterLevel, Boolean fillUnderground)
    {
      for (int x = 0; x < REGION_SIZE; x++)
      {
        for (int z = 0; z < REGION_SIZE; z++)
        {
          int bottomOfFilledGround = fillUnderground ? 0 : terrainHeightsForRegion[x][z];
          for (int y = terrainHeightsForRegion[x][z] + 1; y >= bottomOfFilledGround; y--)
          {
            newRegion.putBlock(x, z, y, SANDSTONE_BLOCK);
          }

          fillWithWaterUpToWaterLevel(terrainHeightsForRegion, newRegion, waterLevel, x, z);
        }
      }
    }

    private static void fillWithWaterUpToWaterLevel(int[][] terrainHeightsForRegion, AbstractRegion newRegion, byte waterLevel, int x, int z)
    {
      if (terrainHeightsForRegion[x][z] < waterLevel * SECTION_HEIGHT)
      {
        for (int k = terrainHeightsForRegion[x][z] + 1; k <= (waterLevel * SECTION_HEIGHT) + 1; k++)
        {
          byte thisBlock = newRegion.getBlock(x, z, k);
          if (thisBlock == AIR_BLOCK || thisBlock == SANDSTONE_BLOCK || thisBlock == WATER_BLOCK)
          {
            newRegion.putBlock(x, z, k, WATER_BLOCK);
          }
        }
      }
    }

    private static void repeatBlockUntilTunnelEndsZ(CityData data, AbstractRegion newRegion, byte[] blockData, int squareX, int squareZ, int blockX, int blockZ, int fixedBlockY)
    {
      SquareData tmpSquare;
      int tunnelLength = 0;
      for (int tmpz = squareZ; tmpz >= 0; tmpz--)
      {
        tmpSquare = data.getSquare(squareX, tmpz);
        if (tmpSquare.buildingType == 66)
        {
          tunnelLength = squareZ - tmpz - 1;
          break;
        }
      }

      for (int tl = 1; tl <= tunnelLength; tl++)
      {
        if (blockZ - (SQUARE_SIZE * tl) >= 0) //TODO: This isn't ok because now tunnels can't continue from one region to another
        {
          newRegion.putBlockData(blockX, blockZ - (SQUARE_SIZE * tl), fixedBlockY, blockData);
        }
      }
    }

    private static void repeatBlockUntilTunnelEndsX(CityData data, AbstractRegion newRegion, byte[] blockData, int squareX, int squareZ, int blockX, int blockZ, int fixedBlockY)
    {
      SquareData tmpSquare;
      int tunnelLength = 0;
      for (int tmpx = squareX; tmpx >= 0; tmpx--)
      {
        tmpSquare = data.getSquare(tmpx, squareZ);
        if (tmpSquare.buildingType == 65)
        {
          tunnelLength = squareX - tmpx - 1;
          break;
        }
      }

      for (int tl = 1; tl <= tunnelLength; tl++)
      {
        if (blockX - (SQUARE_SIZE * tl) >= 0) //TODO: This isn't ok because now tunnels can't continue from one region to another
        {
          newRegion.putBlockData(blockX - (SQUARE_SIZE * tl), blockZ, fixedBlockY, blockData);
        }
      }
    }

    private BuildingModel loadBuildingModel(byte buildingType, bool useRotatedModel)
    {
      Boolean modelLoaded;
      BuildingModel? buildingModel = null;
      if (useRotatedModel)
      {
        modelLoaded = _rotatedModels[buildingType] != null;

        if (!modelLoaded && BuildingModel.modelFileExists(buildingType, _buildingsDir, true))
        {
          _rotatedModels[buildingType] = new BuildingModel(buildingType, _buildingsDir, true);
          modelLoaded = _rotatedModels[buildingType] != null;
        }

        buildingModel = _rotatedModels[buildingType];
      }
      else
      {
        modelLoaded = _buildingModels[buildingType] != null;

        if (!modelLoaded && BuildingModel.modelFileExists(buildingType, _buildingsDir, false))
        {
          _buildingModels[buildingType] = new BuildingModel(buildingType, _buildingsDir, false);
          modelLoaded = _buildingModels[buildingType] != null;
        }

        buildingModel = _buildingModels[buildingType];
      }
      return buildingModel;
    }

    private static Boolean shouldUseRotatedModel(SquareData thisSquare, SquareData? zMinusSquare, SquareData? zPlusSquare)
    {
      Boolean useRotatedModel = false;

      if (check(isRotatable, thisSquare))
      {
        bool bridgeNorth = false;
        bool bridgeSouth = false;

        if (zMinusSquare != null && check(isSameKindOfPart, thisSquare, zMinusSquare) && (check(isBridgePart, zMinusSquare) || check(isSloped, zMinusSquare)))
        {
          bridgeNorth = true;
        }
        if (zPlusSquare != null && check(isSameKindOfPart, thisSquare, zPlusSquare) && (check(isBridgePart, zPlusSquare) || check(isSloped, zPlusSquare)))
        {
          bridgeSouth = true;
        }

        if (bridgeNorth || bridgeSouth)
        {
          useRotatedModel = true;
        }
      }
      return useRotatedModel;
    }

    private static bool check(Func<byte, bool> checkFunction, SquareData square)
    {
      if (square == null)
      {
        return false;
      }
      else
      {
        return checkFunction(square.buildingType);
      }
    }
    private static bool check(Func<byte, byte, bool> checkFunction, SquareData square1, SquareData? square2)
    {
      if (square1 == null || square2 == null)
      {
        return false;
      }
      else
      {
        return checkFunction(square1.buildingType, square2.buildingType);
      }
    }

    private static void fixBridgeSlopes(int[][] terrainHeightsForRegion, SquareData thisSquare, SquareData? xMinusSquare, SquareData? xPlusSquare, SquareData? zMinusSquare, SquareData? zPlusSquare, int squareXIndex, int squareZIndex)
    {
      int squareXOffset = squareXIndex * SQUARE_SIZE;
      int squareZOffset = squareZIndex * SQUARE_SIZE;

      // This tile needs to slope upwards to one side because it is an entrance to a bridge.
      if (check(isSloped, thisSquare))
      {
        // Find out which direction the bridge is going
        Boolean isBridgeInDirectionX = check(isSameKindOfBridgePart, thisSquare, xMinusSquare) || check(isSameKindOfBridgePart, thisSquare, xPlusSquare);
        Boolean isBridgeInDirectionZ = check(isSameKindOfBridgePart, thisSquare, zMinusSquare) || check(isSameKindOfBridgePart, thisSquare, zPlusSquare);

        for (int sx = 0; sx < SQUARE_SIZE; sx++)
        {
          for (int sz = 0; sz < SQUARE_SIZE; sz++)
          {
            int blockX = squareXOffset + sx;
            int blockZ = squareZOffset + sz;
            if (check(isSlopedToTop, thisSquare) && isBridgeInDirectionX)
            {
              terrainHeightsForRegion[blockX][blockZ] += 15 - sx;
            }
            else if (check(isSlopedToRight, thisSquare) && isBridgeInDirectionZ)
            {
              terrainHeightsForRegion[blockX][blockZ] += 15 - sz;
            }
            else if (check(isSlopedToBottom, thisSquare) && isBridgeInDirectionX)
            {
              terrainHeightsForRegion[blockX][blockZ] += sx;
            }
            else if (check(isSlopedToLeft, thisSquare) && isBridgeInDirectionZ)
            {
              terrainHeightsForRegion[blockX][blockZ] += sz;
            }
          }
        }
      }
    }

    private static void fillSquareWithWater(int[][] terrainHeightsForRegion, AbstractRegion newRegion, byte waterLevel, int squareXIndex, int squareZIndex, bool waterXMinus, bool waterXPlus, bool waterZMinus, bool waterZPlus)
    {
      int baseX = squareXIndex * SQUARE_SIZE;
      int baseZ = squareZIndex * SQUARE_SIZE;

      int topSquareHeight = 1 + Math.Max(Math.Max(terrainHeightsForRegion[baseX][baseZ], terrainHeightsForRegion[baseX][baseZ + 15]), Math.Max(terrainHeightsForRegion[baseX + 15][baseZ], terrainHeightsForRegion[baseX + 15][baseZ + 15]));

      for (int wx = 0; wx < SQUARE_SIZE; wx++)
      {
        for (int wz = 0; wz < SQUARE_SIZE; wz++)
        {
          //x,z coordinates of block in Minecraft
          int blockX = baseX + wx;
          int blockZ = baseZ + wz;

          int squareHeight = terrainHeightsForRegion[blockX][blockZ] + 1;

          if (isBlockAboveWater(waterLevel, squareHeight) && topSquareHeight % SECTION_HEIGHT == 1 && squareHeight % SECTION_HEIGHT == 1)
          {
            double dx = Math.Abs((double)wx - 7.5F);
            double dz = Math.Abs((double)wz - 7.5F);
            double distanceFromCenter = Math.Sqrt(dx * dx + dz * dz);

            //make a thick floor
            newRegion.putBlock(blockX, blockZ, squareHeight - 1, SANDSTONE_BLOCK);
            newRegion.putBlock(blockX, blockZ, squareHeight - 2, SANDSTONE_BLOCK);
            newRegion.putBlock(blockX, blockZ, squareHeight - 3, SANDSTONE_BLOCK);

            //make a puddle in the center
            if (distanceFromCenter < 8)
            {
              newRegion.putBlock(blockX, blockZ, squareHeight, WATER_BLOCK);
              if (distanceFromCenter < 6)
              {
                newRegion.putBlock(blockX, blockZ, squareHeight - 1, WATER_BLOCK);
              }
            }
            //add shallow water on the edge if neighbouring square has water
            else if ((wx < 8 && wz < 8 && (waterXMinus || waterZMinus)) ||
                    (wx > 8 && wz < 8 && (waterXPlus || waterZMinus)) ||
                    (wx < 8 && wz > 8 && (waterXMinus || waterZPlus)) ||
                    (wx > 8 && wz > 8 && (waterXPlus || waterZPlus)))
            {
              newRegion.putBlock(blockX, blockZ, squareHeight, WATER_BLOCK);
            }
            //deepen the water in the middle of the edge if neighbouring square has water
            if ((wx < 8 && waterXMinus && dz < 6) || (wx > 8 && waterXPlus && dz < 6) || (wz < 8 && waterZMinus && dx < 6) || (wz > 8 && waterZPlus && dx < 6))
            {
              newRegion.putBlock(blockX, blockZ, squareHeight - 1, WATER_BLOCK);
            }
            //deepen the water in the corner as well if both neighbouring squares have water
            if ((wx < 8 && wz < 8 && waterXMinus && waterZMinus) || (wx > 8 && wz < 8 && waterXPlus && waterZMinus) || (wx < 8 && wz > 8 && waterXMinus && waterZPlus) || (wx > 8 && wz > 8 && waterXPlus && waterZPlus))
            {
              newRegion.putBlock(blockX, blockZ, squareHeight - 1, WATER_BLOCK);
            }
          }
        }
      }
    }

    private static bool isBlockAboveWater(byte waterLevel, int squareHeight)
    {
      return squareHeight - 1 >= waterLevel * SECTION_HEIGHT;
    }

    private static int[][] reticulateSplines(CityData data)
    {
      byte[][] basePoints = readCityHeightmap(data);

      int[][] interpolatedPoints = Util.new2DArray<int>(128 * SQUARE_SIZE + 1, 128 * SQUARE_SIZE + 1);

      //interpolate triangles
      for (int i = 1; i < 129; i++)
      {
        for (int j = 1; j < 129; j++)
        {
          int p1 = basePoints[i - 1][j - 1];// 1-----2
          int p2 = basePoints[i][j - 1];    // |     |
          int p3 = basePoints[i - 1][j];    // |     |
          int p4 = basePoints[i][j];        // 3-----4

          int[][] interpolatedField = interpolateTriangles(p1, p2, p3, p4);
          Util.insertArray(interpolatedField, interpolatedPoints, (i - 1) * SQUARE_SIZE, (j - 1) * SQUARE_SIZE);
        }
      }

      return interpolatedPoints;
    }

    private static byte[][] readCityHeightmap(CityData data)
    {
      byte[][] heightmap = Util.new2DArray<byte>(SQUARES_IN_CITY + 1, SQUARES_IN_CITY + 1);

      for (int i = 0; i < SQUARES_IN_CITY; i++)
      {
        for (int j = 0; j < SQUARES_IN_CITY; j++)
        {
          byte squareHeight = data.getSquare(i, j).altitude;

          if (heightmap[i][j] < squareHeight)
          {
            heightmap[i][j] = squareHeight;
          }
          if (heightmap[i + 1][j] < squareHeight)
          {
            heightmap[i + 1][j] = squareHeight;
          }
          if (heightmap[i][j + 1] < squareHeight)
          {
            heightmap[i][j + 1] = squareHeight;
          }
          if (heightmap[i + 1][j + 1] < squareHeight)
          {
            heightmap[i + 1][j + 1] = squareHeight;
          }
        }
      }
      return heightmap;
    }

    private static int[][] interpolateTriangles(int p1, int p2, int p3, int p4)
    {
      int[][] result = Util.new2DArray<int>(SQUARE_SIZE, SQUARE_SIZE);

      for (int i = 0; i < SQUARE_SIZE; i++)
      {
        for (int j = 0; j < SQUARE_SIZE; j++)
        {

          if (p1 == p2 && p2 == p3 && p3 == p4)//flat
          {
            result[i][j] = p1 * SECTION_HEIGHT;
          }
          else if (p1 == p2 && p3 == p4)//sloping top to bottom or bottom to top
          {
            int diff = p3 - p1;
            result[i][j] = p1 * SECTION_HEIGHT + j * diff;
          }
          else if (p1 == p3 && p2 == p4)//sloping left to right or right to left
          {
            int diff = p2 - p1;
            result[i][j] = p1 * SECTION_HEIGHT + i * diff;
          }
          else if (p2 == p3)//square is split into 2 triangles, upperLeft and lowerRight
          {
            int diffUpperLeft = p2 - p1;
            int diffLowerRight = p4 - p3;
            if (i + j <= SQUARE_SIZE)
            {
              result[i][j] = p1 * SECTION_HEIGHT + (i + j) * diffUpperLeft;
            }
            else
            {
              result[i][j] = p3 * SECTION_HEIGHT + (i + j - SECTION_HEIGHT) * diffLowerRight;
            }
          }
          else if (p1 == p4)//square is split into 2 triangles, upperRight and lowerLeft
          {
            int diffUpperRight = p2 - p1;
            int diffLowerLeft = p3 - p4;
            if (i >= j)
            {
              result[i][j] = p1 * SECTION_HEIGHT + (i - j) * diffUpperRight;
            }
            else
            {
              result[i][j] = p4 * SECTION_HEIGHT + (j - i) * diffLowerLeft;
            }
          }
          else //Irregular terrain shapes, usually at the start of bridges
          {
            int remainderI = SQUARE_SIZE - 1 - i;
            int remainderJ = SQUARE_SIZE - 1 - j;
            int weightP1 = remainderI * remainderJ;
            int weightP2 = i * remainderJ;
            int weightP3 = remainderI * j;
            int weightP4 = i * j;
            result[i][j] = SECTION_HEIGHT * (p1 * weightP1 + p2 * weightP2 + p3 * weightP3 + p4 * weightP4) / ((SQUARE_SIZE - 1) * (SQUARE_SIZE - 1));
          }
        }
      }

      return result;
    }

    private static int getLowestTerrainLevel(int[][] terrainHeights)
    {
      int minLevel = 256;
      for (int i = 0; i < terrainHeights.Length; i++)
      {
        for (int j = 0; j < terrainHeights[i].Length; j++)
        {
          if (terrainHeights[i][j] < minLevel)
          {
            minLevel = terrainHeights[i][j];
          }
        }
      }
      return minLevel;
    }

    private void subtractLowestTerrainLevel(int[][] terrainHeights)
    {
      int minLevel = getLowestTerrainLevel(terrainHeights);
      for (int i = 0; i < terrainHeights.Length; i++)
      {
        for (int j = 0; j < terrainHeights[i].Length; j++)
        {
          terrainHeights[i][j] -= minLevel;
        }
      }
    }

    public static void prepareDir(String outputDir)
    {
      DirectoryInfo dir = new DirectoryInfo(outputDir);
      if (dir.Exists)
      {
        foreach (FileInfo file in dir.GetFiles())
        {
          file.Delete();
        }
        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
          subDir.Delete(true);
        }
      }
      else
      {
        dir.Create();
      }
      dir.CreateSubdirectory("region");
    }

    public static void createLevelDat(String outputDir, String cityName, double[] playerPos, MapperOptions options)
    {
      LevelDat ld = new LevelDat(cityName, playerPos, options.generateTerrain);
      ld.saveToFile(outputDir + "\\level.dat");
    }

    public static void saveRegion(AbstractRegion region, String outputDir)
    {
      region.saveToFolder(outputDir + "\\region");
    }

    private void reportProgress()
    {
      if (_progressCallback != null)
      {
        _progressCallback(_progress);
      }
    }
  }
}
