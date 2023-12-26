using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.SimCityReader
{
    public class Reader
    {
        public static CityData readFile(String fileName)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);
            Dictionary<String, Segment> segments = readSegmentsFromBytes(fileBytes);
            String cityName = fileName.Split('\\').Last().Replace(".sc2", "").Replace(".SC2", ""); // Get city name from filename just in case it can't be read from the file contents

            CityData result = new CityData(segments, cityName);

            return result;
        }

        private static Dictionary<String, Segment> readSegmentsFromBytes(byte[] fileBytes)
        {
            Dictionary<String, Segment> segments = new Dictionary<String, Segment>();

            int offset = 12;

            byte[] segmentBytes;

            while (offset < fileBytes.Length)
            {
                String segmentName = readString(fileBytes, offset, 4);
                offset += 4;
                int segmentLength = (int)readNumber(fileBytes, offset, 4);
                offset += 4;
                segmentBytes = new byte[segmentLength];
                for (int i = 0; i < segmentLength; i++)
                {
                    segmentBytes[i] = fileBytes[offset];
                    offset++;
                }

                segments.Add(segmentName, new Segment(segmentName, segmentBytes));
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
