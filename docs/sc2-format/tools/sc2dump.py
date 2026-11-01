#!/usr/bin/env python3
"""Reference decoder for SimCity 2000 (Windows 95) .sc2 / .scn files.

This is the executable companion of docs/sc2-format. Every rule it applies is
described in those pages; when the two disagree, the docs are wrong or this
script is, and either is a bug.

Usage:
    py -3.11 -I sc2dump.py CITY.SC2            human-readable dump
    py -3.11 -I sc2dump.py CITY.SC2 --json     machine-readable dump
    add --normalise to turn the maps to compass 0 first (conventions.md#orientation)

Standard library only, so it can be run against untrusted files with -I.
"""

import collections
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


# --- segment contents --------------------------------------------------------
#
# A decoder turns a segment's decoded bytes into named fields. A field is either
# a scalar, a list, or a grid: a list of rows, each a list of values, indexed
# grid[row][col] (see docs/sc2-format/conventions.md#the-city-map-128--128-tiles).

DECODERS = {}


def decoder(tag):
    def register(fn):
        DECODERS[tag] = fn
        return fn
    return register


def grid(data, size=128, item=1):
    """Split a row-by-row map into size x size values of item bytes (big-endian)."""
    if item == 1:
        return [list(data[r * size:(r + 1) * size]) for r in range(size)]
    return [[int.from_bytes(data[(r * size + c) * item:(r * size + c + 1) * item], "big")
             for c in range(size)] for r in range(size)]


def map_grid(fn, g):
    """Apply fn to every value of a grid."""
    return [[fn(v) for v in row] for row in g]


def decode_contents(c):
    """Decode every segment that has a decoder; returns {tag: {field: value}}."""
    out = {}
    for s in c.segments:
        fn = DECODERS.get(s.tag)
        if fn is not None:
            out[s.name] = fn(s.data)
    return out


def describe(value):
    """One-line summary of a field for the text dump."""
    if isinstance(value, list) and value and isinstance(value[0], list):
        flat = [v for row in value for v in row]
        common = collections.Counter(flat).most_common(6)
        return "%dx%d grid, %d distinct; most common: %s" % (
            len(value), len(value[0]), len(set(flat)),
            ", ".join("%s x%d" % (v, n) for v, n in common))
    return repr(value)


# --- ALTM and XTER (docs/sc2-format/segments/altm.md, xter.md) ---------------

XTER_KINDS = ((0x00, 0x0E, "land"), (0x10, 0x1E, "underwater"), (0x20, 0x2F, "shore"),
              (0x30, 0x3F, "surface water"), (0x40, 0x46, "surface water"))


def xter_kind(code):
    for lo, hi, kind in XTER_KINDS:
        if lo <= code < hi:
            return kind
    return "unused"


@decoder(b"ALTM")
def decode_altm(data):
    g = grid(data, item=2)
    return {
        "altitude": map_grid(lambda v: v & 0x1F, g),             # bits 0-4
        "water_height": map_grid(lambda v: (v >> 5) & 0x1F, g),  # bits 5-9, meaningful when XTER >= 0x10
        "tunnel_depth": map_grid(lambda v: (v >> 10) & 0x1F, g), # bits 10-14, 0 = no tunnel
        "bit15": map_grid(lambda v: v >> 15, g),                 # never set
    }


@decoder(b"XTER")
def decode_xter(data):
    g = grid(data)
    return {
        "code": g,
        "kind": map_grid(xter_kind, g),
        # slope shape 0x0-0xD for land/underwater/shore; None for surface water (low bits = water edges)
        "slope": map_grid(lambda v: v & 0x0F if v < 0x30 else None, g),
    }


# --- XBLD (docs/sc2-format/segments/xbld.md, reference/tile-codes-xbld.md) ----

XBLD_NAMES = {  # game names, string table ids 594-753 (assignment to codes Inferred)
    0x00: 'Clear terrain', 0x01: 'Rubble', 0x02: 'Rubble', 0x03: 'Rubble',
    0x04: 'Rubble', 0x05: 'Radioactive Waste', 0x06: 'Trees', 0x07: 'Trees',
    0x08: 'Trees', 0x09: 'Trees', 0x0A: 'Trees', 0x0B: 'Trees',
    0x0C: 'Trees', 0x0D: 'Park', 0x0E: 'Power Lines', 0x0F: 'Power Lines',
    0x10: 'Power Lines', 0x11: 'Power Lines', 0x12: 'Power Lines', 0x13: 'Power Lines',
    0x14: 'Power Lines', 0x15: 'Power Lines', 0x16: 'Power Lines', 0x17: 'Power Lines',
    0x18: 'Power Lines', 0x19: 'Power Lines', 0x1A: 'Power Lines', 0x1B: 'Power Lines',
    0x1C: 'Power Lines', 0x1D: 'Road', 0x1E: 'Road', 0x1F: 'Road',
    0x20: 'Road', 0x21: 'Road', 0x22: 'Road', 0x23: 'Road',
    0x24: 'Road', 0x25: 'Road', 0x26: 'Road', 0x27: 'Road',
    0x28: 'Road', 0x29: 'Road', 0x2A: 'Road', 0x2B: 'Road',
    0x2C: 'Railway', 0x2D: 'Railway', 0x2E: 'Railway', 0x2F: 'Railway',
    0x30: 'Railway', 0x31: 'Railway', 0x32: 'Railway', 0x33: 'Railway',
    0x34: 'Railway', 0x35: 'Railway', 0x36: 'Railway', 0x37: 'Railway',
    0x38: 'Railway', 0x39: 'Railway', 0x3A: 'Railway', 0x3B: 'Railway',
    0x3C: 'Railway', 0x3D: 'Railway', 0x3E: 'Railway', 0x3F: 'Tunnel Entrance',
    0x40: 'Tunnel Entrance', 0x41: 'Tunnel Entrance', 0x42: 'Tunnel Entrance', 0x43: 'Road',
    0x44: 'Road', 0x45: 'Railway', 0x46: 'Railway', 0x47: 'Railway',
    0x48: 'Railway', 0x49: 'Highway', 0x4A: 'Highway', 0x4B: 'Highway',
    0x4C: 'Highway', 0x4D: 'Highway', 0x4E: 'Highway', 0x4F: 'Highway',
    0x50: 'Highway', 0x51: 'Suspension Bridge', 0x52: 'Suspension Bridge', 0x53: 'Suspension Bridge',
    0x54: 'Suspension Bridge', 0x55: 'Suspension Bridge', 0x56: 'Bridge', 0x57: 'Bridge',
    0x58: 'Bridge', 0x59: 'Bridge', 0x5A: 'Bridge', 0x5B: 'Bridge',
    0x5C: 'Power Lines', 0x5D: 'Highway Entrance', 0x5E: 'Highway Entrance', 0x5F: 'Highway Entrance',
    0x60: 'Highway Entrance', 0x61: 'Highway', 0x62: 'Highway', 0x63: 'Highway',
    0x64: 'Highway', 0x65: 'Highway', 0x66: 'Highway', 0x67: 'Highway',
    0x68: 'Highway', 0x69: 'Highway', 0x6A: 'Highway Bridge', 0x6B: 'Highway Bridge',
    0x6C: 'Subway to Rail', 0x6D: 'Subway to Rail', 0x6E: 'Subway to Rail', 0x6F: 'Subway to Rail',
    0x70: 'Lower-class homes', 0x71: 'Lower-class homes', 0x72: 'Lower-class homes', 0x73: 'Lower-class homes',
    0x74: 'Middle-class homes', 0x75: 'Middle-class homes', 0x76: 'Middle-class homes', 0x77: 'Middle-class homes',
    0x78: 'Luxury Homes', 0x79: 'Luxury Homes', 0x7A: 'Luxury Homes', 0x7B: 'Luxury Homes',
    0x7C: 'Gas Station', 0x7D: 'Bed & Breakfast Inn', 0x7E: 'Convience Store', 0x7F: 'Gas Station',
    0x80: 'Small office building', 0x81: 'Office building', 0x82: 'Warehouse', 0x83: "Cassidy's Toy Store",
    0x84: 'Warehouse', 0x85: 'Chemical Storage', 0x86: 'Warehouse', 0x87: 'Industrial substation',
    0x88: 'Construction', 0x89: 'Construction', 0x8A: 'Abandoned building', 0x8B: 'Abandoned building',
    0x8C: 'Cheap apartments', 0x8D: 'Apartments', 0x8E: 'Apartments', 0x8F: 'Nice Apartments',
    0x90: 'Nice Apartments', 0x91: 'Condominium', 0x92: 'Condominium', 0x93: 'Condominium',
    0x94: 'Shopping center', 0x95: 'Grocery story', 0x96: 'Office building', 0x97: 'Resort hotel',
    0x98: 'Office building', 0x99: 'Office / Retail', 0x9A: 'Office building', 0x9B: 'Office building',
    0x9C: 'Office building', 0x9D: 'Office building', 0x9E: 'Warehouse', 0x9F: 'Chemical processing',
    0xA0: 'Factory', 0xA1: 'Factory', 0xA2: 'Factory', 0xA3: 'Factory',
    0xA4: 'Factory', 0xA5: 'Factory', 0xA6: 'Construction', 0xA7: 'Construction',
    0xA8: 'Construction', 0xA9: 'Construction', 0xAA: 'Abandoned building', 0xAB: 'Abandoned building',
    0xAC: 'Abandoned building', 0xAD: 'Abandoned building', 0xAE: 'Large apartment building', 0xAF: 'Large apartment building',
    0xB0: 'Condominium', 0xB1: 'Condominium', 0xB2: 'Office park', 0xB3: 'Office tower',
    0xB4: 'Mini-mall', 0xB5: 'Theatre Square', 0xB6: 'Drive-in theater', 0xB7: 'Office tower',
    0xB8: 'Office tower', 0xB9: 'Parking lot', 0xBA: 'Historic office building', 0xBB: 'Corporate headquarters',
    0xBC: 'Chemical processing', 0xBD: 'Large factory', 0xBE: 'Industrial thingamajig', 0xBF: 'Factory',
    0xC0: 'Large warehouse', 0xC1: 'Warehouse', 0xC2: 'Construction', 0xC3: 'Construction',
    0xC4: 'Abandoned building', 0xC5: 'Abandoned building', 0xC6: 'Hydroelectric power', 0xC7: 'Hydroelectric power',
    0xC8: 'Wind power', 0xC9: 'Natural gas power plant', 0xCA: 'Oil power plant', 0xCB: 'Nuclear power plant',
    0xCC: 'Solar power plant', 0xCD: 'Microwave power plant', 0xCE: 'Fusion power plant', 0xCF: 'Coal power plant',
    0xD0: 'City Hall', 0xD1: 'Hospital', 0xD2: 'Police station', 0xD3: 'Fire station',
    0xD4: 'Museum', 0xD5: 'Park', 0xD6: 'School', 0xD7: 'Stadium',
    0xD8: 'Prison', 0xD9: 'College', 0xDA: 'Zoo', 0xDB: 'Statue',
    0xDC: 'Water pump', 0xDD: 'Runway', 0xDE: 'Runway', 0xDF: 'Pier',
    0xE0: 'Crane', 0xE1: 'Control tower', 0xE2: 'Control tower', 0xE3: 'Warehouse',
    0xE4: 'Building', 0xE5: 'Buiding', 0xE6: 'Tarmac', 0xE7: 'F-15b',
    0xE8: 'Hanger', 0xE9: 'Subway Station', 0xEA: 'Radar', 0xEB: 'Water tower',
    0xEC: 'Bus station', 0xED: 'Rail station', 0xEE: 'Parking lot', 0xEF: 'Parking lot',
    0xF0: 'Loading bay', 0xF1: 'Top Secret', 0xF2: 'Cargo yard', 0xF3: 'man',
    0xF4: 'Water treatment plant', 0xF5: 'Library', 0xF6: 'Hangar', 0xF7: 'Church',
    0xF8: 'Marina', 0xF9: 'Missile silo', 0xFA: 'Desalinization plant', 0xFB: 'Plymoth Archology',
    0xFC: 'Forest Arcology', 0xFD: 'Darco Arcology', 0xFE: 'Lanch Archology', 0xFF: 'Braun Llama-dome',
}

# Footprint size of codes 0x70-0xFF (SIMCITY.EXE size table at 0x4e85d8 + 2*code);
# codes 0x61-0x6B are 2x2, everything else below 0x70 is 1x1.
XBLD_SIZE = {c: 1 for c in range(256)}
XBLD_SIZE.update({c: 2 for c in range(0x61, 0x6C)})
XBLD_SIZE.update({c: 2 for c in range(0x8C, 0xAE)})
XBLD_SIZE.update({c: 3 for c in range(0xAE, 0xC6)})
XBLD_SIZE.update({c: 4 for c in range(0xC9, 0xD0)})
XBLD_SIZE.update({c: 3 for c in range(0xD0, 0xD7)})
XBLD_SIZE.update({c: 4 for c in range(0xD7, 0xDB)})
XBLD_SIZE.update({c: 2 for c in range(0xEB, 0xF8)})
XBLD_SIZE.update({0xF8: 3, 0xF9: 2, 0xFA: 3})
XBLD_SIZE.update({c: 4 for c in range(0xFB, 0x100)})


@decoder(b"XBLD")
def decode_xbld(data):
    return {"code": grid(data)}


def corner_bits(compass):
    """XZON corner bits of corners A (min row, min col), B (max row, min col),
    C (max row, max col), D (min row, max col) at this compass (table 0x4dc4d0)."""
    return [0x10 << ((k + compass) & 3) for k in range(4)]


def find_buildings(xbld, xzon, compass):
    """Buildings (codes 0x70+) found from their anchor, corner D.

    Returns (buildings, fragments): buildings as dicts with code, name, size and
    the anchor (row, col) = (min row, max col); fragments as (row, col) tiles of
    multi-tile codes that no well-formed footprint covers.
    See docs/sc2-format/segments/xbld.md#footprints-and-anchors."""
    A, B, C, D = corner_bits(compass)
    buildings, covered = [], set()
    for r in range(128):
        for c in range(128):
            code = xbld[r * 128 + c]
            if code < 0x70:
                continue
            n = XBLD_SIZE[code]
            z = xzon[r * 128 + c] & 0xF0
            if n == 1:
                if z == 0xF0:
                    buildings.append({"code": code, "name": XBLD_NAMES[code], "size": 1, "row": r, "col": c})
                continue
            if z != D:
                continue
            cells = [(r + i, c - j) for i in range(n) for j in range(n)]
            if all(0 <= rr < 128 and 0 <= cc < 128 and xbld[rr * 128 + cc] == code for rr, cc in cells):
                buildings.append({"code": code, "name": XBLD_NAMES[code], "size": n, "row": r, "col": c})
                covered.update(cells)
    fragments = [(r, c) for r in range(128) for c in range(128)
                 if XBLD_SIZE[xbld[r * 128 + c]] >= 2 and xbld[r * 128 + c] >= 0x70 and (r, c) not in covered]
    return buildings, fragments


# --- XZON and XUND (docs/sc2-format/segments/xzon.md, xund.md) ---------------

@decoder(b"XZON")
def decode_xzon(data):
    """Zone type in bits 0-3, corner bits in bits 4-7."""
    g = grid(data)
    return {
        "zone": map_grid(lambda v: v & 0x0F, g),
        "corners": map_grid(lambda v: v & 0xF0, g),
    }


@decoder(b"XUND")
def decode_xund(data):
    """One underground code per tile."""
    return {"code": grid(data)}


# --- XBIT (docs/sc2-format/segments/xbit.md) ---------------------------------

XBIT_FLAGS = ((0x80, "conducts_power"), (0x40, "powered"), (0x20, "carries_water"), (0x10, "watered"),
              (0x08, "mask_or_scratch"), (0x04, "water_covered"), (0x02, "mirror"), (0x01, "salt"))


@decoder(b"XBIT")
def decode_xbit(data):
    """One 0/1 grid per flag."""
    g = grid(data)
    return {name: map_grid(lambda v, mask=mask: 1 if v & mask else 0, g) for mask, name in XBIT_FLAGS}


# --- normalising to compass 0 (docs/sc2-format/conventions.md#orientation) --

def _table(size, cycles, to_zero=""):
    """Build a code remap table from cycle notation: '(a b c) (d e)' maps a->b->c->a, d->e->d."""
    table = list(range(size))
    for cycle in cycles.replace(")", "").split("("):
        codes = [int(x, 16) for x in cycle.split()]
        for a, b in zip(codes, codes[1:] + codes[:1]):
            table[a] = b
    for x in to_zero.split():
        table[int(x, 16)] = 0
    return table


# One clockwise view turn (SIMCITY.EXE 0x465490, compass - 1)
TURN_XBLD = _table(256, "(0E 0F) (10 11 12 13) (14 15 16 17) (18 19 1A 1B) (1D 1E) (1F 20 21 22) (23 24 25 26) "
                        "(27 28 29 2A) (2C 2D) (2E 2F 30 31) (32 33 34 35) (36 37 38 39) (3B 3C 3D 3E) (3F 40 41 42) "
                        "(43 44) (45 46) (47 48) (49 4A) (4B 4C) (4D 4E) (4F 50) (51 55) (52 54) (61 62 63 64) "
                        "(65 66 67 68) (6C 6D 6E 6F)")
TURN_XTER = _table(256, "(01 02 03 04) (05 06 07 08) (09 0A 0B 0C) (11 12 13 14) (15 16 17 18) (19 1A 1B 1C) "
                        "(21 22 23 24) (25 26 27 28) (29 2A 2B 2C) (31 32 33 34) (35 36 37 38) (39 3A 3B 3C) "
                        "(40 41) (42 43 44 45)", "0E 0F 1E 1F 2F 3F 46 47")
TURN_XUND = _table(256, "(01 02) (03 04 05 06) (07 08 09 0A) (0B 0C 0D 0E) (10 11) (12 13 14 15) (16 17 18 19) "
                        "(1A 1B 1C 1D) (1F 20)", "24 25 26 27")
TURN_RAMP = (0x60, 0x5F, 0x5E, 0x5D, 0x5E, 0x5D, 0x60, 0x5F)

GRIDS = {b"ALTM": (128, 2), b"XTER": (128, 1), b"XBLD": (128, 1), b"XZON": (128, 1), b"XUND": (128, 1),
         b"XTXT": (128, 1), b"XBIT": (128, 1), b"XTRF": (64, 1), b"XPLT": (64, 1), b"XVAL": (64, 1),
         b"XCRM": (64, 1), b"XPLC": (32, 1), b"XFIR": (32, 1), b"XPOP": (32, 1), b"XROG": (32, 1)}


def _turn(data, size, item):
    """new[r][c] = old[c][size-1-r]"""
    out = bytearray(len(data))
    for r in range(size):
        for c in range(size):
            src = (c * size + size - 1 - r) * item
            dst = (r * size + c) * item
            out[dst:dst + item] = data[src:src + item]
    return out


def normalise(segments):
    """Turn {tag: decoded bytes} to compass 0 as the game would; returns a new dict.
    XTHG is not adjusted (see the XTHG page)."""
    seg = {tag: bytearray(data) for tag, data in segments.items()}
    misc = seg[b"MISC"]
    for _ in range(int.from_bytes(misc[8:12], "big") & 3):
        for tag, (size, item) in GRIDS.items():
            seg[tag] = _turn(seg[tag], size, item)
        bld, bit = seg[b"XBLD"], seg[b"XBIT"]
        seg[b"XBLD"] = bld = bytearray(TURN_XBLD[v] for v in bld)
        seg[b"XTER"] = bytearray(TURN_XTER[v] for v in seg[b"XTER"])
        seg[b"XUND"] = bytearray(TURN_XUND[v] for v in seg[b"XUND"])
        for i, code in enumerate(bld):
            flagged = bit[i] & 0x02
            if 0x5D <= code <= 0x60:
                k = code - 0x5D + (4 if flagged else 0)
                bld[i] = TURN_RAMP[k]
                bit[i] = bit[i] | 0x02 if k < 4 else bit[i] & ~0x02 & 0xFF
            elif 0x51 <= code <= 0x5C or code in (0x6A, 0x6B):
                if flagged:
                    bit[i] &= ~0x02 & 0xFF
                    bld[i] = TURN_XBLD[code]
                else:
                    bit[i] |= 0x02
    misc[8:12] = (0).to_bytes(4, "big")
    return {tag: bytes(data) for tag, data in seg.items()}


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
        "contents": decode_contents(c),
        "buildings": derived_buildings(c),
    }


def derived_buildings(c):
    """Buildings found across XBLD, XZON and the compass (see find_buildings)."""
    seg = {s.tag: s.data for s in c.segments}
    if not all(t in seg for t in (b"XBLD", b"XZON", b"MISC")):
        return None
    buildings, fragments = find_buildings(seg[b"XBLD"], seg[b"XZON"], u32be(seg[b"MISC"], 8))
    return {"buildings": buildings, "fragments": fragments}


def print_text(c):
    print("FORM size %d, file size %d, %d trailing bytes" % (c.form_size, c.file_size, c.trailing))
    print("%-4s  %8s  %8s  %8s" % ("tag", "offset", "stored", "decoded"))
    for s in c.segments:
        print("%-4s  %8X  %8d  %8d  %s" % (s.name, s.offset, s.length, len(s.data), "; ".join(s.notes)))
    for tag, fields in decode_contents(c).items():
        print("\n%s" % tag)
        for name, value in fields.items():
            print("  %-24s %s" % (name, describe(value)))
    found = derived_buildings(c)
    if found is not None:
        sizes = collections.Counter(b["size"] for b in found["buildings"])
        print("\nBuildings (codes 0x70+): %d (%s), %d fragment tiles" % (
            len(found["buildings"]), ", ".join("%dx%d: %d" % (n, n, sizes[n]) for n in sorted(sizes)),
            len(found["fragments"])))


def main(argv):
    args = [a for a in argv if not a.startswith("--")]
    if len(args) != 1:
        print(__doc__.strip(), file=sys.stderr)
        return 2
    c = read_file(args[0])
    if "--normalise" in argv:
        turned = normalise({s.tag: s.data for s in c.segments})
        for s in c.segments:
            s.data = turned.get(s.tag, s.data)
    if "--json" in argv:
        json.dump(to_dict(c), sys.stdout, indent=2)
        print()
    else:
        print_text(c)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
