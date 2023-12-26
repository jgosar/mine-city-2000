using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public abstract class NBTTag<T> : AnyNBTTag
    {
        internal Type _type;
        internal String _name;
        internal T _content;

        private int _length;

        public override int length
        {
            get
            {
                return _length;
            }
        }

        internal NBTTag(String name, T content)
        {
            _name = name;
            _content = content;
        }

        public NBTTag(byte[] bytes, int offset, Type knownType = Type.UNKNOWN)
        {
            int pointer = offset;

            if (knownType == Type.UNKNOWN)
            {
                if (!Enum.IsDefined(typeof(Type), (object)bytes[pointer]))
                {
                    throw new Exception("Undefined NBTTag type: " + bytes[pointer]);
                }
                _type = (Type)bytes[pointer];
                pointer++;
                short nameLength = (short)ByteConverter.readNumber(bytes, pointer, 2);
                pointer += 2;
                _name = ByteConverter.readString(bytes, pointer, nameLength);
                pointer += nameLength;
            }
            else
            {
                _type = knownType;
                _name = "";
            }

            pointer = readFromBytes(bytes, pointer);

            _length = pointer - offset;
        }

        private int readFromBytes(byte[] bytes, int pointer)
        {
            int bytesStart = pointer;

            pointer = readContentFromBytes(bytes, pointer);

            return pointer;
        }

        protected abstract int readContentFromBytes(byte[] bytes, int pointer);

        public override Object getObjectOnPath(String path)
        {
            String[] splitPath = path.Split(new char[] { '/' });

            if (splitPath[0].Equals(_name))
            {
                if (splitPath.Length == 1)
                {
                    return _content;
                }
                else
                {
                    throw new Exception("Simple NBT tag with path length > 1");
                }
            }

            return null;
        }

        public override byte[] toBytesWithHeader()
        {
            byte[] content = contentToBytes();
            int nameLength = _name.Length;

            byte[] bytes = new byte[1 + 2 + nameLength + content.Length];
            bytes[0] = (byte)_type;
            shortBytes((short)nameLength).CopyTo(bytes, 1);
            stringBytes(_name).CopyTo(bytes, 3);
            content.CopyTo(bytes, 3 + nameLength);

            return bytes;
        }

        internal abstract byte[] contentToBytes();

        internal static byte[] shortBytes(short num)
        {
            byte[] result = new byte[2];
            for (int i = 1; i > -1; i--)
            {
                result[i] = (byte)(num % 256);
                num >>= 8;
            }

            return result;
        }

        internal static byte[] stringBytes(String str)
        {
            Char[] chars = str.ToCharArray();
            byte[] result = new byte[chars.Length];

            for (int i = 0; i < chars.Length; i++)
            {
                result[i] = (byte)Convert.ToInt32(chars[i]);
            }

            return result;
        }
    }
}
