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
