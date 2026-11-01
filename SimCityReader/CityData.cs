using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.SimCityReader
{
  public class CityData
  {
    private String _cityName;
    private List<Building> _buildings = new List<Building>();

    private SquareData[][] _squares;
    private byte[] _terrainCodes = new byte[128 * 128];
    private byte _waterLevel;
    private int _compass;

    private const int MISC_COMPASS_OFFSET = 8;        // 4-byte big-endian number of 90 degree rotations of the view when the city was saved (0..3)
    private const int MISC_WATER_LEVEL_OFFSET = 3651;
    private const byte XBIT_ROTATE = 0x02;            // "rotate the tile by 90 degrees" flag
    private const byte XZON_CORNERS = 0xF0;           // corner bits marking the corners of a building's footprint
    private const byte FIRST_BUILDING_CODE = 0x70;    // XBLD codes from here on are buildings; their footprints are marked in XZON
    private const byte FIRST_WATER_TERRAIN = 0x10;    // XTER codes from here on are under water (sea floor, shore or surface water)

    public CityData(Dictionary<String, Segment> segments, String cityName)
    {
      initializeSquares();

      if (segments.ContainsKey("CNAM")) // Read city name from file contents, if it is included in the file, otherwise use the name passed to the constructor
      {
        interpretCNAM(segments["CNAM"]);
        if (string.IsNullOrWhiteSpace(_cityName))
        {
          _cityName = cityName; // Empty name in the file: fall back to the name passed to the constructor
        }
      }
      else
      {
        _cityName = cityName;
      }
      interpretALTM(segments["ALTM"]); // Read altitude map
      interpretMISC(segments["MISC"]); // Read general water level and the compass
      // Read building positions; XZON marks their footprints. Every city saved by the game has it, hand-built test cities may not
      interpretXBLD(segments["XBLD"], segments.ContainsKey("XZON") ? segments["XZON"] : null);
      interpretXTER(segments["XTER"]); // Read positions of groundwater
      if (segments.ContainsKey("XBIT")) // Per-square flags; every city saved by the game has it, hand-built test cities may not
      {
        interpretXBIT(segments["XBIT"]);
      }

      setWaterOnCoastalSquares();
      computeOrientations();
    }

    private void initializeSquares()
    {
      _squares = new SquareData[128][];
      for (int i = 0; i < 128; i++)
      {
        _squares[i] = new SquareData[128];
        for (int j = 0; j < 128; j++)
        {
          _squares[i][j] = new SquareData();
        }
      }
    }

    private void interpretCNAM(Segment cnamSegment)
    {
      byte[] content = cnamSegment.getContent();
      int offset = 1;
      List<byte> stringBytes = new List<byte>();

      while (offset < content.Length && content[offset] != 0)
      {
        stringBytes.Add(content[offset]);
        offset++;
      }

      _cityName = decodeWindows1252(stringBytes);
    }

    // Characters of Windows code page 1252 bytes 0x80..0x9F; the other bytes are the same as in Unicode.
    // The game stores text in the ANSI code page of the machine that saved it, 1252 on Western systems
    // (docs/sc2-format/conventions.md#strings).
    private static readonly char[] WINDOWS_1252_80_TO_9F = {
      '€', '\u0081', '‚', 'ƒ', '„', '…', '†', '‡',
      'ˆ', '‰', 'Š', '‹', 'Œ', '\u008D', 'Ž', '\u008F',
      '\u0090', '‘', '’', '“', '”', '•', '–', '—',
      '˜', '™', 'š', '›', 'œ', '\u009D', 'ž', 'Ÿ'
    };

    private static String decodeWindows1252(List<byte> bytes)
    {
      StringBuilder sb = new StringBuilder();
      foreach (byte b in bytes)
      {
        sb.Append(b >= 0x80 && b <= 0x9F ? WINDOWS_1252_80_TO_9F[b - 0x80] : (char)b);
      }
      return sb.ToString();
    }

    private void interpretALTM(Segment altmSegment)
    {
      byte[] content = altmSegment.getContent();

      int offset = 0;

      for (int i = 0; i < 128; i++)
      {
        for (int j = 0; j < 128; j++)
        {
          _squares[i][j].altitude = (byte)(content[offset + 1] % 32);
          offset += 2;
        }
      }
    }

    // Figures out which building is positioned where. Every square of a building holds the building's code, and the game marks
    // the corners of each footprint in XZON. Like the game, we find a building from its corner with the lowest x and the highest z,
    // whose corner bit depends on the compass (docs/sc2-format/segments/xbld.md#footprints-and-anchors).
    // Squares of multi-square codes that belong to no complete footprint (left over from demolished or damaged buildings,
    // or from the "Magic Eraser" cheat) are left empty instead of being guessed into phantom buildings.
    private void interpretXBLD(Segment xbldSegment, Segment? xzonSegment)
    {
      byte[] codes = xbldSegment.getContent();
      byte[]? zones = xzonSegment?.getContent();
      bool[,] claimed = new bool[128, 128];

      if (zones != null)
      {
        byte anchorCornerBit = (byte)(0x10 << ((3 + _compass) & 3));
        for (int x = 0; x < 128; x++)
        {
          for (int z = 0; z < 128; z++)
          {
            byte code = codes[x * 128 + z];
            int size = BuildingType.getByCode(code).getSize();
            if (code >= FIRST_BUILDING_CODE && size > 1 && (zones[x * 128 + z] & XZON_CORNERS) == anchorCornerBit)
            {
              tryPlaceBuilding(codes, claimed, x, z - size + 1, code, size);
            }
          }
        }
      }

      for (int x = 0; x < 128; x++)
      {
        for (int z = 0; z < 128; z++)
        {
          byte code = codes[x * 128 + z];
          if (code == 0 || claimed[x, z])
          {
            continue;
          }
          int size = BuildingType.getByCode(code).getSize();
          if (size == 1)
          {
            tryPlaceBuilding(codes, claimed, x, z, code, size);
          }
          else if (code < FIRST_BUILDING_CODE)
          {
            tryPlaceBuilding(codes, claimed, x & ~1, z & ~1, code, size); // 2x2 highway pieces lie on an even 2x2 grid
          }
          else if (zones == null)
          {
            tryPlaceBuilding(codes, claimed, x, z, code, size); // No corner bits: the first square found is the corner with the lowest x and z
          }
        }
      }
    }

    // Places a building with its lowest x and z corner at (x, z) if its whole footprint holds its code and is still free
    private bool tryPlaceBuilding(byte[] codes, bool[,] claimed, int x, int z, byte code, int size)
    {
      if (x < 0 || z < 0 || x + size > 128 || z + size > 128)
      {
        return false;
      }
      for (int i = 0; i < size; i++)
      {
        for (int j = 0; j < size; j++)
        {
          if (codes[(x + i) * 128 + z + j] != code || claimed[x + i, z + j])
          {
            return false;
          }
        }
      }

      _buildings.Add(new Building((byte)x, (byte)z, code));
      for (byte i = 0; i < size; i++)
      {
        for (byte j = 0; j < size; j++)
        {
          claimed[x + i, z + j] = true;
          SquareData square = _squares[x + i][z + j];
          square.buildingType = code;
          square.buildingOffsetX = i;
          square.buildingOffsetZ = j;
        }
      }
      return true;
    }

    private void interpretXTER(Segment xterSegment)
    {
      byte[] content = xterSegment.getContent();

      int offset = 0;
      byte tmp;

      for (int i = 0; i < 128; i++)
      {
        for (int j = 0; j < 128; j++)
        {
          tmp = content[offset];
          _terrainCodes[offset] = tmp;

          if (tmp >= 48 && tmp <= 69)
          {
            _squares[i][j].hasWater = true;
          }
          else
          {
            _squares[i][j].hasWater = false;
          }
          offset++;
        }
      }
    }

    private void interpretMISC(Segment miscSegment)
    {
      byte[] content = miscSegment.getContent();

      _compass = (int)Reader.readNumber(content, MISC_COMPASS_OFFSET, 4);
      _waterLevel = content[MISC_WATER_LEVEL_OFFSET];
    }

    private void interpretXBIT(Segment xbitSegment)
    {
      byte[] content = xbitSegment.getContent();

      int offset = 0;

      for (int i = 0; i < 128; i++)
      {
        for (int j = 0; j < 128; j++)
        {
          _squares[i][j].rotateFlag = (content[offset] & XBIT_ROTATE) != 0;
          offset++;
        }
      }
    }

    // Works out which way each directional building runs. The game draws the tile's sprite mirrored when the rotate flag is set,
    // which turns it by 90 degrees in the isometric view:
    // - Bridges: rotating the view flips the flag along with the map, so the flag set always means "along x", whatever the compass.
    // - Runways and piers: rotating the view leaves the flag alone and the game mirrors them by "flag XOR odd compass" instead,
    //   so the flag's meaning flips when the city was saved with the map turned by 90 or 270 degrees.
    private void computeOrientations()
    {
      bool compassIsOdd = _compass % 2 == 1;

      for (int i = 0; i < 128; i++)
      {
        for (int j = 0; j < 128; j++)
        {
          SquareData sd = _squares[i][j];
          byte code = sd.buildingType;

          if (BuildingType.isBridgePart(code))
          {
            sd.orientation = sd.rotateFlag ? Orientation.AlongX : Orientation.AlongZ;
          }
          else if (BuildingType.isRunway(code) || BuildingType.isPier(code))
          {
            sd.orientation = (sd.rotateFlag != compassIsOdd) ? Orientation.AlongX : Orientation.AlongZ;
          }
          else
          {
            sd.orientation = Orientation.None;
          }
        }
      }
    }

    // Squares just below the water level are water too, unless their terrain is dry land: the game can leave
    // dry squares one level below the sea (docs/sc2-format/segments/xter.md#values)
    private void setWaterOnCoastalSquares()
    {
      for (int i = 0; i < 128; i++)
      {
        for (int j = 0; j < 128; j++)
        {
          SquareData sd = _squares[i][j];
          if (sd.altitude == _waterLevel - 1 && _terrainCodes[i * 128 + j] >= FIRST_WATER_TERRAIN)
          {
            sd.hasWater = true;
          }
        }
      }
    }

    public String getCityName()
    {
      return _cityName;
    }

    public SquareData getSquare(int x, int z)
    {
      return _squares[x][z];
    }

    public byte getWaterLevel()
    {
      return _waterLevel;
    }

    /// <summary>How many times (0..3) the map was turned by 90 degrees when the city was saved.</summary>
    public int getCompass()
    {
      return _compass;
    }

    public List<Building> getBuildings()
    {
      return _buildings;
    }
  }
}
