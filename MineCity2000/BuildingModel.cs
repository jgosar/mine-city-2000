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

    /// <summary>
    /// The name of the model file for a building type and variant, e.g. "198a.mca".
    /// </summary>
    public static String modelFileName(byte typeCode, String modelVariant)
    {
      return typeCode + modelVariant + ".mca";
    }

    public static bool modelFileExists(String folder, byte typeCode, String modelVariant)
    {
      return modelFileExists(new DirectoryBuildingSource(folder), typeCode, modelVariant);
    }

    /// <summary>
    /// Whether the source holds a model file for the given building type and variant.
    /// </summary>
    public static bool modelFileExists(BuildingSource src, byte typeCode, String modelVariant)
    {
      return src.read(modelFileName(typeCode, modelVariant)) != null;
    }

    public BuildingModel(String folder, byte typeCode, String modelVariant)
      : this(new DirectoryBuildingSource(folder), typeCode, modelVariant)
    {
    }

    /// <summary>
    /// Loads the model file for the given building type and variant from the source.
    /// </summary>
    /// <exception cref="FileNotFoundException">The source has no such model file.</exception>
    public BuildingModel(BuildingSource src, byte typeCode, String modelVariant)
      : this(src.read(modelFileName(typeCode, modelVariant))
          ?? throw new FileNotFoundException("Building model not found: " + modelFileName(typeCode, modelVariant)), typeCode)
    {
    }

    /// <summary>
    /// Builds the model from the bytes of its .mca file.
    /// </summary>
    public BuildingModel(byte[] mcaBytes, byte typeCode)
    {
      _type = BuildingType.getByCode(typeCode);

      _sections = new Section?[_type.getSize(), _type.getSize(), 16];

      List<Chunk> chunks = MCAReader.read(mcaBytes);

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
                  target.putBlock(x: x, y: y, z: z, sourceBlocks[bl], tmpData);
                }
                else if (section.getY() == 0 && sourceBlocks[bl] == WOOL_BLOCK && tmpData != 0 && tmpData != 15)//except colored wool
                {
                  target.putBlock(x: x, y: y, z: z, sourceBlocks[bl], tmpData);
                }
                else if (BuildingType.isSloped(typeCode) && sourceBlocks[bl] == WOOL_BLOCK)//for sloped blocks, turn wool to sandstone
                {
                  target.putBlock(x: x, y: y, z: z, SANDSTONE_BLOCK, 0);
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
