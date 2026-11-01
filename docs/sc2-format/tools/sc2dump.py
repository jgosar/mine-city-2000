#!/usr/bin/env python3
"""Reference decoder for SimCity 2000 (Windows 95) .sc2 / .scn files.

This is the executable companion of docs/sc2-format. Every rule it applies is
described in those pages; when the two disagree, the docs are wrong or this
script is, and either is a bug.

Usage:
    py -3.11 -I sc2dump.py CITY.SC2            human-readable dump
    py -3.11 -I sc2dump.py CITY.SC2 --json     machine-readable dump

Standard library only, so it can be run against untrusted files with -I.
"""

import json
import struct
import sys
from dataclasses import dataclass, field

# --- container -------------------------------------------------------------

FORM = b"FORM"
SCDH = b"SCDH"

# Segments the city loader (SIMCITY.EXE 0x4302e0) knows, with the size the
# game decompresses each one into. None = stored raw (not RLE-compressed).
# See docs/sc2-format/container.md#segment-table.
CITY_SEGMENTS = {
    b"MISC": 0x12C0,
    b"ALTM": None,
    b"XTER": 0x4000,
    b"XBLD": 0x4000,
    b"XZON": 0x4000,
    b"XUND": 0x4000,
    b"XTXT": 0x4000,
    b"XLAB": 0x1900,
    b"XMIC": 0x04B0,
    b"XTHG": 0x01E0,
    b"XBIT": 0x4000,
    b"XTRF": 0x1000,
    b"XPLT": 0x1000,
    b"XVAL": 0x1000,
    b"XCRM": 0x1000,
    b"XPLC": 0x0400,
    b"XFIR": 0x0400,
    b"XPOP": 0x0400,
    b"XROG": 0x0400,
    b"XGRP": 0x0D00,
    b"CNAM": None,
}

# Segments read by the scenario code (0x42ae10) by tag *and* id; the city
# loader skips them. Body starts with a 4-byte id.
SCENARIO_SEGMENTS = (b"TEXT", b"SCEN", b"PICT")


class Sc2FormatError(ValueError):
    pass


@dataclass
class Segment:
    tag: bytes
    offset: int          # file offset of the 8-byte segment header
    length: int          # length field: bytes stored after the header
    body: bytes          # the stored bytes (still compressed if compressed)
    data: bytes = None   # decoded contents, filled by decode_segment()
    notes: list = field(default_factory=list)

    @property
    def name(self):
        return self.tag.decode("latin-1")


@dataclass
class Container:
    form_size: int       # FORM size field: bytes after the first 8
    file_size: int
    segments: list
    trailing: int        # bytes after the last segment the FORM size covers


def u32be(b, off):
    return struct.unpack_from(">I", b, off)[0]


def parse_container(raw):
    """Split a file into segments exactly the way the city loader walks it.

    The loader trusts the FORM size, not the file length: it reads segment
    after segment until (4 + sum of 8 + length) reaches the FORM size. There
    is no IFF-style padding to even lengths.
    """
    if len(raw) < 12:
        raise Sc2FormatError("file shorter than the 12-byte header")
    if raw[0:4] != FORM:
        raise Sc2FormatError("not an IFF file: first 4 bytes are %r, not 'FORM'" % raw[0:4])
    form_size = u32be(raw, 4)
    if raw[8:12] != SCDH:
        raise Sc2FormatError("IFF type is %r, not 'SCDH'" % raw[8:12])

    segments = []
    consumed = 4                      # the 'SCDH' type counts towards form_size
    pos = 12
    while consumed < form_size:
        if pos + 8 > len(raw):
            raise Sc2FormatError("segment header at 0x%X runs past end of file" % pos)
        tag = raw[pos:pos + 4]
        length = u32be(raw, pos + 4)
        if pos + 8 + length > len(raw):
            raise Sc2FormatError("segment %r at 0x%X runs past end of file" % (tag, pos))
        segments.append(Segment(tag, pos, length, raw[pos + 8:pos + 8 + length]))
        pos += 8 + length
        consumed += 8 + length
    return Container(form_size, len(raw), segments, len(raw) - pos)


# --- compression -----------------------------------------------------------

@dataclass
class RleResult:
    data: bytes
    input_used: int          # compressed bytes consumed
    complete: bool           # the game's success test: all input consumed
    literal_zero_ops: int    # control byte 0x00 seen
    repeat_one_ops: int      # control byte 0x80 seen


def decode_rle(body, out_size):
    """Decode like SIMCITY.EXE 0x430dc0.

    Control byte c:
      c < 0x80   copy the next c bytes literally (c = 0 copies nothing)
      c >= 0x80  repeat the next byte (c - 0x7F) times (0x80 = once)
    Decoding stops when the input is used up or out_size bytes are produced.
    The game reports an error only if input is left over; output that ends
    short leaves the rest of the destination buffer untouched.
    """
    out = bytearray()
    i = 0
    zeros = ones = 0
    n = len(body)
    while i < n and len(out) < out_size:
        c = body[i]
        if c < 0x80:
            if c == 0:
                zeros += 1
            out += body[i + 1:i + 1 + c]
            i += 1 + c
        else:
            if c == 0x80:
                ones += 1
            if i + 1 < n:
                out += bytes([body[i + 1]]) * (c - 0x7F)
            i += 2
    return RleResult(bytes(out), min(i, n), i == n, zeros, ones)


def decode_segment(seg):
    """Fill seg.data with the segment contents as the game would see them."""
    size = CITY_SEGMENTS.get(seg.tag, "unknown")
    if size is None or size == "unknown":
        seg.data = seg.body
        if size == "unknown" and seg.tag not in SCENARIO_SEGMENTS:
            seg.notes.append("tag not read by the city loader; skipped by the game")
        return seg
    r = decode_rle(seg.body, size)
    seg.data = r.data
    if not r.complete:
        seg.notes.append("RLE: %d compressed bytes left over (game shows a load error)"
                         % (len(seg.body) - r.input_used))
    if len(r.data) != size:
        seg.notes.append("RLE: decoded %d bytes, expected %d" % (len(r.data), size))
    return seg


def read_file(path):
    with open(path, "rb") as f:
        raw = f.read()
    c = parse_container(raw)
    for s in c.segments:
        decode_segment(s)
    return c


# --- output ----------------------------------------------------------------

def to_dict(c):
    return {
        "form_size": c.form_size,
        "file_size": c.file_size,
        "trailing_bytes": c.trailing,
        "segments": [
            {
                "tag": s.name,
                "offset": s.offset,
                "length": s.length,
                "decoded_length": len(s.data),
                "notes": s.notes,
            }
            for s in c.segments
        ],
    }


def print_text(c):
    print("FORM size %d, file size %d, %d trailing bytes" % (c.form_size, c.file_size, c.trailing))
    print("%-4s  %8s  %8s  %8s" % ("tag", "offset", "stored", "decoded"))
    for s in c.segments:
        print("%-4s  %8X  %8d  %8d  %s" % (s.name, s.offset, s.length, len(s.data), "; ".join(s.notes)))


def main(argv):
    args = [a for a in argv if not a.startswith("--")]
    if len(args) != 1:
        print(__doc__.strip(), file=sys.stderr)
        return 2
    c = read_file(args[0])
    if "--json" in argv:
        json.dump(to_dict(c), sys.stdout, indent=2)
        print()
    else:
        print_text(c)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
