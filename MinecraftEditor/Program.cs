using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace com.mc2k.MinecraftEditor
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

            //String inputFile = @"C:\Users\jerne\Desktop\Communism.sc2";
            //String inputFile = @"C:\Users\jerne\Documents\Visual Studio 2010\Projects\SimCityReader\input\Spurdo.sc2";
            //String inputFile = @"C:\Games\Simcity 2000\SC2K\CITIES\žabnca.sc2";
            String inputFile = @"F:\Backup\stari disk laptop\Program Files\MAXIS\SimCity 2000\Cities\mesto 1.sc2";
            //String inputFile = @"C:\Games\Simcity 2000\SC2K\CITIES\TORONTO.SC2";
            //String inputFile = @"C:\Users\jerne\Documents\Visual Studio 2010\Projects\SimCityReader\input\test.sc2";
            String outputDir = @"C:\Users\jerne\AppData\Roaming\.minecraft\saves\generated";
            String buildingsDir = @"C:\Users\jerne\Documents\Visual Studio 2010\Projects\MinecraftEditor\buildings";

            //BuildingModel police = new BuildingModel(210, buildingsDir);

            SCMapper mapper = new SCMapper(buildingsDir);
            mapper.makeMap(inputFile, outputDir);

            //List<Chunk> templateChunks = MCAReader.readFile("C:\Users\Jernej\Documents\Visual Studio 2010\Projects\MinecraftEditor\input\r.0.0.mca");
            int x = 1;
        }

        //static void rocksToGold(Chunk chunk)
        //{
        //    Section[] sections = chunk.getSections();
        //    foreach(Section s in sections){
        //        byte[] blocks = s.getBlocks();
        //        for (int i = 0; i < blocks.Length; i++)
        //        {
        //            if (blocks[i] == 1)
        //            {
        //                blocks[i] = 41;
        //            }
        //        }
        //    }
        //}

        //static List<Chunk> imaginaryData()
        //{
        //    List<Chunk> result = new List<Chunk>();
        //    for (int i = 0; i < 32; i++)
        //    {
        //        for (int j = 0; j < 32; j++)
        //        {
        //            Chunk tmp = new Chunk(i, j);
        //            result.Add(tmp);
        //        }
        //    }

        //    return result;
        //}

        static void createMeadow(int height, AbstractRegion targetRegion)
        {
            for (int i = 0; i < 512; i++)
            {
                for (int j = 0; j < 512; j++)
                {
                    targetRegion.putBlock(i, j, height, 2);
                }
            }
        }
    }
}
