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
    """Decode every segment that has a decoder; returns {tag: {field: value}}.
    Scenario segments are keyed by tag and id ("TEXT 0x80"): a scenario has two TEXT."""
    out = {}
    for s in c.segments:
        fn = DECODERS.get(s.tag)
        if fn is not None:
            key = s.name
            if s.tag in SCENARIO_SEGMENTS:
                key = "%s 0x%02X" % (s.name, scenario_id(s.data))
            out[key] = fn(s.data)
    return out


def describe(value):
    """One-line summary of a field for the text dump."""
    if isinstance(value, list) and value and isinstance(value[0], list):
        flat = [v for row in value for v in row]
        common = collections.Counter(flat).most_common(6)
        return "%dx%d grid, %d distinct; most common: %s" % (
            len(value), len(value[0]), len(set(flat)),
            ", ".join("%s x%d" % (v, n) for v, n in common))
    if isinstance(value, list) and value and isinstance(value[0], str):
        used = [(i, s) for i, s in enumerate(value) if s]
        return "%d strings, %d non-empty: %s%s" % (
            len(value), len(used), ", ".join("%d=%r" % u for u in used[:6]), " ..." if len(used) > 6 else "")
    if isinstance(value, list) and len(value) > 8 and not isinstance(value[0], dict):
        return "%d values: %s ..." % (len(value), ", ".join(map(str, value[:8])))
    text = repr(value)
    if len(text) > 160:
        size = "%d entries" % len(value) if isinstance(value, (list, dict)) else ""
        return "%s %s ... (full value with --json)" % (size, text[:120])
    return text


# --- ALTM and XTER (docs/sc2-format/segments/altm.md, xter.md) ---------------

XTER_KINDS = ((0x00, 0x0E, "land"), (0x10, 0x1E, "underwater"), (0x20, 0x2E, "shore"),
              (0x2E, 0x2F, "surface water"),  # never created; handled like the waterfall 0x3E
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
        "slope": map_grid(lambda v: v & 0x0F if v < 0x30 and v != 0x2E else None, g),
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


# --- XTXT, XLAB, CNAM (docs/sc2-format/segments/xtxt.md, xlab.md, cnam.md) ----

TEXT_ENCODING = "cp1252"  # Windows ANSI; the saving machine's code page (conventions.md#strings)


def xtxt_kind(v):
    """What an XTXT value points to (reference/text-codes-xtxt.md)."""
    if v == 0:
        return "none"
    if v <= 0x32:
        return "sign"              # XLAB slot v
    if v == 0x33:
        return "unused"
    if v <= 0x3C:
        return "builtin_microsim"  # XMIC record v - 0x33, XLAB slot v
    if v <= 0xC8:
        return "microsim"          # XMIC record v - 0x33, XLAB slot v
    if v <= 0xF0:
        return "thing"             # XTHG record v - 0xC9
    if v == 0xFA:
        return "connection"        # neighbour connection sign
    if v >= 0xFB:
        return "disaster"          # in-game only, never saved
    return "unused"


@decoder(b"XTXT")
def decode_xtxt(data):
    g = grid(data)
    return {"code": g, "kind": map_grid(xtxt_kind, g)}


@decoder(b"XLAB")
def decode_xlab(data):
    """256 labels; the game caps the length byte at 23 when it loads and keeps them as C strings (0x430c00)."""
    recs = [data[i * 25:(i + 1) * 25] for i in range(256)]
    return {
        "text": [r[1:1 + min(r[0], 23)].split(b"\0")[0].decode(TEXT_ENCODING, "replace") for r in recs],
        "length_byte": [r[0] for r in recs],
    }


@decoder(b"CNAM")
def decode_cnam(data):
    """City name: the C string from byte 1. Byte 0 is 0x1F or the name length; the game ignores it."""
    return {"name": data[1:].split(b"\0")[0].decode(TEXT_ENCODING, "replace"), "byte0": data[0]}


# --- overlays (docs/sc2-format/segments/minimaps.md) --------------------------

# Density weight per XBLD code used for XPOP (SIMCITY.EXE table 0x4e9648, codes 0x70-0xC5);
# codes 0xC6 and up add 2 per tile (arcologies 0xFB-0xFE: 12); codes below 0x70 add nothing.
XPOP_WEIGHT = {c: 0 for c in range(256)}
for lo, hi, w in ((0x70, 0x89, 1), (0x8C, 0x8F, 2), (0x90, 0x93, 3), (0x94, 0x98, 2), (0x99, 0x9D, 3),
                  (0x9E, 0xA1, 2), (0xA2, 0xA5, 3), (0xA6, 0xA7, 2), (0xA8, 0xA9, 3), (0xAE, 0xC3, 4)):
    XPOP_WEIGHT.update({c: w for c in range(lo, hi + 1)})
XPOP_WEIGHT.update({c: 2 for c in range(0xC6, 0x100)})
XPOP_WEIGHT.update({c: 12 for c in range(0xFB, 0xFF)})


def xpop_from_xbld(xbld):
    """XPOP as step 2 (0x46a840) computes it: 4 x the summed weights of the 4 x 4 tiles of a cell,
    capped at 255. Tiles in rows and columns 0 and 127 are not counted."""
    total = [0] * 1024
    for r in range(1, 127):
        for c in range(1, 127):
            total[(r // 4) * 32 + c // 4] += XPOP_WEIGHT[xbld[r * 128 + c]]
    return [min(255, 4 * v) for v in total]


def _overlay(size):
    def decode(data):
        return {"value": grid(data, size=size)}
    return decode


for _tag in (b"XTRF", b"XPLT", b"XVAL", b"XCRM"):
    decoder(_tag)(_overlay(64))
for _tag in (b"XPLC", b"XFIR", b"XPOP", b"XROG"):
    decoder(_tag)(_overlay(32))


# --- XGRP (docs/sc2-format/segments/xgrp.md) ----------------------------------

XGRP_SERIES = ("city_size", "residents", "commerce", "industry", "traffic", "pollution", "land_value", "crime",
               "power_percent", "water_percent", "health", "education", "unemployment", "gnp",
               "national_population", "fed_rate")
# slots of each 52-value series: newest first within each block
XGRP_BLOCKS = {"months": (0, 12), "half_years": (12, 32), "five_years": (32, 52)}


@decoder(b"XGRP")
def decode_xgrp(data):
    """16 graph series of 52 signed 32-bit values."""
    values = struct.unpack(">832i", data)
    return {name: list(values[i * 52:(i + 1) * 52]) for i, name in enumerate(XGRP_SERIES)}


# --- XMIC (docs/sc2-format/segments/xmic.md, reference/microsim-kinds-xmic.md) ------
#
# 150 records of 8 bytes: building code, byte 1, then three big-endian 16-bit
# words. Record r belongs to XTXT value r + 0x33 and XLAB slot r + 0x33.

XMIC_RECORDS = 150
XMIC_FIRST_OWN = 10          # records 10-149 belong to one building each

# The city-wide systems and their fixed records (SIMCITY.EXE 0x4e7c9a, entries 0x11-0x19)
MICROSIM_FIXED = {0xEC: 1, 0xED: 2, 0xE9: 3, 0xC8: 4, 0xC6: 5, 0xC7: 5,
                  0xD5: 6, 0xD4: 7, 0xF5: 8, 0xF8: 9}
# Codes that get a record of their own (0x4e7c9a entries 1-0x10)
MICROSIM_OWN = frozenset(list(range(0xC9, 0xD4)) + list(range(0xD6, 0xDC))
                         + [0xF3, 0xF4] + list(range(0xFA, 0x100)))

# What byte 1 and the words at offsets 2, 4, 6 mean, per building code, from the
# microsim window's templates (0x4e7da0 + strings 900+, US version) and the yearly
# update 0x44e390. None = not used.
_PLANT = ("age_years", "max_output_mw", "percent_of_capacity", None)
_ARCO = ("conditions_grade", "design_capacity_thousands", "residents", "year_built")
MICROSIM_FIELDS = {
    **{c: _PLANT for c in range(0xC9, 0xD0)},
    0xD0: (None, "employees", "year_built", None),
    0xD1: ("grade", "patients", "doctors", "annual_cost"),
    0xD2: ("annual_cost", "officers", "crimes_reported", "arrests"),
    0xD3: ("annual_cost", "firefighters", "fire_engines", "response_minutes"),
    0xD6: ("grade", "students", "teachers", "annual_cost"),
    0xD7: ("wins", "attendance", "sport", "team_label_slot"),
    0xD8: ("escapes", "inmates", "guards", "percent_of_capacity"),
    0xD9: ("grade", "students", "teachers", "annual_cost"),
    0xDA: ("other_dromedaries", "peruvian_llamas", "andean_llamas", "alpacas"),
    0xDB: (None, "year_built", "pigeons_perched", None),
    0xF3: ("doorstops", "year_built", "approval_rating", "employees"),
    0xF4: ("percent_of_capacity", "untreated_filth_thousand_cubic_feet",
           "treated_filth_thousand_cubic_feet", None),
    0xFA: ("percent_of_capacity", "salt_tons_removed", "employees", None),
    **{c: _ARCO for c in range(0xFB, 0xFF)},
    0xFF: ("weddings", "visitors", "llama_sightings", "complaints"),
    0xEC: (None, "bus_stations", "buses", "passengers"),
    0xED: (None, "rail_stations", None, "passengers"),
    0xE9: (None, "subway_stations", None, "passengers"),
    0xC8: (None, "wind_generators", "max_system_output_mw", None),
    0xC6: (None, "hydro_generators", "max_system_output_mw", None),
    0xC7: (None, "hydro_generators", "max_system_output_mw", None),
    0xD5: (None, "attendance_last_year", "acres", "employees"),
    0xD4: (None, "attendance_last_year", "exhibits", None),
    0xF5: ("grade", "attendance", "books", None),
    0xF8: (None, "boats", None, None),
}
GRADES = ("F", "D-", "D", "D+", "C-", "C", "C+", "B-", "B", "B+", "A-", "A", "A+")
SPORTS = ("football", "baseball", "soccer", "cricket", "rugby")


@decoder(b"XMIC")
def decode_xmic(data):
    """The records in use (byte 0 not 0) and record 0, with named fields."""
    out = []
    for r in range(XMIC_RECORDS):
        code, b1, w2, w4, w6 = struct.unpack_from(">BBHHH", data, 8 * r)
        if code == 0 and r:
            continue
        rec = {"record": r, "xtxt": r + 0x33, "code": "0x%02X" % code,
               "raw": [b1, w2, w4, w6]}
        if r == 0:
            rec["note"] = "unused record"
        else:
            names = MICROSIM_FIELDS.get(code)
            if names:
                rec["fields"] = {n: v for n, v in zip(names, (b1, w2, w4, w6)) if n}
                for n in ("grade", "conditions_grade"):
                    if n in rec["fields"] and rec["fields"][n] < len(GRADES):
                        rec["fields"][n + "_letter"] = GRADES[rec["fields"][n]]
                if code == 0xD7:
                    rec["fields"]["losses"] = 40 - b1 if b1 else 0
                    if w4 < len(SPORTS):
                        rec["fields"]["sport_name"] = SPORTS[w4]
            else:
                rec["note"] = "not a microsim code"
        out.append(rec)
    return {"records": out}


# --- XTHG (docs/sc2-format/segments/xthg.md, reference/thing-ids-xthg.md) ----

XTHG_RECORDS = 40
XTHG_RECORD_SIZE = 12

# Thing type (byte 0) -> name (docs/sc2-format/reference/thing-ids-xthg.md)
THING_TYPES = {
    0: None, 1: "airplane", 2: "helicopter", 3: "cargo ship", 4: "bulldozer (tool)",
    5: "monster", 6: "explosion", 7: "police unit", 8: "fire unit", 9: "sailboat",
    10: "train engine", 11: "train car", 12: "subway train engine", 13: "subway train car",
    14: "military unit", 15: "tornado", 16: "flying rescuer",
}
TRAIN_TYPES = (10, 11, 12, 13)


def decode_thing(i, r):
    """One 12-byte XTHG record as named fields; None for a free record (type 0)."""
    t = r[0]
    if t == 0:
        return None
    out = {"record": i, "xtxt_value": 0xC9 + i, "type": t, "kind": THING_TYPES.get(t, "unknown"),
           "row": r[3], "col": r[4], "xtxt_under": r[10]}
    if t in TRAIN_TYPES:
        out.update({"direction": r[1], "next_car": r[2], "height": r[5],
                    "next_row": r[6], "next_col": r[7], "sprite_direction": r[8] & 7,
                    "unused": [r[9], r[11]]})
    else:
        out.update({"direction": r[1], "state": r[2], "height": r[5],
                    "sub_row": r[6], "sub_col": r[7], "target_row": r[8], "target_col": r[9],
                    "extra": r[11]})
    return out


@decoder(b"XTHG")
def decode_xthg(data):
    """40 records of 12 bytes; record n is the object of XTXT value 0xC9 + n."""
    recs = [decode_thing(i, data[i * XTHG_RECORD_SIZE:(i + 1) * XTHG_RECORD_SIZE])
            for i in range(XTHG_RECORDS)]
    return {"things": [r for r in recs if r is not None]}


def xthg_turn_cw(data):
    """XTHG after one clockwise view turn, as 0x465490 has the game do it (0x466410).

    Record 0 is left alone. For every other used record: the tile moves like the maps,
    (row, col) -> (127 - col, row); the 8-way direction turns by 2 (trains: the 4-way
    direction by 1 and the sprite direction in byte 8 by 2); the second coordinate pair
    (bytes 8, 9, or bytes 6, 7 for trains) moves like the tile; an airplane's runway
    heading (byte 2, bits 4-6) turns by 2.
    """
    t = bytearray(data)
    for i in range(1, XTHG_RECORDS):
        r = t[i * XTHG_RECORD_SIZE:(i + 1) * XTHG_RECORD_SIZE]
        kind = r[0]
        if kind == 0:
            continue
        r[3], r[4] = (127 - r[4]) & 0xFF, r[3]
        if kind in TRAIN_TYPES:
            r[1] = (r[1] + 1) & 3
            r[8] = (r[8] + 2) & 7
            r[6], r[7] = (127 - r[7]) & 0xFF, r[6]
        else:
            r[1] = (r[1] + 2) & 7
            r[8], r[9] = (127 - r[9]) & 0xFF, r[8]
            if kind == 1:
                r[2] = ((r[2] + 0x20) & 0x70) + (r[2] & 0x0F)
        t[i * XTHG_RECORD_SIZE:(i + 1) * XTHG_RECORD_SIZE] = r
    return bytes(t)


# --- SCEN and TMPL (docs/sc2-format/segments/scen.md) ---------------------------
#
# Both are stored raw. data = the whole body: a 4-byte little-endian id, then the
# fields, big-endian like the rest of the file (the scenario loader 0x4345c0 swaps them).

# The layout the Windows 95 game reads (52 bytes after the id; 0x4345c0, 0x413520 step 22)
SCEN_FIELDS = (
    ("disaster", ">h"), ("disaster_row", ">B"), ("disaster_col", ">B"),
    ("time_limit_months", ">h"),
    ("city_size_goal", ">i"), ("residential_goal", ">i"), ("commercial_goal", ">i"),
    ("industrial_goal", ">i"), ("funds_goal", ">i"), ("land_value_goal", ">i"),
    ("life_expectancy_goal", ">h"), ("education_goal", ">h"),
    ("pollution_limit", ">i"), ("crime_limit", ">i"), ("traffic_limit", ">i"),
    ("build_item_1", ">B"), ("build_item_2", ">B"),
    ("build_item_1_tiles", ">h"), ("build_item_2_tiles", ">h"),
)
# The older 48-byte layout that TMPL describes: no life expectancy or education goal
SCEN_FIELDS_TMPL = tuple(f for f in SCEN_FIELDS if f[0] not in ("life_expectancy_goal", "education_goal"))
TMPL_TYPES = {"DBYT": 1, "DWRD": 2, "DLNG": 4}


def _scen_fields(fields, body):
    out, pos = {}, 0
    for name, fmt in fields:
        size = struct.calcsize(fmt)
        out[name] = struct.unpack(fmt, body[pos:pos + size])[0] if pos + size <= len(body) else None
        pos += size
    return out, pos


@decoder(b"SCEN")
def decode_scen(data):
    """Scenario goals. 52-byte bodies use the Windows 95 layout, 48-byte ones the TMPL layout."""
    body = data[4:]
    old = len(body) < 52
    fields, used = _scen_fields(SCEN_FIELDS_TMPL if old else SCEN_FIELDS, body)
    out = {"id": struct.unpack("<I", data[:4])[0], "layout": "tmpl (48 bytes)" if old else "windows95 (52 bytes)"}
    out.update(fields)
    out["disaster_name"] = _name(DISASTERS, out["disaster"])
    for k in (1, 2):
        code = out["build_item_%d" % k]
        out["build_item_%d_name" % k] = XBLD_NAMES.get(code) if code else None
    if old:
        # The Windows 95 game reads every body with the 52-byte layout. The two layouts differ
        # only from byte 30 on (after the land value goal); all-zero bytes read the same either way.
        out["windows95_reads_differently"] = any(body[30:])
    if len(body) > used:
        out["extra_bytes"] = body[used:].hex()
    return out


@decoder(b"TMPL")
def decode_tmpl(data):
    """Field list of SCEN: Pascal-string label, then a 4-character type. Never read by the game."""
    out, pos = [], 4
    while pos < len(data):
        n = data[pos]
        label = data[pos + 1:pos + 1 + n].decode(TEXT_ENCODING)
        kind = data[pos + 1 + n:pos + 5 + n].decode("ascii", "replace")
        out.append({"label": label, "type": kind, "bytes": TMPL_TYPES.get(kind)})
        pos += 5 + n
    return {"id": struct.unpack("<I", data[:4])[0], "fields": out}


def scen_turn_cw(data):
    """SCEN after one clockwise view turn: the disaster spot moves with the map (row, col) -> (127 - col, row)."""
    data = bytearray(data)
    if struct.unpack(">h", data[4:6])[0]:
        row, col = data[6], data[7]
        data[6], data[7] = 127 - col & 0xFF, row
    return bytes(data)


# --- TEXT and PICT (docs/sc2-format/segments/text.md, pict.md) ---------------
#
# Scenario segments are stored raw: a 4-byte little-endian id, then the body.

TEXT_LIMIT = 0x300       # bytes of a TEXT body the game reads (0x42ae10)
PICT_LIMIT = 0x10CC      # bytes of a PICT body the game reads (0x42ae10)
PICT_SIZE = 65           # the game copies 65 rows of 65 pixels ...
PICT_STRIDE = 66         # ... 66 bytes apart, starting 4 bytes into the body (0x475790)
TEXT_SCENARIO_ENCODING = "mac_roman"   # what the game's conversion amounts to (0x42a080)


def scenario_id(data):
    return int.from_bytes(data[:4], "little")


@decoder(b"TEXT")
def decode_text(data):
    """Scenario text 0x80 (scenario list) or 0x81 (briefing); CR marks a paragraph."""
    body = data[4:4 + TEXT_LIMIT].split(b"\0")[0]
    return {
        "id": scenario_id(data),
        "text": body.decode(TEXT_SCENARIO_ENCODING),
        "cut_by_game": len(data) - 4 > TEXT_LIMIT,
    }


@decoder(b"PICT")
def decode_pict(data):
    """The 65 x 65 scenario picture: indices into the PAL_MAC.BMP colours (see pict_palette)."""
    body = data[4:4 + PICT_LIMIT]
    px = body[4:]
    at = lambda i: px[i] if i < len(px) else None   # None: past the stored bytes
    return {
        "id": scenario_id(data),
        "stored_width": int.from_bytes(body[0:2], "little"),
        "stored_height": int.from_bytes(body[2:4], "little"),
        "row_ends": sorted({px[r * PICT_STRIDE + PICT_SIZE] for r in range(PICT_SIZE)
                            if r * PICT_STRIDE + PICT_SIZE < len(px)}),
        "pixels": [[at(r * PICT_STRIDE + c) for c in range(PICT_SIZE)] for r in range(PICT_SIZE)],
    }


def bmp_palette(raw):
    """The 256 (r, g, b) colours of an 8-bit Windows BMP, e.g. the game's PAL_MAC.BMP."""
    header = struct.unpack_from("<I", raw, 14)[0]
    return [(raw[14 + header + 4 * i + 2], raw[14 + header + 4 * i + 1], raw[14 + header + 4 * i])
            for i in range(256)]


def pict_palette(pal_mac):
    """PICT colours: PAL_MAC.BMP with entries 0 and 255 swapped, as the game draws them
    (0x475e60; see pict.md#colours)."""
    pal = list(pal_mac)
    pal[0], pal[255] = pal[255], pal[0]
    return pal


# --- MISC (docs/sc2-format/segments/misc/README.md) ----------------------------
#
# 1200 big-endian 32-bit words. The game copies each into a variable of 32, 16 or
# 8 bits (loader 0x431330), so only the low 16 or 8 bits of the narrower words
# count: some files store a negative 16-bit value as 0x0000FFxx, not 0xFFFFFFxx.

def _words(*spans):
    return frozenset(i for a, b in spans for i in range(a, b + 1))


# Words the game loads into 16-bit variables (read as signed) and 8-bit ones (unsigned)
MISC_16_BIT = _words((1, 3), (7, 8), (22, 23), (28, 28), (30, 30), (124, 379), (388, 437), (454, 478),
                     (912, 912), (1002, 1019), (1027, 1031), (1033, 1034), (1036, 1041),
                     (1043, 1043)) | frozenset(
    w for k in range(11) for w in (91 + 3 * k, 92 + 3 * k)) | frozenset(
    438 + 4 * k for k in range(4)) | frozenset(
    w for s in range(9) for w in (946 + 6 * s, 947 + 6 * s))
MISC_8_BIT = _words((24, 27), (915, 945)) | frozenset(
    w for s in range(9) for w in range(948 + 6 * s, 952 + 6 * s))
# Neighbour population and economy: full 32 bits, unsigned (neighbours.md)
MISC_UNSIGNED = frozenset(w for k in range(4) for w in (439 + 4 * k, 440 + 4 * k))


def misc_words(data):
    """The 1200 MISC words as the game loads them (see MISC_16_BIT, MISC_8_BIT, MISC_UNSIGNED)."""
    out = []
    for i, v in enumerate(struct.unpack(">1200i", data)):
        if i in MISC_16_BIT:
            v &= 0xFFFF
            v -= 0x10000 if v & 0x8000 else 0
        elif i in MISC_8_BIT:
            v &= 0xFF
        elif i in MISC_UNSIGNED:
            v &= 0xFFFFFFFF
        out.append(v)
    return tuple(out)


# --- MISC: general words 0-30, 911-915, 1018-1199 (docs/sc2-format/segments/misc/general.md)

CITY_MODES = ("terrain editor", "city", "disaster")
DIFFICULTIES = ("none", "easy", "medium", "hard")
WEATHER = ("cold", "clear", "hot", "foggy", "chilly", "overcast", "snow", "rain", "windy",
           "blizzard", "hurricane", "tornado")
DISASTERS = ("none", "fire", "flood", "riot", "toxic spill", "air crash", "earthquake", "tornado",
             "monster", "meltdown", "microwave", "volcano", "firestorm", "mass riots", "major flood",
             "pollution accident", "hurricane", "helicopter crash", "plane crash")
MILITARY_BASES = ("not offered", "declined or no site", "army base", "air force base", "navy base",
                  "missile silos")
REWARDS = ("mayor's house", "city hall", "statue", "braun llama dome", "arcologies")
SPEEDS = (None, "paused", "turtle", "llama", "cheetah", "african swallow")


def _name(table, v):
    return table[v] if 0 <= v < len(table) and table[v] is not None else "unknown %d" % v


def misc_general(w):
    """MISC words 0-30, 911-915 and 1018-1199 as named fields; w = the 1200 signed words."""
    view = w[1028]
    return {
        "version": w[0],
        "city_mode": _name(CITY_MODES, w[1]),
        "compass": w[2],
        "founding_year": w[3],
        "days": w[4],
        "date": {"year": w[3] + w[4] // 300, "month": w[4] // 25 % 12, "day": w[4] % 25},
        "funds": w[5],
        "bonds_outstanding": w[6],
        "difficulty": _name(DIFFICULTIES, w[7]),
        "reward_level": w[8],
        "city_value": w[9],
        "land_value_total": w[10],
        "crime_total": w[11],
        "traffic_total": w[12],
        "pollution_total": w[13],
        "unused_14_15": [w[14], w[15]],
        "population_months": w[16],
        "workforce_percent": w[17],
        "life_expectancy": w[18],
        "education_quotient": w[19],
        "national_population_thousands": w[20],
        "gnp": w[21],
        "fed_rate": w[22],
        "national_trend": w[23],
        "heat": w[24],
        "wind": w[25],
        "humidity": w[26],
        "weather": _name(WEATHER, w[27]),
        "pending_disaster": _name(DISASTERS, w[28]),
        "residents_last_month": w[29],
        "rewards_available": [REWARDS[b] for b in range(5) if w[30] >> b & 1],
        "year_end_pending": w[911],
        "sea_level": w[912],
        "terrain_coast": w[913],
        "terrain_river": w[914],
        "military_base": _name(MILITARY_BASES, w[915]),
        "subway_tiles": w[1018],
        "speed": _name(SPEEDS, w[1019]),
        "auto_budget": w[1020],
        "auto_goto": w[1021],
        "sound": w[1022],
        "music": w[1023],
        "no_disasters": w[1024],
        "newspaper_subscription": w[1025],
        "newspaper_extra": w[1026],
        "newspaper_choice": w[1027],
        "view_centre": None if view == -1 else {"low7": view & 0x7F, "high": view >> 8 & 0xFF},
        "zoom": w[1029],
        "building_centre": {"row": w[1030], "col": w[1031]},
        "arcology_residents": w[1032],
        "unused_1033": w[1033],
        "sports_teams": w[1034],
        "zone_population": w[1035],
        "industry_concentration_bonus": w[1036],
        "heavy_industry_level": w[1037],
        "police_arrests": w[1038],
        "prisons_have_room": w[1039],
        "disaster_thing": w[1040],
        "current_disaster": _name(DISASTERS, w[1041]),
        "disaster_active": w[1042],
        "water_treatment_ok": w[1043],
        "other_version_1044_1070": list(w[1044:1071]) if any(w[1044:1071]) else None,
        "tail_nonzero": any(w[1071:1200]),
    }


# --- MISC: population, industry and bonds (docs/sc2-format/segments/misc/population.md) ---

AGE_GROUPS = 20                 # 5-year age groups, 0-4 ... 95-99
INDUSTRIES = ("Steel/Mining", "Textiles", "Petrochemical", "Food", "Construction",
              "Automotive", "Aerospace", "Finance", "Media", "Electronics", "Tourism")
ZONE_POPULATION = ("total", "light_residential", "dense_residential", "light_commercial",
                   "dense_commercial", "light_industrial", "dense_industrial", "abandoned")

# Population units of a zone building by XBLD code (SIMCITY.EXE: level table 0x4e7458,
# units per level 0x4e75b0 = 0, 1, 8, 12, 36). Construction sites count nothing.
_LEVEL_RANGES = ((0x70, 0x87, 1), (0x8A, 0x8B, 1), (0x8C, 0x8F, 2), (0x90, 0x93, 3),
                 (0x94, 0x98, 2), (0x99, 0x9D, 3), (0x9E, 0xA1, 2), (0xA2, 0xA5, 3),
                 (0xAA, 0xAB, 2), (0xAC, 0xAD, 3), (0xAE, 0xC1, 4), (0xC4, 0xC5, 4))
_UNITS = {1: 1, 2: 8, 3: 12, 4: 36}
POPULATION_UNITS = {code: _UNITS[level]
                    for lo, hi, level in _LEVEL_RANGES for code in range(lo, hi + 1)}
ABANDONED = frozenset((0x8A, 0x8B, 0xAA, 0xAB, 0xAC, 0xAD, 0xC4, 0xC5))


def zone_population_from_map(xbld, xzon, compass):
    """MISC words 380-387 as a full pass of growth steps 3-18 would leave them.

    Each zone building counts once, at its corner-D tile; word 380 is the sum of 381-386.
    """
    anchor = (0x80, 0x10, 0x20, 0x40)[compass & 3]
    words = [0] * 8
    for code, z in zip(xbld, xzon):
        zone = z & 0x0F
        if 1 <= zone <= 6 and z & anchor and code in POPULATION_UNITS:
            words[7 if code in ABANDONED else zone] += POPULATION_UNITS[code]
    words[0] = sum(words[1:7])
    return words


def misc_population(w):
    bonds = max(0, min(w[6], 50))
    return {
        "age_groups": [{"ages": "%d-%d" % (5 * i, 5 * i + 4),
                        "people": w[31 + 3 * i],
                        "education_total": w[32 + 3 * i],
                        "health_total": w[33 + 3 * i]} for i in range(AGE_GROUPS)],
        "industries": [{"industry": name,
                        "demand": w[91 + 3 * k],
                        "tax_rate": w[92 + 3 * k],
                        "size": w[93 + 3 * k]} for k, name in enumerate(INDUSTRIES)],
        "zone_population": dict(zip(ZONE_POPULATION, w[380:388])),
        "bond_rates": list(w[388:388 + bonds]),
        "bond_rates_left_over": list(w[388 + bonds:438]),
    }


# --- MISC budget (docs/sc2-format/segments/misc/budget.md) -------------------

BUDGET_FIRST_WORD = 479
BUDGET_ENTRY_WORDS = 27
BUDGET_ENTRIES = (
    "residential_tax", "commercial_tax", "industrial_tax", "ordinances", "bonds",
    "police", "fire", "health", "schools", "colleges",
    "roads", "highways", "bridges", "rail", "subway", "tunnels",
)
# Divisor per entry (SIMCITY.EXE 0x4e9eb0): at the start of a year the game adds
# ytd / (divisor * 12) to the funds; positive = income, negative = expense.
BUDGET_DIVISORS = (75, 75, 75, 75, -100, -1, -1, -2, -4, -1, -1000, -500, -400, -250, -250, -250)

ORDINANCES = (
    "sales_tax", "income_tax", "legalized_gambling", "parking_fines",
    "volunteer_fire_dept", "public_smoking_ban", "free_clinics", "junior_sports",
    "pro_reading_campaign", "anti_drug_campaign", "cpr_training", "neighborhood_watch",
    "tourist_advertising", "business_advertising", "city_beautification", "annual_carnival",
    "energy_conservation", "nuclear_free_zone", "homeless_shelter", "pollution_controls",
)


def misc_budget(w):
    entries = {}
    for e, name in enumerate(BUDGET_ENTRIES):
        b = BUDGET_FIRST_WORD + BUDGET_ENTRY_WORDS * e
        entries[name] = {
            "count": w[b],
            "rate": w[b + 1],
            "year_to_date": w[b + 2],
            "months": [{"count": w[b + 3 + 2 * k], "rate": w[b + 4 + 2 * k]} for k in range(12)],
        }
    flags = w[1000]
    return {
        "budget": entries,
        "ordinance_flags": flags,
        "ordinances": [n for i, n in enumerate(ORDINANCES) if flags >> i & 1],
    }


# --- MISC: tile counts, neighbours, demand, technology, newspapers ------------
# (docs/sc2-format/segments/misc/tile-counts.md, neighbours.md, demand.md,
#  technology.md, newspapers.md)

def _s16(v):
    """The low 16 bits of a MISC word as a signed number, as the game loads it."""
    v &= 0xFFFF
    return v - 0x10000 if v & 0x8000 else v


# Military count index -> XBLD code (index 0: every other tile in zone 7)
MILITARY_CODES = (None, 0xDD, 0xDE, 0xEF, 0xF2, 0xEA, 0xE3, 0xE4, 0xE5,
                  0xF1, 0xE0, 0xE2, 0xE7, 0xE8, 0xF6, 0xF9)

# Neighbour name id -> name (id 0 is "Ocean"; ids 1-36 are strings 548-583 of SIMCITY.EXE)
NEIGHBOUR_NAMES = (
    "Ocean", "Oak Creek", "Denmont", "Fort Verdegris", "Schwinton", "Mill Valley",
    "Petaluma", "PortVille", "Ashland", "Eubancs", "Aurac", "Tent Pegs", "Cherryton",
    "Blake", "Pioneers", "Fortune", "Phippsville", "Jeromi", "Harpersville",
    "Washers Grove", "Stars County", "Villa", "Serviland", "Newton", "Avon", "Dexter",
    "Sinistrel", "Jenna", "Yestonia", "New Boots", "Hoek Creek", "Stimpleton",
    "Little Rouge", "Krighton", "Cats Corner", "Rimmer", "Lister")

# Map edge of neighbour k when the city is normalised to compass 0
NEIGHBOUR_EDGES = ("row 127", "col 127", "row 0", "col 0")

TECHNOLOGIES = (
    "gas power", "nuclear power", "solar power", "wind power", "microwave power",
    "fusion power", "airport", "highways", "buses", "subways", "water treatment",
    "desalination", "plymouth arcology", "forest arcology", "darco arcology",
    "launch arcology", "unused")

NEWSPAPER_NAMES = ("Picayune", "Courier", "Herald", "Journal", "Times", "Chronicle")
OPINION_COLUMNS = ("Fred's Opinion", "Editor's Corner", "Commentary", "Opinion Poll",
                   "Survey", "MisSim's Advice")
WEATHER_COLUMNS = ("Weather Corner", "Weather Report", "Today's Weather",
                   "Weather with Merle", "Weather Forecast", "Weather Talk")


def _pick(names, i):
    return names[i] if 0 <= i < len(names) else i


def misc_counts(w):
    return {
        "building_counts": [_s16(v) for v in w[124:380]],
        "military_counts": {("other" if c is None else "0x%02X" % c): _s16(v)
                            for c, v in zip(MILITARY_CODES, w[1002:1018])},
    }


def misc_neighbours(w):
    out = []
    for k in range(4):
        name_id = _s16(w[438 + 4 * k])
        out.append({
            "edge_at_compass_0": NEIGHBOUR_EDGES[k],
            "name_id": name_id,
            "name": _pick(NEIGHBOUR_NAMES, name_id),
            "population": w[439 + 4 * k] & 0xFFFFFFFF,
            "economy": w[440 + 4 * k] & 0xFFFFFFFF,
            "unused": w[441 + 4 * k],
        })
    return {"neighbours": out}


def misc_demand(w):
    return {
        "demand": {"residential": _s16(w[454]), "commercial": _s16(w[455]),
                   "industrial": _s16(w[456])},
        "demand_unused": list(w[457:462]),
    }


def misc_technology(w):
    # 0 = available; otherwise the year it becomes available
    return {"technology_years": {n: _s16(v) for n, v in zip(TECHNOLOGIES, w[462:479])}}


def misc_newspapers(w):
    papers = []
    for p in range(6):
        b = [v & 0xFF for v in w[916 + 5 * p:921 + 5 * p]]
        papers.append({
            "name": _pick(NEWSPAPER_NAMES, b[0]),
            "layout": b[1],
            "price_style": b[2],
            "opinion_column": _pick(OPINION_COLUMNS, b[3]),
            "weather_column": _pick(WEATHER_COLUMNS, b[4]),
        })
    stories = []
    for s in range(9):
        r = w[946 + 6 * s:952 + 6 * s]
        stories.append({
            "slot": s,
            "kind": "queue" if s < 7 else ("weather" if s == 7 else "opinion"),
            "type": _s16(r[0]),
            "priority": _s16(r[1]),
            "parameter": r[2] & 0xFF,
            "phrase_choices": [v & 0xFF for v in r[3:6]],
        })
    return {"newspapers": papers, "stories": stories}
@decoder(b"MISC")
def decode_misc(data):
    """Named fields of all 1200 words, page by page."""
    w = misc_words(data)
    out = {}
    for part in (misc_general, misc_population, misc_budget, misc_counts, misc_neighbours,
                 misc_demand, misc_technology, misc_newspapers):
        out.update(part(w))
    return out


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
    XTHG records and the SCEN disaster spot are turned with the maps."""
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
        if b"XTHG" in seg:
            seg[b"XTHG"] = bytearray(xthg_turn_cw(seg[b"XTHG"]))
        if b"SCEN" in seg:
            seg[b"SCEN"] = bytearray(scen_turn_cw(seg[b"SCEN"]))
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
    # Labels can hold characters the console's code page lacks; print those escaped.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="backslashreplace")
    args = [a for a in argv if not a.startswith("--")]
    if len(args) != 1:
        print(__doc__.strip(), file=sys.stderr)
        return 2
    c = read_file(args[0])
    if "--normalise" in argv:
        # The last occurrence of a repeated tag is the one the game uses; TEXT is never turned.
        used = {s.tag: s for s in c.segments if s.tag != b"TEXT"}
        turned = normalise({tag: s.data for tag, s in used.items()})
        for tag, s in used.items():
            s.data = turned[tag]
    if "--json" in argv:
        json.dump(to_dict(c), sys.stdout, indent=2)
        print()
    else:
        print_text(c)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
