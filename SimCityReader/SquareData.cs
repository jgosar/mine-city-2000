using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.SimCityReader
{
    /// <summary>
    /// Which way a directional building (bridge, runway, pier) runs across the map.
    /// AlongX means the strip's tiles vary in x, AlongZ that they vary in z. None for buildings without a direction.
    /// </summary>
    public enum Orientation
    {
        None,
        AlongX,
        AlongZ
    }

    public class SquareData
    {
        public byte buildingType;
        public byte altitude;
        public byte buildingOffsetX;
        public byte buildingOffsetZ;
        public Boolean hasWater;
        /// <summary>The raw "rotate the tile by 90 degrees" flag from the XBIT segment (mask 0x02).</summary>
        public bool rotateFlag;
        /// <summary>The direction of the building on this square, derived from rotateFlag, the compass and the neighbours; see CityData.computeOrientations.</summary>
        public Orientation orientation = Orientation.None;

        public SquareData()
        {
        }
    }
}
