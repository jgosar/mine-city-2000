using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class IntArrayNBTTag : NBTTag<int[]>
    {
        public IntArrayNBTTag(String name, int[] content) : base(name, content)
        {
            _type = Type.INT_ARRAY;
        }

        public IntArrayNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            long payloadLength = ByteConverter.readNumber(bytes, pointer, 4);
            pointer += 4;

            int[] payload = new int[payloadLength];
            for (int i = 0; i < payloadLength; i++)
            {
                payload[i] = (int)ByteConverter.readNumber(bytes, pointer, 4);
                pointer += 4;
            }

            _content = payload;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            List<byte> bytes = new List<byte>();
            int[] payload = (int[])_content;
            bytes.AddRange(ByteConverter.intBytes(payload.Length));
            for (int i = 0; i < payload.Length; i++)
            {
                bytes.AddRange(ByteConverter.intBytes(payload[i]));
            }

            return bytes.ToArray();
        }
    }
}
