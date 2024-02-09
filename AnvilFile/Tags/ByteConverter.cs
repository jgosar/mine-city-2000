using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class ByteConverter
    {

        public static long readNumber(byte[] bytes, int offset, int len)
        {
            if (len > 8)
            {
                throw new Exception("Cannot read numbers larger than 8 bytes!");
            }
            if (offset < 0 || len < 1 || (offset + len) >= bytes.Length)
            {
                throw new Exception("Invalid parameters: offset=" + offset + ", len=" + len);
            }

            long result = bytes[offset];
            for (int i = 1; i < len; i++)
            {
                result <<= 8;
                result += bytes[offset + i];
            }

            return result;
        }

        public static String readString(byte[] bytes, int offset, int len)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                sb.Append(Convert.ToChar(bytes[offset + i]));
            }

            return sb.ToString();
        }

        public static float readFloat(byte[] bytes, int offset)
        {
            byte[] reversedBytes = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                reversedBytes[i] = bytes[offset + 3 - i];
            }
            float resultNum = BitConverter.ToSingle(reversedBytes, 0);

            return resultNum;
        }

        public static double readDouble(byte[] bytes, int offset)
        {
            byte[] reversedBytes = new byte[8];

            for (int i = 0; i < 8; i++)
            {
                reversedBytes[i] = bytes[offset + 7 - i];
            }
            double resultNum = BitConverter.ToDouble(reversedBytes, 0);

            return resultNum;
        }

        public static byte[] floatBytes(float num)
        {
            byte[] reversedBytes = BitConverter.GetBytes(num);

            byte[] result = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                result[i] = reversedBytes[3 - i];
            }

            return result;
        }

        public static byte[] doubleBytes(double num)
        {
            byte[] reversedBytes = BitConverter.GetBytes(num);

            byte[] result = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                result[i] = reversedBytes[7 - i];
            }

            return result;
        }

        public static byte[] shortBytes(short num)
        {
            byte[] result = new byte[2];
            for (int i = 1; i > -1; i--)
            {
                result[i] = (byte)(num % 256);
                num >>= 8;
            }

            return result;
        }

        public static byte[] intBytes(int num)
        {
            byte[] result = new byte[4];
            for (int i = 3; i > -1; i--)
            {
                result[i] = (byte)(num % 256);
                num >>= 8;
            }

            return result;
        }

        public static byte[] longBytes(long num)
        {
            byte[] result = new byte[8];
            for (int i = 7; i > -1; i--)
            {
                result[i] = (byte)(num % 256);
                num >>= 8;
            }

            return result;
        }

        public static byte[] stringBytes(String str)
        {
            Char[] chars = str.ToCharArray();
            byte[] result = new byte[chars.Length];

            for (int i = 0; i < chars.Length; i++)
            {
                result[i] = (byte)Convert.ToInt32(chars[i]);
            }

            return result;
        }
    }
}
