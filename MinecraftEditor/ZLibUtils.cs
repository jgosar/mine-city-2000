using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.IO;
using zlib;

namespace com.mc2k.MinecraftEditor
{
    class ZLibUtils
    {
        public static void testDecompress(String file)
        {
            byte[] byteArray = System.IO.File.ReadAllBytes(file);
            byte[] decompressed=new byte[0];
            DecompressData(byteArray, out decompressed);

            System.IO.File.WriteAllBytes(file + ".out", decompressed);
        }

        public static void CompressData(byte[] inData, out byte[] outData)
        {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                CopyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
        }

        public static void DecompressData(byte[] inData, out byte[] outData)
        {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                CopyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
        }

        public static void CopyStream(System.IO.Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[4096];
            int len;
            while ((len = input.Read(buffer, 0, 4096)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }   
    }
}
