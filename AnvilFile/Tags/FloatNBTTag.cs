using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class FloatNBTTag : NBTTag<float>
    {
        public FloatNBTTag(String name, float content) : base(name, content)
        {
            _type = Type.FLOAT;
        }

        public FloatNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            _content = ByteConverter.readFloat(bytes, pointer);
            pointer += 4;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            return ByteConverter.floatBytes(_content);
        }
    }
}
