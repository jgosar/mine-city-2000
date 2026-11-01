#!/usr/bin/env python3
"""Tally format facts over a corpus of SimCity 2000 cities.

The "N/M files" numbers quoted in docs/sc2-format come from this script.

Usage:
    py -3.11 -I sc2check.py DIR [DIR ...]            all checks
    py -3.11 -I sc2check.py DIR --only container     checks whose name starts with "container"

Every *.sc2 and *.scn below each DIR is read; identical files (same bytes) are
counted once. Each check yields short "fact" strings per file; the output
lists, per check, how many files produced each fact.
"""

import collections
import hashlib
import importlib.util
import os
import struct
import sys

_here = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location("sc2dump", os.path.join(_here, "sc2dump.py"))
sc2 = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(sc2)

CHECKS = []


def check(fn):
    CHECKS.append(fn)
    return fn


# --- container -------------------------------------------------------------

@check
def container_header(f):
    yield "FORM size = file size - 8" if f.c.form_size == f.c.file_size - 8 else \
        "FORM size = file size - 8 %+d" % (f.c.form_size - (f.c.file_size - 8))
    yield "no trailing bytes" if f.c.trailing == 0 else "trailing bytes"


@check
def container_tags(f):
    tags = [s.name for s in f.c.segments]
    for t in sorted(set(tags)):
        yield "has %s" % t
    dupes = sorted(t for t, n in collections.Counter(tags).items() if n > 1)
    yield "duplicate tags: %s" % (", ".join(dupes) or "none")


@check
def container_version(f):
    misc = next((s for s in f.c.segments if s.tag == b"MISC"), None)
    if misc is not None:
        yield "MISC int 0 = 0x%X" % sc2.u32be(misc.data, 0)


@check
def container_order(f):
    city = [s.name for s in f.c.segments if s.tag in sc2.CITY_SEGMENTS]
    yield "city segment order: " + " ".join(city)
    yield "scenario segments: " + (" ".join(s.name for s in f.c.segments
                                            if s.tag not in sc2.CITY_SEGMENTS) or "none")


@check
def container_odd_lengths(f):
    # parse_container walks without padding, so if such a file parses with no
    # trailing bytes, odd segments are followed directly by the next header
    odd = [s.name for s in f.c.segments if s.length % 2]
    yield "has odd-length segments (not padded)" if odd else "no odd-length segments"
    for t in sorted(set(odd)):
        yield "odd-length %s" % t


@check
def container_raw_lengths(f):
    for s in f.c.segments:
        if sc2.CITY_SEGMENTS.get(s.tag, 0) is None:
            yield "%s stored length %d" % (s.name, s.length)


@check
def container_scenario_ids(f):
    for s in f.c.segments:
        if s.tag in sc2.SCENARIO_SEGMENTS:
            yield "%s id bytes %s" % (s.name, s.body[:4].hex(" "))


# --- compression -----------------------------------------------------------

@check
def compression_exact(f):
    for s in f.c.segments:
        size = sc2.CITY_SEGMENTS.get(s.tag)
        if size:
            r = sc2.decode_rle(s.body, size)
            ok = r.complete and len(r.data) == size
            yield "%s decodes exactly" % s.name if ok else \
                "%s decodes to %d of %d, input complete=%s" % (s.name, len(r.data), size, r.complete)


@check
def compression_edge_controls(f):
    zeros = ones = 0
    for s in f.c.segments:
        size = sc2.CITY_SEGMENTS.get(s.tag)
        if size:
            r = sc2.decode_rle(s.body, size)
            zeros += r.literal_zero_ops
            ones += r.repeat_one_ops
    yield "uses control 0x00" if zeros else "never uses control 0x00"
    yield "uses control 0x80" if ones else "never uses control 0x80"


@check
def compression_run_lengths(f):
    """Longest literal and repeat runs: shows whether writers cap them."""
    lit = rep = 0
    for s in f.c.segments:
        size = sc2.CITY_SEGMENTS.get(s.tag)
        if not size:
            continue
        b, i = s.body, 0
        while i < len(b):
            c = b[i]
            if c < 0x80:
                lit = max(lit, c)
                i += 1 + c
            else:
                rep = max(rep, c - 0x7F)
                i += 2
    yield "longest literal run %d" % lit
    yield "longest repeat run %d" % rep


# --- helpers for the map checks ------------------------------------------------

def _seg(f, tag):
    return next((s.data for s in f.c.segments if s.tag == tag), None)


def _compass(f):
    return sc2.u32be(_seg(f, b"MISC"), 8)


def _altm(f):
    return struct.unpack(">16384H", _seg(f, b"ALTM"))


def _flood(sources, passable):
    """Tiles reachable from sources through 4-neighbours for which passable(index) holds."""
    seen = set(sources)
    todo = list(sources)
    while todo:
        row, col = divmod(todo.pop(), 128)
        for r, c in ((row - 1, col), (row + 1, col), (row, col - 1), (row, col + 1)):
            i = r * 128 + c
            if 0 <= r < 128 and 0 <= c < 128 and i not in seen and passable(i):
                seen.add(i)
                todo.append(i)
    return seen


# --- ALTM and XTER -------------------------------------------------------------

SLOPE_FROM_RAISED_CORNERS = (0x00, 0x09, 0x0A, 0x02, 0x0B, 0x0D, 0x03, 0x06,
                             0x0C, 0x01, 0x0D, 0x05, 0x04, 0x08, 0x07, None)
# neighbour (d_row, d_col) -> corners it raises when higher: 1 = row-/col-, 2 = row+/col-, 4 = row+/col+, 8 = row-/col+
NEIGHBOUR_CORNERS = ((0, -1, 0x3), (1, -1, 0x2), (1, 0, 0x6), (1, 1, 0x4),
                     (0, 1, 0xC), (-1, 1, 0x8), (-1, 0, 0x9), (-1, -1, 0x1))


@check
def altm_bits(f):
    v = _altm(f)
    yield "bit 15 never set" if all(x < 0x8000 for x in v) else "bit 15 set somewhere"
    top = max(x & 0x1F for x in v)
    yield "altitude <= 30" if top <= 30 else "altitude 31 present"
    depths = [x >> 10 & 0x1F for x in v if x >> 10 & 0x1F]
    yield "has tunnel depths" if depths else "no tunnel depths"


@check
def altm_water_height(f):
    v = _altm(f)
    x = _seg(f, b"XTER")
    sea = sc2.u32be(_seg(f, b"MISC"), 912 * 4)
    yield "sea level (MISC word 912) in 0..31" if sea < 32 else "sea level out of range"
    under = shore = surf = True
    dry_below = False
    for i in range(16384):
        a, w, t = v[i] & 0x1F, v[i] >> 5 & 0x1F, x[i]
        if t < 0x10 and a < sea:
            dry_below = True
        if 0x10 <= t < 0x20 and not (w == sea and w > a):
            under = False
        elif 0x20 <= t < 0x30 and not (w == sea and w == a + 1):
            shore = False
        elif t >= 0x30 and w != a:
            surf = False
    yield "underwater: water height = sea level > altitude" + ("" if under else " VIOLATED")
    yield "shore: water height = sea level = altitude + 1" + ("" if shore else " VIOLATED")
    yield "surface water: water height = altitude" + ("" if surf else " VIOLATED")
    yield "has dry land below sea level" if dry_below else "no dry land below sea level"


@check
def altm_tunnel_portals(f):
    v = _altm(f)
    b = _seg(f, b"XBLD")
    portals = [v[i] >> 10 & 0x1F for i in range(16384) if 0x3F <= b[i] <= 0x42]
    if portals:
        yield "tunnel portals have depth 1" if all(d == 1 for d in portals) else "tunnel portal with depth != 1"


@check
def xter_codes(f):
    codes = set(_seg(f, b"XTER"))
    bad = sorted(c for c in codes if sc2.xter_kind(c) == "unused")
    yield "only documented codes" if not bad else "undocumented codes: " + " ".join("%02X" % c for c in bad)


@check
def xter_water_bit(f):
    x = _seg(f, b"XTER")
    bit = _seg(f, b"XBIT")
    for kind in ("land", "underwater", "shore", "surface water"):
        tiles = [bit[i] & 4 for i in range(16384) if sc2.xter_kind(x[i]) == kind]
        if tiles:
            yield "%s: XBIT 0x04 %s" % (kind, "always set" if all(tiles) else "never set" if not any(tiles) else "mixed")


@check
def xter_slope_vs_altitude(f):
    v = _altm(f)
    x = _seg(f, b"XTER")
    alt = [a & 0x1F for a in v]
    total = same = 0
    for r in range(128):
        for c in range(128):
            t = x[r * 128 + c]
            if t >= 0x30:
                continue
            a = alt[r * 128 + c]
            mask = 0
            for dr, dc, m in NEIGHBOUR_CORNERS:
                rr, cc = r + dr, c + dc
                if 0 <= rr < 128 and 0 <= cc < 128 and alt[rr * 128 + cc] > a:
                    mask |= m
            total += 1
            same += SLOPE_FROM_RAISED_CORNERS[mask] == t & 0x0F
    share = 100.0 * same / total
    yield "slope matches neighbour heights on %s of tiles" % (
        "100%" if same == total else ">= 99%" if share >= 99 else ">= 95%" if share >= 95 else "< 95%")


# --- XBLD ------------------------------------------------------------------------

@check
def xbld_codes(f):
    for code in sorted(set(_seg(f, b"XBLD"))):
        yield "uses code 0x%02X" % code


@check
def xbld_footprints(f):
    """A multi-tile building is found from its anchor: the tile whose XZON
    corner bit is corner D (min row, max col). Its n x n block must hold the
    same code, with corner bits A, B, C, D at its corners and none inside.
    Tiles of multi-tile codes outside every such block are fragments."""
    xbld, xzon = _seg(f, b"XBLD"), _seg(f, b"XZON")
    A, B, C, D = sc2.corner_bits(_compass(f))
    covered = set()
    blocks = bad = 0
    for r in range(128):
        for c in range(128):
            code = xbld[r * 128 + c]
            n = sc2.XBLD_SIZE[code] if code >= 0x70 else 1
            if n < 2 or xzon[r * 128 + c] & 0xF0 != D:
                continue
            cells = [(r + i, c - j) for i in range(n) for j in range(n)]
            want = {(r, c): D, (r, c - n + 1): A, (r + n - 1, c - n + 1): B, (r + n - 1, c): C}
            if all(0 <= rr < 128 and 0 <= cc < 128 and xbld[rr * 128 + cc] == code
                   and xzon[rr * 128 + cc] & 0xF0 == want.get((rr, cc), 0) for rr, cc in cells):
                blocks += 1
                covered.update(cells)
            else:
                bad += 1
    fragments = sum(1 for r in range(128) for c in range(128)
                    if xbld[r * 128 + c] >= 0x70 and sc2.XBLD_SIZE[xbld[r * 128 + c]] >= 2 and (r, c) not in covered)
    yield "has multi-tile buildings" if blocks else "no multi-tile buildings"
    yield "every anchor starts a well-formed block" if bad == 0 else "some anchors start a malformed block"
    yield "no fragments" if fragments == 0 else "has fragments"
    if fragments:
        yield "fragments: %s" % ("1-5 tiles" if fragments <= 5 else "6-25 tiles" if fragments <= 25 else "over 25 tiles")


@check
def xbld_one_tile_buildings(f):
    xbld, xzon = _seg(f, b"XBLD"), _seg(f, b"XZON")
    nibbles = {xzon[i] & 0xF0 for i in range(16384) if xbld[i] >= 0x70 and sc2.XBLD_SIZE[xbld[i]] == 1}
    yield "1x1 buildings (0x70+) carry all four corner bits" if nibbles <= {0xF0} else \
        "some 1x1 buildings (0x70+) lack corner bits"


@check
def xbld_highways(f):
    """0x61-0x6B fill even-aligned uniform 2x2 blocks; 0x49-0x50 sit in
    even-aligned 2x2 blocks made only of highway codes."""
    xbld = _seg(f, b"XBLD")
    hwy = lambda v: 0x49 <= v <= 0x50 or 0x61 <= v <= 0x6B
    big_ok = small_ok = True
    seen = False
    for r in range(128):
        for c in range(128):
            code = xbld[r * 128 + c]
            if not hwy(code):
                continue
            seen = True
            block = [xbld[((r & ~1) + i) * 128 + (c & ~1) + j] for i in (0, 1) for j in (0, 1)]
            if code >= 0x61:
                big_ok &= all(b == code for b in block)
            else:
                small_ok &= all(hwy(b) for b in block)
    if seen:
        yield "0x61-0x6B in uniform even-aligned 2x2 blocks: %s" % big_ok
        yield "0x49-0x50 in even-aligned 2x2 highway blocks: %s" % small_ok
    else:
        yield "no highways"


# --- XZON and XUND -----------------------------------------------------------------

@check
def xzon_values(f):
    z = _seg(f, b"XZON")
    yield "zones only 0-9" if all((v & 0x0F) <= 9 for v in z) else "zone above 9"
    yield "corner bits only 0x00, single bit or 0xF0" if all((v & 0xF0) in (0, 0x10, 0x20, 0x40, 0x80, 0xF0) for v in z) \
        else "other corner bit combination"
    if any((v & 0x0F) == 7 for v in z):
        yield "has zone 7 (military)"


@check
def xzon_corner_rule(f):
    """Bit k sits at corner (k - compass) mod 4; the footprint (same XBLD code) continues away from it."""
    z, b, comp = _seg(f, b"XZON"), _seg(f, b"XBLD"), _compass(f)
    into = {0: (1, 1), 1: (-1, 1), 2: (-1, -1), 3: (1, -1)}
    B = lambda r, c: b[r * 128 + c] if 0 <= r < 128 and 0 <= c < 128 else -1
    bad = 0
    for r in range(128):
        for c in range(128):
            bits = z[r * 128 + c] & 0xF0
            if bits in (0x10, 0x20, 0x40, 0x80):
                dr, dc = into[((bits.bit_length() - 5) - comp) & 3]
                code = B(r, c)
                if not (B(r + dr, c) == code and B(r, c + dc) == code and B(r + dr, c + dc) == code):
                    bad += 1
    yield "every corner bit at its predicted corner" if bad == 0 else "some corner bits off the predicted corner"


@check
def xzon_footprint_walk(f):
    """Walking from corner 0 to corner 1 recovers each multi-tile footprint."""
    z, comp = _seg(f, b"XZON"), _compass(f)
    bit = lambda v: 0x10 << ((v + comp) & 3)
    H = lambda r, c: z[r * 128 + c] & 0xF0 if 0 <= r < 128 and 0 <= c < 128 else None
    bad = 0
    for r in range(128):
        for c in range(128):
            if H(r, c) != bit(0):
                continue
            size = next((k + 1 for k in range(4) if H(r + k, c) == bit(1)), None)
            if not (size and H(r, c + size - 1) == bit(3) and H(r + size - 1, c + size - 1) == bit(2)):
                bad += 1
    yield "every footprint recovered from corners" if bad == 0 else "some footprints not recoverable from corners"


@check
def xund_values(f):
    u = set(_seg(f, b"XUND"))
    yield "codes only 0x00-0x23" if max(u) <= 0x23 else "code above 0x23"
    for code, name in ((0x21, "0x21"), (0x22, "0x22 (silo)"), (0x23, "0x23 (station)")):
        if code in u:
            yield "has " + name
    if u & set(range(0x01, 0x10)):
        yield "has subway"
    if u & set(range(0x10, 0x1F)):
        yield "has pipes"


@check
def xund_station_under(f):
    u, b = _seg(f, b"XUND"), _seg(f, b"XBLD")
    stray = sum(1 for i in range(16384) if u[i] == 0x23 and b[i] != 0xE9 and not 0x6C <= b[i] <= 0x6F)
    if 0x23 in u:
        yield "every 0x23 under XBLD 0xE9 or 0x6C-0x6F" if stray == 0 else "0x23 elsewhere too"


@check
def xund_slope_xter(f):
    """A slope piece sits on the XTER slope with the matching low bits (1-4)."""
    u, t = _seg(f, b"XUND"), _seg(f, b"XTER")
    want = {0x03: 1, 0x04: 2, 0x05: 3, 0x06: 4, 0x12: 1, 0x13: 2, 0x14: 3, 0x15: 4}
    tiles = [i for i in range(16384) if u[i] in want]
    if tiles:
        bad = sum(1 for i in tiles if not (t[i] < 0x31 and (t[i] & 0x0F) == want[u[i]]))
        yield "every slope piece on its XTER slope" if bad == 0 else "some slope pieces off their XTER slope"


# --- XBIT and orientation -------------------------------------------------------------

@check
def xbit_bits_present(f):
    xbit = _seg(f, b"XBIT")
    for mask in (0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01):
        if any(v & mask for v in xbit):
            yield "has 0x%02X" % mask


@check
def xbit_implications(f):
    xbit = _seg(f, b"XBIT")
    xter = _seg(f, b"XTER")
    yield "0x40 implies 0x80" if all(v & 0x80 for v in xbit if v & 0x40) else "0x40 without 0x80"
    yield "0x10 implies 0x20" if all(v & 0x20 for v in xbit if v & 0x10) else "0x10 without 0x20"
    yield "0x04 only on water XTER (>= 0x10)" if all(xter[i] >= 0x10 for i in range(16384) if xbit[i] & 0x04) \
        else "0x04 on land XTER"
    yield "0x01 without 0x04 occurs" if any(v & 0x01 and not v & 0x04 for v in xbit) else "0x01 only with 0x04"


@check
def xbit_power_reach(f):
    xbit, xbld = _seg(f, b"XBIT"), _seg(f, b"XBLD")
    plants = [i for i in range(16384) if 0xC6 <= xbld[i] <= 0xCF and xbit[i] & 0x80]
    reach = _flood(plants, lambda i: xbit[i] & 0x80)
    yield "powered tiles reachable from a plant through 0x80" \
        if all(i in reach for i in range(16384) if xbit[i] & 0x40) else "some powered tiles unreachable"


@check
def xbit_water_reach(f):
    xbit, xbld = _seg(f, b"XBIT"), _seg(f, b"XBLD")
    sources = [i for i in range(16384) if xbld[i] in (0xDC, 0xFA) and xbit[i] & 0x20]
    reach = _flood(sources, lambda i: xbit[i] & 0x20)
    yield "watered tiles reachable from a pump or desalination plant through 0x20" \
        if all(i in reach for i in range(16384) if xbit[i] & 0x10) else "some watered tiles unreachable"


@check
def xbit_mask08(f):
    xbit, xbld, xzon = _seg(f, b"XBIT"), _seg(f, b"XBLD"), _seg(f, b"XZON")
    yield "no 0x08 outside rows/cols 0-63" if not any(
        xbit[i] & 0x08 for i in range(16384) if i // 128 >= 64 or i % 128 >= 64) else "0x08 outside rows/cols 0-63"
    got = [bool(xbit[r * 128 + c] & 0x08) for r in range(64) for c in range(64)]
    want = [any(xbld[(2 * r + a) * 128 + 2 * c + b] > 0x1C or xzon[(2 * r + a) * 128 + 2 * c + b] & 0x0F
                for a in (0, 1) for b in (0, 1)) for r in range(64) for c in range(64)]
    yield "quadrant equals the developed mask" if got == want else \
        "quadrant all clear" if not any(got) else "quadrant partly the mask"


@check
def xbit_rotate_codes(f):
    xbit, xbld = _seg(f, b"XBIT"), _seg(f, b"XBLD")
    for code in sorted({xbld[i] for i in range(16384) if xbit[i] & 0x02}):
        yield "0x02 on XBLD %02X" % code


@check
def xbit_rotate_direction(f):
    """Bridges: along row = flag. Runways/piers: along row = flag XOR odd compass.
    Only tiles with a same-kind neighbour along exactly one axis are judged."""
    xbit, xbld = _seg(f, b"XBIT"), _seg(f, b"XBLD")
    odd = _compass(f) & 1
    kinds = (("bridge", set(range(0x51, 0x5D)), set(range(0x51, 0x5D)), False),
             ("runway", {0xDD}, {0xDD, 0xDE}, True),
             ("pier", {0xDF}, {0xDF}, True))
    for name, codes, group, uses_compass in kinds:
        for i in range(16384):
            if xbld[i] not in codes:
                continue
            row, col = divmod(i, 128)

            def same(r, c):
                return 0 <= r < 128 and 0 <= c < 128 and xbld[r * 128 + c] in group
            along_row = same(row - 1, col) or same(row + 1, col)
            along_col = same(row, col - 1) or same(row, col + 1)
            if along_row == along_col:
                continue
            flag = bool(xbit[i] & 0x02)
            predicted = flag ^ bool(odd) if uses_compass else flag
            yield "%s rule holds" % name if predicted == along_row else "%s rule fails" % name


@check
def orientation_compass(f):
    yield "compass %d" % _compass(f)


# --- XTXT, XLAB and CNAM ------------------------------------------------------------

BUILTIN_MICROSIMS = {0x34: {0xEC}, 0x35: {0xED}, 0x36: {0xE9}, 0x37: {0xC8}, 0x38: {0xC6, 0xC7},
                     0x39: {0xD5}, 0x3A: {0xD4}, 0x3B: {0xF5}, 0x3C: {0xF8}}


@check
def xtxt_values(f):
    t = set(_seg(f, b"XTXT"))
    for lo, hi, name in ((0x01, 0x32, "signs 01-32"), (0x33, 0x33, "33"), (0x34, 0x3C, "built-in microsims 34-3C"),
                         (0x3D, 0xC8, "microsims 3D-C8"), (0xC9, 0xF0, "moving things C9-F0"),
                         (0xF1, 0xF9, "F1-F9"), (0xFA, 0xFA, "neighbour connections FA"), (0xFB, 0xFF, "FB-FF")):
        if any(lo <= v <= hi for v in t):
            yield "has " + name


@check
def xtxt_builtin_microsims(f):
    t, b = _seg(f, b"XTXT"), _seg(f, b"XBLD")
    tiles = [(t[i], b[i]) for i in range(16384) if t[i] in BUILTIN_MICROSIMS]
    if tiles:
        yield "built-in microsim values only on their building" if all(code in BUILTIN_MICROSIMS[v] for v, code in tiles) \
            else "built-in microsim value on another code"


@check
def xtxt_microsim_records(f):
    """A microsim value v (0x3D-0xC8) points to XMIC record v - 0x33, whose byte 0 is the building code."""
    t, b, mic = _seg(f, b"XTXT"), _seg(f, b"XBLD"), _seg(f, b"XMIC")
    tiles = [i for i in range(16384) if 0x3D <= t[i] <= 0xC8]
    if not tiles:
        return
    match = sum(1 for i in tiles if mic[(t[i] - 0x33) * 8] == b[i])
    stale = sum(1 for i in tiles if b[i] == 0)
    yield "every microsim tile matches its XMIC record" if match == len(tiles) else \
        "microsim tiles left on empty land" if match + stale == len(tiles) else "microsim tile on another building"


@check
def xtxt_things(f):
    """A value v in 0xC9-0xF0 sits on the tile stored in XTHG record v - 0xC9 (bytes 3, 4 = row, col)."""
    t, th = _seg(f, b"XTXT"), _seg(f, b"XTHG")
    tiles = [i for i in range(16384) if 0xC9 <= t[i] <= 0xF0]
    if not tiles:
        return
    ok = sum(1 for i in tiles if divmod(i, 128) == (th[(t[i] - 0xC9) * 12 + 3], th[(t[i] - 0xC9) * 12 + 4]))
    yield "every thing marker at its XTHG record's tile" if ok == len(tiles) else "some thing markers elsewhere"


@check
def xtxt_connections(f):
    t, b = _seg(f, b"XTXT"), _seg(f, b"XBLD")
    tiles = [i for i in range(16384) if t[i] == 0xFA]
    if not tiles:
        return
    near_edge = all(min(r, c, 127 - r, 127 - c) <= 1 for r, c in (divmod(i, 128) for i in tiles))
    transport = all(0x1D <= b[i] <= 0x6B for i in tiles)
    yield "0xFA only within one tile of the map edge" if near_edge else "0xFA away from the edge"
    yield "0xFA only on roads, rails and highways" if transport else "0xFA on another code"


@check
def xlab_slots(f):
    lab, t = _seg(f, b"XLAB"), _seg(f, b"XTXT")
    used = {s for s in range(256) if lab[s * 25]}
    yield "slot 0 (mayor) used" if 0 in used else "slot 0 empty"
    yield "slot 51 empty" if 51 not in used else "slot 51 used"
    yield "slots 52-60 all used" if all(s in used for s in range(52, 61)) else "some of slots 52-60 empty"
    yield "slots 201-249 empty" if not used & set(range(201, 250)) else "some of slots 201-249 used"
    yield "slots 251-255 all used" if all(s in used for s in range(251, 256)) else "some of slots 251-255 empty"
    signs = {v for v in t if 1 <= v <= 0x32}
    if signs:
        yield "every sign has its label" if all(lab[v * 25] for v in signs) else "a sign without label"
    if any(lab[s * 25] > 23 for s in range(256)):
        yield "a length byte above 23"


@check
def cnam_layout(f):
    cn = _seg(f, b"CNAM")
    if cn is None:
        yield "no CNAM"
        return
    tags = [s.tag for s in f.c.segments if s.tag in sc2.CITY_SEGMENTS]
    pos = "first" if tags[0] == b"CNAM" else "last" if tags[-1] == b"CNAM" else "middle"
    name = cn[1:].split(b"\0")[0]
    byte0 = "0x1F" if cn[0] == 0x1F else "the name length" if cn[0] == len(name) else "other"
    yield "CNAM %s, byte 0 = %s" % (pos, byte0)
    yield "bytes after the name all zero" if not any(cn[2 + len(name):]) else "bytes after the name not zero"


# --- overlays ----------------------------------------------------------------------

@check
def minimap_misc_totals(f):
    """MISC words 10, 11 and 13 hold the sums of XVAL, XCRM and XPLT; word 12 the traffic total."""
    misc = _seg(f, b"MISC")
    for word, tag in ((10, b"XVAL"), (11, b"XCRM"), (13, b"XPLT")):
        yield "MISC word %d == sum of %s" % (word, tag.decode()) if sc2.u32be(misc, 4 * word) == sum(_seg(f, tag)) \
            else "MISC word %d != sum of %s" % (word, tag.decode())
    traffic, total = sc2.u32be(misc, 48), sum(_seg(f, b"XTRF"))
    yield "MISC word 12 == sum of XTRF" if traffic == total else \
        "MISC word 12 < sum of XTRF" if traffic < total else "MISC word 12 > sum of XTRF"


@check
def minimap_xpop(f):
    xpop = _seg(f, b"XPOP")
    yield "XPOP values are multiples of 4 or 255" if all(v % 4 == 0 or v == 255 for v in xpop) \
        else "XPOP value not a multiple of 4"
    same = sum(a == b for a, b in zip(sc2.xpop_from_xbld(_seg(f, b"XBLD")), xpop))
    yield "XPOP == recomputed from XBLD in %s cells" % (
        "all" if same == 1024 else ">= 90% of" if same >= 922 else "< 90% of")


@check
def minimap_xrog(f):
    rog = set(_seg(f, b"XROG"))
    yield "XROG only 127/128 (no change anywhere)" if rog <= {127, 128} else "XROG has growth or decline"


@check
def minimap_xval_mask(f):
    """Where the XBIT 0x08 mask is present, land value is 0 outside it."""
    xval, xbit = _seg(f, b"XVAL"), _seg(f, b"XBIT")
    if not any(xbit[r * 128 + c] & 8 for r in range(64) for c in range(64)):
        yield "XBIT 0x08 mask absent"
        return
    yield "XVAL nonzero only on mask cells" if all(
        not xval[r * 64 + c] or xbit[r * 128 + c] & 8 for r in range(64) for c in range(64)) \
        else "XVAL nonzero outside the mask"


@check
def minimap_all_zero(f):
    """Cities that were never simulated: no traffic, pollution, value or crime yet."""
    if not any(sum(_seg(f, t)) for t in (b"XTRF", b"XPLT", b"XVAL", b"XCRM")):
        yield "64 x 64 overlays all zero"


# --- XGRP ---------------------------------------------------------------------------

def _xgrp(f):
    values = struct.unpack(">832i", _seg(f, b"XGRP"))
    return [values[i * 52:(i + 1) * 52] for i in range(16)]


def _last_updates(f):
    """Month counts (days // 25) of the last monthly, half-yearly and five-yearly graph update."""
    days = sc2.u32be(_seg(f, b"MISC"), 4 * 4)
    month = days // 25 if days % 25 >= 21 else days // 25 - 1      # graphs update on day 21 of 25
    half = month
    while half % 12 not in (0, 6):
        half -= 1
    five = half
    while not (five % 12 == 0 and (five // 12) % 5 == 0):
        five -= 1
    return month, half, five


@check
def xgrp_history(f):
    yield "no graph history (all zero)" if not any(_seg(f, b"XGRP")) else "has graph history"


@check
def xgrp_blocks(f):
    """Slot 12 is a copy of slot 0 made at the last January/July update, so it now equals the
    monthly slot that has aged by the months since; likewise slot 32 for the five-year block."""
    if not any(_seg(f, b"XGRP")):
        return
    ser = _xgrp(f)
    month, half, five = _last_updates(f)
    yield "slot 12 = monthly slot of the last half-year update" + (
        "" if all(s[12] == s[month - half] for s in ser) else ": no")
    k = (half - five) // 6
    if k <= 19:
        yield "slot 32 = half-year slot of the last five-year update" + (
            "" if all(s[32] == s[12 + k] for s in ser) else ": no")


@check
def xgrp_misc_links(f):
    if not any(_seg(f, b"XGRP")):
        return
    ser = _xgrp(f)
    misc = struct.unpack(">1200i", _seg(f, b"MISC"))
    for series, word, name in ((10, 18, "Health"), (11, 19, "Education"), (12, 1001, "Unemployment"),
                               (13, 21, "GNP"), (14, 20, "Nat'n Pop"), (15, 22, "Fed Rate")):
        yield "%s newest == MISC word %d: %s" % (name, word, ser[series][0] == misc[word])
    counts = misc[124:380]
    arcologies = (counts[0xFB] + counts[0xFC] + counts[0xFD] + counts[0xFE]) // 16
    bonus = (arcologies * 5 - 700) * 4000 if arcologies > 140 else 0
    yield "City Size newest == MISC 1032 + 10 x MISC 380: %s" % (ser[0][0] == misc[1032] + 10 * misc[380] + bonus)


@check
def xgrp_ranges(f):
    ser = _xgrp(f)
    neg = [i for i in range(16) if min(ser[i]) < 0]
    yield "negative values: " + (", ".join(sc2.XGRP_SERIES[i] for i in neg) or "none")
    yield "Water %, Unemployment within 0-100: " + str(all(0 <= v <= 100 for i in (9, 12) for v in ser[i]))
    yield "Power % at most 100: " + str(max(ser[8]) <= 100)


# --- XMIC ----------------------------------------------------------------------

def _xmic(f):
    data = _seg(f, b"XMIC")
    return [struct.unpack_from(">BBHHH", data, 8 * r) for r in range(150)]


@check
def xmic_fixed_records(f):
    r = _xmic(f)
    yield "record 0 byte 0 = 0x%02X" % r[0][0]
    yield "record 0 bytes 1-7 all zero: %s" % (r[0][1:] == (0, 0, 0, 0))
    expected = (0xEC, 0xED, 0xE9, 0xC8, (0xC6, 0xC7), 0xD5, 0xD4, 0xF5, 0xF8)
    ok = all(r[i + 1][0] in (e if isinstance(e, tuple) else (e,)) for i, e in enumerate(expected))
    yield "records 1-9 hold the nine city-wide systems: %s" % ok
    yield "record 5 (hydro) byte 0 = 0x%02X" % r[5][0]


@check
def xmic_own_records(f):
    r = _xmic(f)
    codes = [x[0] for x in r[10:] if x[0]]
    yield "records 10-149: only codes with a record of their own: %s" % all(
        c in sc2.MICROSIM_OWN for c in codes)
    xtxt = _seg(f, b"XTXT")
    on_map = set(v for v in xtxt if 0x3D <= v <= 0xC8)
    for i in range(10, 150):
        used, shown = r[i][0] != 0, (i + 0x33) in on_map
        if used != shown:
            yield "a record %s" % ("in use with no XTXT value on the map" if used
                                   else "free with its XTXT value on the map")
    xbld = _seg(f, b"XBLD")
    wrong = sum(1 for v, b in zip(xtxt, xbld) if 0x3D <= v <= 0xC8 and r[v - 0x33][0] not in (b, 0) and b)
    yield "tiles whose building differs from their record's byte 0: %s" % ("none" if not wrong else "some")


@check
def xmic_misc_links(f):
    r = _xmic(f)
    w = sc2.misc_words(_seg(f, b"MISC"))
    arco = sum(x[3] for x in r[1:] if 0xFB <= x[0] <= 0xFE)
    yield "MISC 1032 == sum of arcology residents (word 4): %s" % (w[1032] == arco)
    arrests = min(sum(x[4] for x in r[1:] if x[0] == 0xD2), 0xFFFF)
    yield "MISC 1038 == sum of police arrests (word 6): %s" % (w[1038] == arrests)
    prisons = sum(1 for b in _seg(f, b"XBLD") if b == 0xD8) // 16
    load = sum(x[4] for x in r[1:] if x[0] == 0xD8)
    expected = 0 if prisons < 1 or load // prisons > 79 else 1
    yield "MISC 1039 == 1 if prisons exist and are on average at most 79%% full: %s" % (w[1039] == expected)
    for x in r[1:]:
        if x[0] == 0xD7:
            yield "stadium team label slot (word 6) == 251 + sport (word 4): %s" % (x[4] == 251 + x[3])
            yield "stadium's sport bit set in MISC 1034: %s" % bool(x[3] < 5 and w[1034] >> x[3] & 1)


@check
def xmic_constants(f):
    r = _xmic(f)
    w = sc2.misc_words(_seg(f, b"MISC"))
    mw = {0xC9: 50, 0xCA: 220, 0xCB: 500, 0xCC: 50, 0xCD: 1600, 0xCE: 2500, 0xCF: 200}
    design = {0xFB: 55, 0xFC: 30, 0xFD: 45, 0xFE: 65}
    for x in r[10:]:
        if x[0] in mw:
            yield "power plant: max output == the plant's MW: %s" % (x[2] == mw[x[0]])
            yield "power plant: age 0-50: %s" % (x[1] <= 50)
        if x[0] in design:
            yield "arcology: design capacity == the type's: %s" % (x[2] == design[x[0]])
            yield "arcology: residents <= design x 1000 + 63: %s" % (x[3] <= design[x[0]] * 1000 + 63)
        if x[0] in (0xD1, 0xD6, 0xD9, 0xFB, 0xFC, 0xFD, 0xFE):
            yield "grade 0-12: %s" % (x[1] <= 12)
        if x[0] == 0xD3:
            yield "fire station: engines == firefighters / 16 + 1: %s" % (x[3] == x[2] // 16 + 1)
    yield "wind: output == 4 x generators: %s" % (r[4][3] == 4 * r[4][2])
    yield "hydro: output == 20 x generators: %s" % (r[5][3] == 20 * r[5][2])
    yield "bus: stations == bus station tiles / 4, buses == tiles: %s" % (
        (r[1][2], r[1][3]) == (w[124 + 0xEC] // 4, w[124 + 0xEC]))
    yield "subway: stations == subway station tiles: %s" % (r[3][2] == w[124 + 0xE9])
    yield "parks: acres == small park + park tiles: %s" % (r[6][3] == w[124 + 0x0D] + w[124 + 0xD5])


# --- XTHG (docs/sc2-format/segments/xthg.md) -----------------------------------

def _things(f):
    data = _seg(f, b"XTHG")
    return [data[i * 12:(i + 1) * 12] for i in range(40)]


@check
def xthg_types(f):
    recs = _things(f)
    yield "record 0 type %d" % recs[0][0]
    yield "types 1-16 only: %s" % all(r[0] <= 16 for r in recs)
    for t in sorted({r[0] for r in recs[1:] if r[0]}):
        yield "has type %d (%s)" % (t, sc2.THING_TYPES.get(t))
    free = [r for r in recs[1:] if not r[0]]
    yield "free records all zero: %s" % all(not any(r) for r in free)


@check
def xthg_markers(f):
    recs = _things(f)
    xtxt = _seg(f, b"XTXT")
    for i, r in enumerate(recs[1:], 1):
        if r[0]:
            ok = r[3] < 128 and r[4] < 128 and xtxt[r[3] * 128 + r[4]] == 0xC9 + i
            yield "type %d: XTXT at (byte 3, byte 4) == 0xC9 + record: %s" % (r[0], ok)
    for v in xtxt:
        if 0xC9 <= v <= 0xF0 and v != 0xC9 and recs[v - 0xC9][0] == 0:
            yield "XTXT marker of a free record"
            break


@check
def xthg_trains(f):
    recs = _things(f)
    for r in recs[1:]:
        if r[0] in (10, 12):
            car1 = r[2]
            ok1 = 0 < car1 < 40 and recs[car1][0] in (11, 13)
            ok2 = ok1 and 0 < recs[car1][2] < 40 and recs[recs[car1][2]][0] in (11, 13)
            yield "engine -> car -> car links valid: %s" % ok2
            yield "engine underground (12) with underground cars: %s" % (
                r[0] == 12 and ok2 and recs[car1][0] == 13) if r[0] == 12 else "engine above ground (10)"


@check
def xthg_fields(f):
    recs = _things(f)
    for r in recs[1:]:
        t = r[0]
        if not t:
            continue
        if t in (10, 11, 12, 13):
            yield "train: direction 0-3: %s" % (r[1] < 4)
        elif t == 9:
            yield "sailboat: direction 0-3: %s" % (r[1] < 4)
            yield "sailboat: byte 2 = %d" % r[2]
        else:
            yield "type %d: direction 0-7: %s" % (t, r[1] < 8)
            yield "type %d: sub-tile bytes 6, 7 within 0-16: %s" % (t, r[6] <= 16 and r[7] <= 16)
        if t in (1, 2, 3):
            yield "type %d: height (byte 5) = %d" % (t, r[5])


@check
def xthg_misc_link(f):
    w = sc2.misc_words(_seg(f, b"MISC"))
    recs = _things(f)
    v = w[1040]
    yield "MISC word 1040 = %s" % ("-1" if v == -1 else
                                   "record of type %d" % recs[v][0] if 0 <= v < 40 else str(v))


# --- SCEN and TMPL (docs/sc2-format/segments/scen.md) ---------------------------

@check
def scen_layout(f):
    data = _seg(f, b"SCEN")
    if data is None:
        yield "no SCEN"
        return
    body = data[4:]
    yield "SCEN id = 0x%X" % struct.unpack("<I", data[:4])[0]
    yield "SCEN body (after the id) = %d bytes" % len(body)
    if len(body) == 48:
        yield "48-byte body: bytes 30-47 all zero (both layouts read the same): %s" % (not any(body[30:]))
    if len(body) > 48:
        yield "52-byte body: bytes 48-51 all zero: %s" % (not any(body[48:52]))


@check
def scen_values(f):
    data = _seg(f, b"SCEN")
    if data is None:
        return
    s = sc2.decode_scen(data)
    yield "disaster = %d (%s)" % (s["disaster"], s["disaster_name"])
    yield "time limit (months) = %d" % s["time_limit_months"]
    if s["disaster"]:
        r, c = s["disaster_row"], s["disaster_col"]
        yield "disaster spot inside the map: %s" % (r < 128 and c < 128)
        if s["disaster"] == 9:
            yield "meltdown spot on a nuclear plant tile (XBLD 0xCB) in stored coordinates: %s" % (
                _seg(f, b"XBLD")[r * 128 + c] == 0xCB)
    goals = [k for k in s if k.endswith(("_goal", "_limit")) and s[k]] + [
        "build_item_%d" % k for k in (1, 2) if s["build_item_%d" % k]]
    yield "goals set: %s" % (", ".join(goals) or "none")


@check
def scen_goals_at_load(f):
    data = _seg(f, b"SCEN")
    if data is None:
        return
    s = sc2.decode_scen(data)
    g = lambda k: s.get(k) or 0
    w = sc2.misc_words(_seg(f, b"MISC"))
    met = (g("city_size_goal") <= w[1035] and g("residential_goal") <= w[479] and
           g("commercial_goal") <= w[506] and g("industrial_goal") <= w[533] and
           g("funds_goal") <= w[5] - w[6] and g("land_value_goal") <= w[10] and
           g("life_expectancy_goal") <= w[18] and g("education_goal") <= w[19] and
           not 0 < g("pollution_limit") < w[13] and not 0 < g("crime_limit") < w[11] and
           not 0 < g("traffic_limit") < w[12] and
           all(not s["build_item_%d" % k] or w[124 + s["build_item_%d" % k]] >= g("build_item_%d_tiles" % k)
               for k in (1, 2)))
    yield "goals already met by the stored MISC values: %s" % met


@check
def tmpl_fields(f):
    data = _seg(f, b"TMPL")
    if data is None:
        yield "no TMPL"
        return
    t = sc2.decode_tmpl(data)
    yield "TMPL id = 0x%X, %d fields" % (t["id"], len(t["fields"]))
    yield "TMPL fields add up to %d bytes" % sum(x["bytes"] or 0 for x in t["fields"])
    yield "TMPL types only DBYT/DWRD/DLNG: %s" % all(x["bytes"] for x in t["fields"])
    scen = _seg(f, b"SCEN")
    yield "file has SCEN: %s, SCEN body = %s bytes" % (scen is not None, len(scen) - 4 if scen else "-")


# --- TEXT and PICT (docs/sc2-format/segments/text.md, pict.md) ----------------

def _scenario(f, tag):
    return [s for s in f.c.segments if s.tag == tag]


@check
def text_segments(f):
    texts = _scenario(f, b"TEXT")
    if not texts:
        return
    ids = sorted(sc2.scenario_id(s.data) for s in texts)
    yield "TEXT ids %s" % " ".join("0x%02X" % i for i in ids)
    for s in texts:
        body = s.data[4:]
        name = "TEXT 0x%02X" % sc2.scenario_id(s.data)
        yield "%s: at most 768 bytes: %s" % (name, len(body) <= sc2.TEXT_LIMIT)
        yield "%s: bytes 0x20-0x7E, CR and tab only: %s" % (
            name, all(0x20 <= b < 0x7F or b in (0x0D, 0x09) for b in body))
        yield "%s: contains CR: %s" % (name, b"\r" in body)
        yield "%s: contains LF or zero: %s" % (name, b"\n" in body or b"\0" in body)


@check
def pict_segments(f):
    picts = _scenario(f, b"PICT")
    if not picts:
        return
    yield "PICT segments: %d" % len(picts)
    for s in picts:
        d = sc2.decode_pict(s.data)
        body = s.data[4:]
        yield "PICT id 0x%02X" % d["id"]
        yield "PICT stored size %dx%d, body %d bytes" % (d["stored_width"], d["stored_height"], len(body))
        yield "PICT body covers the 65 x 65 pixels the game reads: %s" % (
            all(v is not None for row in d["pixels"] for v in row))
        yield "PICT body == 4 + height x (width + 1): %s" % (
            len(body) == 4 + d["stored_height"] * (d["stored_width"] + 1))
        yield "PICT row ends %s" % d["row_ends"]
        px = [v for row in d["pixels"] for v in row]
        yield "PICT pixels 0-154 or 254 only: %s" % all(v <= 154 or v == 254 for v in px)
        edge = {v for v in d["pixels"][0] + d["pixels"][-1] + [r[0] for r in d["pixels"]]}
        yield "PICT top, bottom and left edge one value: %s" % (
            "%d" % edge.pop() if len(edge) == 1 else "no")


# --- MISC: raw words as stored (the decoders in sc2dump see misc_words) -----

def _misc(f):
    return struct.unpack(">1200i", _seg(f, b"MISC"))


# --- MISC: general words (docs/sc2-format/segments/misc/general.md) -------------

REWARD_THRESHOLDS = (2000, 10000, 30000, 60000, 90000, 120000, 500000, 1000000)   # SIMCITY.EXE 0x4e6984


def _win95_cnam(f):
    """True for files whose CNAM has the Windows 95 form: first, byte 0 = 0x1F."""
    tags = [s.tag for s in f.c.segments]
    cnam = _seg(f, b"CNAM")
    return cnam is not None and cnam[0] == 0x1F and tags.index(b"CNAM") < tags.index(b"MISC")


@check
def misc_general_ranges(f):
    w = _misc(f)
    yield "word 1 (mode) = %d" % w[1]
    yield "word 3 (founding year) in 1900/1950/2000/2050: %s" % (w[3] in (1900, 1950, 2000, 2050))
    yield "word 7 (difficulty) = %d" % w[7]
    yield "word 6 (bonds) <= 50: %s" % (0 <= w[6] <= 50)
    yield "word 22 (fed rate) = 0 or 1..25: %s" % (w[22] == 0 or 1 <= w[22] <= 25)
    yield "word 23 (national trend) in 0..3: %s" % (0 <= w[23] <= 3)
    yield "word 27 (weather) in 0..11: %s" % (0 <= w[27] <= 11)
    yield "words 14, 15, 28 all zero: %s" % (w[14] == w[15] == w[28] == 0)
    yield "word 30 (rewards mask) < 32: %s" % (0 <= w[30] < 32)
    yield "word 915 (military base) in 0..5: %s" % (0 <= w[915] <= 5)
    yield "word 1019 (speed) in 1..5: %s" % (1 <= w[1019] <= 5)
    flags = [w[i] for i in (911, 913, 914, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1039, 1043)]
    yield "flag words only 0/1: %s" % all(v in (0, 1) for v in flags) if all(v in (0, 1, 255) for v in flags) \
        else "flag word outside 0/1/255"
    if any(v == 255 for v in flags):
        yield "flag word = 255 (Windows 95 CNAM form: %s)" % _win95_cnam(f)
    yield "word 1040 (disaster thing) = -1: %s" % (w[1040] == -1)
    yield "word 1042 (disaster active) = 0: %s" % (w[1042] == 0)


@check
def misc_general_rewards(f):
    w = _misc(f)
    reached = sum(1 for t in REWARD_THRESHOLDS if t < w[1035])
    yield "word 8 (reward level) %s thresholds passed by word 1035" % (
        "==" if w[8] == reached else "<" if w[8] < reached else ">")
    yield "rewards mask only has bits of levels reached: %s" % (
        all(w[8] >= (1, 2, 3, 5, 6)[b] for b in range(5) if w[30] >> b & 1))


@check
def misc_general_links(f):
    w = _misc(f)
    xgrp = struct.unpack(">832i", _seg(f, b"XGRP"))
    yield "word 29 == Residents graph slot 1: %s (Windows 95 CNAM form: %s)" % (w[29] == xgrp[52 + 1], _win95_cnam(f))
    yield "word 1035 == 10 x word 380: %s" % (w[1035] == 10 * w[380])
    xbld = _seg(f, b"XBLD")
    n = rows = cols = 0
    for i, code in enumerate(xbld):
        if code >= 0x70:
            n += 1
            rows += i // 128
            cols += i % 128
    yield "words 1030, 1031 == building centre: %s" % (
        (w[1030], w[1031]) == (rows // (2 * (n + 1)) * 2, cols // (2 * (n + 1)) * 2))
    xund, xzon = _seg(f, b"XUND"), _seg(f, b"XZON")
    subway = sum(1 for u, z in zip(xund, xzon)
                 if (0x01 <= u <= 0x0F or u in (0x1F, 0x20, 0x22, 0x23)) and z & 0x0F != 7)
    yield "word 1018 (subway tiles) %s count from XUND" % (
        "==" if w[1018] == subway else "<" if w[1018] < subway else ">")


@check
def misc_general_other_version(f):
    w = _misc(f)
    tail = any(w[1044:1200])
    yield "words 1044+ non-zero: %s, Windows 95 CNAM form: %s" % (tail, _win95_cnam(f))
    yield "words 1071+ all zero: %s" % (not any(w[1071:1200]))
    if not tail:
        return
    xgrp = struct.unpack(">832i", _seg(f, b"XGRP"))
    yield "word 1048 == 100 - newest Power %% graph value: %s" % (w[1048] == 100 - xgrp[8 * 52])
    yield "word 1049 == 100 - newest Water %% graph value: %s" % (w[1049] == 100 - xgrp[9 * 52])
    xbld, xzon = _seg(f, b"XBLD"), _seg(f, b"XZON")
    developed = sum(1 for b, z in zip(xbld, xzon) if b > 0x1C or z & 0x0F)
    yield "word 1067 %s developed tiles" % (
        "==" if w[1067] == developed else "within 1% of" if abs(w[1067] - developed) * 100 <= developed else "!=")
    xtxt = _seg(f, b"XTXT")
    yield "word 1044 == number of XTXT 0xFA tiles: %s" % (w[1044] == sum(1 for v in xtxt if v == 0xFA))


# --- MISC: population, industry and bonds ---------------------------------------

@check
def misc_population_age_groups(f):
    w = _misc(f)
    people = [w[31 + 3 * i] for i in range(20)]
    education = [w[32 + 3 * i] for i in range(20)]
    health = [w[33 + 3 * i] for i in range(20)]
    yield "no value negative: %s" % (min(people + education + health) >= 0)
    if not any(people):
        yield "age groups all zero"
        return
    yield "sum of people == word 1035: %s" % (sum(people) == w[1035])
    workers = sum(people[4:11])
    if workers:
        yield "word 17 == workers x 100 / (word 1035 + 1): %s" % (w[17] == workers * 100 // ((w[1035] & 0xFFFFFFFF) + 1))
        yield "word 18 == health of ages 20-54 / workers: %s" % (w[18] == sum(health[4:11]) // workers)
        yield "word 19 == education of ages 20-54 / workers: %s" % (w[19] == sum(education[4:11]) // workers)


@check
def misc_population_zones(f):
    w = _misc(f)
    step = w[4] % 25
    win95 = not any(w[1044:])
    if step <= 2:
        yield "steps 0-2, %s: words 380-387 all zero: %s" % (
            "Windows 95 save" if win95 else "other version", not any(w[380:388]))
        return
    yield "word 380 == sum of 381-386, step %s: %s" % (
        ">= 21" if step >= 21 else "3-20", w[380] == sum(w[381:387]))
    if step < 19:
        return
    want = sc2.zone_population_from_map(_seg(f, b"XBLD"), _seg(f, b"XZON"), w[2])
    have = w[381:388]
    if list(have) == want[1:]:
        yield "steps 19-24: words 381-387 == recount from XBLD/XZON: exact"
    elif abs(sum(have) - sum(want[1:])) <= sum(want[1:]) // 50:
        yield "steps 19-24: words 381-387 == recount from XBLD/XZON: total within 2%"
    else:
        yield "steps 19-24: words 381-387 == recount from XBLD/XZON: further off"


@check
def misc_population_industry(f):
    w = _misc(f)
    demand = [w[91 + 3 * k] for k in range(11)]
    tax = [w[92 + 3 * k] for k in range(11)]
    size = [w[93 + 3 * k] for k in range(11)]
    yield "tax rates 0-20: %s" % all(0 <= t <= 20 for t in tax)
    yield "tax rates all 7 (new-city default): %s" % all(t == 7 for t in tax)
    yield "demand 0-127: %s" % all(0 <= d <= 127 for d in demand)
    yield "sizes >= 0: %s" % all(s >= 0 for s in size)
    total = sum(size)
    if total:
        heavy = (size[0] + size[1] + size[2] + size[5]) * 100 // (total + 1)
        level = -1 if heavy < 20 else (heavy - 20) // 30
        yield "word 1037 == heavy-industry level from the sizes: %s" % (w[1037] == level)


@check
def misc_population_bonds(f):
    w = _misc(f)
    count = w[6]
    yield "bond count 0-50: %s" % (0 <= count <= 50)
    rates = w[388:438]
    if count:
        yield "rates of outstanding bonds 1-20: %s" % all(1 <= r <= 20 for r in rates[:max(0, count)])
    yield "slots after the last bond all zero: %s" % (not any(rates[max(0, count):]))


# --- MISC budget and ordinances --------------------------------------------------

def _kind(f):
    return "scenario" if f.path.lower().endswith(".scn") else "city"


def _r(a, b):
    return range(a, b + 1)


# XBLD codes the monthly update (0x4734a0) counts for each transit entry
BUDGET_TRANSIT_CODES = {
    "roads": list(_r(0x1D, 0x2B)) + list(_r(0x3F, 0x46)) + [0x4B, 0x4C] + list(_r(0x5D, 0x60)),
    "highways": list(_r(0x49, 0x50)) + list(_r(0x61, 0x6B)),
    "bridges": list(_r(0x51, 0x5C)) + [0x6A, 0x6B],
    "rail": list(_r(0x2C, 0x3E)) + list(_r(0x45, 0x48)) + [0x4D, 0x4E] + list(_r(0x6C, 0x6F)),
    "tunnels": list(_r(0x3F, 0x42)),
}


def _ordinance_amount(bit, budget, w):
    """Monthly amount of one ordinance (0x42bca0); C division truncates toward zero."""
    res = budget["residential_tax"]["count"]
    com = budget["commercial_tax"]["count"]
    ind = budget["industrial_tax"]["count"]
    q = lambda a, d: int(a / d)
    return (com, res, 2 * com, q(res, 2),
            q(res, -3), q(com, -6), -q(res, 2), -q(res, 4),
            q(res, -6), q(res, -5), q(res, -6), q(res, -3),
            -com, -ind, -q(res, 4), q(com, -3),
            -(w[1032] + w[1035]), 0, -q(res, 2), -ind)[bit]


@check
def misc_budget_year_to_date(f):
    w = _misc(f)
    budget = sc2.misc_budget(w)["budget"]
    month = w[4] // 25 % 12
    last = 11 if w[911] else month
    ok = all(e["year_to_date"] == sum(m["count"] * m["rate"] for m in e["months"][:last + 1])
             for e in budget.values())
    yield "%s: year_to_date == sum of count x rate over months 0..current: %s" % (_kind(f), ok)


@check
def misc_budget_current_month(f):
    w = _misc(f)
    budget = sc2.misc_budget(w)["budget"]
    m = w[4] // 25 % 12
    for name in ("bonds", "police", "fire", "health", "schools", "colleges"):
        e = budget[name]
        yield "%s: month slot == current count and rate: %s" % (
            name, (e["months"][m]["count"], e["months"][m]["rate"]) == (e["count"], e["rate"]))


@check
def misc_budget_counts(f):
    w = _misc(f)
    budget = sc2.misc_budget(w)["budget"]
    tiles = w[124:380]
    expected = {
        "bonds": w[6],
        "police": tiles[0xD2] // 9, "fire": tiles[0xD3] // 9, "health": tiles[0xD1] // 9,
        "schools": tiles[0xD6] // 9, "colleges": tiles[0xD9] // 16,
        "subway": tiles[0xE9] + w[1018],
    }
    for name, codes in BUDGET_TRANSIT_CODES.items():
        expected[name] = sum(tiles[c] for c in codes)
    expected["roads"] += tiles[0xEC] // 4 * 250
    expected["rail"] += tiles[0xED]
    for name, n in expected.items():
        yield "%s count from MISC tile counts: %s" % (name, budget[name]["count"] == n)


@check
def misc_budget_bond_rate(f):
    w = _misc(f)
    n = w[6]
    if n:
        rate = sc2.misc_budget(w)["budget"]["bonds"]["rate"]
        yield "bonds rate == sum of bond rates x 10000 / count: %s" % (rate == int(sum(w[388:388 + n]) * 10000 / n))


@check
def misc_budget_ordinance_total(f):
    w = _misc(f)
    if w[4] % 25 >= 21:
        yield "saved on day 21 or later (tax bases changed since the monthly update)"
        return
    budget = sc2.misc_budget(w)["budget"]
    total = sum(_ordinance_amount(b, budget, w) for b in range(20) if w[1000] >> b & 1)
    yield "ordinances count == sum of enacted ordinance amounts: %s" % (budget["ordinances"]["count"] == total)


@check
def misc_budget_tax_base(f):
    w = _misc(f)
    if w[4] % 25 < 21:
        yield "saved before day 21 (population words refilled since the last tax-base update)"
        return
    budget = sc2.misc_budget(w)["budget"]
    ok = all(budget[n]["count"] == (w[381 + 2 * i] + w[382 + 2 * i]) * 10 + w[1032] // (6 if i == 0 else 12)
             for i, n in enumerate(("residential_tax", "commercial_tax", "industrial_tax")))
    yield "tax bases == (MISC 381+382, 383+384, 385+386) x 10 + share of MISC 1032: %s" % ok


@check
def misc_budget_rates(f):
    w = _misc(f)
    budget = sc2.misc_budget(w)["budget"]
    taxes = [budget[n]["rate"] for n in ("residential_tax", "commercial_tax", "industrial_tax")]
    yield "tax rates within 0-20: %s" % all(0 <= r <= 20 for r in taxes)
    funding = [budget[n]["rate"] for n in sc2.BUDGET_ENTRIES[5:]]
    yield "funding within 0-100: %s" % all(0 <= r <= 100 for r in funding)
    yield "funding a multiple of 5: %s" % all(r % 5 == 0 for r in funding)
    yield "ordinances rate: %d" % budget["ordinances"]["rate"]
    industries = sum(w[92 + 3 * k] for k in range(11))
    yield "%s: industrial tax rate == average of the 11 industry rates: %s" % (
        _kind(f), taxes[2] == int((industries + 6) / 11))


@check
def misc_budget_year_end_flag(f):
    w = _misc(f)
    yield "word 911 = %d in month %d" % (w[911], w[4] // 25 % 12) if w[911] else "word 911 = 0"


@check
def misc_ordinance_flags(f):
    w = _misc(f)
    yield "bits 20-31 clear: %s" % (w[1000] >> 20 == 0)
    yield "%s: any ordinance enacted: %s" % (_kind(f), w[1000] != 0)


# --- MISC: tile counts, neighbours, demand, technology, newspapers ------------



def _recount(f):
    """Per-code counts outside zone 7, and military counts inside it (as 0x434f80 / 0x441f00)."""
    xbld, xzon = _seg(f, b"XBLD"), _seg(f, b"XZON")
    index = {c: i for i, c in enumerate(sc2.MILITARY_CODES) if c is not None}
    counts, military = [0] * 256, [0] * 16
    for code, zone in zip(xbld, xzon):
        if zone & 0x0F == 7:
            military[index.get(code, 0)] += 1
        else:
            counts[code] += 1
    return counts, military


@check
def misc_counts_buildings(f):
    w = _misc(f)
    counts, _ = _recount(f)
    stored = [sc2._s16(v) for v in w[124:380]]
    yield "stored building counts equal a recount without zone 7: %s" % ("yes" if stored == counts else "no")
    if any(v > 32767 for v in w[124:380]):
        yield "some building count stored as 32768..65535 (negative, not sign-extended)"


@check
def misc_counts_military(f):
    w = _misc(f)
    _, military = _recount(f)
    stored = [sc2._s16(v) for v in w[1002:1018]]
    yield "stored military counts equal a recount of zone 7: %s" % ("yes" if stored == military else "no")
    yield "military counts: %s" % ("some non-zero" if any(stored) else "all 0")


@check
def misc_neighbours_ids(f):
    w = _misc(f)
    ids = [w[438 + 4 * k] for k in range(4)]
    yield "name ids within 0..36: %s" % all(0 <= i <= 36 for i in ids)
    named = [i for i in ids if i]
    yield "non-zero name ids all different: %s" % (len(set(named)) == len(named))
    yield "neighbours with id 0 (Ocean): %s" % (tuple(k for k in range(4) if ids[k] == 0) or "none")
    if ids[0] == 0:
        yield "Ocean neighbour: population %s, economy %s, MISC word 913 %s" % (
            w[439] == 0 and "0" or "non-zero", w[440] == 0 and "0" or "non-zero",
            "set" if w[913] else "0")
    yield "fourth word of every neighbour 0: %s" % all(w[441 + 4 * k] == 0 for k in range(4))


@check
def misc_neighbours_ocean_edge(f):
    # On a coast map, the edge with the most salt water should be neighbour 0's edge:
    # stored edge e (0 row 127, 1 col 127, 2 row 0, 3 col 0) holds neighbour (e + compass) & 3.
    w = _misc(f)
    if w[438] != 0:
        return
    xbit = _seg(f, b"XBIT")
    edges = (
        [127 * 128 + c for c in range(128)], [r * 128 + 127 for r in range(128)],
        [c for c in range(128)], [r * 128 for r in range(128)])
    sea = [sum(1 for i in e if xbit[i] & 0x05 == 0x05) for e in edges]
    best = sea.index(max(sea))
    yield "saltiest edge holds neighbour %d" % ((best + _compass(f)) & 3)


@check
def misc_demand_values(f):
    w = _misc(f)
    yield "R, C, I demand within -2000..2000: %s" % all(-2000 <= v <= 2000 for v in w[454:457])
    yield "words 457-461 all 0: %s" % (not any(w[457:462]))


TECH_BASE_YEARS = (1940, 1950, 1980, 1970, 2020, 2050, 1920, 1920, 1910, 1900,
                   1925, 1980, 1990, 2040, 2090, 2140, 2190)


@check
def misc_technology_years(f):
    w = _misc(f)
    now = w[3] + w[4] // 300
    if not any(w[462:479]):
        yield "all 17 entries 0 (everything available)"
    for base, year in zip(TECH_BASE_YEARS, w[462:479]):
        if year == 0:
            continue
        if not base <= year <= base + 19:
            yield "a year outside base..base+19"
        elif year > now:
            yield "non-zero years: some in the future"
        else:
            yield "non-zero years: some already due"


@check
def misc_newspapers_papers(f):
    w = _misc(f)
    for field, name in enumerate(("name", "layout", "price style", "opinion", "weather")):
        col = sorted(w[916 + 5 * p + field] for p in range(6))
        if col == [0, 1, 2, 3, 4, 5]:
            yield "%s column is a permutation of 0..5" % name
        elif col == [0, 0, 1, 1, 2, 2]:
            yield "%s column is 0, 0, 1, 1, 2, 2" % name
        else:
            yield "%s column is something else" % name


@check
def misc_newspapers_stories(f):
    w = _misc(f)
    slots = [w[946 + 6 * s:952 + 6 * s] for s in range(9)]
    pri = [s[1] for s in slots[:7]]
    yield "slots 0-6 sorted by priority: %s" % all(pri[i] >= pri[i + 1] for i in range(6))
    yield "slot 7 (type, priority): %s" % ("weather (0, 0)" if slots[7][:2] == (0, 0) else str(slots[7][:2]))
    yield "slot 8 type: %s" % ("42..45" if 42 <= slots[8][0] <= 45 else str(slots[8][0]))
    for s in slots[:7]:
        if s[3:] != (255, 255, 255):
            yield "some queued story has phrase choices made"
            break


# --- driver ----------------------------------------------------------------

class Parsed:
    def __init__(self, path, raw):
        self.path = path
        self.raw = raw
        self.c = sc2.parse_container(raw)
        for s in self.c.segments:
            sc2.decode_segment(s)


def find_files(roots):
    seen = set()
    for root in roots:
        for dirpath, _, names in os.walk(root):
            for n in sorted(names):
                if n.lower().endswith((".sc2", ".scn")):
                    p = os.path.join(dirpath, n)
                    with open(p, "rb") as fh:
                        raw = fh.read()
                    h = hashlib.sha1(raw).hexdigest()
                    if h not in seen:
                        seen.add(h)
                        yield p, raw


def main(argv):
    only = None
    roots = []
    it = iter(argv)
    for a in it:
        if a == "--only":
            only = next(it)
        else:
            roots.append(a)
    if not roots:
        print(__doc__.strip(), file=sys.stderr)
        return 2

    checks = [c for c in CHECKS if only is None or c.__name__.startswith(only)]
    tallies = {c.__name__: collections.Counter() for c in checks}
    examples = {}
    total = 0
    failed = []
    for path, raw in find_files(roots):
        try:
            f = Parsed(path, raw)
        except sc2.Sc2FormatError as e:
            failed.append((path, str(e)))
            continue
        total += 1
        for c in checks:
            for fact in set(c(f)):
                tallies[c.__name__][fact] += 1
                examples.setdefault((c.__name__, fact), os.path.basename(path))

    print("%d files parsed, %d rejected" % (total, len(failed)))
    for path, err in failed:
        print("  rejected %s: %s" % (path, err))
    for name, counter in tallies.items():
        print("\n[%s]" % name)
        for fact, n in sorted(counter.items(), key=lambda kv: (-kv[1], kv[0])):
            print("  %4d/%d  %s   (e.g. %s)" % (n, total, fact, examples[(name, fact)]))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
