using System.IO.Compression;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using com.mc2k.MineCity2000;

/// <summary>
/// The C# entry points called from <c>wwwroot/worker.js</c> inside the browser Web Worker.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class ConverterExports
{
    private const string WorkDir = "/work";

    private static BuildingSource? buildings;

    /// <summary>Called by C# to report progress to JavaScript (worker.js posts it to the page).</summary>
    [JSImport("progress", "worker.js")]
    internal static partial void ReportProgress(int percent);

    /// <summary>Hands the contents of buildings.zip to the converter. Call once per worker before <see cref="Convert"/>.</summary>
    /// <param name="buildingsZip">The bytes of buildings.zip (entries such as "198a.mca").</param>
    [JSExport]
    public static void LoadBuildings(byte[] buildingsZip)
    {
        buildings = new ZipBuildingSource(buildingsZip);
    }

    /// <summary>Converts a SimCity 2000 city to a zipped Minecraft world, using the current time as the world timestamp and a random seed.</summary>
    /// <param name="sc2">The contents of the .sc2 file.</param>
    /// <param name="cityName">Name of the world; sanitised before use as a file name.</param>
    /// <param name="fillUnderground">Fill the underground below the city.</param>
    /// <param name="generateTerrain">Generate terrain around the city.</param>
    /// <returns>The zipped world.</returns>
    /// <exception cref="InvalidOperationException"><see cref="LoadBuildings"/> has not been called.</exception>
    /// <exception cref="InvalidDataException">The bytes are not a valid SimCity 2000 city.</exception>
    [JSExport]
    public static byte[] Convert(byte[] sc2, string cityName, bool fillUnderground, bool generateTerrain)
    {
        return convert(sc2, cityName, new MapperOptions(fillUnderground, generateTerrain));
    }

    /// <summary>Like <see cref="Convert"/> but with a fixed timestamp (Unix seconds) and random seed, so the output is reproducible. Used for verification.</summary>
    [JSExport]
    public static byte[] ConvertDeterministic(byte[] sc2, string cityName, bool fillUnderground, bool generateTerrain, int timestamp, int seed)
    {
        return convert(sc2, cityName, new MapperOptions(fillUnderground, generateTerrain, timestamp, seed));
    }

    private static byte[] convert(byte[] sc2, string cityName, MapperOptions options)
    {
        if (buildings == null)
        {
            throw new InvalidOperationException("Call LoadBuildings first");
        }

        int last = -1;
        return WebConverter.convertToZip(sc2, cityName, buildings, options, percent =>
        {
            if (percent != last)
            {
                last = percent;
                ReportProgress(percent);
            }
        }, WorkDir);
    }
}

internal static class Program
{
    // The runtime is driven from worker.js; nothing to do at startup.
    private static void Main()
    {
    }
}
