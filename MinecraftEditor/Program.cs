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
            if (args.Length != 1 || !args[0].Equals("thisisatestrun"))
            {
                Console.WriteLine("Please use MineCity2000.exe to run the program");
                Console.ReadLine();
                return;
            }

            String inputFile = @"F:\Backup\stari disk laptop\Program Files\MAXIS\SimCity 2000\Cities\mesto 1.sc2";
            String outputDir = @"C:\Users\jerne\AppData\Roaming\.minecraft\saves\generated";
            String buildingsDir = @"C:\Users\jerne\Documents\Visual Studio 2010\Projects\MinecraftEditor\buildings";

            SCMapper mapper = new SCMapper(buildingsDir);
            mapper.makeMap(inputFile, outputDir);
        }

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
