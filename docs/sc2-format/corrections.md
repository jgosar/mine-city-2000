# Corrections and additions to the older documents

These pages build on two earlier descriptions of the format:

- **djm**: David Moews, *SimCity 2000 file format* (1995), <https://djm.cc/simcity-2000-info.txt>. It describes the MS-DOS version.
- **OpenCity2k**: *sc2 file spec*, <https://github.com/OpenCity2k/SC2k-docs>.

Both are good, and most of what they say holds. This page lists only where the Windows 95 game's code or the corpus says something different, or settles something they left open. Each entry links to the page with the evidence.

## Container

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| Length field of a compressed segment | djm: unclear whether it is the compressed or uncompressed size | It is the **stored (compressed)** size; the decoded size is fixed per tag | [container.md § Segment header](container.md#segment-header) |
| Padding of odd-length segments | Neither says; standard IFF pads to even lengths | **No padding** | [container.md § Segment header](container.md#segment-header) |
| Segment order | djm: segments come "in order" | Any order is accepted; CNAM is first, last or absent in practice | [container.md § Segment order](container.md#segment-order) |
| Unknown segments | Not stated | Skipped | [container.md § Missing, repeated and unknown segments](container.md#missing-repeated-and-unknown-segments) |
| CNAM | djm: "optional?"; OpenCity2k: byte 0 is `0x1F` | Optional: without it the name comes from the file name. The game ignores byte 0 and reads the name from byte 1. Byte 0 is `0x1F` or the name length, depending on the file | [container.md § After the walk](container.md#after-the-walk), [conventions.md § Strings](conventions.md#strings) |
| MISC word 0 | OpenCity2k: "header, unknown" | **File version**: must be 0x122, otherwise the game refuses the file | [container.md § Version check](container.md#version-check) |
| MISC words 124–379 (building counts) | djm: tile counts per building code | Correct, but the game **recomputes** them from XBLD and XZON on every load, so the stored values are never used. Tiles in the military zone are not counted | [container.md § After the walk](container.md#after-the-walk) |
| Scenario segment order | Not stated | `TEXT`, `SCEN` and `PICT` are looked up by tag and id, so their position does not matter | [container.md § Scenario segments](container.md#scenario-segments) |
| TEXT maximum length | OpenCity2k: unknown | The game reads at most **768 bytes** of text | [container.md § Scenario segments](container.md#scenario-segments) |
| PICT size | Not stated | The game reads at most **4300 bytes** | [container.md § Scenario segments](container.md#scenario-segments) |
| Byte order of scenario ids | OpenCity2k shows the bytes (`80 00 00 00`) without naming an order | **Little-endian**, unlike the rest of the file | [conventions.md § Byte order](conventions.md#byte-order) |
| TMPL | OpenCity2k: "no function in actual game usage" | Confirmed: `SIMCITY.EXE` never refers to the tag | [container.md § Scenario segments](container.md#scenario-segments) |

## Compression

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| Control byte `0x00` | djm: "never seems to occur"; OpenCity2k: range `0..127` without saying what 0 does | Valid: copies nothing. The game never writes it | [compression.md](compression.md#the-rule) |
| Control byte `0x80` | djm: "never seems to occur"; OpenCity2k: not defined | Valid: repeats the next byte **once**. The game never writes it | [compression.md](compression.md#the-rule) |
| Repeat count | OpenCity2k: unsure whether the count is `n` or `n + 1` | Control byte `c ≥ 0x80` repeats `(c & 0x7F) + 1` times, i.e. `c − 0x7F` | [compression.md](compression.md#the-rule) |
| Malformed data | Not stated | Leftover input is an error; input that ends early is silently accepted | [compression.md § Running out of input or space](compression.md#running-out-of-input-or-space) |
| Which segments are compressed | djm: all except ALTM and CNAM | Correct for city segments. The scenario segments (TEXT, SCEN, PICT, TMPL) are raw too | [container.md § Segment table](container.md#segment-table) |

## Orientation

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| Compass (MISC word 2) | OpenCity2k: number of counter-clockwise rotations | Correct, and the stored maps are rotated with it: one clockwise turn moves every map as `new[row][col] = old[col][127 − row]` and remaps the direction-dependent codes | [conventions.md § Orientation](conventions.md#orientation) |
| "Top", "left", "right", "bottom" in code descriptions | Both documents name directions on the screen | Screen directions only hold at one compass value. These pages name directions by row and col | [conventions.md § Orientation](conventions.md#orientation) |
| 64 × 64 overlay mapping | Not stated | Cell (row / 2, col / 2) covers the 2 × 2 tiles starting at (row, col) | [conventions.md § Overlay grids](conventions.md#overlay-grids) |

## ALTM and XTER

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| ALTM bit numbering | OpenCity2k numbers bits from the most significant end | Its bits 11–15 / 6–10 / 0–5 are bits 0–4 / 5–9 / 10–15 on these pages | [altm.md § Bit layout](segments/altm.md#bit-layout) |
| ALTM bit 7 | djm: "seems to be set" for water | Part of the 5-bit water height (bits 5–9). With the usual sea level 4 it is set on every sea tile | [altm.md § Water height](segments/altm.md#water-height-bits-59) |
| ALTM bits 5–9 | djm: unknown; OpenCity2k: water level height | Water surface height, used only on water tiles: the sea level for underwater and shore tiles, the altitude for surface water; leftover data on dry land | [altm.md § Water height](segments/altm.md#water-height-bits-59) |
| ALTM bits 10–14 | djm: unknown; OpenCity2k: "possibly related to tunnel levels" | Tunnel depth: tile altitude − tunnel altitude + 1 on tiles above a road tunnel, 0 elsewhere | [altm.md § Tunnel depth](segments/altm.md#tunnel-depth-bits-1014) |
| ALTM bit 15 | djm: unknown | Unused; never set | [altm.md § Bit layout](segments/altm.md#bit-layout) |
| Sea level | OpenCity2k: MISC `0x0E40` "Water level: water table level" | The sea level (MISC word 912); every underwater and shore tile's water height equals it | [altm.md § Sea level](segments/altm.md#sea-level) |
| XTER `0x0E`, `0x0F`, `0x1E`, `0x1F`, `0x2F`, `0x3F`, `0x46`–`0xFF` | "Unused?" / unknown | Not used: no routine creates them, and turning the view turns them into `0x00` | [terrain-codes-xter.md](reference/terrain-codes-xter.md) |
| XTER `0x2E` | Unknown | Never created, but handled like the waterfall `0x3E` | [terrain-codes-xter.md § Notes](reference/terrain-codes-xter.md#notes) |
| XTER `0x0D` | OpenCity2k: all four corners up | Correct; the game also uses it for saddles (two opposite corners up), which have no shape of their own | [xter.md § Slope shapes](segments/xter.md#slope-shapes) |
| XTER `0x30`–`0x3D` | OpenCity2k: the slope patterns, at the surface | The pattern says which sides border **land** (computed from the water neighbours); the ground under the water is flat | [xter.md § Surface water](segments/xter.md#surface-water) |
| XTER `0x3E` | OpenCity2k: waterfall tiles (e.g. under dams) | Water placed on any sloped tile | [xter.md § Surface water](segments/xter.md#surface-water) |
| XTER `0x42`–`0x45` | OpenCity2k: "Bay, water bottom / left / top / right" | Dead ends: water on exactly one edge side | [xter.md § Surface water](segments/xter.md#surface-water) |
| XTER `0x20`–`0x2D` | djm: described vaguely | Shore: sea floor exactly one level below the water surface | [xter.md § Values](segments/xter.md#values) |

## XBLD

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| Multi-tile storage | Neither document says which tile is which | Every tile holds the code; the corners are marked in XZON with compass-dependent bits; the game's anchor is corner D (min row, max col) | [xbld.md § Footprints and anchors](segments/xbld.md#footprints-and-anchors) |
| Highways `0x49`–`0x50` | Neither says 1 × 1 or 2 × 2 | 1 × 1 tiles (XZON `0xF0`), always in even-aligned 2 × 2 cells; the game treats them as size 2 | [xbld.md § Highways](segments/xbld.md#highways) |
| Large hangar | OpenCity2k: the military table maps both the small (13) and the large (14) hangar to `0xE8`; `0xF6` missing | Index 13 is `0xE8` (1 × 1 hangar), index 14 is `0xF6` (2 × 2 hangar) | [xbld.md § How the game uses it](segments/xbld.md#how-the-game-uses-it) |
| `0x84` / `0x86` | OpenCity2k: both "Warehouse (Small WareHouse 1)" | The game itself names both "Warehouse"; they have different sprites, so this is not a copy error | [tile-codes-xbld.md](reference/tile-codes-xbld.md) |
| `0xAA`–`0xAD`, `0xC4` | OpenCity2k: all "Abandoned Building 7" | The game names every one "Abandoned building"; their sizes differ (2 × 2 and 3 × 3) | [tile-codes-xbld.md](reference/tile-codes-xbld.md) |
| On-ramp rotation | OpenCity2k: "can be rotated 90 degrees by bit 6 in XBIT" | OpenCity2k counts bits from the top, so its bit 6 is mask `0x02`. The view-turn code reads and writes that flag on on-ramps, as on bridges | [xbld.md § Rotation](segments/xbld.md#rotation) |
| `0x59` | Listed as a normal code | Never present in the corpus (raised drawbridge) | [tile-codes-xbld.md](reference/tile-codes-xbld.md) |

## XZON and XUND

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| XZON corner bit names | djm: bit 7 upper left, 6 lower left, 5 lower right, 4 upper right. OpenCity2k: `8` top right, `4` top left, `2` bottom right, `1` bottom left (the two disagree) | The bits are tied to the map, not the screen. Bit *k* (`0x10 << k`) is at footprint corner (*k* − compass) mod 4 of the stored arrays; neither screen naming holds for every compass value | [xzon.md § Which bit is at which corner](segments/xzon.md#which-bit-is-at-which-corner) |
| XZON zone 7 | Both: military base | Correct; in addition the game leaves military tiles out of the per-code building counts and the subway count | [xzon.md § Zone 7, the military base](segments/xzon.md#zone-7-the-military-base) |
| XUND `0x21` | djm: "???"; OpenCity2k: unknown, never seen | Never written by the game (**Unused**) | [xund.md § Values](segments/xund.md#values) |
| XUND `0x22` | djm: "???"; OpenCity2k: missile silo | Missile silo, confirmed in the placement code; but no silo in the corpus carries it | [xund.md § Values](segments/xund.md#values) |
| XUND `0x23` | Both: subway station (djm: "sub/rail") | Under subway stations (XBLD `0xE9`) **and** subway-to-rail connections (`0x6C`–`0x6F`); counts as subway | [xund.md § Values](segments/xund.md#values) |
| XUND crossings `0x1F`, `0x20` | Both: pipe "top-bottom" / "left-right" | `0x1F` = pipe along the rows, subway along the cols; `0x20` the reverse | [xund.md § Values](segments/xund.md#values) |
| XUND piece codes | Both: coded like the XBLD power lines | Correct; in addition every piece code is remapped when the view turns, and slope pieces are taken from the tile's XTER | [xund.md § Rotation](segments/xund.md#rotation) |

## XBIT

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| Bit numbering | djm numbers bits from the bottom (bit 7 = `0x80`); OpenCity2k from the top (its "bit 6, Rotation" = `0x02`) | These pages use masks | [xbit.md § Pitfalls for readers](segments/xbit.md#pitfalls-for-readers) |
| `0x80` | djm: "electrically conductive?"; OpenCity2k: "Powerable" | Correct: power flows only through `0x80` tiles | [xbit.md § 0x80](segments/xbit.md#0x80-conducts-power) |
| `0x40`, `0x10` | Both: powered / watered | Correct, but **recomputed on every load** and in every simulation cycle; the stored values are a snapshot | [xbit.md § What to trust](segments/xbit.md#what-to-trust) |
| `0x20` | djm: "piped? (permeable to water?)" | Correct: water flows only through `0x20` tiles (buildings and pipes) | [xbit.md § 0x20](segments/xbit.md#0x20-carries-water) |
| `0x08` | djm: "???"; OpenCity2k: "XVAL mask … needs scaling, maybe only DOS cities" | Two jobs: a 64 × 64 developed-land mask kept in rows and cols 0–63, rebuilt after loading, and a visited mark for flood fills. Present in 67/114 Windows files, so not DOS-only; never needs reading | [xbit.md § 0x08](segments/xbit.md#0x08-developed-land-mask-and-scratch-mark) |
| `0x04` | djm: "covered with water?"; OpenCity2k: "Water" | Correct; only ever on tiles with a water XTER code | [xbit.md § 0x04](segments/xbit.md#0x04-covered-by-water) |
| `0x02` | djm: "???"; OpenCity2k: "Rotation: rotate the tile by 90°" | A horizontal sprite mirror. For buildings (`0x70` and up: runways, piers) it is XORed with the compass parity; for roads, bridges and on-ramps it is used as it is. The game flips it on bridges and on-ramps when the view turns | [xbit.md § 0x02](segments/xbit.md#0x02-mirror-rotate) |
| `0x01` | djm: "does placing water here give salt water?"; OpenCity2k: "Salt Water" | Correct, and it stays on dry land (160 000 of 268 000 salt tiles are dry) | [xbit.md § 0x01](segments/xbit.md#0x01-salt) |
