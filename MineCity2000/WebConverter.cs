using System.IO.Compression;

namespace com.mc2k.MineCity2000
{
  /// <summary>
  /// Converts a SimCity 2000 city held in memory into a zipped Minecraft world, for hosts (such as the browser)
  /// that have no direct access to the file system layout the command line tool uses.
  /// </summary>
  public static class WebConverter
  {
    /// <summary>
    /// Converts the city and returns the world as a zip with the world folder as its top-level entry
    /// (entries "&lt;city&gt;/level.dat" and "&lt;city&gt;/region/r.X.Z.mca", where &lt;city&gt; is the sanitised city name).
    /// </summary>
    /// <param name="sc2">The contents of the .sc2 file.</param>
    /// <param name="cityName">Used as the file name of the city and the folder name of the world inside <paramref name="workDir"/>.
    /// It is sanitised first: any directory part is dropped, invalid file name characters become '_', and an empty, "." or ".." result becomes "city".</param>
    /// <param name="buildings">Where the building models come from.</param>
    /// <param name="options">Conversion options.</param>
    /// <param name="progress">Receives progress in percent, or null.</param>
    /// <param name="workDir">A scratch directory for intermediate files; it is deleted (best effort) before returning.</param>
    /// <exception cref="InvalidDataException">The bytes are not a SimCity 2000 city, or the file is truncated or corrupt.</exception>
    public static byte[] convertToZip(byte[] sc2, string cityName, BuildingSource buildings, MapperOptions options, SCMapper.ProgressCallback? progress, string workDir)
    {
      if (!hasSimCityHeader(sc2))
      {
        throw new InvalidDataException("Not a SimCity 2000 city file");
      }

      cityName = sanitiseCityName(cityName);

      try
      {
        Directory.CreateDirectory(workDir);
        string cityPath = Path.Combine(workDir, cityName + ".sc2");
        string worldDir = Path.Combine(workDir, cityName);
        File.WriteAllBytes(cityPath, sc2);

        new SCMapper(buildings, progress).makeMap(cityPath, worldDir, options);

        using var buffer = new MemoryStream();
        ZipFile.CreateFromDirectory(worldDir, buffer, CompressionLevel.Fastest, includeBaseDirectory: true);
        return buffer.ToArray();
      }
      finally
      {
        try
        {
          Directory.Delete(workDir, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
      }
    }

    private static string sanitiseCityName(string cityName)
    {
      string name = Path.GetFileName(cityName);
      // On Linux (and so in wasm) only '/' is in GetInvalidFileNameChars, but the zip is unpacked on Windows too.
      foreach (char invalid in Path.GetInvalidFileNameChars())
      {
        name = name.Replace(invalid, '_');
      }
      name = name.Replace('\\', '_');
      name = name.Trim();
      return name.Length == 0 || name == "." || name == ".." ? "city" : name;
    }

    // An IFF file: "FORM", a 4 byte length, then the form type "SCDH".
    private static bool hasSimCityHeader(byte[] bytes)
    {
      return bytes.Length >= 12
        && bytes[0] == 'F' && bytes[1] == 'O' && bytes[2] == 'R' && bytes[3] == 'M'
        && bytes[8] == 'S' && bytes[9] == 'C' && bytes[10] == 'D' && bytes[11] == 'H';
    }
  }
}
