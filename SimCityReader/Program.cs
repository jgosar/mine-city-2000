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
            if (args.Length != 1 || !args[0].Equals("burek"))
            {
                Console.WriteLine("Please use MineCity2000.exe to run the program");
                Console.ReadLine();
                return;
            }

            CityData city = Reader.readFile(@"C:\Users\jerne\Documents\Visual Studio 2010\Projects\SimCityReader\input\Spurdo.sc2");
            //CityData city = Reader.readFile("C:\\Users\\Jernej\\Documents\\Visual Studio 2010\\Projects\\SimCityReader\\input\\test.sc2");
            //CityData city = Reader.readFile("C:\\Program Files\\Maxis\\SimCity 2000\\Cities\\THEBAHAM.sc2");
            //CityData city = Reader.readFile("C:\\Users\\Jernej\\Documents\\Visual Studio 2010\\Projects\\SimCityReader\\input\\map3.sc2");
            //city = Reader.readFile("C:\\Users\\Jernej\\Documents\\Visual Studio 2010\\Projects\\SimCityReader\\input\\map2.sc2");


            //List<Building> buildings = city.getBuildings();

            int x = 0;

            //int[] buildingStats = new int[256];

            //byte[][] buildingMap = data.getBuildingMap();
            //for (int i = 0; i < buildingMap.Length;i++ )
            //{
            //    for (int j = 0; j < buildingMap[i].Length; j++)
            //    {
            //        buildingStats[buildingMap[i][j]] = buildingStats[buildingMap[i][j]] + 1;
            //    }
            //}

            //for (int i = 0; i < 256; i++)
            //{
            //    Console.Write(buildingStats[i] + "\t");
            //    Console.WriteLine(BuildingType.getByCode((byte)i));
            //}

            Console.Read();
        }
    }
}
