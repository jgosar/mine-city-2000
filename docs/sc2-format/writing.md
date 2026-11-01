# Writing files (out of scope for now)

These pages concentrate on **reading**. Writing a file that the game accepts and simulates correctly is a bigger problem: the segments must agree with each other, and some stored values are trusted while others are recomputed.

This page collects what has turned up so far, so that work can start from it later. **It is not a complete guide.**

## Container

- Write `FORM`, the size (file size − 8, big-endian), and `SCDH`; then the segments with no padding ([container.md](container.md)).
- MISC word 0 must be 0x122 ([container.md § Version check](container.md#version-check)).
- Write all 20 city segments; CNAM is optional ([container.md § Missing, repeated and unknown segments](container.md#missing-repeated-and-unknown-segments)).
- Compressed segments must decode to exactly their decoded size with no input left over. Any mix of literal and repeat runs is accepted, but to match the game's own output avoid control bytes `0x00` and `0x80` ([compression.md](compression.md#what-the-game-writes)).
- Write scenario ids little-endian ([conventions.md § Byte order](conventions.md#byte-order)).

## Values the game recomputes on load

The game overwrites these after loading, so a writer does not need to get them right ([container.md § After the walk](container.md#after-the-walk)):

- MISC words 124–379 (tile count per XBLD code);
- the statistics recounted by `0x434b70` (details on the MISC pages, planned);
- the XGRP graph maxima (not stored in the file at all).

## Not yet known

- Which other stored values the game trusts without checking, and which combinations of segments it needs to be consistent (for example XBLD with XZON, XBIT and XTXT).
- What the save routine (`0x433030` writes the header) does differently from the loader, for example where it puts CNAM.
