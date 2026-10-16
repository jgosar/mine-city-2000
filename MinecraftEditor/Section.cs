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
                byte[]? blocks = _blocks;
                byte[]? data = _data;
                byte[]? blockLight = _blockLight;
                byte[]? skyLight = _skyLight;
                if (blocks == null)
                {
                    // Uniform section: serialise from temporary arrays and leave the section itself unallocated,
                    // so that saving a region does not inflate its memory.
                    blocks = createUniformBlocks();
                    data = new byte[2048];
                    blockLight = new byte[2048];
                    skyLight = createSkyLight();
                }

                List<AnyNBTTag> subTags = new List<AnyNBTTag>();
                if (blocks != null) { subTags.Add(new ByteArrayNBTTag("Blocks", blocks)); }
                if (data != null) { subTags.Add(new ByteArrayNBTTag("Data", data)); }
                if (blockLight != null) { subTags.Add(new ByteArrayNBTTag("BlockLight", blockLight)); }
                if (skyLight != null) { subTags.Add(new ByteArrayNBTTag("SkyLight", skyLight)); }
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

        // The block id every position holds while the arrays are not allocated (data is 0 then).
        private byte _uniformBlock = 0;

        public Section()
        {
        }

        /// <summary>
        /// Creates a section that is uniformly air. No arrays are allocated until something other than air
        /// (or a non-zero data value) is written, or until the raw arrays are requested.
        /// </summary>
        /// <param name="y">Vertical position of the section within its chunk, 0..15.</param>
        public Section(byte y)
        {
            _y = y;
        }

        /// <summary>
        /// Creates a section that is uniformly <paramref name="uniformBlock"/> with data 0. No arrays are
        /// allocated until a different block (or a non-zero data value) is written, or until the raw arrays
        /// are requested.
        /// </summary>
        /// <param name="y">Vertical position of the section within its chunk, 0..15.</param>
        /// <param name="uniformBlock">Block id that fills the whole section.</param>
        public Section(byte y, byte uniformBlock)
        {
            _y = y;
            _uniformBlock = uniformBlock;
        }

        /// <summary>
        /// True if the section is uniform air and has never been materialised, i.e. it holds no information.
        /// </summary>
        public bool isEmpty
        {
            get { return _blocks == null && _uniformBlock == 0; }
        }

        /// <summary>True once the block/data/light arrays have been allocated.</summary>
        internal bool isMaterialised
        {
            get { return _blocks != null; }
        }

        /// <summary>
        /// Block id at the given position. Arguments are in (x, y, z) order, each 0..15, which matches Minecraft's
        /// index formula <c>x + 16 * z + 256 * y</c>. Never allocates. Coordinates are not range-checked: callers
        /// must guard, and an out-of-range value aliases another position (or throws past the array).
        /// </summary>
        public byte getBlock(int x, int y, int z)
        {
            if (_blocks == null)
            {
                return _uniformBlock;
            }

            return _blocks[x + 16 * z + 256 * y];
        }

        /// <summary>
        /// Data nibble (0..15) at the given position, in (x, y, z) order. Never allocates. Coordinates are not
        /// range-checked (see <see cref="getBlock"/>).
        /// </summary>
        public byte getData(int x, int y, int z)
        {
            if (_data == null)
            {
                return 0;
            }

            int index = x + 16 * z + 256 * y;
            byte packed = _data[index / 2];
            return (byte)((index % 2 == 0) ? (packed & 0x0F) : (packed >> 4));
        }

        /// <summary>
        /// Sets block id and data nibble at the given position, in (x, y, z) order. Allocates the arrays on the
        /// first write that differs from the uniform fill (a block other than the uniform block, or data other than 0).
        /// <paramref name="data"/> is 0..15: higher bits are masked off. Coordinates are not range-checked (see
        /// <see cref="getBlock"/>).
        /// </summary>
        public void putBlock(int x, int y, int z, byte block, byte data)
        {
            if (_blocks == null && block == _uniformBlock && data == 0)
            {
                return;
            }

            byte[] blocks = materialise();
            int index = x + 16 * z + 256 * y;
            blocks[index] = block;

            if (_data == null)
            {
                _data = new byte[2048];
            }

            byte packed = _data[index / 2];
            if (index % 2 == 0)
            {
                packed = (byte)((packed & 0xF0) | (data & 0x0F));
            }
            else
            {
                packed = (byte)((packed & 0x0F) | ((data & 0x0F) << 4));
            }
            _data[index / 2] = packed;
        }

        /// <summary>
        /// The 4096 block ids, index <c>x + 16 * z + 256 * y</c>. Materialises a uniform section first.
        /// </summary>
        public byte[] getBlocks()
        {
            return materialise();
        }

        /// <summary>
        /// The 2048 packed data nibbles: the value for index i lives in element i / 2, in the low nibble when i is
        /// even and the high nibble when i is odd. Materialises a uniform section first.
        /// </summary>
        public byte[] getData()
        {
            materialise();
            return _data;
        }

        public byte getY()
        {
            return _y;
        }

        private byte[] createUniformBlocks()
        {
            byte[] blocks = new byte[4096];
            if (_uniformBlock != 0)
            {
                Array.Fill(blocks, _uniformBlock);
            }
            return blocks;
        }

        // Sky light is 255 (both nibbles 15) everywhere; the serialised world depends on it.
        private static byte[] createSkyLight()
        {
            byte[] skyLight = new byte[2048];
            Array.Fill(skyLight, (byte)255);
            return skyLight;
        }

        // Allocates the arrays of a uniform section (all positions hold _uniformBlock, data 0, block light 0,
        // sky light 255, i.e. 15 in both nibbles). Returns the blocks array; does nothing if they exist already.
        private byte[] materialise()
        {
            if (_blocks != null)
            {
                return _blocks;
            }

            _blocks = createUniformBlocks();
            _data = new byte[2048];
            _blockLight = new byte[2048];
            _skyLight = createSkyLight();
            return _blocks;
        }
    }
}
