# Game internals: address map of `SIMCITY.EXE`

The functions and variables in SimCity 2000 for Windows 95 that these pages cite as evidence. The names are ours; the executable has no symbols. Use this page to check a claim in a disassembler or decompiler, or to continue the reverse engineering.

**Binary:** `SIMCITY.EXE`, 32-bit PE, image base `0x400000`. The functions were found with Ghidra 12.1 (via the REA tool). Addresses `0x401000`–`0x402fff` are incremental-link thunks that jump to the real functions listed here; cite the real function. A name ending in **?** means only the part of the function that touches the cited data was read.

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
| `0x434b70` | `RecountAfterLoad` | Recounts statistics from XBLD, XZON and XTXT after loading; calls `0x467860` and `0x464550` | [container.md](container.md#after-the-walk) |
| `0x434f80` | `RecountBuildingCodes` | Rebuilds the 256-entry per-XBLD-code tile count (stored in MISC words 124–379), skipping military-zone tiles (XZON zone 7) | [container.md](container.md#after-the-walk) |
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

## View rotation and screen position

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x465490` | `RotateViewClockwise` | Moves every map as `new[row][col] = old[col][127 − row]`; remaps XBLD, XTER and XUND codes through the clockwise tables; flips XBIT `0x02` on bridges and on-ramps; compass − 1. XZON, ALTM and XTXT bytes move unchanged | [conventions.md](conventions.md#orientation) |
| `0x466660` | `RotateViewCounterClockwise` | The exact inverse of `0x465490`; compass + 1 | [conventions.md](conventions.md#orientation) |
| `0x465f00`, `0x467100` | `RotateOverlays` (cw, ccw) | Turn the 64 × 64 and 32 × 32 overlays | [conventions.md](conventions.md#orientation) |
| `0x466410`, `0x467610` | `RotateThings` (cw, ccw) | Turn the XTHG records and some in-memory coordinate tables | [conventions.md](conventions.md#orientation) |
| `0x444e20` | `TileToScreen` | Screen position of a tile: x = (row − col)·w, y = (row + col)·w/2 − altitude·h | [conventions.md](conventions.md#orientation) |
| `0x444bf0` | `ScreenToTile` | Inverse of `0x444e20` (keeps the view centred when it turns) | — |
| `0x410950` | `GenerateTerrain` | Makes a new map; sets the compass to 0 | [conventions.md](conventions.md#orientation) |

## Drawing

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x43c730` | `DrawTile` | Draws a tile. Mirror rules for XBIT `0x02`: never below XBLD `0x1D`, the flag itself for `0x1D`–`0x6F`, flag XOR odd compass from `0x70`. Draws a building only at the footprint corner given by `0x4dc4c8`. Adds an extra sprite when XBIT `& 0xC0 == 0x80` | [xbit.md](segments/xbit.md#0x02-mirror-rotate), [xzon.md](segments/xzon.md#corner-bits) |
| `0x43d620`, `0x43e550` | `DrawTile` variants | Same height, mirror and sprite logic as `0x43c730` (`0x43d620` uses zoned ground sprites from `0x316`) | [xter.md](segments/xter.md#how-the-game-uses-it), [xzon.md](segments/xzon.md#zone-types) |
| `0x485230` | `DrawSprite` | Sprite blitter; its 4th argument is the mirror flag | [xbit.md](segments/xbit.md#0x02-mirror-rotate) |
| `0x4888f0`, `0x488ab0` | `DrawPowerMap`, `DrawWaterMap` | Draw the power and water overlay bitmaps | [xbit.md](segments/xbit.md#how-the-game-uses-it) |

## Terrain and water

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x447020` | `ReshapeTile` | Recomputes a tile's XTER slope from its 8 neighbours' altitudes (table `0x4e7958`); raises the tile if all four corners would be up; applies the sea level (underwater or shore code, water height, XBIT `0x04`) | [xter.md](segments/xter.md#slope-and-altitude) |
| `0x4460b0` | `FloodToLevel` | Turns every tile below a level into water at that level, and drains water tiles at or above it | [xter.md](segments/xter.md#water) |
| `0x446650` | `PlaceWater` | Turns a tile into surface water (`0x30`–`0x45`, or `0x3E` on a slope) and re-evaluates the neighbouring water | [xter.md](segments/xter.md#surface-water) |
| `0x4441c0` | `RaiseTerrain` | Raises a tile by one level, first raising lower neighbours recursively; refuses at altitude 30; takes 25 per tile off MISC word 5 (funds, **Inferred**); clears the XBIT `0x08` mark set by `0x443cb0` | [altm.md](segments/altm.md#altitude-bits-04), [xbit.md](segments/xbit.md#0x08-developed-land-mask-and-scratch-mark) |
| `0x443cb0` | `CheckRaise`? | Recursive check before a raise; marks visited tiles with XBIT `0x08` | [xbit.md](segments/xbit.md#0x08-developed-land-mask-and-scratch-mark) |
| `0x445b50` | `RaiseLowerTerrain` | Raises or lowers a tile by one level | [altm.md](segments/altm.md#how-the-game-uses-it) |
| `0x40e1c0` | `ClearMap` | New empty map: every ALTM word `0x0042`; XTER, XBLD and XBIT 0 | [altm.md](segments/altm.md#altitude-bits-04) |
| `0x4843d0` | `FixWaterEdges`? | Recomputes the shapes of all water tiles (same slope table, but all-four-up → `0x0D`); used by the SimCity Classic import | — |
| `0x447b20` | `GenerateCoast` | Ocean strip at high row indices; sets the ALTM water height and XBIT `0x01` | [xbit.md](segments/xbit.md#0x01-salt) |
| `0x447e20` | `SpreadSalt` | Four passes: salt spreads to water neighbours and is cleared on non-water | [xbit.md](segments/xbit.md#0x01-salt) |
| `0x484320` | `MakeWaterTile` | SimCity Classic import: XBIT `0x04`, ALTM water bits, XTER `0x10` | [xbit.md](segments/xbit.md#0x04-covered-by-water) |
| `0x4829f0`, `0x483250` | `ImportClassic`? | Build the map layers from a SimCity Classic city (**Inferred**) | — |
| `0x45b930` | `BuildBridge` | Raises each bridge end by one level and gives it an edge slope (XTER 1–4) | [xter.md](segments/xter.md#slope-and-altitude) |
| `0x462ed0` | `PlaceHighwayPiece` | Levels a 2 × 2 highway footprint, writing XTER `0x0D` on filled-up tiles | [xter.md](segments/xter.md#slope-and-altitude) |
| `0x463f60` | `HighwayFootprintTop` | Highest point of a 2 × 2 footprint, counting a non-flat tile as altitude + 1 | [xter.md](segments/xter.md#slope-shapes) |
| `0x44bea0` | `BuildTunnel` | Writes the ALTM tunnel depth (bits 10–14) along a new tunnel | [altm.md](segments/altm.md#tunnel-depth-bits-1014) |
| `0x458d40` | `DestroyTile`? | Among other things collapses a tunnel and clears ALTM bits 10–15 | [altm.md](segments/altm.md#tunnel-depth-bits-1014) |
| `0x443270` | `Bulldoze` | Removes surface water; in the underground view demolishes tunnels (depth 1) | [xter.md](segments/xter.md#water) |
| `0x448f50` | `CheckPlacement`? | In the underground view refuses tiles with tunnel depth 1 or 2 | [altm.md](segments/altm.md#tunnel-depth-bits-1014) |

## Buildings

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x45b1c0` | `FindBuildingAnchor` | Given a tile and its XBLD code, returns the footprint size and moves to the anchor (corner D: min row, max col); highways snap to the even 2 × 2 grid | [xbld.md](segments/xbld.md#the-anchor) |
| `0x441f00` | `SetTile` | Writes an XBLD code and keeps the per-code tile count (`0x4ca4c8`) or, for military-zone tiles, the military count (`0x4ca934`) in step | [xbld.md](segments/xbld.md#how-the-game-uses-it) |
| `0x4382b0` | `PlaceTile` | Places runway and pier pieces and sets their XBIT `0x02` flag (also sets `0xC0`); places the 3 × 3 missile silo (XBLD `0xF9`, XUND `0x22`) and its corner bits from `0x4dc4d0` | [xbit.md](segments/xbit.md#0x02-mirror-rotate), [xund.md](segments/xund.md#values) |
| `0x436e80` | `PlaceBuildingTile` | Places a building tile: corner bits `0xF0` for one-tile items, XBIT `0xE0` on every tile of the footprint | [xzon.md](segments/xzon.md#corner-bits), [xbit.md](segments/xbit.md#how-the-game-uses-it) |
| `0x440170` | `OnBuildingPlaced`? | Per-building side effects; for XBLD `0xE9` writes XUND `0x23` | [xund.md](segments/xund.md#how-the-game-uses-it) |
| `0x440c50` | `ClearTileFlags`? | XBIT `&= 0x1F` (clears `0x80`, `0x40`, `0x20`) when tiles are cleared | [xbit.md](segments/xbit.md#how-the-game-uses-it) |
| `0x441400` | `PlacePowerLine`? | XBIT `|= 0x80`, then recomputes power | [xbit.md](segments/xbit.md#0x80-conducts-power) |
| `0x42b520` | `GetNameString` | Loads a string by (category, index): 2000 → id 593 + index, among others | [tile-codes-xbld.md](reference/tile-codes-xbld.md) |

## Zones and underground

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x4142c0` | `PlaceMilitaryBase` | Claims an 8 × 8 area: sets zone 7 on flat, dry, unzoned, unbuilt tiles and takes them out of the per-code tile count | [xzon.md](segments/xzon.md#zone-7-the-military-base) |
| `0x437f90` | `CornerToBlock` | Converts a corner bit to a view corner ((k − compass) & 3) and rebuilds the 2 × 2 block it belongs to | [xzon.md](segments/xzon.md#which-bit-is-at-which-corner) |
| `0x437310` | `ClearZone`? | Clears a tile's zone, keeping the corner bits | [xzon.md](segments/xzon.md#how-the-game-uses-it) |
| `0x442370` | `SetUnderground` | The only XUND writer; keeps the subway tile count | [xund.md](segments/xund.md#how-the-game-uses-it) |
| `0x44bb20` | `LaySubway` | Empty → `0x01`, pipe → crossing, then fixes the shapes | [xund.md](segments/xund.md#how-the-game-uses-it) |
| `0x44bc40` | `FixSubwayShape` | Chooses a subway piece from the tile's XTER slope or its neighbours | [xund.md](segments/xund.md#piece-shapes) |
| `0x44b790` | `LayPipe` | Empty → `0x10`, subway → crossing, sets XBIT `0x20`, then fixes the shapes | [xund.md](segments/xund.md#how-the-game-uses-it) |
| `0x44b8d0` | `FixPipeShape` | Same as `FixSubwayShape` for pipes (+ `0x10`) | [xund.md](segments/xund.md#piece-shapes) |
| `0x441b80` | `PlaceSubwayToRail` | XUND `0x23`, XBLD `0x6C`–`0x6F`, clears XBIT `0x20` | [xund.md](segments/xund.md#how-the-game-uses-it) |

## Power and water simulation

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x413520` | `SimulationStep` | Per-step dispatcher of the simulation cycle: step 1 → `0x467860`, step 2 → `0x46a840`, step 0x14 → `0x464550` | [xbit.md](segments/xbit.md#what-to-trust) |
| `0x467860` | `RecomputePower` | Clears XBIT `0x40` and `0x08` everywhere, then floods from the power plants (XBLD `0xC6`–`0xCF`) | [xbit.md](segments/xbit.md#0x40-powered) |
| `0x4679b0` | `FloodPower` | Two-pass flood through `0x80` tiles with `0x08` as the visited mark; sets `0x40` within the plants' capacity | [xbit.md](segments/xbit.md#0x40-powered) |
| `0x464550` | `RecomputeWater` | Clears XBIT `0x10` (not on water towers `0xEB`) and `0x08`, then floods from powered pumps `0xDC` and desalination plants `0xFA` | [xbit.md](segments/xbit.md#0x10-watered) |
| `0x4649e0` | `FloodWater` | Two-pass flood through `0x20` tiles; pump capacity from fresh-water neighbours (XBIT `& 5 == 4`), desalination from salt-water ones (`== 5`) | [xbit.md](segments/xbit.md#0x10-watered) |
| `0x46a840` | `PollutionStep`? | Computes XPLT; reads and rewrites the XBIT `0x08` mask | [xbit.md](segments/xbit.md#0x08-developed-land-mask-and-scratch-mark) |
| `0x4570f0` | `YieldToWindows`? | Called every 5000 tiles during the floods (**Inferred**: message pump) | [xbit.md](segments/xbit.md#what-to-trust) |

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
| `0x4ca444` | Funds (**Inferred**: `0x4441c0` takes 25 off it per raised tile) | MISC word 5 |
| `0x4ca818` | Sea level (16-bit) | MISC word 912 |
| `0x4ca4c8` | Tile count per XBLD code (256 × 16-bit) | MISC words 124–379, then recomputed |
| `0x4ca934` | Pointer to the 16-entry military tile count (index 0 other, then `0xDD`, `0xDE`, `0xEF`, `0xF2`, `0xEA`, `0xE3`, `0xE4`, `0xE5`, `0xF1`, `0xE0`, `0xE2`, `0xE7`, `0xE8`, `0xF6`, `0xF9`) | not checked yet (MISC, M3) |
| `0x4ca41c` | Subway tile count (codes `0x01`–`0x0F`, `0x1F`, `0x20`, `0x22`, `0x23`, not in zone 7) | maintained by `0x442370`; not checked yet whether MISC stores it |
| `0x4caa50` | Percentage of the city powered (computed by `0x467860`) | — |
| `0x4cc4b8` | Percentage of the city watered (computed by `0x464550`) | — |
| `0x4c7d18` | Corner bit at which buildings are drawn for the current compass | table `0x4dc4c8` |
| `0x4e9e48` | Non-zero while the underground view is shown (**Inferred**) | — |

## Data

| Address | Contents |
|---|---|
| `0x4e736c`–`0x4e740c` | The 21 tags the city loader knows, 8 bytes apart: CNAM, XGRP, XROG, XPOP, XFIR, XPLC, XCRM, XVAL, XPLT, XTRF, XBIT, XTHG, XMIC, XLAB, XTXT, XUND, XZON, XBLD, XTER, ALTM, MISC |
| `0x4e7214`, `0x4e7108`, `0x4e61b8` | The tags `SCEN`, `PICT`, `TEXT` used by the scenario code |
| `0x4e721c`, `0x4e7224` | `SCDH`, `FORM` |
| `0x4e7414` | "Unable To Load. Old File Version = " |
| `0x4e8fa0`, `0x4e9110` | XBLD remap tables for one clockwise / counter-clockwise view turn (256 bytes each, inverses of each other) |
| `0x4e90a0`, `0x4e9210` | XTER remap tables, clockwise / counter-clockwise (72 bytes) |
| `0x4e90e8`, `0x4e9258` | XUND remap tables, clockwise / counter-clockwise (codes `0x00`–`0x23` and a few beyond) |
| `0x4e85d8` | 16-bit footprint size per XBLD code (valid for `0x70`–`0xFF`; lower entries belong to other data) |
| `0x4dc4d0` | XZON corner bits of footprint corners 0–3 per compass, 4 × 4 × uint16: `10 20 40 80`, `20 40 80 10`, `40 80 10 20`, `80 10 20 40` |
| `0x4dc4c8` | Corner bit at which a building is drawn, per compass (4 × uint16): `0x80`, `0x10`, `0x20`, `0x40` |
| `0x4e7958` | 16 entries: raised-corner set → XTER slope shape (entry 15 = `0x32`, meaning "raise the tile instead") |
| `0x4e7a68`, `0x4e7a80` | Row and column offsets of the 8 neighbours (16-bit), in the order (0,−1) (+1,−1) (+1,0) (+1,+1) (0,+1) (−1,+1) (−1,0) (−1,−1) |
| `0x4e7ac0` | 8 bytes: the corners each of those neighbours raises (`03 02 06 04 0C 08 09 01`) |
| `0x4e7628` | 72 × 16-bit: ground sprite of each XTER code (sprite = 1000 + entry) |
| `0x4e7c40` | Subway/pipe shape by neighbour mask (16 × int16; 1 = col − 1, 2 = row + 1, 4 = col + 1, 8 = row − 1) |
| `0x4e7c60` | Subway/pipe slope piece by XTER low bits (int16 per value) |
| `0x4e7c70`, `0x4e7cb8` | Per-XTER-value flags: may an underground line connect along the cols / rows |
| `0x4e7b68` | Per-XTER-low-bits flag (int32): the tile is a simple slope, so the piece is a slope piece |
| String ids 594–753 | Building and tile names (STRINGTABLE resource, UTF-16) |
