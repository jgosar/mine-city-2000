# XTER: Terrain shape and water

One byte per tile describing the ground: its **slope** (which corners are one level up), and whether it is **dry land, sea floor, shore or surface water**. Together with the altitude in [ALTM](altm.md) it gives the full shape of the landscape.

## At a glance

| | |
|---|---|
| Tag | `XTER` |
| Storage | RLE-compressed ([compression.md](../compression.md)) |
| Decoded size | 16 384 bytes (114/114 files) |
| Shape | 128 × 128 tiles, one byte per tile |
| Game buffer | `0x4c9f58` (128 row pointers) |
| Fix-up on load | none |

## Layout

```
code(row, col) = XTER[row * 128 + col]
```

**Confirmed (code)**: every routine indexes it as `xter[row][col]`, with the same row and column as [ALTM](altm.md#layout).

## Values

A code is a **kind** (high nibble) plus a **shape** (low nibble):

| Codes | Kind | Low nibble means | Water height ([ALTM](altm.md#water-height-bits-59)) | XBIT bit 2 |
|---|---|---|---|---|
| `0x00`–`0x0D` | **Dry land** | slope shape | — (leftover, ignore) | clear |
| `0x10`–`0x1D` | **Underwater**: sea floor at least one level below the surface | slope shape of the sea floor | sea level, > altitude | set |
| `0x20`–`0x2D` | **Shore**: sea floor exactly one level below the surface, so raised corners touch it | slope shape | sea level = altitude + 1 | set |
| `0x30`–`0x3D` | **Surface water** (rivers, lakes, ponds) lying on the ground | which sides border land ([§ Surface water](#surface-water)) | = altitude | set |
| `0x3E` | **Waterfall**: surface water on a slope | — | = altitude | set |
| `0x40`–`0x45` | **Surface water** channels and dead ends ([§ Surface water](#surface-water)) | — | = altitude | set |
| everything else | not used | | | |

Confidence: **Confirmed (code)** for the kinds and shapes (`0x447020`, `0x4460b0`, `0x446650`). **Verified (data)**: no file uses any other code (114/114). The water-height column holds in 114/114 files. Land never has XBIT bit 2 (114/114). The water kinds almost always have it, but a few tiles in some files lack it ([§ Water](#water)). The full list of all 256 codes is in [reference/terrain-codes-xter.md](../reference/terrain-codes-xter.md).

### Slope shapes

A tile's four corners are each either at the tile's altitude or **one level higher**. The low nibble of a land, underwater or shore code says which corners are raised. Corners are named by the neighbouring row and column:

```
              col − 1 side          col + 1 side
row − 1 side   (row−, col−) ─────── (row−, col+)
                    │                     │
                    │       tile          │
                    │                     │
row + 1 side   (row+, col−) ─────── (row+, col+)
```

| Shape | Raised corners | Description | OpenCity2k pattern (abxy) |
|---|---|---|---|
| `0` | none | flat | 0000 |
| `1` | (row−, col−), (row−, col+) | slope up towards row − 1 | 1100 |
| `2` | (row−, col−), (row+, col−) | slope up towards col − 1 | 0101 |
| `3` | (row+, col−), (row+, col+) | slope up towards row + 1 | 0011 |
| `4` | (row−, col+), (row+, col+) | slope up towards col + 1 | 1010 |
| `5` | all but (row+, col+) | three corners up | 1101 |
| `6` | all but (row−, col+) | three corners up | 0111 |
| `7` | all but (row−, col−) | three corners up | 1011 |
| `8` | all but (row+, col−) | three corners up | 1110 |
| `9` | (row−, col−) | one corner up | 0100 |
| `A` | (row+, col−) | one corner up | 0001 |
| `B` | (row+, col+) | one corner up | 0010 |
| `C` | (row−, col+) | one corner up | 1000 |
| `D` | all four | flat, one level higher than the altitude | 1111 |

**Confirmed (code)**: the terraforming routine `0x447020` builds this nibble from which neighbours are higher ([§ Slope and altitude](#slope-and-altitude)). The highway routine `0x463f60` counts any tile with a non-zero code as reaching `altitude + 1`. The OpenCity2k column uses its own corner letters, which match these as a = (row−, col+), b = (row−, col−), x = (row+, col+), y = (row+, col−). **Verified**: these letters reproduce all 14 rows. Which screen corner each one is depends on the compass ([conventions.md § Orientation](../conventions.md#orientation)).

There is no shape for a saddle, where only two opposite corners are raised. The game uses `D` instead.

### Surface water

For codes `0x30`–`0x3D` the low nibble does **not** describe the ground: the ground under surface water is flat, at the tile's altitude. It describes which neighbours are **not** water, so that the game can draw banks. It uses the same pattern as the slope table, where "raised" means "land on that side". The water tool `0x446650` picks the code from the four edge neighbours (water = XBIT bit 2). When all four edge neighbours are water, it picks the code from the four diagonal ones instead.

| Code | Water on edge sides | Land on | **Confirmed (code)** `0x446650` |
|---|---|---|---|
| `0x30` | all four, and all four diagonals (or two or more diagonals dry) | — | open water |
| `0x31` | col−, row+, col+ | row − 1 side | bank on one side |
| `0x32` | row+, col+, row− | col − 1 side | |
| `0x33` | col−, col+, row− | row + 1 side | |
| `0x34` | col−, row+, row− | col + 1 side | |
| `0x35` | row+, col+ | row − 1 and col − 1 sides | outer corner |
| `0x36` | col+, row− | col − 1 and row + 1 sides | |
| `0x37` | col−, row− | row + 1 and col + 1 sides | |
| `0x38` | col−, row+ | row − 1 and col + 1 sides | |
| `0x39` | all four; diagonal (row−, col−) is land | that diagonal | inner corner |
| `0x3A` | all four; diagonal (row+, col−) is land | that diagonal | |
| `0x3B` | all four; diagonal (row+, col+) is land | that diagonal | |
| `0x3C` | all four; diagonal (row−, col+) is land | that diagonal | |
| `0x3D` | none | all four sides | isolated pond |
| `0x3E` | — | — | **waterfall**: water placed on a sloped tile |
| `0x40` | col−, col+ | row − 1 and row + 1 sides | channel along the row |
| `0x41` | row+, row− | col − 1 and col + 1 sides | channel along the column |
| `0x42` | row+ only | the other three sides | dead end |
| `0x43` | col+ only | | dead end |
| `0x44` | row− only | | dead end |
| `0x45` | col− only | | dead end |

"col−" means the neighbour at (row, col − 1), "row+" the neighbour at (row + 1, col), and so on.

The codes are recomputed whenever the water next to a tile changes, but a reader should take them as stored. The game never derives them on load.

## Slope and altitude

How the slope relates to the neighbours' altitudes, as the terraforming routine `0x447020` computes it (**Confirmed (code)**):

1. Each of the 8 neighbours that has a **higher altitude** raises the corners it touches. An edge neighbour raises two corners, a diagonal neighbour one:

   | Neighbour | Raises |
   |---|---|
   | (row, col − 1) | (row−, col−), (row+, col−) |
   | (row + 1, col − 1) | (row+, col−) |
   | (row + 1, col) | (row+, col−), (row+, col+) |
   | (row + 1, col + 1) | (row+, col+) |
   | (row, col + 1) | (row−, col+), (row+, col+) |
   | (row − 1, col + 1) | (row−, col+) |
   | (row − 1, col) | (row−, col−), (row−, col+) |
   | (row − 1, col − 1) | (row−, col−) |

2. The set of raised corners is looked up in a 16-entry table (`0x4e7958`), which gives the shapes above. The two saddles map to `D`. If **all four** corners are raised, the game raises the tile's altitude by one instead and makes it flat.
3. If the altitude is now below the sea level, the code becomes the underwater (`+0x10`) or shore (`+0x20`) version ([§ Water](#water)).

**The stored code is authoritative.** Other routines set shapes directly:

- the bridge builder `0x45b930` raises each bridge end by one level and gives it an edge slope (shapes 1–4), with no regard to the neighbours;
- the highway builder `0x462ed0` levels its 2 × 2 footprint, using `D` on the tiles it fills up;
- older or hand-edited cities may not follow the rule at all.

**Verified (data)**: recomputing the shape from the neighbours reproduces the stored shape on every tile in 11/114 files, on at least 99% of tiles in 83/114, on 95–99% in 19/114, and below 95% in 1/114. Over half of the differing tiles carry a road, rail, highway or power line (XBLD `0x0E`–`0x6F`). The most common single difference is a stored `D` where the neighbours suggest three raised corners. **Never recompute XTER from ALTM**; read both.

## Water

Water tiles are those with code `0x10` or higher. For each kind, the [ALTM](altm.md) water height holds as shown in [§ Values](#values) (**Verified (data)** 114/114).

How tiles become water (**Confirmed (code)**):

- **Terraforming** (`0x447020`). A tile whose altitude ends up below the sea level (MISC word 912; see [altm.md § Sea level](altm.md#sea-level)) becomes `0x20 + shape` if it is exactly one level below, otherwise `0x10 + shape`. Its water height becomes the sea level and XBIT bit 2 is set. A tile at or above the sea level keeps its plain shape and has XBIT bit 2 cleared.
- **Flooding to a level** (`0x4460b0`). Each tile below the level gets water height = level and XBIT bit 2. A land tile becomes `0x10 + shape`, or `0x20 + shape` if it is exactly one level below and not flat (flat tiles always become `0x10`). Surface water becomes `0x10`. Tiles at or above the level that were water lose XBIT bit 2 and go back to their plain shape.
- **Placing water** (`0x446650`). A flat land tile becomes surface water (`0x30`–`0x45`, chosen as in [§ Surface water](#surface-water)). A sloped land tile becomes `0x3E`. The water height becomes the altitude, XBIT bit 2 is set, and the neighbouring water tiles are re-evaluated.
- **Bulldozing water** (`0x443270`). `0x3E` is re-shaped from the neighbours (`0x447020`). Other surface water becomes `0x00`.

XBIT bit 2 ([xbit.md § Bit layout](xbit.md#bit-layout)) is the game's per-tile "has water" flag (**Verified (data)**):

| Kind | Files where every tile of this kind has the bit |
|---|---|
| Land | never set: 114/114 |
| Shore | 100 of the 100 files with shore tiles |
| Underwater | 73 of the 99 files with underwater tiles; the other 26 have 127 tiles without it |
| Surface water | 93 of the 103 files with surface water; the other 10 have 1 848 tiles without it |

None of the exceptions lie over a tunnel, and most are empty tiles (XBLD `0x00`). Why they lack the bit is **Unknown**.

## How the game uses it

| Routine | Use |
|---|---|
| `0x43c730`, `0x43d620`, `0x43e550` (tile drawing) | Picks the ground sprite: sprite `1000 + table[code]` from the 16-bit table at `0x4e7628`. Land shapes use their own sprites. **All** underwater codes `0x10`–`0x1D` use one flat-water sprite. Shore `0x20 + n` and surface water `0x30 + n` share sprite `1000 + 0x10E + n`. `0x40`–`0x45` have their own. Height from ALTM: altitude for codes below `0x10`, water height otherwise |
| `0x447020`, `0x4460b0`, `0x446650` | Create and change codes ([§ Water](#water)) |
| `0x443270` (bulldozer) | Removes surface water ([§ Water](#water)) |
| `0x434b70` (after loading) | Tests for dry land (code below `0x10`) while recounting statistics |
| `0x448a90`, `0x448f50`, `0x449bb0` and others (placement checks) | Compare the code with `0x10`, `0x20`, `0x30` or `0x40` (that is, test the kind) to allow or refuse building |

## Rotation

Rotating the view moves every tile ([conventions.md § Orientation](../conventions.md#orientation)) and **remaps** its XTER code, because shapes have a direction (**Confirmed (code)**). The tables cover codes `0x00`–`0x47`:

| Routine | Compass | Table | Mapping |
|---|---|---|---|
| `0x465490` | −1 | `0x4e90a0` | shapes `1→2→3→4→1`, `5→6→7→8→5`, `9→A→B→C→9` (within each of the four kinds); `0x40↔0x41`; `0x42→0x43→0x44→0x45→0x42` |
| `0x466660` | +1 | `0x4e9210` | the inverse: `1→4→3→2→1`, `5→8→7→6→5`, `9→C→B→A→9`; `0x40↔0x41`; `0x42→0x45→0x44→0x43→0x42` |

Unchanged by both: `0x00`, `0x0D`, `0x10`, `0x1D`, `0x20`, `0x2D`, `0x2E`, `0x30`, `0x3D`, `0x3E`. Unused codes (`0x0E`, `0x0F`, `0x1E`, `0x1F`, `0x2F`, `0x3F`, `0x46`, `0x47`) become `0x00`.

The mapping matches the corner definitions above. `0x465490` moves a tile from (row, col) to (127 − col, row), so the corner (row−, col−) ends up as (row+, col−): shape `9` becomes `A`.

## Evidence

| Claim | Code | Data (`sc2check.py --only xter`) |
|---|---|---|
| Kinds and the slope table | `0x447020`, table `0x4e7958`, neighbour tables `0x4e7a68`/`0x4e7a80`/`0x4e7ac0` | only documented codes, 114/114 |
| Shape `D` = all four corners up | `0x463f60`, `0x462ed0` | — |
| Surface-water codes | `0x446650` | — |
| Water height per kind | `0x447020`, `0x4460b0`, `0x446650` | 114/114 (`sc2check.py --only altm_water`) |
| XBIT bit 2 per kind | same | see [§ Water](#water) |
| Slope vs neighbours | `0x447020` | 11 / 83 / 19 / 1 files at 100% / ≥99% / ≥95% / <95% |
| Rotation tables | `0x465490`, `0x466660` | — |
| Drawing | `0x43c730`, sprite table `0x4e7628` | — |

## Pitfalls for readers

- **Surface water is not on a slope.** For `0x30`–`0x45` the low nibble describes the banks, not the ground. The ground is flat at the altitude, and the water surface is at the altitude too.
- **Underwater tiles all look the same from above.** The slope of the sea floor is stored but drawn as flat water.
- **Shape `D` is not a saddle.** It is a flat tile whose surface is one level above its ALTM altitude.
- **Do not derive XTER from ALTM**, or the reverse ([§ Slope and altitude](#slope-and-altitude)).
- **Water test:** use code ≥ `0x10`. XBIT bit 2 is the game's own flag but is missing on a few water tiles in some files.
