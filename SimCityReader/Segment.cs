using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.SimCityReader
{
    public class Segment
    {
        String _name;
        byte[] _content;

        public Segment(String name, byte[] bytes)
        {
            _name = name;
            if (name.Equals("CNAM") || name.Equals("ALTM"))
            {
                _content = bytes;
            }
            else
            {
                _content = decompress(bytes);
            }
        }

        private byte[] decompress(byte[] bytes)
        {
            List<byte> result = new List<byte>();

            int offset = 0;

            byte commandByte=0;

            while (offset < bytes.Length)
            {
                commandByte = bytes[offset];
                offset++;

                if (commandByte > 0 && commandByte < 128)
                {
                    for (int i = 0; i < commandByte; i++)
                    {
                        result.Add(bytes[offset]);
                        offset++;
                    }
                }
                else if (commandByte > 128)
                {
                    for (int i = 0; i < commandByte - 127; i++)
                    {
                        result.Add(bytes[offset]);
                    }
                    offset++;
                }
                else
                {
                    throw new Exception("Unexpected decompressor state");
                }
            }

            return result.ToArray();
        }

        public byte[] getContent()
        {
            return _content;
        }
    }
}
