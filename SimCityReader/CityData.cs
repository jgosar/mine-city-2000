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

        public CityData(Dictionary<String, Segment> segments){
            initializeSquares();

            interpretCNAM(segments["CNAM"]);
            interpretALTM(segments["ALTM"]);
            interpretXBLD(segments["XBLD"]);
            interpretXTER(segments["XTER"]);
            interpretMISC(segments["MISC"]);

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
            int offset=1;
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

            int offset = 0;

            byte[][] tmpMap = new byte[128][];
            for (int i = 0; i < 128; i++)
            {
                tmpMap[i] = new byte[128];
                for (int j = 0; j < 128; j++)
                {
                    tmpMap[i][j] = content[offset];
                    offset ++;
                }
            }

            Building tmpBuilding;

            for (byte i = 0; i < 128; i++)
            {
                for (byte j = 0; j < 128; j++)
                {
                    if (tmpMap[i][j] != 0)
                    {
                        tmpBuilding = new Building(i, j, tmpMap[i][j]);
                        _buildings.Add(tmpBuilding);
                        _squares[i][j].buildingType = tmpMap[i][j];

                        for(byte i2=0;i2<tmpBuilding.getSize();i2++){
                            for (byte j2 = 0; j2 < tmpBuilding.getSize();j2++ )
                            {
                                _squares[i + i2][j + j2].buildingType = _squares[i][j].buildingType;
                                _squares[i + i2][j + j2].buildingOffsetX = i2;
                                _squares[i + i2][j + j2].buildingOffsetZ = j2;

                                tmpMap[i + i2][j + j2] = 0;
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
    }
}
