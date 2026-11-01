# MISC: tile counts (words 124–379 and 1002–1017)

Two tables of tile counts. Words 124–379 count the tiles of each XBLD code outside the military base. Words 1002–1017 count the tiles of the military base, by building. The game recomputes the first table on every load, but it trusts the second.

## At a glance

| | Building counts | Military counts |
|---|---|---|
| Words | 124–379 (bytes `0x1F0`–`0x5EF`) | 1002–1017 (bytes `0xFA8`–`0xFE7`) |
| Entries | 256, one per XBLD code | 16 |
| Loaded into | `0x4ca4c8` (16-bit each) | `0x4ca934` (16-bit each) |
| Counts | tiles **not** in XZON zone 7 | tiles **in** XZON zone 7 |
| Recomputed on load | yes (`0x434f80`) | **no** |

Both tables are 16-bit in the game. The Windows 95 saver writes each entry sign-extended to 32 bits. Some files hold values from 32 768 to 65 535 instead: negative counts written without sign extension by another writer (see [§ Pitfalls](#pitfalls-for-readers)).

## Building counts: words 124–379

Word 124 + *c* is the number of tiles whose XBLD code is *c*, not counting tiles in the military zone (XZON zone 7). One tile of a multi-tile building counts once, so a 3 × 3 building adds 9 to its code's entry. Entry 0 counts empty tiles.

| Words | Bytes | Type | Name | Meaning | Confidence |
|---|---|---|---|---|---|
| 124 + *c* | `0x1F0` + 4*c* | int16 | tiles of XBLD code *c* | Number of non-military tiles carrying code *c* | **Confirmed (code)**, recomputed on load |

### How the game uses it

- **Recomputed on load.** After the segment walk, `0x434f80` clears the table and counts every tile of XBLD whose XZON zone is not 7 ([container.md § After the walk](../../container.md#after-the-walk)). The stored values are never used.
- **Kept up to date while playing.** `0x441f00`, the routine that writes an XBLD code, takes one off the old code's entry and adds one to the new code's, unless the tile is in zone 7.
- **Read by the simulation**, among other places, by the graph update `0x413e50` and the neighbours window `0x46c562`. Both add the arcology entries (`0xFB`–`0xFE`), divide by 16 (one arcology is 16 tiles) and, above 140 arcologies, add a population bonus ([xgrp.md § Graph series](../xgrp.md#graph-series)). The seaport and airport growth in `0x4358b0` compares entries too.

**For a reader:** recount from XBLD and XZON if you need the numbers; do not trust the stored table.

## Military counts: words 1002–1017

Word 1002 + *i* counts the tiles in XZON zone 7 that carry the building of index *i*. Index 0 counts every other zone-7 tile, including empty land, trees and roads.

| Index | Word | Byte | XBLD code | Building |
|---|---|---|---|---|
| 0 | 1002 | `0xFA8` | any other | empty land, trees, roads, … inside the base |
| 1 | 1003 | `0xFAC` | `0xDD` | runway |
| 2 | 1004 | `0xFB0` | `0xDE` | runway crossing |
| 3 | 1005 | `0xFB4` | `0xEF` | parking lot (2 × 2) |
| 4 | 1006 | `0xFB8` | `0xF2` | cargo yard (2 × 2) |
| 5 | 1007 | `0xFBC` | `0xEA` | radar |
| 6 | 1008 | `0xFC0` | `0xE3` | warehouse |
| 7 | 1009 | `0xFC4` | `0xE4` | building |
| 8 | 1010 | `0xFC8` | `0xE5` | building |
| 9 | 1011 | `0xFCC` | `0xF1` | top secret (2 × 2) |
| 10 | 1012 | `0xFD0` | `0xE0` | crane |
| 11 | 1013 | `0xFD4` | `0xE2` | control tower |
| 12 | 1014 | `0xFD8` | `0xE7` | F-15b |
| 13 | 1015 | `0xFDC` | `0xE8` | hangar (1 × 1) |
| 14 | 1016 | `0xFE0` | `0xF6` | hangar (2 × 2) |
| 15 | 1017 | `0xFE4` | `0xF9` | missile silo |

(**Confirmed (code)**: the index of each code is in `0x441f00`; the building names are in [tile-codes-xbld.md](../../reference/tile-codes-xbld.md).) Every entry is a tile count, so a 2 × 2 building adds 4.

### How the game uses it

- **Not recomputed on load.** The loader copies the 16 values and nothing recounts them; `0x434f80` only rebuilds the building counts (**Confirmed (code)**).
- **Kept up to date by `0x441f00`.** For a tile in zone 7 it takes one off the old code's entry and adds one to the new code's, through the table above.
- **Started by `0x4142c0`**, which claims the base land: each tile it turns into zone 7 adds one to index 0.
- **Read by zone growth `0x4358b0`**, which picks the next military building to grow by comparing the counts (for example more parking lots or hangars, depending on how many of each the base already has). Which comparisons apply depends on the base type in MISC word 915 (army, air force, navy, missile silos) ([general.md](general.md)). The exact ratios are **Inferred** from that routine.

**For a reader:** the table is the game's own bookkeeping. To list the base's buildings, read XBLD and XZON instead.

## Evidence

| Claim | Code | Corpus (114 files) |
|---|---|---|
| Building counts exclude zone 7 and are recounted on load | `0x434f80`, `0x441f00` | stored table equals the recount in 76/114; never equal to a count that includes zone 7 when they differ |
| Military index per code | `0x441f00` | — |
| Military counts are not recounted | `0x431330`, `0x434f80` | equal to a recount of zone 7 in 105/114 |
| Base land adds to index 0 | `0x4142c0` | 21/114 files have a non-zero military count |

The 38 files whose building counts differ from a recount are stale in a few entries (trees and empty land mostly), or hold negative counts. The 9 files whose military counts differ include `TORONTO.SC2` (49 tiles counted, no zone-7 tile on the map) and `JUNETOWN.SC2` (a base on the map, all counts 0).

## Pitfalls for readers

- **Read the entries as 16-bit signed.** The game keeps 16-bit counts. In 28/114 files some stored building counts lie between 32 768 and 65 535, and one file has a military count of 65 533: these are small negative counts (65 535 = −1) written without sign extension. Take the low 16 bits as a signed number, as the loader does.
- **The building counts are not trustworthy in a file.** 38/114 files disagree with a recount. The game ignores them anyway.
- **The military counts can be stale**, and the game keeps using them until tiles in the base change.
- **Only tiles in zone 7 are counted as military.** Airport and seaport buildings outside the base (`0xDD`–`0xF2` are shared codes) go to the building counts.

## Links

- [MISC word index](README.md)
- [xbld.md](../xbld.md), [xzon.md § Zone 7](../xzon.md#zone-7-the-military-base)
- [container.md § After the walk](../../container.md#after-the-walk)
