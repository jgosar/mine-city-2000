using System.Text;

namespace MineCity2000.Tests;

// Builds a minimal in-memory SimCity 2000 file: "FORM" + length + "SCDH" + the segments the reader needs.
// ALTM and CNAM are stored raw, XBLD/XTER/MISC are run-length encoded like the real format.
public class IffBuilder
{
  public const int MISC_WATER_LEVEL_OFFSET = 3651; // CityData.interpretMISC reads the water level from here

  private readonly byte[] _altm = new byte[32768];  // 128 * 128 squares, 2 bytes each, altitude in the second byte
  private readonly byte[] _xbld = new byte[16384];  // 128 * 128 building codes
  private readonly byte[] _xter = new byte[16384];  // 128 * 128 terrain codes
  private readonly byte[] _misc = new byte[4096];
  private byte[]? _cnam;

  public IffBuilder altitude(int x, int z, byte altitude)
  {
    _altm[(x * 128 + z) * 2 + 1] = altitude;
    return this;
  }

  public IffBuilder building(int x, int z, byte code)
  {
    _xbld[x * 128 + z] = code;
    return this;
  }

  public IffBuilder terrain(int x, int z, byte code)
  {
    _xter[x * 128 + z] = code;
    return this;
  }

  public IffBuilder waterLevel(byte level)
  {
    _misc[MISC_WATER_LEVEL_OFFSET] = level;
    return this;
  }

  // CNAM layout: a length byte followed by the name; the reader stops at the first zero byte.
  public IffBuilder cityName(string name)
  {
    byte[] nameBytes = Encoding.UTF8.GetBytes(name);
    _cnam = new byte[] { (byte)nameBytes.Length }.Concat(nameBytes).ToArray();
    return this;
  }

  public IffBuilder rawCnam(byte[] cnam)
  {
    _cnam = cnam;
    return this;
  }

  public byte[] build()
  {
    var body = new List<byte>();
    body.AddRange(Encoding.ASCII.GetBytes("SCDH"));
    if (_cnam != null) body.AddRange(segment("CNAM", _cnam));
    body.AddRange(segment("ALTM", _altm));
    body.AddRange(segment("XBLD", rle(_xbld)));
    body.AddRange(segment("XTER", rle(_xter)));
    body.AddRange(segment("MISC", rle(_misc)));

    var file = new List<byte>();
    file.AddRange(Encoding.ASCII.GetBytes("FORM"));
    file.AddRange(bigEndianInt(body.Count));
    file.AddRange(body);
    return file.ToArray();
  }

  private static byte[] segment(string name, byte[] content)
  {
    var result = new List<byte>();
    result.AddRange(Encoding.ASCII.GetBytes(name));
    result.AddRange(bigEndianInt(content.Length));
    result.AddRange(content);
    return result.ToArray();
  }

  // Mirrors Segment.decompress: a command byte 1..127 copies that many literal bytes, a command byte c > 128
  // repeats the following byte c - 127 times (so runs are 2..128 long); 128 itself is invalid.
  public static byte[] rle(byte[] data)
  {
    var result = new List<byte>();
    int i = 0;
    while (i < data.Length)
    {
      int run = 1;
      while (i + run < data.Length && data[i + run] == data[i] && run < 128) run++;
      if (run == 1)
      {
        result.Add(1);
        result.Add(data[i]);
      }
      else
      {
        result.Add((byte)(127 + run));
        result.Add(data[i]);
      }
      i += run;
    }
    return result.ToArray();
  }

  private static byte[] bigEndianInt(int value)
  {
    return new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
  }
}
