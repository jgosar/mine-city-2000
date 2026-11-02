# XZON: zones and building corners

One byte per tile. The low four bits say how the tile is zoned (residential, commercial, industrial, or one of the special zones: military base, airport, seaport). The high four bits mark the **corners of a building's footprint**: they show where each multi-tile building begins and ends.

## At a glance

| | |
|---|---|
| Size | 16384 bytes, 128 × 128 tiles, row by row ([conventions.md](../conventions.md#the-city-map-128--128-tiles)) |
| Storage | RLE-compressed ([compression.md](../compression.md)) |
| Element | 1 byte: bits 0–3 zone, bits 4–7 corner bits |
| Game buffer | `0x4ca1f0` (128 row pointers) |
| Changes with the compass? | The bytes move with the map, unchanged; what a corner bit means depends on the compass ([Rotation](#rotation)) |

## Layout

| Bits | Mask | Field | Values | Confidence |
|---|---|---|---|---|
| 0–3 | `0x0F` | Zone type | 0–9 ([Zone types](#zone-types)) | **Confirmed (code)**: every reader of the zone masks with `0x0F` (renderer `0x43c730`, placement `0x4382b0`, recount `0x434f80`). **Verified (data)**: only 0–9 occur, 114/114 |
| 4–7 | `0xF0` | Corner bits | `0x00`, one of `0x10`/`0x20`/`0x40`/`0x80`, or `0xF0` ([Corner bits](#corner-bits)) | **Confirmed (code)**: renderer `0x43c730`, `0x437f90`, placement `0x4382b0`. **Verified (data)**: no other value occurs, 114/114 |

The two fields are independent. A zoned tile keeps its zone when a building, road or power line is built on it, and corner bits are set whatever the zone.

## Zone types

| Value | Zone | Confidence | Evidence |
|---|---|---|---|
| 0 | none | **Verified (data)** | 1 430 743 tiles: roads, trees, power lines, empty land, unzoned buildings |
| 1 | light residential | **Verified (data)** | Shares its small houses (XBLD `0x74`–`0x7A`) with zone 2 |
| 2 | dense residential | **Verified (data)** | Also carries the 2×2 apartment codes `0x8C`–`0x93` |
| 3 | light commercial | **Verified (data)** | Shares its small shops (XBLD `0x7C`–`0x83`) with zone 4 |
| 4 | dense commercial | **Verified (data)** | Also carries 2×2 offices `0x98`–`0x9D` |
| 5 | light industrial | **Verified (data)** | Shares its small factories (XBLD `0x84`–`0x87`) with zone 6 |
| 6 | dense industrial | **Verified (data)** | Also carries 2×2 and 3×3 factories (`0xA2`–`0xA5`, `0xBC`–`0xC1`) |
| 7 | **military base** | **Confirmed (code)** | Military-base placement `0x4142c0` writes 7 into the tiles it claims |
| 8 | airport | **Verified (data)** | Found on airport tiles (`0xDD`, `0xE4`–`0xE6`, `0xEE`, `0xF6`, …) |
| 9 | seaport | **Verified (data)** | Found on seaport tiles (`0xE0`, `0xE3`, `0xF0`, `0xF2`) |
| 10–15 | — | **Unknown** | Never seen (0/114 files) |

Zone names follow the older documents. The data confirms the pairs: the light and dense versions of a zone share their smallest buildings, and the special zones sit on their own buildings. For building names see the [XBLD tile codes](../reference/tile-codes-xbld.md). Which codes stand on which zone, code by code, is in [cross-segment.md § Zones and buildings](../cross-segment.md#zones-and-buildings).

For a zoned tile with no building, the renderer draws a ground sprite chosen as *base + zone* (base `0x50A` in one renderer, `0x316` in another; `0x43c730`, `0x43d620`). So each of the nine zone values has its own ground graphic (**Confirmed (code)**).

### Zone 7, the military base

The military base is a special zone, and the game treats its tiles differently (**Confirmed (code)**):

- **Placement** (`0x4142c0`): when the base is created, a tile in its 8 × 8 area gets zone 7 if it meets all of these conditions: XTER is 0 (flat, dry land), it is unzoned, nothing is built on it except trees or rubble (XBLD < `0x0D`), and XBIT bit `0x04` is clear. The base's ground tiles are military even where nothing is built, so 223 zone-7 tiles in the corpus are bare land (XBLD `0x00`).
- **Building counts** (`0x434f80`): when the game recounts how many tiles carry each XBLD code (stored in MISC words 124–379, [tile-counts.md](misc/tile-counts.md#building-counts-words-124379)), it skips zone 7. The military tiles are counted separately, in MISC words 1002–1017. Military tiles never count as city buildings. Placement also decrements the count for each tile it claims.
- **Subway count** (`0x442370`): subway pieces on a zone 7 tile are not added to the city's subway tile count (MISC word 1018, [general.md](misc/general.md#counters)).

## Corner bits

Each corner bit marks **one corner of a building footprint**. Buildings are square: 1×1, 2×2, 3×3 or 4×4 tiles.

| Footprint | Corner tiles | Edge and interior tiles |
|---|---|---|
| 1×1 | the single tile holds **all four** bits: `0xF0` | — |
| 2×2 | each of the 4 tiles holds **one** bit | — |
| 3×3, 4×4 | each of the 4 corner tiles holds **one** bit | `0x00` |
| not a building (roads, rails, power lines, bridges, trees, empty land) | — | `0x00` |

(**Confirmed (code)**: one-tile placements set `0xF0` (`0x436e80`, `0x4382b0`); placing a 3×3 sets one bit on each of its four corners (`0x4382b0`). **Verified (data)**: the tile counts with bits `0x10`, `0x20`, `0x40` and `0x80` are equal to within 0.05%, and the walk below recovers 68 590 of 68 643 multi-tile footprints.)

### Which bit is at which corner

The bits are tied to the **map**, not to the screen. Call the bits *k* = 0, 1, 2, 3 for `0x10`, `0x20`, `0x40`, `0x80`, and number the corners of a footprint, as stored in the file, like this:

| Corner *v* | Position in the stored arrays | Name on [xbld.md](xbld.md#corner-bits-mark-the-outline) |
|---|---|---|
| 0 | lowest row, lowest col | A |
| 1 | highest row, lowest col | B |
| 2 | highest row, highest col | C |
| 3 | lowest row, highest col | D, the anchor |

Then **bit *k* is at corner *v* = (*k* − compass) mod 4**, where compass is MISC word 2 ([conventions.md](../conventions.md#the-compass-the-map-is-saved-as-currently-viewed)). Spelled out:

| Compass | lowest row, lowest col | highest row, lowest col | highest row, highest col | lowest row, highest col |
|---|---|---|---|---|
| 0 | `0x10` | `0x20` | `0x40` | `0x80` |
| 1 | `0x20` | `0x40` | `0x80` | `0x10` |
| 2 | `0x40` | `0x80` | `0x10` | `0x20` |
| 3 | `0x80` | `0x10` | `0x20` | `0x40` |

(**Confirmed (code)**: `0x437f90` converts a corner bit to a view corner with exactly this formula. The 3×3 placement in `0x4382b0` sets the four corners from a table at `0x4dc4d0`: four bits per compass value, in the order corner 0, 1, 2, 3. That table is the one above, row for row. **Verified (data)**: 274 427 of 274 571 single-bit tiles (99.95%) sit at the corner this predicts: the footprint, judged by equal XBLD codes, continues in both directions away from that corner.)

A 3×3 building in a file saved with compass 0:

| | col *c* | col *c*+1 | col *c*+2 |
|---|---|---|---|
| **row *r*** | `0x10` | `0x00` | `0x80` |
| **row *r*+1** | `0x00` | `0x00` | `0x00` |
| **row *r*+2** | `0x20` | `0x00` | `0x40` |

### Where the building is drawn

The renderer draws a multi-tile building's sprite once, at the tile whose corner bit matches a mask chosen by the compass: `0x80`, `0x10`, `0x20`, `0x40` for compass 0–3 (table at `0x4dc4c8`, used by `0x43c730`). By the formula above, that is always **corner 3: lowest row, highest col** of the stored arrays (**Confirmed (code)**). Which screen corner that is is described in [conventions.md](../conventions.md#orientation).

### Finding footprints from XZON alone

```
for each tile (r, c) whose corner bits == bit at corner 0:
    size = the first k in 1..4 such that tile (r + k - 1, c) holds the bit at corner 1
    the footprint is rows r .. r+size-1, cols c .. c+size-1
bit at corner v = 0x10 << ((v + compass) mod 4)
1×1 buildings: tiles whose corner bits are 0xF0
```

**Verified (data)**: this recovers 68 590 of 68 643 multi-tile footprints (55 911 2×2, 10 288 3×3, 2 391 4×4). The other 53 are in 7 files; see [Pitfalls](#pitfalls-for-readers). The XBLD page explains the other way, from building codes and sizes, and how the two relate: [xbld.md](xbld.md#footprints-and-anchors).

## How the game uses it

| Use | Function | What it does with XZON |
|---|---|---|
| Drawing | `0x43c730`, `0x43d620` | Zone picks the ground sprite of a zoned empty tile; the corner mask decides on which tile a building's sprite is drawn |
| Placing a building | `0x4382b0`, `0x436e80` | Sets `0xF0` on one-tile placements; sets one corner bit on a 3×3 corner tile; checks zones before placing |
| Corner → block | `0x437f90` | Turns a corner bit into a view corner, then works out the 2×2 block of tiles that the corner belongs to and replaces those four tiles |
| Clearing a zone | `0x437310` | Keeps the corner bits (`& 0xF0`) |
| Military base | `0x4142c0` | Writes zone 7; skips zone 7 tiles when choosing a site |
| Recount after load | `0x434f80`, `0x434b70` | Skips zone 7 when counting building codes; uses zone ≠ 0 in another statistic |
| Underground | `0x442370` | Does not count subway on zone 7 tiles |

### How the corner bits line up with XBLD

In the corpus the corner bits split the XBLD codes into clean groups (**Verified (data)**, groups that make up more than 2% of a code's tiles):

| Corner bits seen | XBLD codes | Meaning |
|---|---|---|
| always `0x00` | `00`, `06`–`0C`, `0E`–`3A`, `3C`, `3E`–`48`, `51`–`60` | Not a building: land, trees, power lines, roads, rails, tunnels, crossings, bridges, on-ramps |
| always `0xF0` | `05`, `0D`, `49`–`50`, `6C`–`8B`, `C6`–`C8`, `DB`–`EA` | 1×1 buildings |
| only single bits | `61`–`6B`, `8C`–`AD`, `EB`–`F7` | 2×2 footprints (including highway pieces) |
| single bits and `0x00` | `AE`–`C5`, `C9`–`DA`, `F8`–`FF` | 3×3 and 4×4 footprints |
| `0x00` or `0xF0` | `01`–`04`, `3B`, `3D` | Rubble and two rail pieces: sometimes left over from a 1×1 building |

The names and sizes of the codes are in the [XBLD tile codes](../reference/tile-codes-xbld.md).

## Rotation

When the player rotates the view, the rotation routines (`0x465490` and `0x466660`) move every XZON byte to its new position **without changing it** (**Confirmed (code)**). A footprint's corners therefore move to different array positions while keeping their bits. This is why the corner table above depends on the compass.

To normalise a file to compass 0, rotate the arrays back ([conventions.md](../conventions.md#orientation)) and keep the bytes as they are: afterwards bit `0x10` is at the lowest-row, lowest-col corner of every footprint.

## Evidence

| Claim | Code | Data (`sc2check.py --only xzon`) |
|---|---|---|
| Zone in bits 0–3, values 0–9 | `0x43c730`, `0x4382b0`, `0x434f80` | 114/114 files use only 0–9 |
| Zone 7 = military base | `0x4142c0` | zone 7 in 17/114 files |
| Corner bits only `0`, single, or `0xF0` | `0x4382b0`, `0x436e80` | 114/114 |
| Bit *k* at corner (*k* − compass) mod 4 | `0x437f90`; `0x4382b0` with table `0x4dc4d0` | 274 427 / 274 571 single-bit tiles; 105/114 files without exception |
| Sprite drawn at corner 3 | `0x43c730` with table `0x4dc4c8` | — |
| Footprint walk | — | 68 590 / 68 643 footprints; 107/114 files without exception |
| Bytes unchanged by rotation | `0x465490`, `0x466660` | — |

## Pitfalls for readers

- **The corner bits are not screen corners.** The older documents name the bits "top left", "upper right" and so on. Those names hold for only one compass value, and the two documents disagree with each other ([corrections.md](../corrections.md)). Use the compass formula.
- **A few hand-edited or unusual cities break the rule.** 144 single-bit tiles in 9 files (mostly `FLOATING.SC2`, `PETER3.SC2` and `RETREAT.SC2`) do not line up with their XBLD footprint. A robust reader trusts XBLD for what is built and uses XZON corners as a cross-check.
- **Zone stays under buildings and infrastructure.** A tile with a road or power line can still be zoned, and an airport's power lines keep zone 8. Do not use "zone ≠ 0" to mean "empty zoned lot".
- **Zone 7 covers bare land.** Military tiles can be empty (XBLD `0x00`) or trees.
- **Stray corner bits on empty land.** 9 of the 4 140 zoned tiles with nothing built on them (XBLD `0x00`) carry a corner bit. Treat that as leftover noise, not as a building.
