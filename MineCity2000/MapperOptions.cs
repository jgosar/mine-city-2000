using System;

namespace com.mc2k.MineCity2000
{
    public class MapperOptions
    {
        public Boolean fillUnderground;
        public Boolean generateTerrain;

        public MapperOptions(Boolean fillUnderground, Boolean generateTerrain) {
            this.fillUnderground = fillUnderground;
            this.generateTerrain = generateTerrain;
        }
    }
}
