using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace com.mc2k.MinecraftEditor
{
  class AbstractWorld
  {
    AbstractRegion[][] _regions;
    int _startX;
    int _startZ;

    public AbstractWorld(int startX, int startZ, int sizeX, int sizeZ)
    {
      _startX = startX;
      _startZ = startZ;

      _regions = new AbstractRegion[sizeX][];
      for (int i = 0; i < sizeX; i++)
      {
        _regions[i] = new AbstractRegion[sizeZ];
        for (int j = 0; j < sizeZ; j++)
        {
          _regions[i][j] = new AbstractRegion(startX + i, startZ + j);
        }
      }
    }

    public void save(String outputDir)
    {
      DirectoryInfo dir = new DirectoryInfo(outputDir);
      if (dir.Exists)
      {
        foreach (FileInfo file in dir.GetFiles())
        {
          file.Delete();
        }
        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
          subDir.Delete(true);
        }
      }
      else
      {
        dir.Create();
      }
      dir.CreateSubdirectory("region");

      LevelDat ld = new LevelDat("Generated world", new double[] { 0.0, 0.0, 0.0 });
      ld.saveToFile(outputDir + "\\level.dat");

      for (int i = 0; i < _regions.Length; i++)
      {
        for (int j = 0; j < _regions[i].Length; j++)
        {
          _regions[i][j].saveToFolder(outputDir + "\\region");
        }
      }
    }

    public byte getBlock(int x, int z, int y)
    {
      int regionX = Util.positiveMod(x, 512);
      int regionZ = Util.positiveMod(z, 512);
      if (x < 0)
      {
        x -= 512;
      }
      if (z < 0)
      {
        z -= 512;
      }

      return _regions[(x / 512) - _startX][(z / 512) - _startZ].getBlock(regionX, regionZ, y);
    }

    public void putBlock(int x, int z, int y, byte block)
    {
      int regionX = Util.positiveMod(x, 512);
      int regionZ = Util.positiveMod(z, 512);
      if (x < 0)
      {
        x -= 512;
      }
      if (z < 0)
      {
        z -= 512;
      }

      _regions[(x / 512) - _startX][(z / 512) - _startZ].putBlock(regionX, regionZ, y, block);
    }
  }
}
