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
| `0x431330` | `LoadMisc` | RLE-decodes MISC, swaps it, checks word 0 == 0x122, copies words 0–1043 into their variables (many into 16- or 8-bit ones) | [container.md](container.md#version-check), [misc/README.md](segments/misc/README.md#how-the-words-are-stored) |
| `0x433a40` | `SaveMisc` | The reverse of `0x431330`: packs the variables into words 0–1043 and writes zeros from word 1044 on; word 29 comes from the Residents graph, word 1028 from the view centre | [misc/README.md](segments/misc/README.md#how-the-words-are-stored), [general.md](segments/misc/general.md#layout) |
| `0x4300e0` | `OpenCity` | Loads a file (`0x4302e0`, or the SimCity Classic import), then restores the view from MISC words 1028 and 1029 through `0x412570` | [general.md](segments/misc/general.md#view) |
| `0x431ee0` | `NameFromFileName` | Sets the city name to the upper-cased file name without its directory | [container.md](container.md#after-the-walk), [cnam.md](segments/cnam.md#how-the-game-uses-it) |
| `0x434b70` | `RecountAfterLoad` | Recounts statistics from XBLD, XZON and XTXT after loading (among them the road neighbour connections, from XTXT `0xFA`); sums the outstanding bond rates (`0x4ca400`); resets the flood timer; calls `0x467860` and `0x464550` | [container.md](container.md#after-the-walk) |
| `0x434f80` | `RecountBuildingCodes` | Rebuilds the 256-entry per-XBLD-code tile count (stored in MISC words 124–379), skipping military-zone tiles (XZON zone 7) | [container.md](container.md#after-the-walk), [tile-counts.md](segments/misc/tile-counts.md#building-counts-words-124379) |
| `0x431fd0` | `ComputeGraphMaxima` | Sets each of the 16 graph maxima (`0x4cc6f0`) to the largest of its 52 XGRP values, at least the floor in `0x4e72b0`; series 1–3 take series 0's maximum | [xgrp.md](segments/xgrp.md#graph-scale-not-stored) |
| `0x430c00` | `ConvertLabels` | Turns the 256 length-prefixed XLAB records into C strings, capping the length at 23 | [xlab.md](segments/xlab.md#layout) |
| `0x430d50` | `ClearLabels` | Clears all 256 labels (used when XLAB is missing) | [container.md](container.md#after-the-walk) |
| `0x42ae10` | `ReadScenarioSegment` | Finds a `TEXT`/`SCEN`/`PICT` segment by tag and little-endian id and reads its body | [container.md](container.md#scenario-segments) |
| `0x42cef0`, `0x42d940` | `AllocCityBuffers`?, `FreeCityBuffers`? | Allocate and free the segment buffers | — |
| `0x42d820` | `AllocateGraphs` | Allocates the 3328-byte XGRP buffer and points the 16 series pointers (`0x4cc470`) at it, 0xD0 bytes apart | [xgrp.md](segments/xgrp.md#layout) |
| `0x432350` | `SaveCity` | Save routine: writes CNAM first, then each segment from its buffer. It writes no TEXT, SCEN or PICT, so a saved scenario is an ordinary city | [cnam.md](segments/cnam.md#how-the-game-uses-it) |
| `0x433030` | `WriteIffHeader`? | Writes the `FORM` header for the save routine | [writing.md](writing.md#not-yet-known) |
| `0x49e9d8` | `CString::Left` | MFC library: the first *n* characters of a string (used by `0x432990`) | [cnam.md](segments/cnam.md#how-the-game-uses-it) |
| `0x480ae0` | `StopMusic` | Closes the MCI music device (`MCI_CLOSE`); the MISC loader calls it (through the thunk `0x402be4`) when the music option, word 1023, is 0 | [general.md](segments/misc/general.md#game-options) |
| `0x432990` | `WriteCnam` | Writes the tag, length 32, byte `0x1F`, then 31 bytes starting at the name (whatever follows it in memory) | [cnam.md](segments/cnam.md#layout) |
| `0x430c90` | `UnconvertLabels` | Before saving: turns the 256 C-string labels back into length + text; writes only bytes 0 … length | [xlab.md](segments/xlab.md#layout) |
| `0x40e250` | `ResetCityData` | New city: clears the overlays (XROG to `0x7F`), zeroes all 832 XGRP values and sets the graph maxima from `0x4e6750`, resets the power and water figures | [minimaps.md](segments/minimaps.md#xrog-rate-of-growth), [xgrp.md](segments/xgrp.md#graph-scale-not-stored) |
| `0x42e460` | `StartCity` | New game, after the terrain is made: founding year, difficulty, starting funds (20 000, 10 000, or 10 000 and one bond), national figures, technology years (base year + random 0–19), "Ocean" as neighbour 0 on a coast map | [general.md](segments/misc/general.md#money-and-difficulty), [technology.md](segments/misc/technology.md#how-the-game-uses-it), [neighbours.md](segments/misc/neighbours.md#how-the-game-uses-it) |
| `0x4348e0` | `ResetGameState` | Clears the date, pending disaster, military base and some options; founding year 1900; building centre (64, 64) | [general.md](segments/misc/general.md#header-and-date) |
| `0x408290` | `LoadOptions`? | Reads the speed, sound, music, auto-goto, auto-budget, disaster and autosave options from the `[OPTIONS]` settings | [general.md](segments/misc/general.md#game-options) |
| `0x484cd0` | `ImportClassicSettings`? | SimCity Classic import: sea level, coast and river flags, national figures, technology years | [technology.md](segments/misc/technology.md#how-the-game-uses-it) |
| `0x44cb60` | `NewCity` | Clears XTXT, labels 1–255 and XTHG; sets label 0 to the registered mayor's name; writes the system codes into XMIC records 0–9, zeroes records 1–9 and names them in XLAB slots 52–60 | [xlab.md](segments/xlab.md#slot-0-the-mayor), [xmic.md](segments/xmic.md#records) |

## Byte-order helpers

| Address | Our name | What it does |
|---|---|---|
| `0x42a930` | `Swap32` | Reverses the 4 bytes of a 32-bit value |
| `0x42a960` | `TagValue` | Loads a 4-character tag as a value comparable with what was read from the file |
| `0x42a7e0` | `Swap32Array` | Swaps every 4-byte word of a buffer (MISC, XGRP) |
| `0x42a880` | `Swap16Array` | Swaps every 2-byte word of a buffer (ALTM) |
| `0x42a820` | `SwapMicrosims` | Swaps the 16-bit words at bytes 2, 4 and 6 of every 8-byte record (XMIC) |
| `0x42a8c0` | `Swap16` | Reverses the 2 bytes of a 16-bit value |
| `0x42a900` | `Swap16InPlace` | Swaps a 16-bit value in place (SCEN fields) |

## Simulation cycle

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x413520` | `SimulationStep` | Increments the day counter (MISC word 4), then runs step = day mod 25: step 0 → `0x40d400`, the year-end budget `0x4731e0` while MISC word 911 is set, the monthly budget `0x4734a0`, and clears MISC words 380–387; step 1 → `0x467860` (power); step 2 → `0x46a840` (overlays); steps 3–18 → `0x4358b0` (zone growth); step 19 → `0x4586f0` (traffic decay); step 20 → `0x464550` (water); step 21 → `0x468150`, `0x468d30` and `0x413e50` (graphs); step 22 → the population milestones (MISC words 8 and 30). In months 3 and 7 it opens the newspaper when subscribed | [minimaps.md](segments/minimaps.md#how-the-game-uses-it), [xgrp.md](segments/xgrp.md#when-samples-are-taken), [xbit.md](segments/xbit.md#what-to-trust), [population.md](segments/misc/population.md#zone-population), [budget.md](segments/misc/budget.md#every-month) |
| `0x4570f0` | `YieldToWindows`? | Called every 5000 tiles during the floods (**Inferred**: message pump) | [xbit.md](segments/xbit.md#what-to-trust) |

## View rotation and screen position

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x465490` | `RotateViewClockwise` | Moves every map as `new[row][col] = old[col][127 − row]`; remaps XBLD, XTER and XUND codes through the clockwise tables; flips XBIT `0x02` on bridges and on-ramps; compass − 1. XZON, ALTM and XTXT bytes move unchanged | [conventions.md](conventions.md#orientation) |
| `0x466660` | `RotateViewCounterClockwise` | The exact inverse of `0x465490`; compass + 1 | [conventions.md](conventions.md#orientation) |
| `0x465f00`, `0x467100` | `RotateOverlays` (cw, ccw) | Turn the 64 × 64 and 32 × 32 overlays; the values are not changed | [minimaps.md](segments/minimaps.md#rotation) |
| `0x466410`, `0x467610` | `RotateThings` (cw, ccw) | Turn XTHG records 1–39 (tile, direction, target or next tile, airplane runway heading) and the unsaved unit-position tables | [xthg.md](segments/xthg.md#rotation) |
| `0x444e20` | `TileToScreen` | Screen position of a tile: x = (row − col)·w, y = (row + col)·w/2 − altitude·h | [conventions.md](conventions.md#orientation) |
| `0x444bf0` | `ScreenToTile` | Inverse of `0x444e20` (keeps the view centred when it turns) | — |
| `0x410950` | `GenerateTerrain` | Makes a new map; sets the compass to 0 | [conventions.md](conventions.md#orientation) |

## Drawing

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x43c730` | `DrawTile` | Draws a tile. Mirror rules for XBIT `0x02`: never below XBLD `0x1D`, the flag itself for `0x1D`–`0x6F`, flag XOR odd compass from `0x70`. Draws a building only at the footprint corner given by `0x4dc4c8`. Adds an extra sprite when XBIT `& 0xC0 == 0x80` | [xbit.md](segments/xbit.md#0x02-mirror-rotate), [xzon.md](segments/xzon.md#corner-bits) |
| `0x43d620`, `0x43e550` | `DrawTile` variants | Same height, mirror and sprite logic as `0x43c730` (`0x43d620` uses zoned ground sprites from `0x316`) | [xter.md](segments/xter.md#how-the-game-uses-it), [xzon.md](segments/xzon.md#zone-types) |
| `0x485230` | `DrawSprite` | Sprite blitter; its 4th argument is the mirror flag | [xbit.md](segments/xbit.md#0x02-mirror-rotate) |
| `0x43bb00` | `DrawDataOverlay` | City-view data overlay; the mode in `0x4ca404` picks XTRF, XPOP, XROG, XCRM, XPLC, XPLT, XVAL, XFIR, power, water or growth bands; values below 16 are not drawn | [minimaps.md](segments/minimaps.md#cell-mapping) |
| `0x4886c0`, `0x4891b0`, `0x489300`, `0x488f10`, `0x489060`, `0x489450`, `0x488c60`, `0x488db0` | `DrawTrafficMap` … `DrawGrowthMap` | Map-window bitmaps for XTRF, XPLT, XVAL, XCRM, XPLC, XFIR, XPOP, XROG: 16 shades of `value >> 4`; XROG in three bands | [minimaps.md](segments/minimaps.md#cell-mapping) |
| `0x4888f0`, `0x488ab0` | `DrawPowerMap`, `DrawWaterMap` | Draw the power and water map bitmaps | [xbit.md](segments/xbit.md#how-the-game-uses-it) |
| `0x44d9a0` | `DrawTileText` | Draws a sign or connection text (when labels are shown), or the moving object of XTXT `0xC9`–`0xF0` via `0x450bb0` | [xtxt.md](segments/xtxt.md#how-the-game-uses-it) |
| `0x450bb0` | `DrawThing` | Draws XTHG record *n*; clears a stale marker whose record is elsewhere (not for train types 10, 11) | [xtxt.md](segments/xtxt.md#moving-objects-0xc90xf0), [xthg.md](segments/xthg.md#the-xtxt-marker) |
| `0x46f4a2` | `DrawGraphs` | Graph window: draws slots 11 → 0, 31 → 12 or 51 → 32 of each series, scaled by `0x4cc6f0` | [xgrp.md](segments/xgrp.md#how-the-graph-window-reads-it) |
| `0x47a240` | `ExpandMessage` | Expands message and newspaper story templates (with a temporary OEM conversion); keeps a story's phrase choices in its slot | [xlab.md](segments/xlab.md#how-the-game-uses-it), [newspapers.md](segments/misc/newspapers.md#how-the-queue-works) |

## Terrain and water

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x447020` | `ReshapeTile` | Recomputes a tile's XTER slope from its 8 neighbours' altitudes (table `0x4e7958`); raises the tile if all four corners would be up; applies the sea level (underwater or shore code, water height, XBIT `0x04`) | [xter.md](segments/xter.md#slope-and-altitude) |
| `0x4460b0` | `FloodToLevel` | Turns every tile below a level into water at that level, and drains water tiles at or above it | [xter.md](segments/xter.md#water) |
| `0x446360`, `0x4463d0` | `SeaLevelUp`, `SeaLevelDown` | Raise (up to 31) or lower (down to 0) the sea level, MISC word 912, by one and flood to it with `0x4460b0`; reached only through the window's message map | [xter.md](segments/xter.md#water) |
| `0x446650` | `PlaceWater` | Turns a tile into surface water (`0x30`–`0x45`, or `0x3E` on a slope) and re-evaluates the neighbouring water | [xter.md](segments/xter.md#surface-water) |
| `0x4441c0` | `RaiseTerrain` | Raises a tile by one level, first raising lower neighbours recursively; refuses at altitude 30; takes 25 per tile off MISC word 5 (funds, **Inferred**); clears the XBIT `0x08` mark set by `0x443cb0` | [altm.md](segments/altm.md#altitude-bits-04), [xbit.md](segments/xbit.md#0x08-developed-land-mask-and-scratch-mark) |
| `0x443cb0` | `CheckRaise`? | Recursive check before a raise; marks visited tiles with XBIT `0x08` | [xbit.md](segments/xbit.md#0x08-developed-land-mask-and-scratch-mark) |
| `0x445b50` | `RaiseLowerTerrain` | Raises or lowers a tile by one level | [altm.md](segments/altm.md#how-the-game-uses-it) |
| `0x447c70` | terraforming tool? | Raises or lowers the altitude by one level (only that part read) | [altm.md](segments/altm.md#how-the-game-uses-it) |
| `0x448a90` | `CheckPlacement`? | One of the placement checks that test the XTER kind (`0x10`, `0x20`, `0x30`, `0x40`) to allow or refuse building | [xter.md](segments/xter.md#how-the-game-uses-it) |
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
| `0x458d40` | `DestroyTile`? | Among other things collapses a tunnel and clears ALTM bits 10–15; destroys worn-out power plants and launched arcologies; clears a destroyed stadium's sport bit in MISC word 1034 | [altm.md](segments/altm.md#tunnel-depth-bits-1014), [xmic.md](segments/xmic.md#misc-words-set-from-xmic) |
| `0x443270` | `Bulldoze` | Removes surface water; in the underground view demolishes tunnels (depth 1) | [xter.md](segments/xter.md#water) |
| `0x448f50` | `CheckPlacement`? | In the underground view refuses tiles with tunnel depth 1 or 2 | [altm.md](segments/altm.md#tunnel-depth-bits-1014) |

## Buildings

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x45b1c0` | `FindBuildingAnchor` | Given a tile and its XBLD code, returns the footprint size and moves to the anchor (corner D: min row, max col); highways snap to the even 2 × 2 grid | [xbld.md](segments/xbld.md#the-anchor) |
| `0x441f00` | `SetTile` | Writes an XBLD code and keeps the per-code tile count (`0x4ca4c8`) or, for military-zone tiles, the military count (`0x4ca934`) in step | [xbld.md](segments/xbld.md#how-the-game-uses-it), [tile-counts.md](segments/misc/tile-counts.md#military-counts-words-10021017) |
| `0x4382b0` | `PlaceTile` | Places runway and pier pieces and sets their XBIT `0x02` flag (also sets `0xC0`); places the 3 × 3 missile silo (XBLD `0xF9`, XUND `0x22`) and its corner bits from `0x4dc4d0` | [xbit.md](segments/xbit.md#0x02-mirror-rotate), [xund.md](segments/xund.md#values) |
| `0x436e80` | `PlaceBuildingTile` | Places a building tile: corner bits `0xF0` for one-tile items, XBIT `0xE0` on every tile of the footprint; a new one-tile home's wealth comes from XVAL >> 6 (at most 2) | [xzon.md](segments/xzon.md#corner-bits), [xbit.md](segments/xbit.md#how-the-game-uses-it), [minimaps.md](segments/minimaps.md#how-the-game-uses-it) |
| `0x440170` | `OnBuildingPlaced`? | Per-building side effects; for XBLD `0xE9` writes XUND `0x23` | [xund.md](segments/xund.md#how-the-game-uses-it) |
| `0x440c50` | `ClearTileFlags`? | XBIT `&= 0x1F` (clears `0x80`, `0x40`, `0x20`) when tiles are cleared | [xbit.md](segments/xbit.md#how-the-game-uses-it) |
| `0x4417b0` | sets XBIT `0x02`? | One of the writers of the XBIT `0x02` flag (only that part read) | [open-questions.md](open-questions.md#xbit) |
| `0x441400` | `PlacePowerLine`? | XBIT `|= 0x80`, then recomputes power | [xbit.md](segments/xbit.md#0x80-conducts-power) |
| `0x42b520` | `GetNameString` | Loads a string by (category, index): 1000 → id 547 + index (neighbour names), 1001 → 583 + index, 2000 → 593 + index, 2100 → the table at `0x4e7140`, 2200 → 776 + index. Only the neighbour-name callers (category 1000) were found | [neighbours.md](segments/misc/neighbours.md#names), [open-questions.md](open-questions.md#xbld) |

## Zones and underground

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x4142c0` | `PlaceMilitaryBase` | Claims an 8 × 8 area: sets zone 7 on flat, dry, unzoned, unbuilt tiles and takes them out of the per-code tile count; records the outcome in MISC word 915 | [xzon.md](segments/xzon.md#zone-7-the-military-base), [general.md](segments/misc/general.md#terrain-options-and-the-military-base) |
| `0x437f90` | `CornerToBlock` | Converts a corner bit to a view corner ((k − compass) & 3) and rebuilds the 2 × 2 block it belongs to | [xzon.md](segments/xzon.md#which-bit-is-at-which-corner) |
| `0x437310` | `ClearZone`? | Clears a tile's zone, keeping the corner bits | [xzon.md](segments/xzon.md#how-the-game-uses-it) |
| `0x442370` | `SetUnderground` | The only XUND writer; keeps the subway tile count | [xund.md](segments/xund.md#how-the-game-uses-it) |
| `0x44bb20` | `LaySubway` | Empty → `0x01`, pipe → crossing, then fixes the shapes | [xund.md](segments/xund.md#how-the-game-uses-it) |
| `0x44bc40` | `FixSubwayShape` | Chooses a subway piece from the tile's XTER slope or its neighbours | [xund.md](segments/xund.md#piece-shapes) |
| `0x44b790` | `LayPipe` | Empty → `0x10`, subway → crossing, sets XBIT `0x20`, then fixes the shapes | [xund.md](segments/xund.md#how-the-game-uses-it) |
| `0x44b8d0` | `FixPipeShape` | Same as `FixSubwayShape` for pipes (+ `0x10`) | [xund.md](segments/xund.md#piece-shapes) |
| `0x441b80` | `PlaceSubwayToRail` | XUND `0x23`, XBLD `0x6C`–`0x6F`, clears XBIT `0x20` | [xund.md](segments/xund.md#how-the-game-uses-it) |

## Text, labels and microsims

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x44d7c0` | `SignTool` | Places, edits or removes a sign (XTXT 1–50); refuses tiles with XTXT `0x33` and up | [xtxt.md](segments/xtxt.md#signs-0x010x32) |
| `0x44d970` | `FindFreeSign` | First sign number 1–50 not in use | [xtxt.md](segments/xtxt.md#signs-0x010x32) |
| `0x44ced0` | `DeleteText` | Removes a sign and its label, or frees the XMIC record (byte 0 = 0) and XLAB slot of values `0x3D`–`0xC8`; clears XTHG byte 10; leaves `0x34`–`0x3C` alone | [xtxt.md](segments/xtxt.md#how-the-game-uses-it), [xmic.md](segments/xmic.md#records-10149-buildings) |
| `0x44cf60` | `AllocMicrosim` | Picks an XMIC record for a building code (table `0x4e7c9a`): refuses codes below `0xC6`; kinds from `0x11` reuse the fixed record kind − `0x10`; when the table is full an arcology takes the first record below `0xFB`. Writes byte 0 and the default name, calls `0x44ddc0`, returns the XTXT value | [xtxt.md](segments/xtxt.md#microsims-0x3d0xc8), [xmic.md](segments/xmic.md#records-10149-buildings) |
| `0x44ddc0` | `InitMicrosim` | Starting figures of a placed microsim; adds to the counts of the city-wide systems | [microsim-kinds-xmic.md](reference/microsim-kinds-xmic.md) |
| `0x44f7d0` | `CapByPopulation` | min(*x*, (MISC 1035 + MISC 1032 + arcology bonus) ÷ *d*) | [xmic.md](segments/xmic.md#when-the-figures-change) |
| `0x44f890` | `FindMicrosimTile` | Finds a tile of the building of an XMIC record (for wear-out) | [microsim-kinds-xmic.md](reference/microsim-kinds-xmic.md#power-plants-0xc90xcf) |
| `0x44d1b0`, `0x44d1d5` | `OpenMicrosimWindow` | Click on a microsim tile: opens the microsim window (or queries the tile with `0x4719a0` when the record is free); `0x44d1d5` opens it for renaming | [xmic.md](segments/xmic.md#the-microsim-window) |
| `0x427080`, `0x4272e0`, `0x427720` | `MicrosimDialog` | The microsim window: constructor, set-up (sprite, name from XLAB), painting of the five statistics lines and the building's sound | [xmic.md](segments/xmic.md#the-microsim-window) |
| `0x44d360` | `DrawMicrosimLine` | Expands one template line (`#0`–`#3`, `#G`, `#S`, `#T`, `#W`) for an XMIC record | [xmic.md](segments/xmic.md#the-microsim-window) |
| `0x4501af` | `LoadMicrosimStrings` | At start-up: default names (strings 831–888) and window templates (`0x4e7da0`), with language patches | [microsim-kinds-xmic.md](reference/microsim-kinds-xmic.md) |
| `0x414b40` | `MayorHouseClick`? | Runs for XTXT `0x6F` before the window: the opinion poll `0x472510` and the mayor's-house update of every record | [xmic.md](segments/xmic.md#when-the-figures-change) |
| `0x47c970` | `TeamDialog` | Asks for a new stadium's team (opened from `0x440170`) | [microsim-kinds-xmic.md](reference/microsim-kinds-xmic.md#stadium-0xd7) |
| `0x48cdd8` | `rand` | C library random number; the monthly population routine `0x468d30` and the opinion poll use it | [microsim-kinds-xmic.md](reference/microsim-kinds-xmic.md) |
| `0x47fc70`, `0x47faf0`, `0x47faa0` | `Random6`, `RandomMod`, `LcgMod` | The shift register of `0x47fcb0` as 0–63 and mod *n*; a linear congruential generator (× `0x41C64E6D` + `0x3039`) mod *n* | [microsim-kinds-xmic.md](reference/microsim-kinds-xmic.md) |
| `0x44e390` | `UpdateMicrosims` | Yearly (from `0x4734a0` in January): recomputes XMIC records 1–149, wears out power plants, launches Launch arcologies; sets MISC word 1032 (arcology residents), 1038 (police arrests) and 1039 (prisons have room) | [xmic.md](segments/xmic.md#when-the-figures-change), [microsim-kinds-xmic.md](reference/microsim-kinds-xmic.md) |
| `0x449bb0`, `0x449f50`, `0x4621f0` | `RoadTool`, `RailTool`, `HighwayTool` | At the map edge, offer a neighbour connection (strings 109, 110, 246) and write XTXT `0xFA` | [xtxt.md](segments/xtxt.md#neighbour-connections-0xfa) |
| `0x44aed0`, `0x44b3a0`, `0x462910` | connection readers? | Read XTXT `0xFA` on edge tiles (only that part read) | [xtxt.md](segments/xtxt.md#how-the-game-uses-it) |
| `0x450690`, `0x452d00`, `0x452fa0`, `0x453280`, `0x453460`, `0x453600`, `0x455500`, `0x45f090`, `0x442a30` | moving-object routines | Move XTHG objects and write XTXT `0xC9` + *n* on the tile of object *n* ([§ Moving objects](#moving-objects)) | [xtxt.md](segments/xtxt.md#moving-objects-0xc90xf0), [xthg.md](segments/xthg.md#the-xtxt-marker) |

## Moving objects

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x450890` | `AnimateThings` | Resets the per-type counters, then updates records 1–39 by type (1 `0x453980`, 2 `0x4540c0`, 3 `0x456760`, 5 `0x4548c0`, 6 `0x4546f0`, 9 `0x4562e0`, 10/12 `0x455500`, 15 `0x454ea0`, 16 `0x455020`); types 4, 7, 8, 11, 13, 14 are not updated | [xthg.md](segments/xthg.md#how-the-game-uses-it) |
| `0x450850` | `FindFreeThing` | First record 1–39 with type 0; 0 when none | [xthg.md](segments/xthg.md#allocation) |
| `0x450690` | `PlaceUnit` | Creates a dispatched unit (7, 8, 14) on a tile; in city mode 2 takes record 39 when none is free | [xthg.md](segments/xthg.md#allocation) |
| `0x4507d0` | `RemoveThing` | Frees a record and writes 0 on its tile | [xthg.md](segments/xthg.md#the-xtxt-marker) |
| `0x4516f0` | `MoveThing` | Moves a record one step at its type's speed; puts the old tile's XTXT value back from byte 10, keeps the new tile's in byte 10, writes the marker; skips tiles with another marker | [xthg.md](segments/xthg.md#the-xtxt-marker) |
| `0x452830`, `0x452ba0`, `0x452d00` | `SpawnAirplane`, `SpawnHelicopter`, `SpawnShip` | Airplane from a map edge or a runway; helicopter on a runway; cargo ship two tiles inside a map edge on deep water | [thing-ids-xthg.md](reference/thing-ids-xthg.md) |
| `0x453380`, `0x453460` | `SpawnSailboat` | Sailboat on water next to a marina | [thing-ids-xthg.md](reference/thing-ids-xthg.md#9-sailboat) |
| `0x453590`, `0x453600` | `SpawnTrain` | Engine and two cars on rail next to a rail station; refuses a fifth engine | [thing-ids-xthg.md](reference/thing-ids-xthg.md#1013-trains) |
| `0x452fa0` | `SpawnRescuer` | Flying rescuer (type 16); its target marker (byte 11) from the disaster number | [thing-ids-xthg.md](reference/thing-ids-xthg.md#16-flying-rescuer) |
| `0x453280` | `MakeExplosion` | Explosion (type 6) at a tile, with height, disaster number and fire flag | [thing-ids-xthg.md](reference/thing-ids-xthg.md#6-explosion) |
| `0x453980`, `0x4540c0`, `0x456760`, `0x4562e0` | `UpdateAirplane`, `UpdateHelicopter`, `UpdateShip`, `UpdateSailboat` | Move the vehicles; the helicopter also gives the traffic report over XTRF above 169 | [thing-ids-xthg.md](reference/thing-ids-xthg.md) |
| `0x454520`, `0x454650` | `AvoidArcology`, `AircraftCollision` | Turn an aircraft away from an arcology ahead; make a helicopter near an airplane crash | [thing-ids-xthg.md](reference/thing-ids-xthg.md) |
| `0x4546f0`, `0x4548c0`, `0x454ea0`, `0x455020` | `UpdateExplosion`, `UpdateMonster`, `UpdateTornado`, `UpdateRescuer` | Disaster objects; the explosion can start fires (MISC word 28) | [thing-ids-xthg.md](reference/thing-ids-xthg.md) |
| `0x455500`, `0x455dc0`, `0x455ef0`, `0x455fc0` | `UpdateTrain`, `TrainOffTrack`, `FollowEngine`, `ReverseTrain` | Move an engine and its cars; switch 10 ↔ 12 on subway connections | [thing-ids-xthg.md](reference/thing-ids-xthg.md#1013-trains) |
| `0x451f10` | `DrawMonster` | Draws the monster from body parts (sprites 478–489) | [thing-ids-xthg.md](reference/thing-ids-xthg.md) |
| `0x451200` | `DrawTrain` | Draws the trains | [thing-ids-xthg.md](reference/thing-ids-xthg.md) |
| `0x456600` | thing heading? | Reads and changes the direction byte (only that part read) | [xthg.md](segments/xthg.md#layout) |
| `0x456c70` | ship route? | Cargo ship routine (not read; may set its first target) | [open-questions.md](open-questions.md#xmic-and-xthg) |
| `0x427c10` | `QueryTilePicture`? | Draws the tile picture of the query window, with a sailboat sprite on a tile with a sailboat (**Inferred**) | [xthg.md](segments/xthg.md#how-the-game-uses-it) |
| `0x45e7d0`, `0x45ee10`, `0x45f090` | disasters 18, 8, 7 | Create the crashing airplane, the monster and the tornado; the last two set MISC word 1040 | [general.md](segments/misc/general.md#disasters) |
| `0x44f910` | `StartDispatch` | Sets the unit limits; removes existing units through their markers | [thing-ids-xthg.md](reference/thing-ids-xthg.md#7-8-14-emergency-units) |
| `0x44fb50`, `0x44fd60`, `0x44ff70` | `DispatchPolice`, `DispatchFire`, `DispatchMilitary` | Place a unit with the dispatch tool | [thing-ids-xthg.md](reference/thing-ids-xthg.md#7-8-14-emergency-units) |
| `0x442a30`, `0x442db0` | level terrain, bulldozer drag | Use record 0 as a type-4 cursor marked with XTXT `0xC9` | [xthg.md](segments/xthg.md#record-0) |

## Disasters

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x45cf10` | `DispatchDisaster` | Starts the pending disaster (`0x4ca420`, MISC word 28, cases 1–18); keeps it in MISC word 1041, sets word 1042 while it runs and the city mode (word 1) to 2; sets word 1040 to −1 when it ends. A disaster stays under way while a monster, tornado, explosion or crashing airplane exists; may send the flying rescuer (`0x452fa0`) | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff), [general.md](segments/misc/general.md#disasters) |
| `0x471bc0` | `ChooseDisaster` | Picks the next disaster into MISC word 28 (hurricane weather on a coast → 16, tornado weather → 7, …); case 4 (the toxic spill, **Inferred**) starts at the most polluted 64 × 64 cell above 149 | [minimaps.md](segments/minimaps.md#how-the-game-uses-it), [general.md](segments/misc/general.md#disasters) |
| `0x45f760` | `UpdateDisasters` | Processes the XTXT markers `0xFB`–`0xFF` each tick; fire spreads with XFIR + 8 as a random threshold; zeroes XTRF under destroyed tiles | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff), [minimaps.md](segments/minimaps.md#xfir-fire-coverage) |
| `0x460e10` | `StartFire` | Writes XTXT `0xFF`; zeroes XTRF under removed road or rail | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x4611e0` | `BurnOut` | Turns a burnt tile into rubble | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x461000` | `FloodTiles` | Marks dry tiles up to a level with XTXT `0xFC`, removes signs and microsims, zeroes XTRF | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x461140` | `MoveRiotFront` | Moves `0xFE` along roads and leaves `0xFD` behind (only on XTXT below `0x33`) | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x461190` | `MarkFB` | Writes `0xFB` (only on XTXT below `0x33`) | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x4612d0` | `ClearRiot` | Clears `0xFD` and `0xFE` | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x45e920`, `0x45db00`, `0x45e5f0`, `0x45e590`, `0x45eb20`, `0x45f2b0`, `0x45f5c0`, `0x45e2a0`, `0x45e030`, `0x45e180`, `0x45d9d0`, `0x45d8b0`, `0x45d4e0` | disaster starters | The routines `0x45cf10` calls for disasters 1, 2, 3 (up to three times), 4, 6, 9, 10, 11, 12, 13, 14, 15 and 16 (7, 8 and 18 are in [§ Moving objects](#moving-objects); 5 and 17 start nothing). Fire 1, earthquake 6 and firestorm 12 call `StartFire` `0x460e10`; major flood 14 calls the flood starter `0x45db00`; mass riots 13 the riot starter `0x45e5f0`. Markers written: `0xFC` (2, 16), `0xFB` (4, 10, 15), `0xFF` and `0xFB` (11) | [general.md](segments/misc/general.md#disasters), [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x461330` | `SpreadFire`? | Spreads `0xFF` to neighbours (only that part read) | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |

## Power and water

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x467860` | `RecomputePower` | Clears XBIT `0x40` and `0x08` everywhere, floods from the power plants (XBLD `0xC6`–`0xCF`), then turns the power delivered into a percentage of plant capacity | [xbit.md](segments/xbit.md#0x40-powered) |
| `0x4679b0` | `FloodPower` | Two-pass flood through `0x80` tiles with `0x08` as the visited mark; sets `0x40` within the plants' capacity | [xbit.md](segments/xbit.md#0x40-powered) |
| `0x464550` | `RecomputeWater` | Clears XBIT `0x10` (not on water towers `0xEB`) and `0x08`, floods from powered pumps `0xDC` and desalination plants `0xFA`, sets the treatment-plant flag from the water delivered, then turns it into a percentage of pump capacity | [xbit.md](segments/xbit.md#0x10-watered) |
| `0x4649e0` | `FloodWater` | Two-pass flood through `0x20` tiles; pump capacity from fresh-water neighbours (XBIT `& 5 == 4`), desalination from salt-water ones (`== 5`) | [xbit.md](segments/xbit.md#0x10-watered) |

## Overlays and graphs

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x46a840` | `OverlayStep` | Simulation step 2: rebuilds the XBIT `0x08` mask and recomputes XPLT, XVAL, XPLC, XFIR, XPOP, XROG and XCRM, in that order; stores the XVAL, XCRM and XPLT sums in MISC words 10, 11 and 13 | [minimaps.md](segments/minimaps.md#values), [xbit.md](segments/xbit.md#0x08-developed-land-mask-and-scratch-mark) |
| `0x46ba90` | `SpreadCoverage` | Adds a police or fire station's strength to a diamond of 32 × 32 cells around it (full, then × 4/5, then less, out to distance 3) | [minimaps.md](segments/minimaps.md#xplc-police-coverage) |
| `0x46ba20` | `AddCoverageCell` | Adds to one 32 × 32 cell, clamped to 0–255 and to the grid | [minimaps.md](segments/minimaps.md#xplc-police-coverage) |
| `0x4358b0` | `ZoneGrowthStep` | Simulation steps 3–18: step 3 + 4*r* + *c* visits the tiles with row ≡ *r* and col ≡ *c* (mod 4). Grows the zones (needs XVAL above 31, 95 or 191), sends trips, adds each zone building's population units to MISC words 381–386 (abandoned ones to 387), grows the military base (MISC word 915), and lets underfunded transport decay | [minimaps.md](segments/minimaps.md#how-the-game-uses-it), [population.md](segments/misc/population.md#zone-population) |
| `0x457620` | `AddTripTraffic`? | Adds a trip's weight to XTRF along its route, capped at 255; counts bus, rail and subway trips (`0x4c85a0`, `0x4c85a4`, `0x4c8600`) | [minimaps.md](segments/minimaps.md#xtrf-traffic), [microsim-kinds-xmic.md](reference/microsim-kinds-xmic.md#bus-rail-and-subway-0xec-0xed-0xe9) |
| `0x4586f0` | `DecayTraffic` | Step 19: XTRF −= XTRF >> 2 everywhere; stores the sum in MISC word 12 | [minimaps.md](segments/minimaps.md#xtrf-traffic) |
| `0x4540c0` | `TrafficReport`? | Triggers the traffic helicopter report on a cell above 169, at most every 5 s | [minimaps.md](segments/minimaps.md#xtrf-traffic) |
| `0x468d30` | `MonthlyPopulation` | Step 21: the national economy (MISC words 20–23) and the neighbours' figures; the age groups (deaths, ageing with schools and colleges, births with hospitals, migration) and MISC words 17–19; the industries (demand, sizes), the industry bonus and the heavy-industry level (words 1036, 1037) | [population.md](segments/misc/population.md#how-the-age-groups-change), [minimaps.md](segments/minimaps.md#xplt-pollution) |
| `0x468150` | `SumPopulation` | Step 21, before `0x413e50`: MISC word 380 = sum of words 381–386, word 1035 = 10 × word 380 (also added to word 16); the people to add or remove; the three tax bases (budget entries 0–2) and the R, C, I demand; word 29 and the weather (words 24–27); one due invention (1 in 8); newspaper stories | [xgrp.md](segments/xgrp.md#graph-series), [population.md](segments/misc/population.md#zone-population), [demand.md](segments/misc/demand.md#how-the-game-uses-it) |
| `0x413e50` | `UpdateGraphs` | Day 21 of each month: computes the 16 newest values, shifts the monthly block, and the half-year (months 0 and 6) and five-year (month 0 of years divisible by 5) blocks; raises the graph maxima | [xgrp.md](segments/xgrp.md#when-samples-are-taken) |

## Budget, bonds and ordinances

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x4734a0` | `MonthlyBudget` | Day 0 of each month: at the year end adds each entry's year to date ÷ (12 × divisor) to the funds and zeroes it; records the month's count and rate and adds count × rate to the year to date; sets MISC word 911 after month 11; recounts budget entries 3–15; may enact a random ordinance | [budget.md](segments/misc/budget.md#every-month), [ordinances.md](segments/misc/ordinances.md#how-the-game-uses-it) |
| `0x4731e0` | `ShowYearBudget` | Opens the year's Budget window unless auto-budget is on | [budget.md](segments/misc/budget.md#the-year-end) |
| `0x416d70` | `BudgetWindowLayout`? | Lays out the Budget window; uses the year-end layout while MISC word 911 is 1 (only that part read) | [general.md](segments/misc/general.md#budget-year-end) |
| `0x4189c0` | `UpdateBudgetWindow` | Each line's estimate for the year and the totals, as year to date ÷ (12 × divisor) | [budget.md](segments/misc/budget.md#layout) |
| `0x418330` | `StepTaxRates` | Budget window arrows: all three property tax rates ± 1 (industry through `0x473d80`), 0–20 | [budget.md](segments/misc/budget.md#rates) |
| `0x4184a0`, `0x418590`, `0x418680`, `0x418770`, `0x4188b0` | `StepFunding` | Budget window arrows for police, fire, health, education, transport: funding ± 5, 0–100 | [budget.md](segments/misc/budget.md#rates) |
| `0x419b60`, `0x419f60`, `0x419fa0`, `0x419ff0` | property tax dialog | Shows and sets the residential, commercial and industrial rates | [budget.md](segments/misc/budget.md#rates) |
| `0x41c2b0`, `0x41c880` … `0x41ca10` | transport dialog | Shows and sets the six transport fundings | [budget.md](segments/misc/budget.md#rates) |
| `0x473d80` | `ChangeIndustryTaxes` | Adds a delta to all 11 industry tax rates (0–20) and stores their rounded average as the industrial tax rate | [budget.md](segments/misc/budget.md#rates), [population.md](segments/misc/population.md#industries) |
| `0x41b270` | `BondButton` | Recomputes the city value (`0x46a270`) and offers a bond at the Fed Rate + 1, unless bonds × 2500 ÷ (value + 1) ≥ 6 | [population.md](segments/misc/population.md#bonds), [general.md](segments/misc/general.md#money-and-difficulty) |
| `0x473a40` | `IssueBond` | Refuses at 50 bonds; on yes: funds + 10 000, the rate into the next slot, bond count + 1, budget entry 4's count and average rate. Ghidra shows the same code again as `0x473a5f` | [population.md](segments/misc/population.md#bonds), [budget.md](segments/misc/budget.md#rates) |
| `0x46a270` | `ComputeCityValue` | MISC word 9 from the per-code tile counts minus the subway count | [general.md](segments/misc/general.md#money-and-difficulty) |
| `0x40ca30` | `CheatCodes`? | Typed cheat codes; one issues a bond at 25 % | [population.md](segments/misc/population.md#bonds) |
| `0x42bca0` | `OrdinanceAmount` | Monthly amount of ordinance *n* (0–19) from the tax bases | [ordinances.md](segments/misc/ordinances.md#amounts) |
| `0x4194b0` | `InitOrdinanceWindow` | Ticks the enacted ordinances and totals their amounts per group of four and overall (budget entry 3) | [ordinances.md](segments/misc/ordinances.md#bits) |
| `0x419680` | `ToggleOrdinance` | Flips bit *n* of MISC word 1000 and updates the totals | [ordinances.md](segments/misc/ordinances.md#how-the-game-uses-it) |
| `0x41a490` | `AdvisorMessages` | Picks each advisor's message from the budget, ordinances, demand and statistics (the education advisor compares ages 5–14 and 15–19 with school and college capacity) | [budget.md](segments/misc/budget.md#what-the-rates-do-in-the-simulation), [ordinances.md](segments/misc/ordinances.md#bits), [population.md](segments/misc/population.md#how-the-game-uses-it) |

## Population, rewards, technology and newspapers

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x46db92` | `DrawPopulationWindow` | Population window: a bar per age group (people, average life expectancy, average education) and MISC words 17–19 | [population.md](segments/misc/population.md#age-groups) |
| `0x47fcb0` | `Random7` | 16-bit shift-register random number (feedback `0x1BF5`), 0–127 | [population.md](segments/misc/population.md#how-the-industries-change) |
| `0x441e50` | `SetRewardAvailable` | Sets or clears a bit of MISC word 30; updates the Rewards button when the mask changes between 0 and non-zero | [general.md](segments/misc/general.md#rewards) |
| `0x43f220` | `PlaceReward`? | Clears a reward's bit of MISC word 30 when it is built | [general.md](segments/misc/general.md#rewards) |
| `0x412290` | `CheatEverything`? | Clears the technology years, sets the reward level to 6 and the reward bits, adds 500 000 to the funds | [general.md](segments/misc/general.md#rewards), [technology.md](segments/misc/technology.md#how-the-game-uses-it) |
| `0x46a080` | `UpdateToolMenus` | Rebuilds the build menus from the technology years (Nuclear Free Zone blocks nuclear) and the military base type | [technology.md](segments/misc/technology.md#layout) |
| `0x46c562` | `DrawNeighboursWindow` | Neighbors window: neighbour (compass + 0/1/2/3) & 3 at bottom-right / bottom-left / top-left / top-right | [neighbours.md](segments/misc/neighbours.md#which-edge-each-neighbour-is-on) |
| `0x422f10` | `DrawDemandIndicator` | R, C, I bars: value × half height ÷ 2000 | [demand.md](segments/misc/demand.md#how-the-game-uses-it) |
| `0x406d70` | `BuildNewspaperMenu` | Lists papers 0 … min(MISC word 8, 5) by name; ticks MISC words 1025–1027; no menu in the terrain editor | [newspapers.md](segments/misc/newspapers.md#papers-words-916945) |
| `0x479610` | `InitNewspapers` | Allocates the paper and story buffers, sets the newspaper options and calls `0x479730` | — |
| `0x479730` | `ShuffleNewspapers` | Gives the six papers shuffled names, layouts, price styles and columns; fills the nine story slots with types 11–19 | [newspapers.md](segments/misc/newspapers.md#papers-words-916945) |
| `0x47b5c0` | `PostStory` | Inserts a story into slots 0–6 by priority; opens the paper for major stories when MISC word 1026 is on | [newspapers.md](segments/misc/newspapers.md#how-the-queue-works) |
| `0x479a70` | `DecayStories` | Monthly: lowers each queued story's priority and re-sorts slots 0–6 | [newspapers.md](segments/misc/newspapers.md#how-the-queue-works) |
| `0x477880` | `OpenNewspaper` | Newspaper dialog: writes slot 7 (weather) and slot 8 (opinion column) | [newspapers.md](segments/misc/newspapers.md#the-three-kinds-of-slot) |
| `0x477c12` | `DrawNewspaper` | Masthead, price wording by era, opinion and weather columns | [newspapers.md](segments/misc/newspapers.md#papers-words-916945) |
| `0x47b400` | `WeatherReport` | Formats heat − 100, wind and humidity for the newspaper | [general.md](segments/misc/general.md#weather) |
| `0x4791d0` | `LoadNewspaperData` | Loads the story templates and per-type tables from the `DATA` file (records 1000–1005) | [newspapers.md](segments/misc/newspapers.md#how-the-queue-works) |
| `0x472510` | `OpinionPoll` | Samples 100 citizens over 7 problems, ranks them (`0x4c9410`) and sets the mayor rating (`0x4e9904`) | [newspapers.md](segments/misc/newspapers.md#the-three-kinds-of-slot) |
| `0x405650` | `InitInstance`? | Start-up: loads the month names into `0x4c71f8` and the tool menu names (table `0x4e9a58`) | [xgrp.md](segments/xgrp.md#when-samples-are-taken) |
| `0x445460` | `ViewCentreTile`? | The tile under a screen point, packed as low byte and high byte (used by the saver for MISC word 1028) | [general.md](segments/misc/general.md#view) |

## Scenarios

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x42f500`, `0x42f820` | `StartScenario` | The two routines that start a scenario: `0x4345c0`, then read `TEXT 0x81` (message `0xEF` if it is missing), convert it and show it | [scen.md](segments/scen.md#how-the-game-uses-it), [text.md](segments/text.md#how-the-game-uses-it) |
| `0x4345c0` | `LoadScenario` | Loads the file as a city (`0x4300e0`), sets the scenario flag `0x4cad44`, reads `SCEN 0x80` into `0x4cc918` and swaps its fields from big-endian, copies the disaster to MISC word 28 (`0x4ca420`) and the spot to `0x4caa58`/`0x4caa5c` (string 239 if there is no SCEN) | [scen.md](segments/scen.md#how-the-game-uses-it) |
| `0x413520` step 22 | `ScenarioCheck` | Monthly, while `0x4cad44` is set: all goals met → `0x4149b0(1)`; otherwise the time limit goes down by one, and at 0 → `0x4149b0(0)`. For every city: funds below −100000 → `0x4149b0(2)` | [scen.md](segments/scen.md#how-the-game-uses-it) |
| `0x4149b0` | `EndScenario` | 0 lost (string 286), 1 won (string 285), 2 bankrupt (string 287); clears `0x4cad44`; after a loss sets the game state `0x4c7334` to 7 | [scen.md](segments/scen.md#how-the-game-uses-it) |
| `0x405ab0` | `MainTick` | Among other things starts the pending disaster (`0x45cf10`) on the next tick | [scen.md](segments/scen.md#how-the-game-uses-it) |
| `0x425854` | `ScenarioListInit`? | Scenario dialog set-up: lists the `*.scn` files, loads `pal_mac.bmp` from the graphics folder, creates the 65 × 65 bitmap | [pict.md](segments/pict.md#how-the-game-uses-it) |
| `0x426180` | `ReadScenarioListData` | Reads `PICT 0x80` and `TEXT 0x80` of one scenario file | [text.md](segments/text.md#how-the-game-uses-it), [pict.md](segments/pict.md#how-the-game-uses-it) |
| `0x425e40` | `DrawScenarioEntry` | Draws one list entry: the picture, and `TEXT 0x80` beside it (`\r` and tab shown as spaces) | [text.md](segments/text.md#how-the-game-uses-it), [pict.md](segments/pict.md#how-the-game-uses-it) |
| `0x426260` | `IsScenarioFileNew` | True if the file's creation date is after 1993-12-30; picks how the picture's colours are mapped | [pict.md](segments/pict.md#colours) |
| `0x42a080` | `ConvertScenarioText` | `OemToCharA`, 31 character fix-ups, and drops a `-` before `!:?;,.`; the net effect is reading Mac Roman | [text.md](segments/text.md#character-set) |
| `0x42dc20` | `ShowMessageWindow`? | Shows the `TEXT 0x81` briefing | [text.md](segments/text.md#how-the-game-uses-it) |
| `0x474bc0` | `CreateDib8` | Creates an 8-bit top-down bitmap | [pict.md](segments/pict.md#how-the-game-uses-it) |
| `0x475790` | `CopyPictPixels` | Copies 65 rows of 65 bytes, 66 bytes apart, from body + 4 | [pict.md](segments/pict.md#layout) |
| `0x4759b0` | `SetDibColours` | Gives the bitmap the `PAL_MAC.BMP` colours | [pict.md](segments/pict.md#colours) |
| `0x475e20`, `0x475c90` | `RemapDibOld` | Files dated up to 1993-12-30: nearest-colour remap without the swap | [pict.md](segments/pict.md#colours) |
| `0x475e60` | `RemapDibSwap` | Swaps colours 0 and 255, then the nearest-colour remap | [pict.md](segments/pict.md#colours) |
| `0x476d60` | `NearestColour` | Index of the nearest of *n* colours; `0x475e60` passes the game palette, which it gets from the application object `0x4c7010` through `0x4069b0` | [pict.md](segments/pict.md#colours) |
| `0x45e590` | disaster 4 | Writes XTXT `0xFB` at the disaster spot | [scen.md](segments/scen.md#the-disaster-spot) |
| `0x45f2b0` | meltdown | Uses the disaster spot; without one, the first XBLD `0xCB` tile | [scen.md](segments/scen.md#the-disaster-spot) |

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
| `0x4cb3ec` | XMIC ([xmic.md](segments/xmic.md)) | 150 × 8 |
| `0x4ca434` | XTHG ([xthg.md](segments/xthg.md)) | 40 × 12 |
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

### MISC variables

Every variable the MISC loader `0x431330` fills, in word order ([misc/README.md](segments/misc/README.md#word-index)). "16" and "8" mark variables narrower than 32 bits.

| Address | Holds | MISC word |
|---|---|---|
| `0x4e72a8` | Temporary buffer for the decoded segment | — |
| `0x4ca42c` (16) | City mode: 0 terrain editor, 1 city, 2 disaster | 1 |
| `0x4c942c` (16) | Compass (0–3) | 2 |
| `0x4ca5f4` (16) | Founding year | 3 |
| `0x4cae04` | Day counter: 25-day months, 300-day years; simulation step = value mod 25 | 4 |
| `0x4ca444` | Funds | 5 |
| `0x4cc4e8` | Bonds outstanding | 6 |
| `0x4cb404` (16) | Difficulty 0–3 | 7 |
| `0x4cb010` (16) | Reward level (population milestones passed) | 8 |
| `0x4ca4d0` | City value | 9 |
| `0x4ca440`, `0x4cb454`, `0x4cc6f4`, `0x4cc910` | Sums of XVAL, XCRM, XTRF (after the decay), XPLT | 10–13 |
| `0x4cad28`, `0x4cb018` | Never used | 14, 15 |
| `0x4ca5f0` | Population-months (written, never read) | 16 |
| `0x4cb460`, `0x4cb410`, `0x4cc4b4` | Work force %, life expectancy, education quotient | 17–19 |
| `0x4ca928`, `0x4cb014`, `0x4ca938` (16), `0x4ca1bc` (16) | National population (thousands), GNP, Fed Rate, national trend | 20–23 |
| `0x4cade0`, `0x4cae0c`, `0x4cb3d4`, `0x4cb40c` (8 each) | Heat, wind, humidity, weather type | 24–27 |
| `0x4ca420` (16) | Pending disaster | 28 |
| `0x4ca194` | Residential population last month | 29 |
| `0x4e9a24` | Rewards available (bit mask; the loader keeps 16 bits) | 30 |
| `0x4c94b4`, `0x4c94bc`, `0x4caa70` | Pointers to the 20 age groups' people, education totals, health totals | 31–90 |
| `0x4ca3f4` (16), `0x4cb3e0` (16), `0x4cb3f0` | Pointers to the 11 industries' demand, tax rate, size | 91–123 |
| `0x4ca4c8` (16) | Tile count per XBLD code; recomputed on load | 124–379 |
| `0x4ca428` | Population units: total, six zone types, abandoned buildings | 380–387 |
| `0x4c9ef0` (16) | 50 bond rates, % | 388–437 |
| `0x4cc4dc` (16), `0x4cad10`, `0x4ca804`, `0x4ca92c` | Pointers to the 4 neighbours' name ids, populations, economy figures, unused words | 438–453 |
| `0x4cad58` | The four neighbour names, 32 bytes each, built by the loader from the name ids | 438, 442, 446, 450 |
| `0x4cc8f8` (16) | R, C, I demand, then 5 unused (from `0x4cc8fe` on, nothing but the load and save loops refers to them) | 454–461 |
| `0x4cb430` (16) | 17 technology years | 462–478 |
| `0x4c85a0`, `0x4c85a4`, `0x4c8600` | Bus, rail and subway trips since the last yearly microsim update | — |
| `0x4cc4cc` | Budget table: 16 entries × `0x70` bytes; + `0x60` count, + `0x64` rate, + `0x68` year to date, + `0x6C` estimate (not saved), + 4*k* and + `0x30` + 4*k* month *k* count and rate | 479–910 |
| `0x4cad2c` | Year-end flag | 911 |
| `0x4ca818` (16) | Sea level | 912 |
| `0x4c94c0`, `0x4cb3f8` | Coast and river terrain options | 913, 914 |
| `0x4cc4e4` (8) | Military base: offer result and type | 915 |
| `0x4cad24` (8) | Pointer to the 6 newspapers, 5 bytes each | 916–945 |
| `0x4ca1c0` | Pointer to the 9 story slots, 8 bytes each | 946–999 |
| `0x4caa40` | Ordinances in force (bits 0–19) | 1000 |
| `0x4ca5f8` | Unemployment % (copy of the newest Unemployment graph value) | 1001 |
| `0x4ca934` (16) | Pointer to the 16 military tile counts (index 0 other, then `0xDD`, `0xDE`, `0xEF`, `0xF2`, `0xEA`, `0xE3`, `0xE4`, `0xE5`, `0xF1`, `0xE0`, `0xE2`, `0xE7`, `0xE8`, `0xF6`, `0xF9`); not recomputed on load | 1002–1017 |
| `0x4ca41c` (16) | Subway tiles (codes `0x01`–`0x0F`, `0x1F`, `0x20`, `0x22`, `0x23`, not in zone 7); kept by `0x442370`, not recomputed on load | 1018 |
| window + `0x308` (16), `0x4cc4b0`, `0x4ca5d8`, window + `0x1e4`, window + `0x1e0`, `0x4cc4d4` | Speed, auto budget, auto goto, sound, music, no disasters | 1019–1024 |
| `0x4cb3d0`, `0x4cc4bc`, `0x4ca808` (16) | Newspaper subscription, extras, choice | 1025–1027 |
| `0x4c7cb0`, `0x4c7cb4`, `0x4c7cb8` | View centre (two halves), zoom; used once after loading | 1028, 1029 |
| `0x4cb458`, `0x4cb45c` (16) | Centre of the buildings (row, col); recomputed in step 2 | 1030, 1031 |
| `0x4c94c4` | Arcology residents (sum of XMIC word 4 of the arcologies) | 1032 |
| `0x4caddc` (16) | Never used | 1033 |
| `0x4cc4e0` (16) | Sports teams (bit mask) | 1034 |
| `0x4caa74` | Population: 10 × word 380 | 1035 |
| `0x4ca1e8` (16), `0x4c9428` (16) | Industry bonus; heavy-industry level (−1, or (heavy share − 20) ÷ 30) | 1036, 1037 |
| `0x4ca430` (16), `0x4ca1dc` (16) | Police arrests; prisons have room | 1038, 1039 |
| `0x4ca81c` (16), `0x4ca93c` (16), `0x4c9ee8` | XTHG record of the disaster's monster or tornado (−1 none); disaster most recently started; disaster under way | 1040–1042 |
| `0x4cb3dc` (16) | 1 if the water treatment plants (XBLD `0xF4` tiles / 4 × 2000) cover the water delivered; recomputed by `0x464550` | 1043 |

### Other variables

| Address | Holds | Source |
|---|---|---|
| `0x4cb01c`, `0x4cb020` | Month index (days / 25 mod 12) and year (days / 300) | computed |
| `0x4ca400` | Sum of the outstanding bond rates | recomputed by `0x434b70` |
| `0x4ca930` | Residential, commercial, industrial population units (words 381 + 382, 383 + 384, 385 + 386) | computed by `0x468150` |
| `0x4ca4cc`, `0x4cb1a8` | People to add / remove in this month's migration | computed by `0x468150`, `0x468d30` |
| `0x4c7b08` | Ordinance subtotals per group of four (5 × int32) | computed by `0x4194b0` |
| `0x4c9410` | Opinion poll problems, ranked (7 × 16-bit) | computed by `0x472510` |
| `0x4e9904` | Mayor rating (percent of polled citizens without a complaint) | computed by `0x472510` |
| `0x4caa50` | Power delivered as a percentage of total plant capacity (`0x4c93b0`), capped at 100; the Power % graph is 100 minus this | computed by `0x467860` |
| `0x4cc4b8` | During the water flood: water delivered; afterwards the percentage of pump capacity (`0x4c93ac`); the Water % graph is 100 minus this | computed by `0x464550` |
| `0x4cc4d8` | Road neighbour connections | recounted by `0x434b70` from XTXT `0xFA` |
| `0x4ca3f0` | Rail and highway neighbour connections | not traced yet |
| `0x4e99c0` … `0x4e99dc` | Moving objects of each kind (airplanes, helicopters, cargo ships, rescuers, sailboats, monsters, train engines, tornadoes), recounted in each animation pass; the spawn limits read them | `0x450890` |
| `0x4caa58`, `0x4caa5c` | Disaster spot (row, col) | set from SCEN by `0x4345c0`, and by the dispatcher |
| `0x4cad44` | Scenario in progress ([scen.md](segments/scen.md#how-the-game-uses-it)); also keeps the monster at variant 0 and lets explosions start fires with disasters off | set by `0x4345c0` and `0x42de20` (state `0xd`); cleared by `0x4149b0` and `0x40e250`; read by `0x413520` and `0x45ee10` |
| `0x4cc918` | SCEN record (52 bytes, Windows 95 layout, native byte order; a 48-byte body leaves bytes 48–51 as they were) | `0x4345c0` |
| `0x4c7334` | Game state; 7 after a lost scenario (**Inferred**: ends the game) | `0x4149b0` |
| `0x4ea04c` | 1 when the display has fewer than 5 bits per pixel; the scenario picture is then not remapped | — |
| `0x4c7da0`/`0x4c7dc0`, `0x4c85b0`/`0x4c85d0`, `0x4c85f8`/`0x4c8608` | Positions of the police, fire and military units (not saved; turned by `0x466410`) | `0x44f910` |
| `0x4c93a8` | Flood timer | reset by `0x434b70` |
| `0x4c93b4` | Developed tiles (XBLD above `0x1C`, or zoned) | recomputed by `0x434b70` and `0x46a840` |
| `0x4cc6f0` | 16 graph maxima (y scale), not saved | computed by `0x431fd0` |
| `0x4ca404` | City-view data overlay mode | — |
| `0x4e9e54` | Label display switch | — |
| `0x4cde68` | Names the player gave to microsims, by building code mod 500 | — |
| `0x4c7d18` | Corner bit at which buildings are drawn for the current compass | table `0x4dc4c8` |
| `0x4e9e48` | Non-zero while the underground view is shown (**Inferred**) | — |
| `0x4c7010` | The application object; `0x4069b0` returns the game palette from it | — |
| `0x4c71f8` | The 12 month names, 12 bytes apart (application object + `0x1E8`): strings 33220–33231, "January" … "December", in month-index order; the short names (33240–33251) follow at + `0x90`. The newspaper date uses entry *month index* | `0x405650`; read by `0x477c12`, `0x4731e0` |
| `0x4cb3e8` | Weather table, second index (meaning not known) | [open-questions.md](open-questions.md#misc) |
| `0x4cc6f8` | Scratch grid of simulation step 2, not fully cleared between passes | `0x46a840` |
| `0x4e80cc`, `0x4e80d0` | A cargo ship's start point, kept for the way back (not saved) | `0x452d00` |

## Data

| Address | Contents |
|---|---|
| `0x4e736c`–`0x4e740c` | The 21 tags the city loader knows, 8 bytes apart: CNAM, XGRP, XROG, XPOP, XFIR, XPLC, XCRM, XVAL, XPLT, XTRF, XBIT, XTHG, XMIC, XLAB, XTXT, XUND, XZON, XBLD, XTER, ALTM, MISC |
| `0x4e7214`, `0x4e7108`, `0x4e61b8` | The tags `SCEN`, `PICT`, `TEXT` used by the scenario code. Only read: nothing writes these segments |
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
| `0x4e95b8` | Pollution emitted per XBLD code (bytes; codes `0x70` and up used) |
| `0x4e9648` | Density weight per XBLD code for XPOP (overlaps the end of `0x4e95b8`) |
| `0x4e7498` | Building class per XBLD code (1 construction, 2 abandoned) |
| `0x4e72b0` | Graph scale floors used on load: 1, 1, 1, 1, 1, 1, 1, 1, 100, 100, 150, 90, 15, 1, 1, 15 |
| `0x4e6750` | Graph scale floors for a new city (Health and Education 90 and 150: swapped compared with `0x4e72b0`) |
| `0x4e6984` | Population milestones (uint32): 2 000, 10 000, 30 000, 60 000, 90 000, 120 000, 500 000, 1 000 000 |
| `0x4e9538` | Demand factor per difficulty (double): 0, 1.2, 1.1, 0.95 |
| `0x4e9558` | GNP growth per national trend (int16): 6, 3, 0, −3 |
| `0x4dbeb8` | Weather transitions: 8 possible next types per weather type |
| `0x4dc038`, `0x4dc048`, `0x4dc058` | Heat, wind and humidity targets per weather type (12 bytes each) |
| `0x4e98b8` | String ids of the news-ticker messages (int32 per message). `0x471bc0` shows message 9 + weather type for weather 9–11 (`0x4e98d4` + 4 × weather): strings 281–283, "Blizzard Warning", "Hurricane Warning", "Tornado Warning" |
| `0x4e7458` | Population level per XBLD code − `0x70` (16-bit, codes `0x70`–`0xC5`): 1 = 1 × 1, 2 and 3 = lower and upper 2 × 2, 4 = 3 × 3 |
| `0x4e75b0` | Population units per level (16-bit): 0, 1, 8, 12, 36 |
| `0x4e7508` | The building-class table `0x4e7498` indexed by code − `0x70` |
| `0x4e9458`, `0x4e9508` | National industry ratios: 4 eras (1900, 1950, 2000, 2050) × 11 int32, and from 2100 on |
| `0x4e9eb0`, `0x4e6ad8` | Budget divisors per entry (16 × int16): 75, 75, 75, 75, −100, −1, −1, −2, −4, −1, −1000, −500, −400, −250, −250, −250 |
| `0x4e6730` | New-city budget rates per entry (16 × int16): 7, 7, 7, 1, 0, then 100 × 11 |
| `0x4e6b68`, `0x4e6af8`, `0x4e6b48`, `0x4e6c40` | Control ids of the Ordinances and Budget windows |
| `0x4e99e8` | Technology base years (17 × int16): 1940, 1950, 1980, 1970, 2020, 2050, 1920, 1920, 1910, 1900, 1925, 1980, 1990, 2040, 2090, 2140, 2190 |
| `0x4e9328` | Demand change by tax rate 0–22 (int16): 200, 160, 120, 100, 75, 50, 25, 0, −25, −50, −100, … −700 |
| `0x4ea4b0`, `0x4ea4c8`, `0x4ea4e0` | String ids of the newspaper names (360–365), opinion columns (354–359), weather columns (347–352) |
| `0x4ea158` | String ids of the newspaper price wordings, 3 styles × 5 eras (377–391) |
| String ids 548–583 | Neighbour town names (name id 1–36; category 1000 of `0x42b520`) |
| `0x4e7c9a` | Microsim kind per building code: 0 none, 1–16 a record of its own, 17–25 the fixed record kind − 16 (entries below `0xC6` belong to neighbouring tables) |
| `0x4e7da0` | Microsim window templates: 26 kinds × 5 string ids − 900 (−1 = no line) |
| `0x4c6a8a`, `0x4c7de0` | Default microsim names, 25 bytes per building code; `0x4c7de0` is where code `0xC6` starts (strings 831–888) |
| `0x4dc1d0`, `0x4e7e24` | Building prices, and the index into them per code (a worn-out plant's rebuild cost) |
| `0x4e8080`, `0x4e80a8` | Sprite base and minimum zoom per thing type |
| `0x4e80e0`, `0x4e80f0` | 8-way row and col steps of moving objects |
| `0x4e82e0`, `0x4e82f0`, `0x4e8360`, `0x4e8370` | 4-way row and col steps for trains and for sailboats |
| `0x4e8100`, `0x4e81f8` | Default height (byte 5) and speed per thing type |
| String ids 786–790 | Sports: Football, Baseball, Soccer, Cricket, Rugby |
| String ids 33200–33211 | Cold, Clear, Hot, Foggy, Chilly, Overcast, Snow, Rain, Windy, Blizzard, Hurricane, Tornado (the weather types 0–11 in order; no code reference found) |
| String ids 33212–33219, 33232–33239 | None, Fire, Flood, Riots, Pollution, Crash, Earthquake, Tornado; Monster, Meltdown, Microwave, Volcano, Fire Storm, Mass Riots, Major Flood, Chemical Spill (disasters 0–15 in order, **Inferred**; no code reference found) |
| String ids 33220–33231, 33240–33251 | Month names, long and short (loaded by `0x405650`) |
| String ids 900–982 | Microsim window templates |
| String ids 594–753 | Building and tile names (STRINGTABLE resource, UTF-16) |
