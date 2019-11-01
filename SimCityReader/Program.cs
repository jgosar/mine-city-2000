using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.SimCityReader
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1 || !args[0].Equals("thisisatestrun"))
            {
                Console.WriteLine("Please use MineCity2000.exe to run the program");
                Console.ReadLine();
                return;
            }

            CityData city = Reader.readFile(@"..\..\..\input\Spurdo.sc2");

            printBuildingStats(city);
        }

        static void printBuildingStats(CityData city){
            List<Building> buildings = city.getBuildings();

            int[] buildingStats = new int[256];

            buildings.ForEach(building =>
            {
                buildingStats[building.getCode()]++;
            });

            for (int i = 0; i < 256; i++)
            {
                Console.Write(buildingStats[i] + "\t");
                Console.WriteLine(BuildingType.getByCode((byte)i));
            }

            Console.Read();
        }
    }
}
