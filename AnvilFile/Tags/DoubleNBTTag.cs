using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class DoubleNBTTag : NBTTag<double>
    {
        public DoubleNBTTag(String name, double content) : base(name, content)
        {
            _type = Type.DOUBLE;
        }

        public DoubleNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            _content = ByteConverter.readDouble(bytes, pointer);
            pointer += 8;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            return ByteConverter.doubleBytes(_content);
        }
    }
}
