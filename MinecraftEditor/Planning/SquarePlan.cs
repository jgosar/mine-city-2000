using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.MinecraftEditor.Planning
{
    class SquarePlan
    {
        byte[][] heights;
        bool[][] water;

        public SquarePlan(byte heightNE, byte heightNW, byte heightSE, byte heightSW)
        {
            heights = Util.new2DArray<byte>(2, 2);
            heights[0][0] = heightNE;
            heights[0][1] = heightNW;
            heights[1][0] = heightSE;
            heights[1][1] = heightSW;

            water = Util.new2DArray<bool>(3, 3);
        }

        internal void setWater(int offsetI, int offsetJ, bool isWater)
        {
            water[offsetI + 1][offsetJ + 1] = isWater;
        }

        internal void addBuilding(byte p, byte p_2, byte p_3)
        {
            throw new NotImplementedException();
        }
    }
}
