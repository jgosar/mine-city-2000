using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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

            // Test loading of cities for all cities in the input folder
            List<CityData> allCities = loadAllCities(@"..\..\..\input\cities");

            // Count the number of buildings by type in all test maps and print them to console (See building_stats.xlsx)
            printBuildingStats(allCities);
        }

        private static List<CityData> loadAllCities(string citiesDir)
        {
            string[] cityFiles = Directory.GetFiles(citiesDir, "*.sc2", SearchOption.TopDirectoryOnly);
            return cityFiles.ToList().Select(cityFile => Reader.readFile(cityFile)).ToList();
        }

        private static void printBuildingStats(List<CityData> cities)
        {
            int[] buildingStats = new int[256];

            cities.ForEach(city =>
            {
                List<Building> buildings = city.getBuildings();

                buildings.ForEach(building =>
                {
                    buildingStats[building.getCode()]++;
                });
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
