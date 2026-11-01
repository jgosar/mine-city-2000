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
| `0x431ee0` | `NameFromFileName` | Sets the city name to the upper-cased file name without its directory | [container.md](container.md#after-the-walk), [cnam.md](segments/cnam.md#how-the-game-uses-it) |
| `0x434b70` | `RecountAfterLoad` | Recounts statistics from XBLD, XZON and XTXT after loading (among them the road neighbour connections, from XTXT `0xFA`); resets the flood timer; calls `0x467860` and `0x464550` | [container.md](container.md#after-the-walk) |
| `0x434f80` | `RecountBuildingCodes` | Rebuilds the 256-entry per-XBLD-code tile count (stored in MISC words 124–379), skipping military-zone tiles (XZON zone 7) | [container.md](container.md#after-the-walk) |
| `0x431fd0` | `ComputeGraphMaxima` | Sets each of the 16 graph maxima (`0x4cc6f0`) to the largest of its 52 XGRP values, at least the floor in `0x4e72b0`; series 1–3 take series 0's maximum | [xgrp.md](segments/xgrp.md#graph-scale-not-stored) |
| `0x430c00` | `ConvertLabels` | Turns the 256 length-prefixed XLAB records into C strings, capping the length at 23 | [xlab.md](segments/xlab.md#layout) |
| `0x430d50` | `ClearLabels` | Clears all 256 labels (used when XLAB is missing) | [container.md](container.md#after-the-walk) |
| `0x42ae10` | `ReadScenarioSegment` | Finds a `TEXT`/`SCEN`/`PICT` segment by tag and little-endian id and reads its body | [container.md](container.md#scenario-segments) |
| `0x42cef0`, `0x42d940` | `AllocCityBuffers`?, `FreeCityBuffers`? | Allocate and free the segment buffers | [minimaps.md](segments/minimaps.md#layout) |
| `0x42d820` | `AllocateGraphs` | Allocates the 3328-byte XGRP buffer and points the 16 series pointers (`0x4cc470`) at it, 0xD0 bytes apart | [xgrp.md](segments/xgrp.md#layout) |
| `0x432350` | `SaveCity` | Save routine: writes CNAM first, then each segment from its buffer | [cnam.md](segments/cnam.md#how-the-game-uses-it) |
| `0x432990` | `WriteCnam` | Writes the tag, length 32, byte `0x1F`, then 31 bytes starting at the name (whatever follows it in memory) | [cnam.md](segments/cnam.md#layout) |
| `0x430c90` | `UnconvertLabels` | Before saving: turns the 256 C-string labels back into length + text; writes only bytes 0 … length | [xlab.md](segments/xlab.md#layout) |
| `0x40e250` | `ResetCityData` | New city: clears the overlays (XROG to `0x7F`), zeroes all 832 XGRP values and sets the graph maxima from `0x4e6750`, resets the power and water figures | [minimaps.md](segments/minimaps.md#xrog-rate-of-growth), [xgrp.md](segments/xgrp.md#graph-scale-not-stored) |
| `0x44cb60` | `NewCity` | Clears XTXT, labels 1–255 and XTHG; sets label 0 to the registered mayor's name | [xlab.md](segments/xlab.md#slot-0-the-mayor) |

## Byte-order helpers

| Address | Our name | What it does |
|---|---|---|
| `0x42a930` | `Swap32` | Reverses the 4 bytes of a 32-bit value |
| `0x42a960` | `TagValue` | Loads a 4-character tag as a value comparable with what was read from the file |
| `0x42a7e0` | `Swap32Array` | Swaps every 4-byte word of a buffer (MISC, XGRP) |
| `0x42a880` | `Swap16Array` | Swaps every 2-byte word of a buffer (ALTM) |
| `0x42a820` | `SwapMicrosims` | Swaps the 16-bit words at bytes 2, 4 and 6 of every 8-byte record (XMIC) |
| `0x42a8c0` | `Swap16` | Reverses the 2 bytes of a 16-bit value |

## Simulation cycle

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x413520` | `SimulationStep` | Dispatcher of the 25-step cycle (step = MISC word 4 mod 25): step 1 → `0x467860` (power), step 2 → `0x46a840` (overlays), steps 3–18 → `0x4358b0` (zone growth), step 19 → `0x4586f0` (traffic decay), step 20 → `0x464550` (water), step 21 → `0x468150`, `0x468d30` and `0x413e50` (graphs) | [minimaps.md](segments/minimaps.md#how-the-game-uses-it), [xgrp.md](segments/xgrp.md#when-samples-are-taken), [xbit.md](segments/xbit.md#what-to-trust) |
| `0x4570f0` | `YieldToWindows`? | Called every 5000 tiles during the floods (**Inferred**: message pump) | [xbit.md](segments/xbit.md#what-to-trust) |

## View rotation and screen position

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x465490` | `RotateViewClockwise` | Moves every map as `new[row][col] = old[col][127 − row]`; remaps XBLD, XTER and XUND codes through the clockwise tables; flips XBIT `0x02` on bridges and on-ramps; compass − 1. XZON, ALTM and XTXT bytes move unchanged | [conventions.md](conventions.md#orientation) |
| `0x466660` | `RotateViewCounterClockwise` | The exact inverse of `0x465490`; compass + 1 | [conventions.md](conventions.md#orientation) |
| `0x465f00`, `0x467100` | `RotateOverlays` (cw, ccw) | Turn the 64 × 64 and 32 × 32 overlays; the values are not changed | [minimaps.md](segments/minimaps.md#rotation) |
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
| `0x43bb00` | `DrawDataOverlay` | City-view data overlay; the mode in `0x4ca404` picks XTRF, XPOP, XROG, XCRM, XPLC, XPLT, XVAL, XFIR, power, water or growth bands; values below 16 are not drawn | [minimaps.md](segments/minimaps.md#cell-mapping) |
| `0x4886c0`, `0x4891b0`, `0x489300`, `0x488f10`, `0x489060`, `0x489450`, `0x488c60`, `0x488db0` | `DrawTrafficMap` … `DrawGrowthMap` | Map-window bitmaps for XTRF, XPLT, XVAL, XCRM, XPLC, XFIR, XPOP, XROG: 16 shades of `value >> 4`; XROG in three bands | [minimaps.md](segments/minimaps.md#cell-mapping) |
| `0x4888f0`, `0x488ab0` | `DrawPowerMap`, `DrawWaterMap` | Draw the power and water map bitmaps | [xbit.md](segments/xbit.md#how-the-game-uses-it) |
| `0x44d9a0` | `DrawTileText` | Draws a sign or connection text (when labels are shown), or the moving object of XTXT `0xC9`–`0xF0` via `0x450bb0` | [xtxt.md](segments/xtxt.md#how-the-game-uses-it) |
| `0x450bb0` | `DrawThing` | Draws XTHG record *n* | [xtxt.md](segments/xtxt.md#moving-objects-0xc90xf0) |
| `0x46f4a2` | `DrawGraphs` | Graph window: draws slots 11 → 0, 31 → 12 or 51 → 32 of each series, scaled by `0x4cc6f0` | [xgrp.md](segments/xgrp.md#how-the-graph-window-reads-it) |
| `0x47a240` | `ExpandMessage` | Expands message templates (with a temporary OEM conversion) | [xlab.md](segments/xlab.md#how-the-game-uses-it) |

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
| `0x436e80` | `PlaceBuildingTile` | Places a building tile: corner bits `0xF0` for one-tile items, XBIT `0xE0` on every tile of the footprint; a new one-tile home's wealth comes from XVAL >> 6 (at most 2) | [xzon.md](segments/xzon.md#corner-bits), [xbit.md](segments/xbit.md#how-the-game-uses-it), [minimaps.md](segments/minimaps.md#how-the-game-uses-it) |
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

## Text, labels and microsims

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x44d7c0` | `SignTool` | Places, edits or removes a sign (XTXT 1–50); refuses tiles with XTXT `0x33` and up | [xtxt.md](segments/xtxt.md#signs-0x010x32) |
| `0x44d970` | `FindFreeSign` | First sign number 1–50 not in use | [xtxt.md](segments/xtxt.md#signs-0x010x32) |
| `0x44ced0` | `DeleteText` | Removes a sign and its label; clears XTHG byte 10; leaves `0x34`–`0x3C` alone | [xtxt.md](segments/xtxt.md#how-the-game-uses-it) |
| `0x44cf60` | `AllocMicrosim` | Picks an XMIC record for a building code (table `0x4e7c9a`), writes its byte 0 and default name, returns the XTXT value | [xtxt.md](segments/xtxt.md#microsims-0x3d0xc8) |
| `0x44e390` | `UpdateMicrosims` | Microsim routine; among other things sums the word at offset 4 of the XMIC records of types `0xEC`, `0xED`, `0xF3`–`0xF5`, `0xF8`, `0xFA`–`0xFE` into MISC word 1032 | [xgrp.md](segments/xgrp.md#graph-series) |
| `0x449bb0`, `0x449f50`, `0x4621f0` | `RoadTool`, `RailTool`, `HighwayTool` | At the map edge, offer a neighbour connection (strings 109, 110, 246) and write XTXT `0xFA` | [xtxt.md](segments/xtxt.md#neighbour-connections-0xfa) |
| `0x450690`, `0x452d00`, `0x452fa0`, `0x453280`, `0x453460`, `0x453600`, `0x455500`, `0x45f090`, `0x442a30` | moving-object routines | Move XTHG objects and write XTXT `0xC9` + *n* on the tile of object *n* | [xtxt.md](segments/xtxt.md#moving-objects-0xc90xf0) |

## Disasters

| Address | Our name | What it does | Cited in |
|---|---|---|---|
| `0x45cf10` | `DispatchDisaster` | Starts the pending disaster (`0x4ca420`, cases 1–18) | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x471bc0` | `ChooseDisasterSpot`? | Picks where a disaster starts; case 4 starts at the most polluted 64 × 64 cell above 149 | [minimaps.md](segments/minimaps.md#how-the-game-uses-it) |
| `0x45f760` | `UpdateDisasters` | Processes the XTXT markers `0xFB`–`0xFF` each tick; fire spreads with XFIR + 8 as a random threshold; zeroes XTRF under destroyed tiles | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff), [minimaps.md](segments/minimaps.md#xfir-fire-coverage) |
| `0x460e10` | `StartFire` | Writes XTXT `0xFF`; zeroes XTRF under removed road or rail | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x4611e0` | `BurnOut` | Turns a burnt tile into rubble | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x461000` | `FloodTiles` | Marks dry tiles up to a level with XTXT `0xFC`, removes signs and microsims, zeroes XTRF | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x461140` | `MoveRiotFront` | Moves `0xFE` along roads and leaves `0xFD` behind (only on XTXT below `0x33`) | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x461190` | `MarkFB` | Writes `0xFB` (only on XTXT below `0x33`) | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |
| `0x4612d0` | `ClearRiot` | Clears `0xFD` and `0xFE` | [xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff) |

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
| `0x4358b0` | `ZoneGrowthStep`? | Simulation steps 3–18: grows the zones in one block of the map; needs XVAL above 31, 95 or 191; sends trips | [minimaps.md](segments/minimaps.md#how-the-game-uses-it) |
| `0x457620` | `AddTripTraffic`? | Adds a trip's weight to XTRF along its route, capped at 255 | [minimaps.md](segments/minimaps.md#xtrf-traffic) |
| `0x4586f0` | `DecayTraffic` | Step 19: XTRF −= XTRF >> 2 everywhere; stores the sum in MISC word 12 | [minimaps.md](segments/minimaps.md#xtrf-traffic) |
| `0x4540c0` | `TrafficReport`? | Triggers the traffic helicopter report on a cell above 169, at most every 5 s | [minimaps.md](segments/minimaps.md#xtrf-traffic) |
| `0x468d30` | `IndustryMix`? | Computes the heavy-industry level `0x4c9428` (step 21) | [minimaps.md](segments/minimaps.md#xplt-pollution) |
| `0x468150` | `SumPopulation` | Sets MISC word 380 to the sum of words 381–386 and the residents / commerce / industry inputs; runs before `0x413e50` | [xgrp.md](segments/xgrp.md#graph-series) |
| `0x413e50` | `UpdateGraphs` | Day 21 of each month: computes the 16 newest values, shifts the monthly block, and the half-year (months 0 and 6) and five-year (month 0 of years divisible by 5) blocks; raises the graph maxima | [xgrp.md](segments/xgrp.md#when-samples-are-taken) |

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
| `0x4cae04` | Day counter: 25-day months, 300-day years; simulation step = value mod 25 | MISC word 4 |
| `0x4cb01c`, `0x4cb020` | Month index (days / 25 mod 12) and year (days / 300) | computed |
| `0x4ca444` | Funds (**Inferred**: `0x4441c0` takes 25 off it per raised tile) | MISC word 5 |
| `0x4ca440` | Land value total (sum of XVAL) | MISC word 10, recomputed in step 2 |
| `0x4cb454` | Crime total (sum of XCRM) | MISC word 11, recomputed in step 2 |
| `0x4cc6f4` | Traffic total (sum of XTRF after the decay) | MISC word 12, recomputed in step 19 |
| `0x4cc910` | Pollution total (sum of XPLT) | MISC word 13, recomputed in step 2 |
| `0x4ca420` | Pending disaster | MISC word 28 |
| `0x4ca4c8` | Tile count per XBLD code (256 × 16-bit) | MISC words 124–379, then recomputed |
| `0x4ca428` | Population: word 380 = sum of 381–386, then three pairs, then 387 unemployed | MISC words 380–387 |
| `0x4ca818` | Sea level (16-bit) | MISC word 912 |
| `0x4ca5f8` | Unemployment percentage (copy of the newest Unemployment graph value) | MISC word 1001 |
| `0x4c94c4` | Population living in large microsim buildings (**Inferred**) | MISC word 1032 |
| `0x4ca934` | Pointer to the 16-entry military tile count (index 0 other, then `0xDD`, `0xDE`, `0xEF`, `0xF2`, `0xEA`, `0xE3`, `0xE4`, `0xE5`, `0xF1`, `0xE0`, `0xE2`, `0xE7`, `0xE8`, `0xF6`, `0xF9`) | not checked yet (MISC, M3) |
| `0x4ca41c` | Subway tile count (codes `0x01`–`0x0F`, `0x1F`, `0x20`, `0x22`, `0x23`, not in zone 7) | maintained by `0x442370`; not checked yet whether MISC stores it |
| `0x4caa50` | Power delivered as a percentage of total plant capacity (`0x4c93b0`), capped at 100; the Power % graph is 100 minus this | computed by `0x467860` |
| `0x4cc4b8` | During the water flood: water delivered; afterwards the percentage of pump capacity (`0x4c93ac`); the Water % graph is 100 minus this | computed by `0x464550` |
| `0x4cb3dc` | 1 if the water treatment plants (XBLD `0xF4` tiles / 4 × 2000) cover the water delivered | MISC (word not pinned yet, M3); recomputed by `0x464550` |
| `0x4c9428` | Heavy-industry level | MISC (M3), recomputed by `0x468d30` |
| `0x4caa40` | Ordinance flags (**Inferred**: bits `0x4`, `0x10`, `0x800`, `0x80000` change crime, fire, police and pollution in step 2) | MISC (M3) |
| `0x4cc4cc` | Budget data; police funding at + `0x294`, fire at + `0x304` | MISC (M3) |
| `0x4cc4d8` | Road neighbour connections | recounted by `0x434b70` from XTXT `0xFA` |
| `0x4ca3f0` | Rail and highway neighbour connections | not traced yet |
| `0x4cad58` | The four neighbour city names, 32 bytes each | not traced yet |
| `0x4c93a8` | Flood timer | reset by `0x434b70` |
| `0x4c93b4` | Developed tiles (XBLD above `0x1C`, or zoned) | recomputed by `0x434b70` and `0x46a840` |
| `0x4cb458`, `0x4cb45c` | Centre of the buildings (row, col) | recomputed in step 2 |
| `0x4cc6f0` | 16 graph maxima (y scale), not saved | computed by `0x431fd0` |
| `0x4ca404` | City-view data overlay mode | — |
| `0x4e9e54` | Label display switch | — |
| `0x4cde68` | Names the player gave to microsims, by building code mod 500 | — |
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
| `0x4e7c9a` | Microsim kind per building code: 0 none, 1–`0x10` a new record from 10 on, `0x11` and up a fixed record (entry − `0x10`) |
| `0x4c6a8a` | Default microsim names, 25 bytes per building code |
| `0x4e95b8` | Pollution emitted per XBLD code (bytes; codes `0x70` and up used) |
| `0x4e9648` | Density weight per XBLD code for XPOP (overlaps the end of `0x4e95b8`) |
| `0x4e7498` | Building class per XBLD code (1 construction, 2 abandoned) |
| `0x4e72b0` | Graph scale floors used on load: 1, 1, 1, 1, 1, 1, 1, 1, 100, 100, 150, 90, 15, 1, 1, 15 |
| `0x4e6750` | Graph scale floors for a new city (Health and Education 90 and 150: swapped compared with `0x4e72b0`) |
| String ids 594–753 | Building and tile names (STRINGTABLE resource, UTF-16) |
