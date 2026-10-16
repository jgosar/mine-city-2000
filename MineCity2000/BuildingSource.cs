namespace com.mc2k.MineCity2000
{
  /// <summary>
  /// Supplies the building model files (the .mca files from the "buildings" folder) to the converter,
  /// wherever they are stored: a directory, a zip held in memory, and so on.
  /// </summary>
  public interface BuildingSource
  {
    /// <summary>
    /// The bytes of the model file with the given name (e.g. "198a.mca"), or null if there is no such file.
    /// Every call returns a fresh array that the caller may keep.
    /// </summary>
    byte[]? read(string fileName);
  }

  /// <summary>
  /// A <see cref="BuildingSource"/> backed by a directory on disk.
  /// </summary>
  public sealed class DirectoryBuildingSource : BuildingSource
  {
    private readonly string _dir;

    /// <param name="dir">The directory that holds the model files.</param>
    public DirectoryBuildingSource(string dir)
    {
      _dir = dir;
    }

    /// <inheritdoc/>
    public byte[]? read(string fileName)
    {
      string path = Path.Combine(_dir, fileName);
      if (!File.Exists(path))
      {
        return null;
      }

      try
      {
        return File.ReadAllBytes(path);
      }
      catch (FileNotFoundException)
      {
        return null;
      }
      catch (DirectoryNotFoundException)
      {
        return null;
      }
    }
  }
}
