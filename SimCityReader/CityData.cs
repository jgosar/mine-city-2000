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
        private int _compass;

        private const int MISC_COMPASS_OFFSET = 8;        // 4-byte big-endian number of 90 degree rotations of the view when the city was saved (0..3)
        private const int MISC_WATER_LEVEL_OFFSET = 3651;
        private const byte XBIT_ROTATE = 0x02;            // "rotate the tile by 90 degrees" flag

        public CityData(Dictionary<String, Segment> segments, String cityName)
        {
            initializeSquares();

            if (segments.ContainsKey("CNAM")) // Read city name from file contents, if it is included in the file, otherwise use the name passed to the constructor
            {
                interpretCNAM(segments["CNAM"]);
                if (string.IsNullOrWhiteSpace(_cityName))
                {
                    _cityName = cityName; // Empty name in the file: fall back to the name passed to the constructor
                }
            }
            else
            {
                _cityName = cityName;
            }
            interpretALTM(segments["ALTM"]); // Read altitude map
            interpretXBLD(segments["XBLD"]); // Read building positions
            interpretXTER(segments["XTER"]); // Read positions of groundwater
            interpretMISC(segments["MISC"]); // Read general water level and the compass
            if (segments.ContainsKey("XBIT")) // Per-square flags; every city saved by the game has it, hand-built test cities may not
            {
                interpretXBIT(segments["XBIT"]);
            }

            setWaterOnCoastalSquares();
            computeOrientations();
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

        private void interpretXBLD(Segment xbldSegment)
        {
            byte[] content = xbldSegment.getContent();

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

        private void interpretXTER(Segment xterSegment)
        {
            byte[] content = xterSegment.getContent();

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

            _compass = (int)Reader.readNumber(content, MISC_COMPASS_OFFSET, 4);
            _waterLevel = content[MISC_WATER_LEVEL_OFFSET];
        }

        private void interpretXBIT(Segment xbitSegment)
        {
            byte[] content = xbitSegment.getContent();

            int offset = 0;

            for (int i = 0; i < 128; i++)
            {
                for (int j = 0; j < 128; j++)
                {
                    _squares[i][j].rotateFlag = (content[offset] & XBIT_ROTATE) != 0;
                    offset++;
                }
            }
        }

        // Works out which way each directional building runs. The game draws the tile's sprite mirrored when the rotate flag is set,
        // which turns it by 90 degrees in the isometric view (SIMCITY.EXE, tile drawing at 0x43C730, rotating the view at 0x465490):
        // - Bridges: rotating the view flips the flag along with the map, so the flag set always means "along x", whatever the compass.
        // - Runways and piers: rotating the view leaves the flag alone and the game mirrors them by "flag XOR odd compass" instead,
        //   so the flag's meaning flips when the city was saved with the map turned by 90 or 270 degrees.
        private void computeOrientations()
        {
            bool compassIsOdd = _compass % 2 == 1;

            for (int i = 0; i < 128; i++)
            {
                for (int j = 0; j < 128; j++)
                {
                    SquareData sd = _squares[i][j];
                    byte code = sd.buildingType;

                    if (BuildingType.isBridgePart(code))
                    {
                        sd.orientation = sd.rotateFlag ? Orientation.AlongX : Orientation.AlongZ;
                    }
                    else if (BuildingType.isRunway(code) || BuildingType.isPier(code))
                    {
                        sd.orientation = (sd.rotateFlag != compassIsOdd) ? Orientation.AlongX : Orientation.AlongZ;
                    }
                    else
                    {
                        sd.orientation = Orientation.None;
                    }
                }
            }
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

        /// <summary>How many times (0..3) the map was turned by 90 degrees when the city was saved.</summary>
        public int getCompass()
        {
            return _compass;
        }

        public List<Building> getBuildings()
        {
            return _buildings;
        }
    }
}
