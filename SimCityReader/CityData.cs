using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.SimCityReader
{
    public class CityData
    {
        private String _cityName;
        private List<Building> _buildings = new List<Building>();

        private SquareData[][] _squares;
        private byte _waterLevel;

        public CityData(Dictionary<String, Segment> segments, String cityName)
        {
            initializeSquares();

            if (segments.ContainsKey("CNAM")) // Read city name from file contents, if it is included in the file, otherwise use the name passed to the constructor
            {
                interpretCNAM(segments["CNAM"]);
            }
            else
            {
                _cityName = cityName;
            }
            interpretALTM(segments["ALTM"]); // Read altitude map
            interpretXBLD(segments["XBLD"]); // Read building positions
            interpretXTER(segments["XTER"]); // Read positions of groundwater
            interpretMISC(segments["MISC"]); // Read general water level

            setWaterOnCoastalSquares();
        }

        private void initializeSquares()
        {
            _squares = new SquareData[128][];
            for (int i = 0; i < 128; i++)
            {
                _squares[i] = new SquareData[128];
                for (int j = 0; j < 128; j++)
                {
                    _squares[i][j] = new SquareData();
                }
            }
        }

        private void interpretCNAM(Segment cnamSegment)
        {
            byte[] content = cnamSegment.getContent();
            int offset = 1;
            List<byte> stringBytes = new List<byte>();

            while (offset < content.Length && content[offset] != 0)
            {
                stringBytes.Add(content[offset]);
                offset++;
            }

            _cityName = System.Text.Encoding.Default.GetString(stringBytes.ToArray());
        }

        private void interpretALTM(Segment altmSegment)
        {
            byte[] content = altmSegment.getContent();

            int offset = 0;

            for (int i = 0; i < 128; i++)
            {
                for (int j = 0; j < 128; j++)
                {
                    _squares[i][j].altitude = (byte)(content[offset + 1] % 32);
                    offset += 2;
                }
            }
        }

        private void interpretXBLD(Segment altmSegment)
        {
            byte[] content = altmSegment.getContent();

            // Rad 128x128 map of which building type is on which field
            int offset = 0;

            byte[][] buildingTypesMap = new byte[128][];
            for (int i = 0; i < 128; i++)
            {
                buildingTypesMap[i] = new byte[128];
                for (int j = 0; j < 128; j++)
                {
                    buildingTypesMap[i][j] = content[offset];
                    offset++;
                }
            }

            // Figure out which building is positioned where.
            // All buildings have fixed sizes, so once we find the first firld that is part of the building, we can guess where the rest are located
            Building tmpBuilding;

            for (byte i = 0; i < 128; i++)
            {
                for (byte j = 0; j < 128; j++)
                {
                    if (buildingTypesMap[i][j] != 0)
                    {
                        // We have found the upper left corner of a building
                        tmpBuilding = new Building(i, j, buildingTypesMap[i][j]);
                        _buildings.Add(tmpBuilding);
                        _squares[i][j].buildingType = buildingTypesMap[i][j];

                        // Walk throught the rest of the building's fields and take note of their offsets relative to the upper left corner
                        for (byte i2 = 0; i2 < tmpBuilding.getSize(); i2++)
                        {
                            for (byte j2 = 0; j2 < tmpBuilding.getSize(); j2++)
                            {
                                if (i + i2 < 128 && j + j2 < 128)
                                {
                                    _squares[i + i2][j + j2].buildingType = _squares[i][j].buildingType;
                                    _squares[i + i2][j + j2].buildingOffsetX = i2;
                                    _squares[i + i2][j + j2].buildingOffsetZ = j2;

                                    buildingTypesMap[i + i2][j + j2] = 0; // Mark all of the fields with zeros in the temporary map, so this code will not take note of them again
                                }
                                else
                                {
                                    // This normally isn't possible, except if somebody used the "Magic Eraser" SimCity 2000 cheat near the edge of the map.
                                    // But since even the game renders these cases wrong, we don't need to worry about them here, so we can safely do nothing.
                                }
                            }
                        }
                    }
                }
            }
        }

        private void interpretXTER(Segment altmSegment)
        {
            byte[] content = altmSegment.getContent();

            int offset = 0;
            byte tmp;

            for (int i = 0; i < 128; i++)
            {
                for (int j = 0; j < 128; j++)
                {
                    tmp = content[offset];

                    if (tmp >= 48 && tmp <= 69)
                    {
                        _squares[i][j].hasWater = true;
                    }
                    else
                    {
                        _squares[i][j].hasWater = false;
                    }
                    offset++;
                }
            }
        }

        private void interpretMISC(Segment miscSegment)
        {
            byte[] content = miscSegment.getContent();

            _waterLevel = content[3651];
        }

        private void setWaterOnCoastalSquares()
        {
            for (int i = 0; i < 128; i++)
            {
                for (int j = 0; j < 128; j++)
                {
                    SquareData sd = _squares[i][j];
                    if (sd.altitude == _waterLevel - 1)
                    {
                        sd.hasWater = true;
                    }
                }
            }
        }

        public String getCityName()
        {
            return _cityName;
        }

        public SquareData getSquare(int x, int z)
        {
            return _squares[x][z];
        }

        public byte getWaterLevel()
        {
            return _waterLevel;
        }

        public List<Building> getBuildings()
        {
            return _buildings;
        }
    }
}
