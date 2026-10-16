using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using com.mc2k.SimCityReader;
using com.mc2k.MinecraftEditor;

namespace com.mc2k.MineCity2000
{
  class BuildingModel
  {
    private const int BEDROCK_BLOCK = 7;
    private const int WOOL_BLOCK = 35;
    private const int SANDSTONE_BLOCK = 24;
    //_sections=sizexsizex16 sections, null where the model has nothing
    //Section=16x16x16 blocks, each with a data nibble
    private Section?[,,] _sections;
    BuildingType _type;

    public static bool modelFileExists(String folder, byte typeCode, String modelVariant)
    {
      return File.Exists(Path.Combine(folder, typeCode + modelVariant + ".mca"));
    }

    public BuildingModel(String folder, byte typeCode, String modelVariant)
    {
      _type = BuildingType.getByCode(typeCode);

      _sections = new Section?[_type.getSize(), _type.getSize(), 16];

      List<Chunk> chunks = MCAReader.readFile(Path.Combine(folder, typeCode + modelVariant + ".mca"));

      foreach (Chunk chunk in chunks)
      {
        if (chunk.getXPos() < _type.getSize() && chunk.getZPos() < _type.getSize())
        {
          foreach (Section section in chunk.getSections())
          {
            byte[] sourceBlocks = section.getBlocks();
            byte[] sourceData = section.getData();
            Section? target = _sections[chunk.getXPos(), chunk.getZPos(), section.getY()];

            for (int bl = 0; bl < sourceBlocks.Length; bl++)
            {
              if (sourceBlocks[bl] != 0 || sourceData[bl / 2] != 0)
              {
                // a section exists as soon as the source section holds anything non-zero, even if every block of it is filtered out below
                if (target == null)
                {
                  target = new Section(section.getY());
                  _sections[chunk.getXPos(), chunk.getZPos(), section.getY()] = target;
                }

                int x = bl % 16;
                int y = bl / 256;
                int z = (bl - (256 * y)) / 16;

                byte tmpData = sourceData[bl / 2];

                if (bl % 2 == 1)
                {
                  tmpData >>= 4;
                }
                else
                {
                  tmpData <<= 4;
                  tmpData >>= 4;
                }

                //ignore wool and bedrock below the top layer of the bottom section
                if (section.getY() > 0 || y == 15 || (sourceBlocks[bl] != WOOL_BLOCK && sourceBlocks[bl] != BEDROCK_BLOCK))
                {
                  target.putBlock(x, y, z, sourceBlocks[bl], tmpData);
                }
                else if (section.getY() == 0 && sourceBlocks[bl] == WOOL_BLOCK && tmpData != 0 && tmpData != 15)//except colored wool
                {
                  target.putBlock(x, y, z, sourceBlocks[bl], tmpData);
                }
                else if (BuildingType.isSloped(typeCode) && sourceBlocks[bl] == WOOL_BLOCK)//for sloped blocks, turn wool to sandstone
                {
                  target.putBlock(x, y, z, SANDSTONE_BLOCK, 0);
                }
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// The section of chunk (chunkX, chunkZ) at height index sectionY (0..15), or null if the model has nothing there.
    /// </summary>
    public Section? getSection(int chunkX, int chunkZ, int sectionY)
    {
      return _sections[chunkX, chunkZ, sectionY];
    }
  }
}
