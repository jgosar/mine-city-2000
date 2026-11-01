# Game internals: address map of `SIMCITY.EXE`

The functions and variables in SimCity 2000 for Windows 95 that these pages cite as evidence. The names are ours; the executable has no symbols. Use this page to check a claim in a disassembler or decompiler, or to continue the reverse engineering.

**Binary:** `SIMCITY.EXE`, 32-bit PE, image base `0x400000`. The functions were found with Ghidra 12.1 (via the REA tool). Addresses `0x401000`–`0x402fff` are incremental-link thunks that jump to the real functions listed here; cite the real function.

## City loading

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x4302e0` | `LoadCity` | Opens the container, walks the segments, dispatches on the tag, then runs the post-load steps | [container.md](container.md) |
| `0x431120` | `ReadIffHeader` | Checks `FORM`, reads the size, checks `SCDH`; sends non-`FORM` files to `0x482850` | [container.md](container.md#file-header) |
| `0x482850` | `TryImportClassic` | Accepts a non-IFF file of exactly 27 248 bytes (a SimCity Classic city, **Inferred**) | [container.md](container.md#file-header) |
| `0x430dc0` | `ReadRleSegment` | Reads `length` bytes and RLE-decodes them into a buffer of a given size | [compression.md](compression.md) |
| `0x430d90` | `ReadRawSegment` | Reads `length` bytes into a buffer as they are (used for ALTM) | [container.md](container.md#segment-table) |
| `0x4310c0` | `SkipSegment` | Reads `length` bytes and throws them away (unknown tags) | [container.md](container.md#missing-repeated-and-unknown-segments) |
| `0x431330` | `LoadMisc` | RLE-decodes MISC, swaps it, checks word 0 == 0x122, copies every word into its variable | [container.md](container.md#version-check) |
| `0x431ee0` | `NameFromFileName` | Sets the city name to the upper-cased file name without its directory | [container.md](container.md#after-the-walk) |
| `0x434b70` | `RecountAfterLoad` | Recounts statistics from XBLD, XZON and XTXT after loading | [container.md](container.md#after-the-walk) |
| `0x434f80` | `RecountBuildingCodes` | Rebuilds the 256-entry per-XBLD-code tile count (stored in MISC words 124–379) | [container.md](container.md#after-the-walk) |
| `0x431fd0` | `ComputeGraphMaxima` | Computes the maximum of each of the 16 XGRP series of 52 values | [container.md](container.md#after-the-walk) |
| `0x430c00` | `ConvertLabels` | Turns the 256 length-prefixed XLAB records into C strings, capping the length at 23 | [container.md](container.md#after-the-walk) |
| `0x430d50` | `ClearLabels` | Clears all 256 labels (used when XLAB is missing) | [container.md](container.md#after-the-walk) |
| `0x42ae10` | `ReadScenarioSegment` | Finds a `TEXT`/`SCEN`/`PICT` segment by tag and little-endian id and reads its body | [container.md](container.md#scenario-segments) |

## Byte-order helpers

| Address | Our name | What it does |
|---|---|---|
| `0x42a930` | `Swap32` | Reverses the 4 bytes of a 32-bit value |
| `0x42a960` | `TagValue` | Loads a 4-character tag as a value comparable with what was read from the file |
| `0x42a7e0` | `Swap32Array` | Swaps every 4-byte word of a buffer (MISC, XGRP) |
| `0x42a880` | `Swap16Array` | Swaps every 2-byte word of a buffer (ALTM) |
| `0x42a820` | `SwapMicrosims` | Swaps the 16-bit words at bytes 2, 4 and 6 of every 8-byte record (XMIC) |
| `0x42a8c0` | `Swap16` | Reverses the 2 bytes of a 16-bit value |

## View rotation

| Address | Our name | What it does |
|---|---|---|
| `0x465490`, `0x466660` | `RotateView` (two directions) | Rotate every map array in place and step the compass |
| `0x43c730` | `DrawTile` | Draws a tile; applies the XBIT rotate flag as a horizontal mirror |
| `0x4382b0` | `PlaceTile` | Places runway and pier pieces and sets their XBIT rotate flag |

## Variables

### Segment buffers

The map variables hold 128 row pointers into one 16 KB buffer (ALTM: 32 KB). The others hold a pointer to the buffer.

| Address | Holds | Size |
|---|---|---|
| `0x4cae10` | ALTM | 128 × 128 × 2 |
| `0x4c9f58` | XTER | 128 × 128 |
| `0x4cc4f0` | XBLD | 128 × 128 |
| `0x4ca1f0` | XZON | 128 × 128 |
| `0x4cb1d0` | XUND | 128 × 128 |
| `0x4ca600` | XTXT | 128 × 128 |
| `0x4cab10` | XBIT | 128 × 128 |
| `0x4ca198` | XLAB | 256 × 25 |
| `0x4cb3ec` | XMIC | 150 × 8 |
| `0x4ca434` | XTHG | 480 |
| `0x4ca940` | XTRF | 64 × 64 |
| `0x4ca828` | XPLT | 64 × 64 |
| `0x4cb0a8` | XVAL | 64 × 64 |
| `0x4ca4d8` | XCRM | 64 × 64 |
| `0x4c9430` | XPLC | 32 × 32 |
| `0x4caa78` | XFIR | 32 × 32 |
| `0x4ca448` | XPOP | 32 × 32 |
| `0x4cb028` | XROG | 32 × 32 |
| `0x4cc470` | XGRP (16 pointers, one per series) | 16 × 52 × 4 |
| `0x4ca1a0` | City name (MFC string; its length is at `0x4ca1a4`) | — |

### Other variables

| Address | Holds | Loaded from |
|---|---|---|
| `0x4e72a8` | Temporary buffer for the decoded MISC segment | MISC |
| `0x4c942c` | Compass (0–3) | MISC word 2 |
| `0x4ca4c8` | Tile count per XBLD code (256 × 16-bit) | MISC words 124–379, then recomputed |

### Data

| Address | Contents |
|---|---|
| `0x4e736c`–`0x4e740c` | The 21 tags the city loader knows, 8 bytes apart: CNAM, XGRP, XROG, XPOP, XFIR, XPLC, XCRM, XVAL, XPLT, XTRF, XBIT, XTHG, XMIC, XLAB, XTXT, XUND, XZON, XBLD, XTER, ALTM, MISC |
| `0x4e7214`, `0x4e7108`, `0x4e61b8` | The tags `SCEN`, `PICT`, `TEXT` used by the scenario code |
| `0x4e721c`, `0x4e7224` | `SCDH`, `FORM` |
| `0x4e7414` | "Unable To Load. Old File Version = " |
