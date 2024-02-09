using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class IntNBTTag : NBTTag<int>
    {
        public IntNBTTag(String name, int content) : base(name, content)
        {
            _type = Type.INT;
        }

        public IntNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            _content = (int)ByteConverter.readNumber(bytes, pointer, 4);
            pointer += 4;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            return ByteConverter.intBytes(_content);
        }
    }
}
