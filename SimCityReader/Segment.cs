using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace com.mc2k.SimCityReader
{
  public class Segment
  {
    // The segments the game run-length encodes; all others (ALTM, CNAM, the scenario segments TEXT/SCEN/PICT/TMPL
    // and unknown tags) are stored as is. See docs/sc2-format/container.md.
    static readonly HashSet<String> COMPRESSED_SEGMENTS = new HashSet<String> {
            "MISC", "XTER", "XBLD", "XZON", "XUND", "XTXT", "XLAB", "XMIC", "XTHG", "XBIT",
            "XTRF", "XPLT", "XVAL", "XCRM", "XPLC", "XFIR", "XPOP", "XROG", "XGRP"
        };

    String _name;
    byte[] _content;

    public Segment(String name, byte[] bytes)
    {
      _name = name;
      if (COMPRESSED_SEGMENTS.Contains(name))
      {
        _content = decompress(bytes);
      }
      else
      {
        _content = bytes;
      }
    }

    private byte[] decompress(byte[] bytes)
    {
      List<byte> result = new List<byte>();

      int offset = 0;

      byte commandByte = 0;

      while (offset < bytes.Length)
      {
        commandByte = bytes[offset];
        offset++;

        // Like the game's decoder: 0..127 copies that many literal bytes (0 copies nothing),
        // 128..255 repeats the next byte commandByte - 127 times (128 repeats it once)
        if (commandByte < 128)
        {
          for (int i = 0; i < commandByte; i++)
          {
            result.Add(bytes[offset]);
            offset++;
          }
        }
        else
        {
          for (int i = 0; i < commandByte - 127; i++)
          {
            result.Add(bytes[offset]);
          }
          offset++;
        }
      }

      return result.ToArray();
    }

    public byte[] getContent()
    {
      return _content;
    }
  }
}
