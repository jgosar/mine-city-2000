using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.mc2k.AnvilFile.Tags;
using com.mc2k.AnvilFile;

namespace com.mc2k.MinecraftEditor
{
    public class Chunk: NBTConvertible
    {
        public override CompoundNBTTag NBTTag
        {
            get
            {
                List<AnyNBTTag> subTags = new List<AnyNBTTag>();

                if (_heightMap != null) { subTags.Add(new IntArrayNBTTag("HeightMap", _heightMap)); }
                subTags.Add(new ByteNBTTag("TerrainPopulated", (byte)(_terrainPopulated ? 1 : 0)));
                subTags.Add(new IntNBTTag("xPos", _xPos));
                subTags.Add(new IntNBTTag("zPos", _zPos));
                subTags.Add(new LongNBTTag("LastUpdate", _lastUpdate));
                if (_entities != null) { subTags.Add(AnyNBTTag.makeListTag("Entities", _entities)); }
                else { subTags.Add(new ListNBTTag<byte>("Entities", new byte[0])); }
                if (_tileEntities != null) { subTags.Add(AnyNBTTag.makeListTag("TileEntities", _tileEntities)); }
                else { subTags.Add(new ListNBTTag<byte>("TileEntities", new byte[0])); }
                if (_biomes != null) { subTags.Add(new ByteArrayNBTTag("Biomes", _biomes)); }
                if (_sections != null) { subTags.Add(AnyNBTTag.makeListTag("Sections", _sections)); }

                if (_lightPopulated != null) { subTags.Add(new ByteNBTTag("LightPopulated", (byte)(_lightPopulated.GetValueOrDefault() ? 1 : 0))); }
                if (_v != null) { subTags.Add(new ByteNBTTag("V", _v.GetValueOrDefault())); }
                if (_inhabitedTime != null) { subTags.Add(new IntNBTTag("InhabitedTime", _inhabitedTime.Value)); }
                if (_tileTicks != null) { subTags.Add(AnyNBTTag.makeListTag("TileTicks", _tileTicks)); }

                CompoundNBTTag levelTag = new CompoundNBTTag("Level", subTags);

                CompoundNBTTag value = new CompoundNBTTag("", new List<AnyNBTTag>() { levelTag });

                return value;
            }
            set
            {

                Object xPos = value.getObjectOnPath("/Level/xPos");
                Object zPos = value.getObjectOnPath("/Level/zPos");
                Object lastUpdate = value.getObjectOnPath("/Level/LastUpdate");
                Object lightPopulated = value.getObjectOnPath("/Level/LightPopulated");
                Object terrainPopulated = value.getObjectOnPath("/Level/TerrainPopulated");
                Object v = value.getObjectOnPath("/Level/V");
                Object inhabitedTime = value.getObjectOnPath("/Level/InhabitedTime");
                Object biomes = value.getObjectOnPath("/Level/Biomes");
                Object heightMap = value.getObjectOnPath("/Level/HeightMap");
                Object sections = value.getObjectOnPath("/Level/Sections");
                Object entities = value.getObjectOnPath("/Level/Entities");
                Object tileEntities = value.getObjectOnPath("/Level/TileEntities");
                Object tileTicks = value.getObjectOnPath("/Level/TileTicks");

                if (xPos != null) { _xPos = (int)xPos; }
                if (zPos != null) { _zPos = (int)zPos; }
                if (lastUpdate != null) { _lastUpdate = (long)lastUpdate; }
                if (lightPopulated != null)
                {
                    _lightPopulated = ((byte)lightPopulated == 1);
                }
                if (terrainPopulated != null) { _terrainPopulated = ((byte)terrainPopulated == 1); }
                if (v != null)
                {
                    _v = (byte)v;
                }
                if (inhabitedTime != null)
                {
                    _inhabitedTime = (int)inhabitedTime;
                }
                if (biomes != null) { _biomes = (byte[])biomes; }
                if (heightMap != null) { _heightMap = (int[])heightMap; }

                if (sections != null)
                {
                    AnyNBTTag[] sectionTags = (AnyNBTTag[])sections;
                    _sections = new Section[sectionTags.Length];
                    for (int i = 0; i < sectionTags.Length; i++)
                    {
                        _sections[i] = new Section() { NBTTag = (CompoundNBTTag)sectionTags[i] };
                    }
                }

                if (entities != null && entities is CompoundNBTTag[])
                {
                    CompoundNBTTag[] entityTags = (CompoundNBTTag[])entities;
                    _entities = new Entity[entityTags.Length];
                    for (int i = 0; i < entityTags.Length; i++)
                    {
                        _entities[i] = new Entity() { NBTTag = entityTags[i] };
                    }
                }

                if (tileEntities != null && tileEntities is CompoundNBTTag[])
                {
                    CompoundNBTTag[] tileEntityTags = (CompoundNBTTag[])tileEntities;
                    _tileEntities = new TileEntity[tileEntityTags.Length];
                    for (int i = 0; i < tileEntityTags.Length; i++)
                    {
                        _tileEntities[i] = new TileEntity() { NBTTag = tileEntityTags[i] };
                    }
                }

                if (tileTicks != null && tileTicks is CompoundNBTTag[])
                {
                    CompoundNBTTag[] tileTickTags = (CompoundNBTTag[])tileTicks;
                    _tileTicks = new TileTick[tileTickTags.Length];
                    for (int i = 0; i < tileTickTags.Length; i++)
                    {
                        _tileTicks[i] = new TileTick() { NBTTag = tileTickTags[i] };
                    }
                }
            }
        }

        private int _xPos;
        private int _zPos;
        private long _lastUpdate;
        private bool? _lightPopulated = null;
        private bool _terrainPopulated;
        private byte? _v = null;
        private int? _inhabitedTime = null;
        private byte[] _biomes = null;
        private int[] _heightMap = null;
        private Section[] _sections = null;
        private Entity[] _entities = null;
        private TileEntity[] _tileEntities = null;
        private TileTick[] _tileTicks = null;

        public Chunk()
        {
        }

        public Chunk(int xPos, int zPos)
        {
            _xPos = xPos;
            _zPos = zPos;
            _terrainPopulated = true;
            _lastUpdate = 0;
            _heightMap = new int[256];
            for(int i=0;i<_heightMap.Length;i++){
                _heightMap[i] = 2;
            }
            _biomes = new byte[256];
            for (int i = 0; i < _biomes.Length; i++)
            {
                _biomes[i] = 4;
            }

            _sections = new Section[0];
        }

        public Section[] getSections(){
            return _sections;
        }

        public int getXPos()
        {
            return _xPos;
        }

        public int getZPos()
        {
            return _zPos;
        }

        public void putBlock(int x, int y, int z, byte blockCode)
        {
            Section rightSection = null;

            foreach(Section section in _sections)
            {
                if (section.getY() == y / 16)
                {
                    rightSection = section;
                    break;
                }
            }

            if (rightSection == null)
            {
                rightSection = new Section((byte)(y/16));
                List<Section> tmp = _sections.ToList();
                tmp.Add(rightSection);
                _sections = tmp.ToArray();
            }

            byte[] blocks = rightSection.getBlocks();

            int indexY = (y % 16) * 256;
            int indexZ = z * 16;
            int indexX = x;

            blocks[indexX + indexZ + indexY] = blockCode;
        }

        internal void setSections(Section[] sections)
        {
            _sections = sections;
        }
    }
}
