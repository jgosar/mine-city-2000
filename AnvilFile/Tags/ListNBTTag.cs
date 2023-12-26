using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class ListNBTTag<T> : NBTTag<T[]>
    {
        internal Type _listType;

        public ListNBTTag(String name, T[] content): base(name, content)
        {
            _type = Type.LIST;

            if (typeof(T) == typeof(byte))
            {
                _listType = Type.BYTE;
            }
            else if (typeof(T) == typeof(short))
            {
                _listType = Type.SHORT;
            }
            else if (typeof(T) == typeof(int))
            {
                _listType = Type.INT;
            }
            else if (typeof(T) == typeof(long))
            {
                _listType = Type.LONG;
            }
            else if (typeof(T) == typeof(float))
            {
                _listType = Type.FLOAT;
            }
            else if (typeof(T) == typeof(double))
            {
                _listType = Type.DOUBLE;
            }
            else if (typeof(T) == typeof(AnyNBTTag))
            {
                _listType = Type.COMPOUND;
            }
        }

        public ListNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            _listType = (Type)ByteConverter.readNumber(bytes, pointer, 1);
            pointer++;
            long listLength = ByteConverter.readNumber(bytes, pointer, 4);
            pointer += 4;

            if (typeof(T) == typeof(byte))
                    {
                        byte[] tmp = new byte[listLength];

                        for (int i = 0; i < listLength; i++)
                        {
                            tmp[i] = (byte)ByteConverter.readNumber(bytes, pointer, 1);
                            pointer++;
                        }

                        _content = (T[])(object)tmp;
                    }
            else if (typeof(T) == typeof(short))
                    {
                        short[] tmp = new short[listLength];

                        for (int i = 0; i < listLength; i++)
                        {
                            tmp[i] = (short)ByteConverter.readNumber(bytes, pointer, 2);
                            pointer += 2;
                        }

                        _content = (T[])(object)tmp;
                    }
            else if (typeof(T) == typeof(int))
                    {
                        int[] tmp = new int[listLength];

                        for (int i = 0; i < listLength; i++)
                        {
                            tmp[i] = (int)ByteConverter.readNumber(bytes, pointer, 4);
                            pointer += 4;
                        }

                        _content = (T[])(object)tmp;
                    }
            else if (typeof(T) == typeof(long))
                    {
                        long[] tmp = new long[listLength];

                        for (int i = 0; i < listLength; i++)
                        {
                            tmp[i] = ByteConverter.readNumber(bytes, pointer, 8);
                            pointer += 8;
                        }

                        _content = (T[])(object)tmp;
                    }
            else if (typeof(T) == typeof(float))
                    {
                        float[] tmp = new float[listLength];

                        for (int i = 0; i < listLength; i++)
                        {
                            tmp[i] = ByteConverter.readFloat(bytes, pointer);
                            pointer += 4;
                        }

                        _content = (T[])(object)tmp;
                    }
            else if (typeof(T) == typeof(double))
                    {
                        double[] tmp = new double[listLength];

                        for (int i = 0; i < listLength; i++)
                        {
                            tmp[i] = ByteConverter.readDouble(bytes, pointer);
                            pointer += 8;
                        }

                        _content = (T[])(object)tmp;
                    }
            else if (typeof(T) == typeof(AnyNBTTag))
                    {
                        AnyNBTTag[] tmp = new AnyNBTTag[listLength];

                        for (int i = 0; i < listLength; i++)
                        {
                            tmp[i] = AnyNBTTag.parseTag(bytes, pointer, _listType);//Type.UNKNOWN);
                            pointer += tmp[i].length;
                        }

                        _content = (T[])(object)tmp;
                    }

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)_listType);
            bytes.AddRange(ByteConverter.intBytes(_content.Length));

            if (typeof(T) == typeof(byte))
            {
                bytes.AddRange((byte[])(object)_content);
            }
            else if (typeof(T) == typeof(short))
            {
                for (int i = 0; i < _content.Length; i++)
                {
                    bytes.AddRange(ByteConverter.shortBytes((short)(object)_content[i]));
                }
            }
            else if (typeof(T) == typeof(int))
            {
                for (int i = 0; i < _content.Length; i++)
                {
                    bytes.AddRange(ByteConverter.intBytes((int)(object)_content[i]));
                }
            }
            else if (typeof(T) == typeof(long))
            {
                for (int i = 0; i < _content.Length; i++)
                {
                    bytes.AddRange(ByteConverter.longBytes((long)(object)_content[i]));
                }
            }
            else if (typeof(T) == typeof(float))
            {
                for (int i = 0; i < _content.Length; i++)
                {
                    bytes.AddRange(ByteConverter.floatBytes((float)(object)_content[i]));
                }
            }
            else if (typeof(T) == typeof(double))
            {
                for (int i = 0; i < _content.Length; i++)
                {
                    bytes.AddRange(ByteConverter.doubleBytes((double)(object)_content[i]));
                }
            }
            else if (typeof(T) == typeof(AnyNBTTag))
            {
                for (int i = 0; i < _content.Length; i++)
                {
                    bytes.AddRange(((CompoundNBTTag)(object)_content[i]).contentToBytes());
                }
            }

            return bytes.ToArray();
        }
    }
}
