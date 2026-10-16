using System;

namespace com.mc2k.MineCity2000
{
    public class MapperOptions
    {
        public Boolean fillUnderground;
        public Boolean generateTerrain;

        // Unix seconds written into the region file chunk timestamps and level.dat. Resolved once at construction.
        public long timestamp { get; }
        // Seed written into level.dat. Resolved once at construction.
        public long randomSeed { get; }

        public MapperOptions(Boolean fillUnderground, Boolean generateTerrain, long? timestamp = null, long? randomSeed = null) {
            this.fillUnderground = fillUnderground;
            this.generateTerrain = generateTerrain;
            this.timestamp = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.randomSeed = randomSeed ?? new Random().Next();
        }
    }
}
