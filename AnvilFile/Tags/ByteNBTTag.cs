using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class ByteNBTTag : NBTTag<byte>
    {

        public ByteNBTTag(String name, byte content) : base(name, content)
        {
            _type = Type.BYTE;
        }

        public ByteNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            _content = (byte)ByteConverter.readNumber(bytes, pointer, 1);
            pointer++;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            return new byte[]{_content};
        }
    }
}
