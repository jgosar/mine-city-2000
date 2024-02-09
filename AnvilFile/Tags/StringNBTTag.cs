using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class StringNBTTag : NBTTag<String>
    {
        public StringNBTTag(String name, String content) : base(name, content)
        {
            _type = Type.STRING;
        }

        public StringNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            long payloadLength = ByteConverter.readNumber(bytes, pointer, 2);
            pointer += 2;

            _content = ByteConverter.readString(bytes, pointer, (int)payloadLength);
            pointer += (int)payloadLength;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(ByteConverter.shortBytes((short)_content.Length));
            bytes.AddRange(ByteConverter.stringBytes(_content));

            return bytes.ToArray();
        }
    }
}
