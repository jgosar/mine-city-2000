using System;

namespace com.mc2k.MineCity2000
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

            String inputFile = @"..\..\..\input\cities\CENTERVL.SC2";
            String outputDir = @"C:\Users\jerne\AppData\Roaming\.minecraft\saves\generated"; // Change this to your Minecraft worlds directory
            String buildingsDir = @"..\..\..\buildings";

            SCMapper mapper = new SCMapper(buildingsDir);
            mapper.makeMap(inputFile, outputDir);
        }
    }
}
