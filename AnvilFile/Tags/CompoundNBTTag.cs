using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.AnvilFile.Tags
{
    public class CompoundNBTTag : NBTTag<List<AnyNBTTag>>
    {
        public CompoundNBTTag(String name, List<AnyNBTTag> content) : base(name, content)
        {
            _type = Type.COMPOUND;
        }

        public CompoundNBTTag(byte[] bytes, int offset, Type type) : base(bytes, offset, type)
        {
        }

        protected override int readContentFromBytes(byte[] bytes, int pointer)
        {
            List<AnyNBTTag> subTags = new List<AnyNBTTag>();

            Boolean end = false;
            AnyNBTTag tmp;
            while (!end)
            {
                tmp = AnyNBTTag.parseTag(bytes, pointer);
                pointer += tmp.length;

                subTags.Add(tmp);

                end = ((Type)bytes[pointer] == Type.END);
            }

            pointer++;

            _content = subTags;

            return pointer;
        }

        internal override byte[] contentToBytes()
        {
            List<byte> bytes = new List<byte>();
            List<AnyNBTTag> subTags = (List<AnyNBTTag>)_content;

            foreach (AnyNBTTag subTag in subTags)
            {
                bytes.AddRange(subTag.toBytesWithHeader());
            }

            bytes.Add(0);

            return bytes.ToArray();
        }

        public override Object getObjectOnPath(String path)
        {
            String[] splitPath = path.Split(new char[] { '/' });

            if (splitPath[0].Equals(_name))
            {
                checkPathLength(splitPath);

                String subPath = path.Substring(path.IndexOf("/") + 1);
                List<AnyNBTTag> subTags = _content;

                foreach (AnyNBTTag subTag in subTags)
                {
                    Object result = subTag.getObjectOnPath(subPath);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private void checkPathLength(String[] splitPath)
        {
            if (splitPath.Length == 1)
            {
                throw new Exception("Commpound NBT tag with path length 1");
            }
        }
    }
}
