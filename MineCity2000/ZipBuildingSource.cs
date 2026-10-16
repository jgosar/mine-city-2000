using System.IO.Compression;

namespace com.mc2k.MineCity2000
{
  /// <summary>
  /// A <see cref="BuildingSource"/> backed by a zip archive held in memory. Entries may be stored at the root
  /// ("123.mca") or under a single folder ("buildings/123.mca"). The archive stays open for repeated reads.
  /// </summary>
  public sealed class ZipBuildingSource : BuildingSource
  {
    private readonly ZipArchive _zip;
    private readonly Dictionary<string, ZipArchiveEntry> _entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);

    /// <param name="zipBytes">The contents of the zip file.</param>
    /// <exception cref="InvalidDataException">The bytes are not a valid zip archive.</exception>
    public ZipBuildingSource(byte[] zipBytes)
    {
      _zip = new ZipArchive(new MemoryStream(zipBytes, writable: false), ZipArchiveMode.Read);
      foreach (ZipArchiveEntry entry in _zip.Entries)
      {
        string name = entry.FullName.Replace('\\', '/');
        name = name.Substring(name.LastIndexOf('/') + 1);
        if (name.Length > 0)
        {
          _entries.TryAdd(name, entry);
        }
      }
    }

    /// <inheritdoc/>
    public byte[]? read(string fileName)
    {
      if (!_entries.TryGetValue(fileName, out ZipArchiveEntry? entry))
      {
        return null;
      }

      // ZipArchive is not safe for concurrent use; copying the bytes out keeps every read independent.
      lock (_zip)
      {
        using Stream stream = entry.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
      }
    }
  }
}
