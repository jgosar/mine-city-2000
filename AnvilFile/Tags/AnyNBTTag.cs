using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public abstract class AnyNBTTag
    {
        public enum Type : byte { END, BYTE, SHORT, INT, LONG, FLOAT, DOUBLE, BYTE_ARRAY, STRING, LIST, COMPOUND, INT_ARRAY, UNKNOWN }

        public abstract int length
        {
            get;
        }

        public static AnyNBTTag parseTag(byte[] bytes, int offset, Type knownType = Type.UNKNOWN)
        {
            int pointer = offset;

            if(!Enum.IsDefined(typeof(Type), (object)bytes[pointer])){
                throw new Exception("Undefined NBTTag type: " + bytes[pointer]);
            }
            Type type;
            if (knownType == Type.UNKNOWN)
            {
                type = (Type)bytes[pointer];
            }
            else
            {
                type = knownType;
            }

            switch (type)
            {
                case Type.BYTE:
                    {
                        return new ByteNBTTag(bytes, offset, knownType);
                    }
                case Type.SHORT:
                    {
                        return new ShortNBTTag(bytes, offset, knownType);
                    }
                case Type.INT:
                    {
                        return new IntNBTTag(bytes, offset, knownType);
                    }
                case Type.LONG:
                    {
                        return new LongNBTTag(bytes, offset, knownType);
                    }
                case Type.FLOAT:
                    {
                        return new FloatNBTTag(bytes, offset, knownType);
                    }
                case Type.DOUBLE:
                    {
                        return new DoubleNBTTag(bytes, offset, knownType);
                    }
                case Type.BYTE_ARRAY:
                    {
                        return new ByteArrayNBTTag(bytes, offset, knownType);
                    }
                case Type.STRING:
                    {
                        return new StringNBTTag(bytes, offset, knownType);
                    }
                case Type.LIST:
                    {
                        short nameLength = (short)ByteConverter.readNumber(bytes, pointer+1, 2);
                        Type listType = (Type)ByteConverter.readNumber(bytes, pointer+1+2+nameLength, 1);
                        switch (listType)
                        {
                            case Type.BYTE:
                                {
                                    return new ListNBTTag<byte>(bytes, offset, knownType);
                                }
                            case Type.SHORT:
                                {
                                    return new ListNBTTag<short>(bytes, offset, knownType);
                                }
                            case Type.INT:
                                {
                                    return new ListNBTTag<int>(bytes, offset, knownType);
                                }
                            case Type.LONG:
                                {
                                    return new ListNBTTag<long>(bytes, offset, knownType);
                                }
                            case Type.FLOAT:
                                {
                                    return new ListNBTTag<float>(bytes, offset, knownType);
                                }
                            case Type.DOUBLE:
                                {
                                    return new ListNBTTag<double>(bytes, offset, knownType);
                                }
                            case Type.COMPOUND:
                                {
                                    return new ListNBTTag<AnyNBTTag>(bytes, offset, knownType);
                                }
                            default:
                                {
                                    throw new Exception("Unknown NBT tag type for list element: " + listType);
                                }
                        }
                    }
                case Type.COMPOUND:
                    {
                        return new CompoundNBTTag(bytes, offset, knownType);
                    }
                case Type.INT_ARRAY:
                    {
                        return new IntArrayNBTTag(bytes, offset, knownType);
                    }
                default:
                    {
                        throw new Exception("Unknown NBT tag type for element: " + type);
                    }
            }
        }

        public abstract byte[] toBytesWithHeader();

        public abstract Object getObjectOnPath(String path);

        public static AnyNBTTag makeListTag(String name, Object[] elements)
        {
            AnyNBTTag[] subTags = new AnyNBTTag[elements.Length];

            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] is AnyNBTTag)
                {
                    subTags[i] = (AnyNBTTag)elements[i];
                }
                else if (elements[i] is NBTConvertible)
                {
                    subTags[i] = ((NBTConvertible)elements[i]).NBTTag;
                }
            }

            AnyNBTTag result = new ListNBTTag<AnyNBTTag>(name, subTags);

            return result;
        }
    }
}
