using System.Text;
using com.mc2k.SimCityReader;

namespace MineCity2000.Tests;

// Builds a minimal in-memory SimCity 2000 file: "FORM" + length + "SCDH" + the segments the reader needs.
// ALTM and CNAM are stored raw, XBLD/XTER/XZON/MISC/XBIT are run-length encoded like the real format.
// XBIT and XZON are only written once xbit() / withXzon() have been called, so by default the city has neither, like a hand-built city.
public class IffBuilder
{
  public const int MISC_WATER_LEVEL_OFFSET = 3651; // CityData.interpretMISC reads the water level from here
  public const int MISC_COMPASS_OFFSET = 8;        // ... and the compass (4-byte big-endian) from here
  public const byte XBIT_ROTATE = 0x02;

  private readonly byte[] _altm = new byte[32768];  // 128 * 128 squares, 2 bytes each, altitude in the second byte
  private readonly byte[] _xbld = new byte[16384];  // 128 * 128 building codes
  private readonly byte[] _xter = new byte[16384];  // 128 * 128 terrain codes
  private readonly byte[] _xzon = new byte[16384];  // 128 * 128 zone and corner bits, only written after withXzon()
  private bool _writeXzon;
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

  // Places a building like the game does: its code on every square of the footprint, with (x, z) the corner with the lowest x
  // and z, and the footprint's corner bits in XZON for the current compass (so call compass() first). Buildings below 0x70
  // (roads, rails, highways, ...) carry no corner bits.
  public IffBuilder building(int x, int z, byte code)
  {
    int size = BuildingType.getByCode(code).getSize();
    for (int i = 0; i < size; i++)
    {
      for (int j = 0; j < size; j++)
      {
        _xbld[(x + i) * 128 + z + j] = code;
      }
    }
    if (code >= 0x70)
    {
      int compass = _misc[MISC_COMPASS_OFFSET + 3];
      // corners with the lowest x/lowest z, highest x/lowest z, highest x/highest z, lowest x/highest z
      (int, int)[] corners = { (x, z), (x + size - 1, z), (x + size - 1, z + size - 1), (x, z + size - 1) };
      for (int k = 0; k < 4; k++)
      {
        var (cx, cz) = corners[k];
        _xzon[cx * 128 + cz] |= (byte)(0x10 << ((k + compass) & 3));
      }
    }
    return this;
  }

  // Writes a single XBLD square, e.g. a leftover piece of a demolished building
  public IffBuilder buildingSquare(int x, int z, byte code)
  {
    _xbld[x * 128 + z] = code;
    return this;
  }

  // Makes build() emit an XZON segment with the corner bits written by building(), like every city saved by the game
  public IffBuilder withXzon()
  {
    _writeXzon = true;
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

  // CNAM as the Windows 95 game writes it: 32 bytes, 0x1F, the name, a zero byte and leftover memory after it
  // (docs/sc2-format/segments/cnam.md). Use rawCnam() for names that are not plain ASCII.
  public IffBuilder cityName(string name)
  {
    _cnam = Enumerable.Repeat((byte)0xAA, 32).ToArray();
    _cnam[0] = 0x1F;
    byte[] nameBytes = Encoding.ASCII.GetBytes(name);
    Array.Copy(nameBytes, 0, _cnam, 1, nameBytes.Length);
    _cnam[1 + nameBytes.Length] = 0;
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
    if (_writeXzon) body.AddRange(segment("XZON", rle(_xzon)));
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
