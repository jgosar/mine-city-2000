using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.mc2k.AnvilFile.Tags;

namespace com.mc2k.MinecraftEditor
{
    public class AbstractRegion
    {
        //Region=32x32x16 sections
        //Section=16x16x16 blockDatas
        //blockData=block+data
        private byte[][][][][][][] _blockDatas;
        private int _regionX;
        private int _regionZ;

        public AbstractRegion(int regionX, int regionZ)
        {
            _regionX = regionX;
            _regionZ = regionZ;

            _blockDatas = new byte[32][][][][][][];
            for (int i = 0; i < 32; i++)
            {
                _blockDatas[i] = new byte[32][][][][][];
                for (int j = 0; j < 32; j++)
                {
                    _blockDatas[i][j] = new byte[16][][][][];
                }
            }
        }

        public byte getBlock(int x, int z, int y)
        {
            return getBlockData(x,z,y)[0];
        }

        public byte[] getBlockData(int x, int z, int y)
        {
            if (y >= 256)
            {
                return new byte[2];
            }

            byte[][][][] section = _blockDatas[x / 16][z / 16][y / 16];

            if (section == null)
            {
                section = new byte[16][][][];
                for (int i = 0; i < 16; i++)
                {
                    section[i] = new byte[16][][];
                    for (int j = 0; j < 16; j++)
                    {
                        section[i][j] = new byte[16][];
                        for (int k = 0; k < 16; k++)
                        {
                            section[i][j][k] = new byte[2];
                        }
                    }
                }
            }

            return section[x % 16][y % 16][z % 16];
        }

        public void putBlock(int x, int z, int y, byte block)
        {
            if (y >= 256)
            {
                return;
            }

            byte[][][][] section = _blockDatas[x / 16][z / 16][y / 16];

            if (section == null)
            {
                section = new byte[16][][][];
                for (int i = 0; i < 16; i++)
                {
                    section[i] = new byte[16][][];
                    for (int j = 0; j < 16; j++)
                    {
                        section[i][j] = new byte[16][];
                        for (int k = 0; k < 16; k++)
                        {
                            section[i][j][k] = new byte[2];
                        }
                    }
                }

                _blockDatas[x / 16][z / 16][y / 16] = section;
            }

            section[x % 16][z % 16][y % 16][0] = block;
            section[x % 16][z % 16][y % 16][1] = 0;
        }

        public void putBlockData(int x, int z, int y, byte[] block)
        {
            if (y >= 256)
            {
                return;
            }

            byte[][][][] section = _blockDatas[x / 16][z / 16][y / 16];

            if (section == null)
            {
                section = new byte[16][][][];
                for (int i = 0; i < 16; i++)
                {
                    section[i] = new byte[16][][];
                    for (int j = 0; j < 16; j++)
                    {
                        section[i][j] = new byte[16][];
                        for (int k = 0; k < 16; k++)
                        {
                            section[i][j][k] = new byte[2];
                        }
                    }
                }

                _blockDatas[x / 16][z / 16][y / 16] = section;
            }

            section[x % 16][z % 16][y % 16] = block;
        }

        public void saveToFolder(String outputFolder)
        {
            String fileName = "r." + _regionX + "." + _regionZ + ".mca";
            List<Chunk> chunks = new List<Chunk>();

            for (byte i = 0; i < 32; i++)
            {
                for (byte j = 0; j < 32; j++)
                {
                    List<Section> chunkSections = new List<Section>();
                    for (byte k = 0; k < 16; k++)
                    {
                        if (_blockDatas[i][j][k] != null)
                        {
                            Section tmpSection = new Section(k);
                            fillSection(tmpSection, _blockDatas[i][j][k]);

                            chunkSections.Add(tmpSection);
                        }
                    }

                    if (chunkSections.Count != 0)
                    {
                        Chunk tmpChunk = new Chunk(32 * _regionX + i, 32 * _regionZ + j);
                        tmpChunk.setSections(chunkSections.ToArray());
                        chunks.Add(tmpChunk);
                    }
                }
            }

            MCAWriter.writeToFile(chunks, outputFolder + "\\" + fileName);
        }

        private void fillSection(Section tmpSection, byte[][][][] blockDatas)
        {
            byte[] blockBytes = tmpSection.getBlocks();
            byte[] dataBytes = tmpSection.getData();

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        blockBytes[i + 16 * j + 256 * k] = blockDatas[i][j][k][0];

                        byte dataValue = dataBytes[(i + 16 * j + 256 * k)/2];
                        if (i % 2 == 1)
                        {
                            dataValue <<= 4;
                            dataValue >>= 4;
                            dataValue += (byte)(blockDatas[i][j][k][1] * 16);
                        }
                        else
                        {
                            dataValue >>= 4;
                            dataValue <<= 4;
                            dataValue += blockDatas[i][j][k][1];
                        }

                        dataBytes[(i + 16 * j + 256 * k) / 2] = dataValue;
                    }
                }
            }
        }
    }
}
