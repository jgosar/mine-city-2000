using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.IO;

namespace com.mc2k.MinecraftEditor
{
    class ZLibUtils
    {
        public static void CompressData(byte[] inData, out byte[] outData)
        {
            using var output = new MemoryStream();
            using (var z = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true)) { z.Write(inData, 0, inData.Length); }
            outData = output.ToArray();
        }

        public static void DecompressData(byte[] inData, out byte[] outData)
        {
            using var input = new MemoryStream(inData);
            using var z = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            z.CopyTo(output);
            outData = output.ToArray();
        }
    }
}
