using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class ByteArrayNBTTag : NBTTag<byte[]>
    {
        public ByteArrayNBTTag(String name, byte[] content) : base(name, content)
        {
            _type = Type.BYTE_ARRAY;
        }

        public ByteArrayNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            long payloadLength = ByteConverter.readNumber(bytes, pointer, 4);
            pointer += 4;

            byte[] payload = new byte[payloadLength];
            Array.Copy(bytes, pointer, payload, 0, payloadLength);
            pointer += (int)payloadLength;

            _content = payload;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(ByteConverter.intBytes(_content.Length));
            bytes.AddRange(_content);

            return bytes.ToArray();
        }
    }
}
