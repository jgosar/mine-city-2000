using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.mc2k.AnvilFile.Tags;

namespace com.mc2k.MinecraftEditor
{
    public class MCAReader
    {
        public static List<Chunk> readFile(String fileName)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);

            List<Chunk> chunks = new List<Chunk>();

            for (int offset = 0; offset < 4096; offset += 4)
            {
                //Read chunk metadata
                int chunkOffset = (int)ByteConverter.readNumber(fileBytes, offset, 3) * 4096;

                //irrelevant
                //int sectors = (int)NBTTag.readNumber(fileBytes, offset+3, 1);
                //int timestamp = (int)NBTTag.readNumber(fileBytes, offset+4096, 3);

                if (chunkOffset != 0)
                {
                    //Read chunk
                    long chunkLength = ByteConverter.readNumber(fileBytes, chunkOffset, 4);

                    byte compressionType = fileBytes[chunkOffset + 4];

                    if (compressionType == 1)
                    {
                        throw new Exception("gzip compression not supported!");
                    }

                    byte[] chunkData = new byte[chunkLength - 1];

                    for (int i = 0; i < chunkLength - 1; i++)
                    {
                        chunkData[i] = fileBytes[chunkOffset + 5 + i];
                    }

                    byte[] decompressedChunk = new byte[0];

                    if (compressionType == 2)
                    {
                        ZLibUtils.DecompressData(chunkData, out decompressedChunk);
                    }

                    //System.IO.File.WriteAllBytes(@"..\..\..\output\\orig.chunk", decompressedChunk);

                    //byte[] recompressedChunk = new byte[0];
                    //ZLibUtils.CompressData(decompressedChunk, out recompressedChunk);

                    CompoundNBTTag rootTag = (CompoundNBTTag)AnyNBTTag.parseTag(decompressedChunk, 0);

                    //Console.Write("Pointer= " + offset + " ChunkOffset=" + chunkOffset + " ");
                    Chunk tmp = new Chunk() { NBTTag = rootTag };

                    chunks.Add(tmp);
                }
            }

            return chunks;
        }
    }
}
