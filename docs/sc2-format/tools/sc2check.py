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
