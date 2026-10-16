using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.mc2k.AnvilFile.Tags;

namespace com.mc2k.MinecraftEditor
{
    /// <summary>
    /// One Minecraft region (32x32 chunks, 512x512 blocks, 256 high) held in memory as 32x32x16 lazily created
    /// <see cref="Section"/>s. Positions are given as (x, z, y) with x and z relative to the region (0..511) and
    /// y the height (0..255); this historical order differs from <see cref="Section"/>, which takes (x, y, z).
    /// Coordinates are not range-checked (only y >= 256 is tolerated): callers must guard, and an out-of-range x or z
    /// aliases another position or throws. Data values are 0..15; higher bits are masked off.
    /// </summary>
    public class AbstractRegion
    {
        //Region=32x32x16 sections
        //Section=16x16x16 blocks, each with a data nibble; null until something is written
        private Section?[,,] _sections = new Section?[32, 32, 16];
        private int _regionX;
        private int _regionZ;

        public AbstractRegion(int regionX, int regionZ)
        {
            _regionX = regionX;
            _regionZ = regionZ;
        }

        /// <summary>
        /// Block id at (x, z, y). Air for y >= 256 and for sections that were never written; never allocates.
        /// </summary>
        public byte getBlock(int x, int z, int y)
        {
            if (y >= 256)
            {
                return 0;
            }

            Section? section = _sections[x / 16, z / 16, y / 16];
            return section == null ? (byte)0 : section.getBlock(x % 16, y % 16, z % 16);
        }

        /// <summary>
        /// Data nibble (0..15) at (x, z, y). 0 for y >= 256 and for sections that were never written; never allocates.
        /// </summary>
        public byte getData(int x, int z, int y)
        {
            if (y >= 256)
            {
                return 0;
            }

            Section? section = _sections[x / 16, z / 16, y / 16];
            return section == null ? (byte)0 : section.getData(x % 16, y % 16, z % 16);
        }

        /// <summary>
        /// Sets the block at (x, z, y) and resets its data nibble to 0. Ignored for y >= 256.
        /// </summary>
        public void putBlock(int x, int z, int y, byte block)
        {
            putBlock(x, z, y, block, 0);
        }

        /// <summary>
        /// Sets block id and data nibble (0..15) at (x, z, y). Ignored for y >= 256.
        /// </summary>
        public void putBlock(int x, int z, int y, byte block, byte data)
        {
            if (y >= 256)
            {
                return;
            }

            Section? section = _sections[x / 16, z / 16, y / 16];

            if (section == null)
            {
                section = new Section((byte)(y / 16));
                _sections[x / 16, z / 16, y / 16] = section;
            }

            section.putBlock(x % 16, y % 16, z % 16, block, data);
        }

        /// <summary>
        /// Replaces the whole 16x16x16 section of chunk (chunkX, chunkZ) at height index sectionY (0..15) with
        /// a uniform fill of <paramref name="block"/> (data 0), without allocating block arrays.
        /// </summary>
        public void fillSection(int chunkX, int chunkZ, int sectionY, byte block)
        {
            _sections[chunkX, chunkZ, sectionY] = new Section((byte)sectionY, block);
        }

        /// <summary>The section of chunk (chunkX, chunkZ) at height index sectionY, or null if never written.</summary>
        internal Section? getSection(int chunkX, int chunkZ, int sectionY)
        {
            return _sections[chunkX, chunkZ, sectionY];
        }

        /// <summary>TEMPORARY, removed in 5b: use <see cref="putBlock(int, int, int, byte, byte)"/>.</summary>
        public void putBlockData(int x, int z, int y, byte[] blockData)
        {
            putBlock(x, z, y, blockData[0], blockData[1]);
        }

        /// <summary>TEMPORARY, removed in 5b: use <see cref="getBlock"/> and <see cref="getData"/>.</summary>
        public byte[] getBlockData(int x, int z, int y)
        {
            return new byte[] { getBlock(x, z, y), getData(x, z, y) };
        }

        public void saveToFolder(String outputFolder)
        {
            saveToFolder(outputFolder, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        public void saveToFolder(String outputFolder, int timestamp)
        {
            String fileName = "r." + _regionX + "." + _regionZ + ".mca";
            List<Chunk> chunks = new List<Chunk>();

            for (byte i = 0; i < 32; i++)
            {
                for (byte j = 0; j < 32; j++)
                {
                    List<Section> chunkSections = new List<Section>();
                    for (byte k = 0; k < 16; k++)
                    {
                        // Every section that was ever written is saved, including one that only received air:
                        // the pre-flat-storage implementation allocated a section on any write and saved all
                        // allocated sections, and the generated worlds must stay byte-identical. Sections that
                        // were never touched are null here and are skipped.
                        Section? section = _sections[i, j, k];
                        if (section != null)
                        {
                            chunkSections.Add(section);
                        }
                    }

                    if (chunkSections.Count != 0)
                    {
                        Chunk tmpChunk = new Chunk(32 * _regionX + i, 32 * _regionZ + j);
                        tmpChunk.setSections(chunkSections.ToArray());
                        chunks.Add(tmpChunk);
                    }
                }
            }

            MCAWriter.writeToFile(chunks, Path.Combine(outputFolder, fileName), timestamp);
        }
    }
}
