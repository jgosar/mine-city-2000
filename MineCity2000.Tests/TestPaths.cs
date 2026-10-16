namespace MineCity2000.Tests;

public static class TestPaths
{
  // Walks up from the test binary directory to the directory that holds mine-city-2000.sln.
  public static string repoRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "mine-city-2000.sln")))
      {
        return dir.FullName;
      }
      dir = dir.Parent;
    }
    throw new DirectoryNotFoundException("Could not find mine-city-2000.sln above " + AppContext.BaseDirectory);
  }

  public static string buildingsDir()
  {
    return Path.Combine(repoRoot(), "buildings");
  }
}
