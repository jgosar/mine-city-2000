using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.mc2k.SimCityReader;

namespace com.mc2k.MinecraftEditor.Planning
{
    class CityPlan
    {
        private const int SQUARES_IN_CITY = 128;//How many squares are in a city

        SquarePlan[][] _squares;

        public CityPlan(CityData data)
        {
            createSquares(data);
            byte _waterLevel = data.getWaterLevel();
        }

        private void createSquares(CityData data)
        {
            byte[][] heightmap = readCityHeightmap(data);
            _squares = new SquarePlan[heightmap.Length - 1][];
            for (int i = 0; i < SQUARES_IN_CITY; i++)
            {
                _squares[i] = new SquarePlan[heightmap.Length - 1];
                for (int j = 0; j < SQUARES_IN_CITY; j++)
                {
                    _squares[i][j] = new SquarePlan(heightmap[i][j], heightmap[i][j + 1], heightmap[i + 1][j], heightmap[i + 1][j + 1]);

                    for (int offsetI = -1; offsetI <= 1; offsetI++)
                    {
                        for (int offsetJ = -1; offsetJ <= 1; offsetJ++)
                        {
                            Boolean isWater = hasWaterAt(i + offsetI, j + offsetJ, data);
                            _squares[i][j].setWater(offsetI, offsetJ, isWater);
                        }
                    }

                    SquareData thisSquare = data.getSquare(i, j);
                    if (thisSquare.buildingType != 0)
                    {
                        _squares[i][j].addBuilding(thisSquare.buildingType, thisSquare.buildingOffsetX, thisSquare.buildingOffsetZ);
                    }
                }
            }
        }

        public static byte[][] readCityHeightmap(CityData data)
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

        private bool hasWaterAt(int i, int j, CityData data)
        {
            if (Util.isBetween(i, 0, SQUARES_IN_CITY - 1) && Util.isBetween(j, 0, SQUARES_IN_CITY - 1))
            {
                return data.getSquare(i, j).hasWater;
            }
            return false;
        }
    }
}
