using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace com.mc2k.SimCityReader
{
    public class Reader
    {
        public static CityData readFile(String fileName)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);
            String cityName = Path.GetFileNameWithoutExtension(fileName); // Get city name from filename just in case it can't be read from the file contents

            return read(fileBytes, cityName);
        }

        public static CityData read(byte[] fileBytes, String cityName)
        {
            Dictionary<String, Segment> segments = readSegmentsFromBytes(fileBytes);

            try
            {
                return new CityData(segments, cityName);
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException || ex is ArgumentException || ex is OverflowException)
            {
                // a well-formed segment whose body is too short for the fixed 128x128 maps
                throw new InvalidDataException("City file is truncated or corrupt", ex);
            }
        }

        private static Dictionary<String, Segment> readSegmentsFromBytes(byte[] fileBytes)
        {
            Dictionary<String, Segment> segments = new Dictionary<String, Segment>();

            int offset = 12;

            byte[] segmentBytes;

            try
            {
                while (offset < fileBytes.Length)
                {
                    if (offset + 8 >= fileBytes.Length)
                    {
                        throw new InvalidDataException("City file is truncated or corrupt");
                    }
                    String segmentName = readString(fileBytes, offset, 4);
                    offset += 4;
                    int segmentLength = (int)readNumber(fileBytes, offset, 4);
                    offset += 4;
                    if (segmentLength < 0 || segmentLength > fileBytes.Length - offset)
                    {
                        throw new InvalidDataException("City file is truncated or corrupt");
                    }
                    segmentBytes = new byte[segmentLength];
                    for (int i = 0; i < segmentLength; i++)
                    {
                        segmentBytes[i] = fileBytes[offset];
                        offset++;
                    }

                    segments.Add(segmentName, new Segment(segmentName, segmentBytes));
                }
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException || ex is OverflowException || ex is ArgumentException)
            {
                // a truncated file ends in the middle of a segment header or body; a corrupt length is negative or past the end
                throw new InvalidDataException("City file is truncated or corrupt", ex);
            }

            foreach (String required in new[] { "ALTM", "XBLD", "XTER", "MISC" })
            {
                if (!segments.ContainsKey(required))
                {
                    throw new InvalidDataException("City file is truncated or corrupt");
                }
            }
            return segments;
        }

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
    }
}
