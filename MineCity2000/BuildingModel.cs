using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using com.mc2k.SimCityReader;
using com.mc2k.MinecraftEditor;

namespace com.mc2k.MineCity2000
{
    class BuildingModel
    {
        private const int BEDROCK_BLOCK = 7;
        private const int WOOL_BLOCK = 35;
        private const int SANDSTONE_BLOCK = 24;
        //_blockDatas=sizexsizex16 sections
        //Section=16x16x16 blockDatas
        //blockData=block+data
        private byte[][][][][][][] _blockDatas;
        BuildingType _type;

        public static bool modelFileExists(byte typeCode, String folder, bool rotated)
        {
            if (rotated)
            {
                return File.Exists(folder + "\\" + typeCode + "r.mca");
            }
            else
            {
                return File.Exists(folder + "\\" + typeCode + ".mca");
            }
        }

        public BuildingModel(byte typeCode, String folder, bool rotated)
        {
            _type = BuildingType.getByCode(typeCode);

            _blockDatas = new byte[_type.getSize()][][][][][][];
            for (int i = 0; i < _type.getSize(); i++)
            {
                _blockDatas[i] = new byte[_type.getSize()][][][][][];
                for (int j = 0; j < _type.getSize(); j++)
                {
                    _blockDatas[i][j] = new byte[16][][][][];
                }
            }

            List<Chunk> chunks;
            if (rotated)
            {
                chunks = MCAReader.readFile(folder + "\\" + typeCode + "r.mca");
            }
            else
            {
                chunks = MCAReader.readFile(folder + "\\" + typeCode + ".mca");
            }

            byte[][][][] tmpSectionData; 

            foreach(Chunk chunk in chunks){
                if (chunk.getXPos() < _type.getSize() && chunk.getZPos() < _type.getSize())
                {
                    foreach (Section section in chunk.getSections())
                    {
                        for (int bl = 0; bl < section.getBlocks().Length; bl++)
                        {
                            if (section.getBlocks()[bl] != 0 || section.getData()[bl / 2] != 0)
                            {
                                tmpSectionData = _blockDatas[chunk.getXPos()][chunk.getZPos()][section.getY()];

                                if (tmpSectionData == null)
                                {
                                    tmpSectionData = new byte[16][][][];
                                    for (int i = 0; i < 16; i++)
                                    {
                                        tmpSectionData[i] = new byte[16][][];
                                        for (int j = 0; j < 16; j++)
                                        {
                                            tmpSectionData[i][j] = new byte[16][];
                                            for (int k = 0; k < 16; k++)
                                            {
                                                tmpSectionData[i][j][k] = new byte[2];
                                            }
                                        }
                                    }

                                    _blockDatas[chunk.getXPos()][chunk.getZPos()][section.getY()] = tmpSectionData;
                                }

                                int x = bl%16;
                                int y = bl/256;
                                int z = (bl - (256 * y)) / 16;

                                byte tmpData = section.getData()[bl / 2];

                                if (bl % 2 == 1)
                                {
                                    tmpData >>= 4;
                                }
                                else
                                {
                                    tmpData <<= 4;
                                    tmpData >>= 4;
                                }

                                //ignore wool and bedrock below the top layer of the bottom section
                                if (section.getY() > 0 || y == 15 || (section.getBlocks()[bl] != WOOL_BLOCK && section.getBlocks()[bl] != BEDROCK_BLOCK))
                                {
                                    tmpSectionData[x][z][y][0] = section.getBlocks()[bl];
                                    tmpSectionData[x][z][y][1] = tmpData;
                                }
                                else if (section.getY() == 0 && section.getBlocks()[bl] == WOOL_BLOCK && tmpData != 0 && tmpData!=15)//except colored wool
                                {
                                    tmpSectionData[x][z][y][0] = section.getBlocks()[bl];
                                    tmpSectionData[x][z][y][1] = tmpData;
                                }
                                else if (BuildingType.isSloped(typeCode) && section.getBlocks()[bl] == WOOL_BLOCK)//for sloped blocks, turn wool to sandstone
                                {
                                    tmpSectionData[x][z][y][0] = SANDSTONE_BLOCK;
                                    tmpSectionData[x][z][y][1] = 0;
                                }
                            }
                        }
                    }
                }
            }
        }

        public byte[][][][][] getChunkData(int x, int z){
            return _blockDatas[x][z];
        }
    }
}
