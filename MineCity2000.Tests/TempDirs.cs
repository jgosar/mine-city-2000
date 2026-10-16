namespace MineCity2000.Tests;

internal static class TempDirs
{
  // Cleanup must never fail a passing test: a transient file lock (e.g. antivirus on a Windows runner) is ignored.
  public static void deleteBestEffort(string dir)
  {
    try
    {
      Directory.Delete(dir, true);
    }
    catch (IOException)
    {
    }
    catch (UnauthorizedAccessException)
    {
    }
  }
}
