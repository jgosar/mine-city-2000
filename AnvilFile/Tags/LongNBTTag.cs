using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class LongNBTTag : NBTTag<long>
    {
        public LongNBTTag(String name, long content) : base(name, content)
        {
            _type = Type.LONG;
        }

        public LongNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            _content = (long)ByteConverter.readNumber(bytes, pointer, 8);
            pointer += 8;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            return ByteConverter.longBytes(_content);
        }
    }
}
