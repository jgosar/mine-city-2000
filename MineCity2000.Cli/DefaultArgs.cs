using System.Runtime.InteropServices;

namespace com.mc2k.MineCity2000
{
  public static class DefaultArgs
  {
    // One option per line: "-o /path with spaces" -> ["-o", "/path with spaces"]
    public static string[] parse(string text)
    {
      List<string> result = new List<string>();
      foreach (string rawLine in text.Split('\n'))
      {
        string line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith("#")) continue;
        int space = line.IndexOf(' ');
        if (space < 0) { result.Add(line); continue; }
        result.Add(line.Substring(0, space));
        result.Add(line.Substring(space + 1).Trim());
      }
      return result.ToArray();
    }

    public static string? defaultSavesDir()
    {
      string candidate;
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "saves");
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "minecraft", "saves");
      else
        candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".minecraft", "saves");
      return Directory.Exists(candidate) ? candidate : null;
    }
  }
}
