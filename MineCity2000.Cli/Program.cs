using System;
using System.ComponentModel;
using System.Linq;

namespace com.mc2k.MineCity2000
{
  class Program
  {

    private static string helpText = @"Usage: ./MineCity2000 <SimCity2000 file> [OPTIONS]

-o, --output
            Output directory. Recommended to be something like 'C:\Users\[username]\AppData\Roaming\.minecraft\saves' if you want to open the city directly in Minecraft. If this parameter is not used, the cities are placed into the Minecraft saves folder of the current user if it exists, otherwise into a `generated` folder next to the program. Each city will be saved into a subfolder of the selected path, which will be named the same as the city file.

-f, --fillUnderground
            Fills the underground sections of the city with sandstone, so you can't fall through when digging down. This will increase memory usage and generation time.

-g, --generateTerrain
            Lets Minecraft generate its own terrain around the city instead of creating a wall around the city with nothing outside.

-c, --continue
            Continues console execution after the command is finished, instead of waiting for the user to press a key after reading the output.

--timestamp <unix seconds>, --seed <number>
            Fix the timestamps and random seed written into the world, so that two runs produce identical files (used by the tests).";

    static void Main(string[] args)
    {
      if (args.Length < 1 || !args[0].ToLower().EndsWith(".sc2") || args[0] == "help")
      {
        Console.WriteLine(helpText);

        Console.WriteLine("Press any key to continue.");
        Console.ReadLine();
        return;
      }

      string inputFile, buildingsDir, outputDir;
      bool fillUnderground, generateTerrain, continueCmd;
      long? timestamp, seed;
      parseArgs(args, out inputFile, out buildingsDir, out outputDir, out fillUnderground, out generateTerrain, out continueCmd, out timestamp, out seed);

      Console.WriteLine($"Converting city {inputFile}");
      Console.WriteLine($"Writing output to {outputDir}");
      Console.WriteLine($"Filling underground: {fillUnderground}");
      Console.WriteLine($"Generating terrain around city: {generateTerrain}");

      SCMapper mapper = new SCMapper(buildingsDir, progressCallback);

      MapperOptions options = new MapperOptions(fillUnderground, generateTerrain, timestamp, seed);
      mapper.makeMap(inputFile, outputDir, options);

      Console.WriteLine($"\nDone!");

      if (!continueCmd)
      {
        Console.WriteLine("Press any key to continue.");
        Console.ReadLine();
      }
    }

    private static void parseArgs(string[] args, out string inputFile, out string buildingsDir, out string outputDir, out bool fillUnderground, out bool generateTerrain, out bool continueCmd, out long? timestamp, out long? seed)
    {
      inputFile = args[0];
      String cityName = Path.GetFileNameWithoutExtension(inputFile).ToLower();


      String workingDir = AppContext.BaseDirectory;
      String defaultArgsPath = Path.Combine(workingDir, "default_args.txt");
      String[] defaultArgs = File.Exists(defaultArgsPath) ? DefaultArgs.parse(File.ReadAllText(defaultArgsPath)) : [];

      String[] allArgs = args.Concat(defaultArgs).ToArray();

      String[]? outputArg = parseArg(["-o", "--output"], 1, allArgs);
      String[]? fillUndergroundArg = parseArg(["-f", "--fillUnderground"], 0, allArgs);
      String[]? generateTerrainArg = parseArg(["-g", "--generateTerrain"], 0, allArgs);
      String[]? continueArg = parseArg(["-c", "--continue"], 0, allArgs);
      String[]? timestampArg = parseArg(["--timestamp"], 1, allArgs);
      String[]? seedArg = parseArg(["--seed"], 1, allArgs);

      buildingsDir = Path.Combine(workingDir, "buildings");
      outputDir = DefaultArgs.defaultSavesDir() ?? Path.Combine(workingDir, "generated");
      if (outputArg != null)
      {
        outputDir = outputArg[0];
      }

      outputDir = Path.Combine(outputDir, cityName);

      fillUnderground = false;
      if (fillUndergroundArg != null)
      {
        fillUnderground = true;
      }

      generateTerrain = false;
      if (generateTerrainArg != null)
      {
        generateTerrain = true;
      }

      continueCmd = false;
      if (continueArg != null)
      {
        continueCmd = true;
      }

      timestamp = timestampArg != null ? long.Parse(timestampArg[0]) : null;
      seed = seedArg != null ? long.Parse(seedArg[0]) : null;
    }

    private static string[]? parseArg(string[] aliases, int length, string[] args)
    {
      for (int i = 0; i < args.Length; i++)
      {
        if (aliases.Any(args[i].Equals))
        {
          if (length == 0)
          {
            return [];
          }
          if (args.Length >= i + 1 + length)
          {
            string[] result = new string[length];
            Array.Copy(args, i + 1, result, 0, length);
            return result;
          }
        }
      }
      return null;
    }

    private static void progressCallback(int progressPercentage)
    {
      Console.Write($"\rProgress: {progressPercentage}%");
    }
  }
}
