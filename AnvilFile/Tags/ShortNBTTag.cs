using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class ShortNBTTag : NBTTag<short>
    {
        public ShortNBTTag(String name, short content) : base(name, content)
        {
            _type = Type.SHORT;
        }

        public ShortNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            _content = (short)ByteConverter.readNumber(bytes, pointer, 2);
            pointer += 2;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            return ByteConverter.shortBytes(_content);
        }
    }
}
