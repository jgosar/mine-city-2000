using System;
using System.ComponentModel;
using System.Linq;

namespace com.mc2k.MineCity2000
{
  class Program
  {

    private static string helpText = @"Usage: ./MineCity2000 <SimCity2000 file> [OPTIONS]

-o, --output
            Output directory Recommended to be something like 'C:\Users\[username]\AppData\Roaming\.minecraft\saves' if you want to open the city directly in minecraft). If this parameter is not used, the generated cities will be placed into the `generated` subfolder of the current directory. Each city will be saved into a subfolder of the selected path, which will be named the same as the city file.

-f, --fillUnderground
            Fills the underground sections of the city with sandstone, so you can't fall through when digging down. This will increase memory usage and generation time.

-g, --generateTerrain
            Lets Minecraft generate its own terain around the city instead creating a wall around the city with nothing outside.

-c, --continue
            Continues console execution after the command is finished, instead of waiting for the user to press a key after reading the output.";

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
      parseArgs(args, out inputFile, out buildingsDir, out outputDir, out fillUnderground, out generateTerrain, out continueCmd);

      Console.WriteLine($"Converting city {inputFile}");
      Console.WriteLine($"Writing output to {outputDir}");
      Console.WriteLine($"Filling underground: {fillUnderground}");
      Console.WriteLine($"Generating terrain around city: {generateTerrain}");

      SCMapper mapper = new SCMapper(buildingsDir, progressCallback);

      MapperOptions options = new MapperOptions(fillUnderground, generateTerrain);
      mapper.makeMap(inputFile, outputDir, options);

      Console.WriteLine($"\nDone!");

      if (!continueCmd)
      {
        Console.WriteLine("Press any key to continue.");
        Console.ReadLine();
      }
    }

    private static void parseArgs(string[] args, out string inputFile, out string buildingsDir, out string outputDir, out bool fillUnderground, out bool generateTerrain, out bool continueCmd)
    {
      inputFile = args[0];
      String cityName = inputFile.ToLower().Split('\\').Last().Replace(".sc2", "");


      String workingDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      String defaultArgsText = File.Exists($@"{workingDir}\default_args.txt") ? File.ReadAllText($@"{workingDir}\default_args.txt") : "";
      String[] defaultArgs = defaultArgsText.Split("\r\n").Where(row => !row.StartsWith("#")).SelectMany(row => row.Split(" ")).ToArray();

      String[] allArgs = args.Concat(defaultArgs).ToArray();

      String[]? outputArg = parseArg(["-o", "--output"], 1, allArgs);
      String[]? fillUndergroundArg = parseArg(["-f", "--fillUnderground"], 0, allArgs);
      String[]? generateTerrainArg = parseArg(["-g", "--generateTerrain"], 0, allArgs);
      String[]? continueArg = parseArg(["-c", "--continue"], 0, allArgs);

      buildingsDir = $@"{workingDir}\buildings";
      outputDir = $@"{workingDir}\generated";
      if (outputArg != null)
      {
        outputDir = outputArg[0];
      }

      outputDir = @$"{outputDir}\{cityName}";

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
