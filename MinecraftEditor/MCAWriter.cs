using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.mc2k.AnvilFile.Tags;

namespace com.mc2k.MinecraftEditor
{
    class MCAWriter
    {
        public static void writeToFile(List<Chunk> chunks, String fileName)
        {
            List<byte> output = new List<byte>();

            byte[] chunkLocations = new byte[4096];
            byte[] timestamps = new byte[4096];
            List<byte> chunkContents = new List<byte>();

            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)t.TotalSeconds;
            byte[] timestampBytes = ByteConverter.intBytes(timestamp);

            foreach (Chunk chunk in chunks)
            {
                int locatorOffset = 4 * (Util.positiveMod(chunk.getXPos(), 32) + Util.positiveMod(chunk.getZPos(), 32) * 32);
                byte[] chunkLocation = ByteConverter.intBytes((chunkContents.Count + 8192) / 4096);
                chunkLocations[locatorOffset] = chunkLocation[1];
                chunkLocations[locatorOffset + 1] = chunkLocation[2];
                chunkLocations[locatorOffset + 2] = chunkLocation[3];

                byte[] chunkBytes = chunkToBytes(chunk);
                chunkContents.AddRange(chunkBytes);

                byte chunkLength = (byte)(chunkBytes.Count() / 4096);
                chunkLocations[locatorOffset + 3] = chunkLength;

                timestamps[locatorOffset] = timestampBytes[0];
                timestamps[locatorOffset + 1] = timestampBytes[1];
                timestamps[locatorOffset + 2] = timestampBytes[2];
                timestamps[locatorOffset + 3] = timestampBytes[3];
            }

            output.AddRange(chunkLocations);
            output.AddRange(timestamps);
            output.AddRange(chunkContents);

            System.IO.File.WriteAllBytes(fileName, output.ToArray());
        }

        private static byte[] chunkToBytes(Chunk chunk)
        {
            byte[] chunkSize;
            byte compressionType = 2;

            byte[] uncompressed = chunk.NBTTag.toBytesWithHeader();
            byte[] compressed = new byte[0];
            ZLibUtils.CompressData(uncompressed, out compressed);

            chunkSize = ByteConverter.intBytes(compressed.Length+1);

            List<byte> result= new List<byte>();
            result.AddRange(chunkSize);
            result.Add(compressionType);
            result.AddRange(compressed);

            //Padding to len%4096=0
            byte[] padding = new byte[4096 - (result.Count % 4096)];
            result.AddRange(padding);

            return result.ToArray();
        }

        private static void insertIntoArray(byte[] array, byte[] newContent, int offset)
        {
            for (int i = 0; i < newContent.Length; i++)
            {
                array[i + offset] = newContent[i];
            }
        }
    }
}
