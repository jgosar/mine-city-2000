using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.MinecraftEditor
{
    public class Util
    {
        public static int positiveMod(int arg1, int arg2)
        {
            if (arg1 % arg2 >= 0)
            {
                return arg1 % arg2;
            }
            else
            {
                return arg1 % arg2 + arg2;
            }
        }

        public static T[][] new2DArray<T>(int width, int height)
        {
            T[][] interpolatedPoints = new T[height][];
            for (int i = 0; i < height; i++)
            {
                interpolatedPoints[i] = new T[width];
            }
            return interpolatedPoints;
        }

        public static void insertArray(int[][] subArray, int[][] mainArray, int offsetX, int offsetZ)
        {
            for (int i = 0; i < subArray.Length; i++)
            {
                for (int j = 0; j < subArray[i].Length; j++)
                {
                    mainArray[offsetX + i][offsetZ + j] = subArray[i][j];
                }
            }
        }

        public static bool isBetween(int number, int min, int max)
        {
            return number >= min && number <= max;
        }
    }
}
