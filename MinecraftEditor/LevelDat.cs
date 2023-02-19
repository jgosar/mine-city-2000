using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using com.mc2k.AnvilFile.Tags;

namespace com.mc2k.MinecraftEditor
{
    public class LevelDat
    {
        private String _levelName;
        private String _generatorName;
        private double[] _playerPos;

        public LevelDat(String levelName, double[] playerPosition, Boolean generateTerrain)
        {
            _levelName = levelName;
            _playerPos = playerPosition;
            _generatorName = generateTerrain ? "default" : "flat";
        }

        public CompoundNBTTag toNBTTag()
        {
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            long timestamp = (long)t.TotalSeconds;
            Random random = new Random();

            List<AnyNBTTag> subTags = new List<AnyNBTTag>();


            subTags.Add(new ByteNBTTag("thundering", (byte)0));
            subTags.Add(new LongNBTTag("LastPlayed", timestamp * 1000));
            subTags.Add(new PlayerData(_playerPos).toNBTTag());
            subTags.Add(new ByteNBTTag("initialized", (byte)1));
            subTags.Add(new LongNBTTag("RandomSeed", (long)random.Next()));
            subTags.Add(new IntNBTTag("GameType", 1));
            subTags.Add(new ByteNBTTag("MapFeatures", (byte)0));
            subTags.Add(new IntNBTTag("version", 19133));
            subTags.Add(new ByteNBTTag("allowCommands", (byte)1));
            subTags.Add(new LongNBTTag("Time", 1234L));
            subTags.Add(new ByteNBTTag("raining", (byte)0));
            subTags.Add(new IntNBTTag("thunderTime", int.MaxValue));
            subTags.Add(new IntNBTTag("SpawnX", (int)_playerPos[0]));
            subTags.Add(new ByteNBTTag("hardcore", (byte)0));
            subTags.Add(new IntNBTTag("SpawnY", (int)_playerPos[1]));
            subTags.Add(new IntNBTTag("SpawnZ", (int)_playerPos[2]));
            subTags.Add(new StringNBTTag("LevelName",_levelName));
            subTags.Add(new StringNBTTag("generatorName",_generatorName));
            subTags.Add(new LongNBTTag("SizeOnDisk", 0L));
            subTags.Add(new IntNBTTag("rainTime", int.MaxValue));
            subTags.Add(new IntNBTTag("generatorVersion", 0));

            CompoundNBTTag dataTag = new CompoundNBTTag("Data", subTags);
            List<AnyNBTTag> subTags2 = new List<AnyNBTTag>();
            subTags2.Add(dataTag);

            return new CompoundNBTTag("", subTags2);
        }

        public void saveToFile(String fileName)
        {
            CompoundNBTTag tag = toNBTTag();

            byte[] output = tag.toBytesWithHeader();

            Stream outFile = new FileStream(fileName, FileMode.Create);

            using (Stream outGzipStream = new GZipStream(outFile, CompressionMode.Compress))
            {
                outGzipStream.Write(output, 0, output.Length);
            }
        }
    }

    class PlayerData
    {
        double[] _motion = new double[3];
        double[] _pos = new double[3];
        float[] _rotation = new float[2];

        public PlayerData(double[] pos)
        {
            _motion = new double[] { 0.0, 0.0, 0.0 };
            if (pos == null)
            {
                _pos = new double[] { 128.0, 128.0, 128.0 };
            }
            else
            {
                _pos = pos;
            }
            _rotation = new float[] { 135F, 60F };
        }

        public CompoundNBTTag toNBTTag()
        {
            List<AnyNBTTag> abilityTags = new List<AnyNBTTag>();
            abilityTags.Add(new ByteNBTTag("flying", (byte)1));
            abilityTags.Add(new ByteNBTTag("instabuild", (byte)1));
            abilityTags.Add(new ByteNBTTag("mayfly", (byte)1));
            abilityTags.Add(new ByteNBTTag("invulnreable", (byte)1));
            abilityTags.Add(new ByteNBTTag("mayBuild", (byte)1));
            abilityTags.Add(new FloatNBTTag("flySpeed", 0.2F));
            abilityTags.Add(new FloatNBTTag("walkSpeed", 0.1F));

            List<AnyNBTTag> subTags = new List<AnyNBTTag>();

            subTags.Add(new ListNBTTag<double>("Motion", _motion));

            subTags.Add(new FloatNBTTag("foodExhaustionLevel", 0.0F));
            subTags.Add(new IntNBTTag("foodTickTimer", 0));
            subTags.Add(new IntNBTTag("XpLevel", 0));
            subTags.Add(new ShortNBTTag("Health", (short)20));
            subTags.Add(new ListNBTTag<byte>("Inventory", new byte[0]));
            subTags.Add(new ShortNBTTag("AttackTime", (short)0));
            subTags.Add(new ByteNBTTag("Sleeping", (byte)0));
            subTags.Add(new ShortNBTTag("Fire", (short)-20));
            subTags.Add(new IntNBTTag("playerGameType", 1));
            subTags.Add(new IntNBTTag("foodLevel", 20));
            subTags.Add(new ShortNBTTag("DeathTime", (short)0));
            subTags.Add(new FloatNBTTag("XpP", 0.0F));
            subTags.Add(new ListNBTTag<byte>("EnderItems", new byte[0]));
            subTags.Add(new ShortNBTTag("SleepTimer", (short)0));
            subTags.Add(new ShortNBTTag("HurtTime", (short)0));
            subTags.Add(new ByteNBTTag("OnGround", (byte)0));
            subTags.Add(new IntNBTTag("Dimension", 0));
            subTags.Add(new ShortNBTTag("Air", (short)300));
            subTags.Add(new ListNBTTag<double>("Pos", _pos));
            subTags.Add(new FloatNBTTag("foodSaturationLevel", 5.0F));
            subTags.Add(new CompoundNBTTag("abilities", abilityTags));
            subTags.Add(new FloatNBTTag("FallDistance", 0.0F));
            subTags.Add(new IntNBTTag("XpTotal", 0));

            subTags.Add(new ListNBTTag<float>("Rotation", _rotation));

            CompoundNBTTag result = new CompoundNBTTag("Player", subTags);

            return result;
        }
    }
}
