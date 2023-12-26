using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.mc2k.AnvilFile.Tags;
using com.mc2k.AnvilFile;

namespace com.mc2k.MinecraftEditor
{
    public class Section : NBTConvertible
    {
        public override CompoundNBTTag NBTTag {
            get
            {
                List<AnyNBTTag> subTags = new List<AnyNBTTag>();
                if (_blocks != null) { subTags.Add(new ByteArrayNBTTag("Blocks", _blocks)); }
                if (_data != null) { subTags.Add(new ByteArrayNBTTag("Data", _data)); }
                if (_blockLight != null) { subTags.Add(new ByteArrayNBTTag("BlockLight", _blockLight)); }
                if (_skyLight != null) { subTags.Add(new ByteArrayNBTTag("SkyLight", _skyLight)); }
                subTags.Add(new ByteNBTTag("Y", _y));

                if (_add != null) { subTags.Add(new ByteArrayNBTTag("Add", _add)); }

                CompoundNBTTag sectionTag = new CompoundNBTTag("", subTags);

                return sectionTag;
            }
            set
            {
                Object y = value.getObjectOnPath("/Y");
                Object blocks = value.getObjectOnPath("/Blocks");
                Object add = value.getObjectOnPath("/Add");
                Object data = value.getObjectOnPath("/Data");
                Object blockLight = value.getObjectOnPath("/BlockLight");
                Object skyLight = value.getObjectOnPath("/SkyLight");

                if (y != null) { _y = (byte)y; }
                if (blocks != null) { _blocks = (byte[])blocks; }
                if (add != null) { _add = (byte[])add; }
                if (data != null) { _data = (byte[])data; }
                if (blockLight != null) { _blockLight = (byte[])blockLight; }
                if (skyLight != null) { _skyLight = (byte[])skyLight; }
            }
        }

        private byte _y;
        private byte[] _blocks = null;
        private byte[] _add = null;
        private byte[] _data = null;
        private byte[] _blockLight = null;
        private byte[] _skyLight = null;

        public Section()
        {
        }

        public Section(byte y)
        {
            _y = y;
            _blocks = new byte[4096];
            _data = new byte[2048];
            _blockLight = new byte[2048];
            _skyLight = new byte[2048];

            for (int i = 0; i < 2048; i++)
            {
                _skyLight[i] = 255;
            }
        }

        public byte[] getBlocks()
        {
            return _blocks;
        }

        public byte[] getData()
        {
            return _data;
        }

        public byte getY()
        {
            return _y;
        }
    }
}
