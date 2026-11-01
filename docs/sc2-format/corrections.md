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
| CNAM | djm: "optional?"; OpenCity2k: byte 0 is `0x1F` | Optional: without it the name comes from the file name. The game ignores byte 0 and reads the name from byte 1. Byte 0 is `0x1F` or the name length, depending on the file | [cnam.md § Layout](segments/cnam.md#layout) |
| MISC word 0 | OpenCity2k: "header, unknown" | **File version**: must be 0x122, otherwise the game refuses the file | [container.md § Version check](container.md#version-check) |
| MISC words 124–379 (building counts) | djm: tile counts per building code | Correct, but the game **recomputes** them from XBLD and XZON on every load, so the stored values are never used. Tiles in the military zone are not counted | [container.md § After the walk](container.md#after-the-walk), [tile-counts.md](segments/misc/tile-counts.md#building-counts-words-124379) |
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

## Text and labels

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| XTXT `0xFB`–`0xFF` | djm: sports team names (Llamas, Alpacas, …) | In XTXT they are temporary disaster markers. The team names are **XLAB slots** 251–255, which no XTXT value points to | [xtxt.md § Disaster markers](segments/xtxt.md#disaster-markers-0xfb0xff), [xlab.md § Label slots](segments/xlab.md#label-slots) |
| XTXT `0xFB`–`0xFF` names | OpenCity2k: toxic cloud, flood, rioters, mass-riot rioters, fire | Fire (`0xFF`) and flood (`0xFC`) are confirmed in the code; the others fit the code's behaviour but are not tied to a disaster yet | [xtxt.md § Disaster markers](segments/xtxt.md#disaster-markers-0xfb0xff) |
| XTXT `0xC9`–`0xF0` | djm: not listed; OpenCity2k: 39 values, "index to XTHG, probably" | Moving objects: XTHG record value − `0xC9`; 40 values, records 0–39 | [xtxt.md § Moving objects](segments/xtxt.md#moving-objects-0xc90xf0) |
| XTXT `0x33` | djm: "??? microsimulator" | Never used: it would be XMIC record 0 and XLAB slot 51 | [xtxt.md § Values](segments/xtxt.md#values) |
| XTXT and XLAB `0x3D`–`0xC8` | OpenCity2k: 139 microsim labels | 140 values (XMIC records 10–149); the corpus uses `0xC8` | [xtxt.md § Microsims](segments/xtxt.md#microsims-0x3d0xc8) |
| XLAB padding | djm: padded with zeros to 24 bytes; OpenCity2k: text, then a zero terminator, padding sometimes garbage | There is no terminator: the save writes only the length and the text, so the rest of the record holds older text. Length 24 occurs; the game uses at most 23 | [xlab.md § Layout](segments/xlab.md#layout) |
| XLAB slot 250 | OpenCity2k: text of the last connection sign | Correct: a scratch buffer that the drawing code rebuilds, not a city label | [xlab.md § Slot 250](segments/xlab.md#slot-250-connection-sign-scratch) |
| CNAM byte 0 | djm: name length, zero padding; OpenCity2k: always `0x1F` | Both occur: `0x1F` in 50/107 files (the Windows 95 writer), the name length in 57/107. Zero padding is rare (12/107). The game ignores byte 0 | [cnam.md § Layout](segments/cnam.md#layout) |
| Character set | OpenCity2k: ASCII | Windows ANSI: code page 1252 in the corpus | [conventions.md § Strings](conventions.md#strings) |

## Overlays

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| 32 × 32 overlay mapping | OpenCity2k: "show in city appears to split into 4x4 blocks" | Confirmed: tile (row, col) is in cell (row / 4, col / 4) | [minimaps.md § Cell mapping](segments/minimaps.md#cell-mapping) |
| Overlay values | djm: meanings marked "(?)"; neither document gives formulas | Formulas for all eight, from simulation step 2 and the traffic routines | [minimaps.md § Values](segments/minimaps.md#values) |
| XPOP | djm: "Population (?)" | A building density computed from XBLD alone: 4 × the summed per-code weights of the 4 × 4 tiles, capped at 255. Not people | [minimaps.md § XPOP](segments/minimaps.md#xpop-building-density) |
| XROG | OpenCity2k: `7F` no change; values above mean growth, below decline | A running average of the change in XPOP; 127 and 128 both mean no change. The map view draws 131 and up as growth, below 125 as decline | [minimaps.md § XROG](segments/minimaps.md#xrog-rate-of-growth) |
| XVAL | OpenCity2k: shown "human friendly 1 indexed" | Stored 0–255; 0 outside the developed-land mask | [minimaps.md § XVAL](segments/minimaps.md#xval-land-value) |
| MISC word 12, "Traffic Count" (byte `0x30`) | OpenCity2k: not a sum of XTRF | The sum of XTRF right after the step-19 decay; trips added since then make XTRF larger (equal in 69/114 files) | [minimaps.md § XTRF](segments/minimaps.md#xtrf-traffic) |
| MISC word 13, "Pollution" (byte `0x34`) | OpenCity2k: unknown | The sum of XPLT (114/114) | [minimaps.md § XPLT](segments/minimaps.md#xplt-pollution) |

## XGRP

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| Contents | djm: unknown; OpenCity2k: 16 series of 12 monthly, 20 half-yearly and 20 five-yearly values | Correct, and the exact rules are known: newest value first in each block; slot 12 takes a copy of slot 0 in months 0 and 6, slot 32 a copy of slot 12 in month 0 of years divisible by 5; the update runs on day 21 of the month | [xgrp.md § When samples are taken](segments/xgrp.md#when-samples-are-taken) |
| Series 13 | OpenCity2k: "GNF (GNP?), in 1000s" | GNP. The game shows no unit | [xgrp.md § Graph series](segments/xgrp.md#graph-series) |
| MISC words 18 and 19, "Work Force LE / EQ" | OpenCity2k: life expectancy and education quotient | Correct; they are also the newest Health and Education graph values (109/109) | [xgrp.md § Graph series](segments/xgrp.md#graph-series) |
| MISC words 21 and 22, "National Value" and "National Tax" | OpenCity2k: unknown | GNP and the Fed Rate: the newest values of series 13 and 15 | [xgrp.md § Graph series](segments/xgrp.md#graph-series) |
| MISC word 1001, "unemployed" (byte `0x0FA4`) | OpenCity2k: unknown | The unemployment **percentage**, a copy of the newest value of series 12 (109/109) | [xgrp.md § Graph series](segments/xgrp.md#graph-series) |

## MISC

OpenCity2k numbers MISC by byte offset (hex), djm by word (#*n*); the rows give both. djm describes only a few words (#3, #4, #5, #20 and the building counts), and they are correct.

| Topic | Older document says | What the game does | Evidence |
|---|---|---|---|
| Value types | OpenCity2k: 4-byte integers, except the military counts ("16x2B") | Every word is 4 bytes, but the game keeps only the low 16 or 8 bits of many; some files store negative 16-bit values without sign extension | [misc/README.md § Widths](segments/misc/README.md#widths) |
| Word 8 (byte `0x020`) | OpenCity2k: "City Status", reward tier 0–6 | The number of population milestones passed (2 000 … 1 000 000), 0–8; level 4 offers the military base; never goes down | [general.md § Rewards](segments/misc/general.md#rewards) |
| Word 9 (`0x024`) | OpenCity2k: "City Value", something to do with total city value | An infrastructure value computed only when the player asks for a bond, to limit borrowing; 0 in 87/114 files | [general.md § Money and difficulty](segments/misc/general.md#money-and-difficulty) |
| Words 14, 15 (`0x038`, `0x03C`) | OpenCity2k: "City Fame", "Advertising", unknown | Never used; 0 in 114/114 | [general.md § Layout](segments/misc/general.md#layout) |
| Word 16 (`0x040`) | OpenCity2k: "Garbage", unknown | A running sum of the population, added to every month and never read | [general.md § Statistics](segments/misc/general.md#statistics) |
| Word 17 (`0x044`) | OpenCity2k: "Work Force %", the share of the population that is working | The working-age groups' share of the population, not employment (unemployment is word 1001) | [general.md § Statistics](segments/misc/general.md#statistics) |
| Word 28 (`0x070`) | OpenCity2k: "Disasters", a disaster type | The disaster waiting to start; cleared when it starts, so 0 in saved files. The one started is word 1041 | [general.md § Disasters](segments/misc/general.md#disasters) |
| Word 29 (`0x074`) | OpenCity2k: residential population, unknown exactly | The residential population at the previous monthly update | [general.md § Statistics](segments/misc/general.md#statistics) |
| Words 31–90 (`0x07C`–`0x168`) | OpenCity2k: population, health and education graph data, interleaved in that order | Per 5-year age group: people, **education** total, **health** total, not graph values | [population.md § Age groups](segments/misc/population.md#age-groups) |
| Words 91–123 (`0x16C`–`0x1EC`) | OpenCity2k: industry window data as ratios, tax rates and demand, ranges unknown | Per industry: demand (0–127), tax rate (0–20 %), size; the ratio is size ÷ total | [population.md § Industries](segments/misc/population.md#industries) |
| Words 380–387 (`0x5F0`–`0x60C`) | OpenCity2k: populated, residential, commercial and industrial tile counts, with unknown words between | Population **units**, not tiles: the total, light and dense residential, commercial and industrial, then abandoned buildings (387) | [population.md § Zone population](segments/misc/population.md#zone-population) |
| Words 388–437 (`0x610`–`0x6D7`) | OpenCity2k: bond data, at most 50, signed 32-bit | The interest rate (%) of each bond, 16-bit in the game; only the first word-6 slots are bonds, the rest can be stale | [population.md § Bonds](segments/misc/population.md#bonds) |
| Neighbours (words 438–453) | OpenCity2k: neighbour index, population, value and fame, ordered lower left, upper left, unknown, upper right, bottom right | Name id, population, an economy figure nothing reads, and a word that is always 0. Neighbour *k* belongs to a map edge, not a screen position | [neighbours.md](segments/misc/neighbours.md) |
| Neighbour names | Not stated | Stored as a number: 0 = "Ocean", 1–36 = town names in the executable | [neighbours.md § Names](segments/misc/neighbours.md#names) |
| R, C, I demand (words 454–461) | OpenCity2k: three values at `0x718`, −2000 … +2000 | Correct; the next five words are unused | [demand.md](segments/misc/demand.md) |
| Technology entry 16 (`0x778`) | OpenCity2k: "Highways. Odd, but observed in a few cities" | A 17th slot with base year 2190 that unlocks nothing; highways are entry 7 | [technology.md § Layout](segments/misc/technology.md#layout) |
| Technology years | OpenCity2k: 0 if the city was saved after the technology was invented | Correct; in addition the year is fixed when the city is made (base year + 0–19) and an invention arrives with a 1-in-8 chance each month once its year has come | [technology.md](segments/misc/technology.md#how-the-game-uses-it) |
| Budget entries (words 479–910) | OpenCity2k: entries start with the rate; the bonds entry at bytes `0x930`–`0x994` | Every entry is 27 words starting with the count (the tax base for taxes), then the rate, then the year to date; the bonds entry starts at `0x92C` (word 587) | [budget.md § Layout](segments/misc/budget.md#layout) |
| Budget word 2 | OpenCity2k: unknown (taxes, services); "Sum Bond Rate" (bonds), "incorrect" after the negative-funds trick | The year to date: Σ count × rate over this year's months, booked into the funds at the year end. That is why it never matched a sum of rates | [budget.md § Layout](segments/misc/budget.md#layout) |
| Tax entry word 0 | OpenCity2k: "current population (maybe)" | The tax base: (words 381 + 382, 383 + 384 or 385 + 386) × 10 plus a share of word 1032, recomputed on day 21 | [budget.md § Entries](segments/misc/budget.md#entries) |
| Ordinances entry (`0x8C0`–`0x92B`) | OpenCity2k: exact contents unknown | Count = net monthly amount of the enacted ordinances, rate = 1 | [budget.md § Entries](segments/misc/budget.md#entries), [ordinances.md § Amounts](segments/misc/ordinances.md#amounts) |
| Transport counts | OpenCity2k: counts of buildings; bus stations have no funding, so "aren't stored" | Tile counts of the listed XBLD codes; each bus station adds 250 to the Roads count, so it costs road funding | [budget.md § Transport counts](segments/misc/budget.md#transport-counts) |
| Word 911 (`0xE3C`) | OpenCity2k: "Year end", unknown | 1 from December's budget until the year's totals are booked at the start of the new year | [budget.md § The year end](segments/misc/budget.md#the-year-end) |
| Word 915 (`0xE4C`) | OpenCity2k: 0 not offered, 1 refused or no site, 2 army, 3 air, 4 navy, 5 silos | Confirmed, except that no code was found writing 4 | [general.md § Terrain options](segments/misc/general.md#terrain-options-and-the-military-base) |
| Words 916–945 (`0xE50`) | OpenCity2k: newspaper list, 6 × 5 bytes, contents unknown | The six papers' name, layout, price style, opinion column and weather column | [newspapers.md § Papers](segments/misc/newspapers.md#papers-words-916945) |
| Words 946–999 (`0xEC8`) | OpenCity2k: newspaper list, 9 × 6 bytes, to `0x0EFC` | 9 records of 6 **words** (to `0xF9F`): the story queue, the weather item and the opinion item | [newspapers.md § Stories](segments/misc/newspapers.md#stories-words-946999) |
| Word 1000 (`0xFA0`), ordinances | OpenCity2k: a bit set, finance ordinances in the lowest bits; no names per bit | All 20 bits named, with their monthly amounts and effects | [ordinances.md § Bits](segments/misc/ordinances.md#bits) |
| Military counts (words 1002–1017) | OpenCity2k: 16 × 2 bytes; index 0 "unknown, total count of military tiles?" | 16 words (16-bit values in the game). Index 0 counts every zone-7 tile that is not one of the 15 listed buildings | [tile-counts.md § Military counts](segments/misc/tile-counts.md#military-counts-words-10021017) |
| Words 1028–1031 (`0x1010`–`0x101C`) | OpenCity2k: `0x1010` unknown, `0x1014` zoom and position, `0x1018`/`0x101C` "View X/Y" | 1028 is the tile at the view centre (packed), 1029 the zoom. 1030/1031 are not the view but the centre of the city's buildings, recomputed in step 2 | [general.md § View](segments/misc/general.md#view), [§ Counters](segments/misc/general.md#counters) |
| Word 1033 (`0x1024`) | OpenCity2k: "Connection Tiles" | Never used (only loaded and saved) | [general.md § Layout](segments/misc/general.md#layout) |
| Word 1034 (`0x1028`) | OpenCity2k: "Sports Teams", a count | A bit mask, one bit per team | [general.md § Counters](segments/misc/general.md#counters) |
| Words 1036, 1037, 1039 (`0x1030`, `0x1034`, `0x103C`) | OpenCity2k: "Industry Bonus", "Pollution Bonus", "Police Bonus", unknown | The largest industry's share, the heavy-industry level, and the police stations' load | [general.md § Counters](segments/misc/general.md#counters) |
| Words 1040, 1041 (`0x1040`, `0x1044`) | OpenCity2k: "Disaster", unknown | 1040 = XTHG record of the monster or tornado (−1 none); 1041 = the disaster most recently started | [general.md § Disasters](segments/misc/general.md#disasters) |
| Words 1043, 1044 (`0x104C`, `0x1050`) | OpenCity2k: "Go Disaster", "Sewer Bonus" | Word 1043 is the water-treatment flag; the Windows 95 game does not load word 1044 at all | [general.md § Counters](segments/misc/general.md#counters), [§ Words 1044–1199](segments/misc/general.md#words-10441199) |
| Bytes `0x1054`–`0x10B4` | OpenCity2k: zero in all cities checked | Zero in Windows 95 saves; 60/114 corpus files from another version store power %, water % and the developed-tile count there | [general.md § Words 1044–1199](segments/misc/general.md#words-10441199) |
