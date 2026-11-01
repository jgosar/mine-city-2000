#!/usr/bin/env python3
"""Check that every relative link and #anchor in docs/sc2-format resolves.

Usage:
    py -3.11 -I linkcheck.py [DOCS_DIR]      (default: the folder above this script)

Anchors are generated the way GitHub does it: lower-case the heading, drop
everything except letters, digits, spaces, hyphens and underscores, turn
spaces into hyphens, and add -1, -2, ... to repeats.
"""

import os
import re
import sys

LINK = re.compile(r"(?<!!)\[[^\]]*\]\(([^)\s]+)\)")
HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")


def strip_code(lines):
    """Yield (line_number, line) outside fenced code blocks, with inline code removed."""
    fenced = False
    for n, line in enumerate(lines, 1):
        if line.lstrip().startswith("```"):
            fenced = not fenced
            continue
        if not fenced:
            yield n, re.sub(r"`[^`]*`", lambda m: m.group(0) if "](" not in m.group(0) else "", line)


def slug(text):
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)   # links -> their text
    text = text.replace("`", "").strip().lower()
    text = re.sub(r"[^\w\- ]", "", text)
    return text.replace(" ", "-")


def anchors(path):
    seen = {}
    result = set()
    with open(path, encoding="utf-8") as f:
        for _, line in strip_code(f.read().splitlines()):
            m = HEADING.match(line)
            if m:
                s = slug(m.group(2))
                n = seen.get(s, 0)
                seen[s] = n + 1
                result.add(s if n == 0 else "%s-%d" % (s, n))
    return result


def main(argv):
    root = os.path.abspath(argv[0] if argv else os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
    cache = {}
    errors = 0
    files = 0
    for dirpath, _, names in os.walk(root):
        for name in sorted(names):
            if not name.endswith(".md"):
                continue
            files += 1
            path = os.path.join(dirpath, name)
            with open(path, encoding="utf-8") as f:
                lines = f.read().splitlines()
            for n, line in strip_code(lines):
                for target in LINK.findall(line):
                    if re.match(r"^[a-z]+:", target):
                        continue                      # http:, https:, mailto:
                    file_part, _, anchor = target.partition("#")
                    dest = os.path.normpath(os.path.join(dirpath, file_part)) if file_part else path
                    where = "%s:%d" % (os.path.relpath(path, root), n)
                    if not os.path.exists(dest):
                        print("%s: missing file %s" % (where, target))
                        errors += 1
                        continue
                    if anchor:
                        if not dest.endswith(".md"):
                            continue
                        if dest not in cache:
                            cache[dest] = anchors(dest)
                        if anchor not in cache[dest]:
                            print("%s: missing anchor %s" % (where, target))
                            errors += 1
    print("%d files checked, %d broken links" % (files, errors))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
