using System.Text;

namespace MineCity2000.Tests;

// Builds a minimal in-memory SimCity 2000 file: "FORM" + length + "SCDH" + the segments the reader needs.
// ALTM and CNAM are stored raw, XBLD/XTER/MISC/XBIT are run-length encoded like the real format.
// XBIT is only written once xbit() has been called, so by default the city has no XBIT segment, like the real optional case.
public class IffBuilder
{
  public const int MISC_WATER_LEVEL_OFFSET = 3651; // CityData.interpretMISC reads the water level from here
  public const int MISC_COMPASS_OFFSET = 8;        // ... and the compass (4-byte big-endian) from here
  public const byte XBIT_ROTATE = 0x02;

  private readonly byte[] _altm = new byte[32768];  // 128 * 128 squares, 2 bytes each, altitude in the second byte
  private readonly byte[] _xbld = new byte[16384];  // 128 * 128 building codes
  private readonly byte[] _xter = new byte[16384];  // 128 * 128 terrain codes
  private readonly byte[] _misc = new byte[4096];
  private byte[]? _xbit;                            // 128 * 128 flag bytes, null until xbit() is called
  private byte[]? _cnam;
  private int? _altmLength;
  private readonly List<(string name, byte[] content)> _extra = new();

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

  // The compass is the number of 90 degree turns of the view when the city was saved (0..3).
  public IffBuilder compass(int compass)
  {
    byte[] bytes = bigEndianInt(compass);
    Array.Copy(bytes, 0, _misc, MISC_COMPASS_OFFSET, bytes.Length);
    return this;
  }

  // Sets the XBIT flag byte of a square; this also makes build() emit an XBIT segment.
  public IffBuilder xbit(int x, int z, byte flags)
  {
    _xbit ??= new byte[16384];
    _xbit[x * 128 + z] = flags;
    return this;
  }

  // CNAM layout: a length byte followed by the name; the reader stops at the first zero byte.
  public IffBuilder cityName(string name)
  {
    byte[] nameBytes = Encoding.UTF8.GetBytes(name);
    _cnam = new byte[] { (byte)nameBytes.Length }.Concat(nameBytes).ToArray();
    return this;
  }

  // Stores only the first <length> bytes of ALTM, producing a well-formed segment whose body is too short.
  public IffBuilder altmLength(int length)
  {
    _altmLength = length;
    return this;
  }

  public IffBuilder rawCnam(byte[] cnam)
  {
    _cnam = cnam;
    return this;
  }

  // Appends a segment stored exactly as given (no RLE) after all the others, like the TEXT/SCEN/PICT segments of a scenario.
  public IffBuilder extraSegment(string name, byte[] content)
  {
    _extra.Add((name, content));
    return this;
  }

  public byte[] build()
  {
    var body = new List<byte>();
    body.AddRange(Encoding.ASCII.GetBytes("SCDH"));
    if (_cnam != null) body.AddRange(segment("CNAM", _cnam));
    body.AddRange(segment("ALTM", _altmLength is int n ? _altm.Take(n).ToArray() : _altm));
    body.AddRange(segment("XBLD", rle(_xbld)));
    body.AddRange(segment("XTER", rle(_xter)));
    body.AddRange(segment("MISC", rle(_misc)));
    if (_xbit != null) body.AddRange(segment("XBIT", rle(_xbit)));
    foreach (var (name, content) in _extra) body.AddRange(segment(name, content));

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

  // Encodes like the game's own saves: a command byte 1..127 copies that many literal bytes, a command byte c > 128
  // repeats the following byte c - 127 times (so runs are 2..128 long). The game also decodes 0 (copy nothing)
  // and 128 (repeat once) but never writes them; see docs/sc2-format/compression.md.
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
