# XUND: underground

One byte per tile describing what lies under it: a **subway** piece, a **water pipe** piece, a crossing of the two, a **subway station** (or a subway-to-rail connection), or a **missile silo**. Road and rail tunnels are not stored here.

## At a glance

| | |
|---|---|
| Size | 16384 bytes, 128 × 128 tiles, row by row ([conventions.md](../conventions.md#the-city-map-128--128-tiles)) |
| Storage | RLE-compressed ([compression.md](../compression.md)) |
| Element | 1 byte: a code `0x00`–`0x23` |
| Game buffer | `0x4cb1d0` (128 row pointers) |
| Changes with the compass? | Yes: piece codes are remapped on every view rotation ([Rotation](#rotation)) |

## Layout

Each byte is one code; there are no bit fields. Codes `0x24`–`0xFF` never occur (**Verified (data)**, 0/114 files) and are not handled by the game: the rotation tables stop at `0x23` (**Confirmed (code)**).

## Values

| Codes | Contents | Confidence |
|---|---|---|
| `0x00` | nothing | **Confirmed (code)**, **Verified (data)** |
| `0x01`–`0x0F` | subway piece; the shape is code − `0x01` in the [shape table](#piece-shapes) | **Confirmed (code)** `0x44bb20`, `0x44bc40`; **Verified (data)** |
| `0x10`–`0x1E` | water pipe piece; the shape is code − `0x10` in the same table | **Confirmed (code)** `0x44b790`, `0x44b8d0`; **Verified (data)** |
| `0x1F` | crossing: pipe along the rows, subway along the cols | **Confirmed (code)**: laying a pipe on subway `0x01` gives `0x1F`, and laying a subway on pipe `0x11` gives `0x1F`. **Verified (data)** |
| `0x20` | crossing: pipe along the cols, subway along the rows | **Confirmed (code)**: pipe on subway `0x02`, subway on pipe `0x10`. **Verified (data)** |
| `0x21` | never used | **Unused**: no code writes it, 0/114 files |
| `0x22` | missile silo | **Confirmed (code)**: placing XBLD `0xF9` writes `0x22` under all 9 tiles (`0x4382b0`). But 0 of the 27 silo tiles in the corpus have it; see [Pitfalls](#pitfalls-for-readers) |
| `0x23` | subway station, or subway-to-rail connection | **Confirmed (code)**: written when XBLD `0xE9` (station) or `0x6C`–`0x6F` (connection) is placed (`0x440170`, `0x441b80`). **Verified (data)**: 1 874 of 1 879 `0x23` tiles lie under those codes |

Every code with its tile counts and rotation partners is listed in the [underground code reference](../reference/underground-codes-xund.md).

### Piece shapes

Subway pieces `0x01`–`0x0F` and pipe pieces `0x10`–`0x1E` use the same 15 shapes in the same order. Directions are **array directions**, not screen directions:

- **row−1** / **row+1**: the neighbouring tile in the previous / next row,
- **col−1** / **col+1**: the neighbouring tile in the previous / next column.

| Shape | Subway | Pipe | Kind | Connects to | Higher end |
|---|---|---|---|---|---|
| 0 | `0x01` | `0x10` | straight | col−1, col+1 | — |
| 1 | `0x02` | `0x11` | straight | row−1, row+1 | — |
| 2 | `0x03` | `0x12` | slope | row−1, row+1 | row−1 |
| 3 | `0x04` | `0x13` | slope | col−1, col+1 | col−1 |
| 4 | `0x05` | `0x14` | slope | row−1, row+1 | row+1 |
| 5 | `0x06` | `0x15` | slope | col−1, col+1 | col+1 |
| 6 | `0x07` | `0x16` | bend | row+1, col−1 | — |
| 7 | `0x08` | `0x17` | bend | row+1, col+1 | — |
| 8 | `0x09` | `0x18` | bend | row−1, col+1 | — |
| 9 | `0x0A` | `0x19` | bend | row−1, col−1 | — |
| 10 | `0x0B` | `0x1A` | T-junction | row−1, row+1, col−1 | — |
| 11 | `0x0C` | `0x1B` | T-junction | row+1, col−1, col+1 | — |
| 12 | `0x0D` | `0x1C` | T-junction | row−1, row+1, col+1 | — |
| 13 | `0x0E` | `0x1D` | T-junction | row−1, col−1, col+1 | — |
| 14 | `0x0F` | `0x1E` | crossroads | all four | — |

Confidence:

- **Connections.** **Confirmed (code)**: the subway shape is picked from a 16-entry table (`0x4e7c40`, used by `0x44bc40`), indexed by which neighbours already hold subway: 1 = col−1, 2 = row+1, 4 = col+1, 8 = row−1. Indices built that way give exactly the shapes above. A neighbour only counts if its terrain allows a connection along that axis (XTER checked through tables `0x4e7cb8` and `0x4e7c70`). Pipes use the same table plus `0x10` (`0x44b8d0`). **Verified (data)**: on 27 574 of 27 648 subway tiles (99.7%) and 132 075 of 132 785 pipe tiles (99.5%), every neighbouring piece of the same kind is one the shape connects to. The few exceptions are parallel lines running side by side.
- **Slopes follow the tile's own terrain.** **Confirmed (code)**: if the tile's XTER value is below `0x31` and its low four bits are a simple slope, the game does not look at neighbours. It takes the slope piece from a table (`0x4e7c60`): XTER low bits 1, 2, 3, 4 → shape 2, 3, 4, 5 (subway `0x03`–`0x06`, pipe `0x12`–`0x15`); see [xter.md](xter.md#values). **Verified (data)**: 6 947 of 6 961 slope tiles sit on the matching XTER value. The "higher end" column agrees with the terrain height (ALTM, [altm.md](altm.md#bit-layout)) on 5 959 tiles and contradicts it on 52; on 949 the neighbouring heights are level.

The older documents say these shapes are coded like the power lines in XBLD (`0x0E`–`0x1C`, [tile codes](../reference/tile-codes-xbld.md)). The order is indeed the same.

## How the game uses it

| Function | What it does with XUND | Confidence |
|---|---|---|
| `0x442370` | The only routine that writes a code. It also keeps the **subway tile count** (`0x4ca41c`): +1 for each tile set to `0x01`–`0x0F`, `0x1F`, `0x20`, `0x22` or `0x23`, −1 when such a tile is overwritten. Tiles in a military base (XZON zone 7) are not counted | **Confirmed (code)** |
| `0x44bb20` | Lays subway: empty → `0x01`; pipe `0x10` → `0x20`; pipe `0x11` → `0x1F`. Then fixes the shape of the tile and its four neighbours (`0x44bc40`) | **Confirmed (code)** |
| `0x44bc40` | Picks a subway piece: a slope from the tile's own XTER, otherwise a shape from the neighbours that hold subway (`0x01`–`0x0F`, `0x1F`, `0x20`, `0x22`, `0x23`) | **Confirmed (code)** |
| `0x44b790` | Lays pipe: empty → `0x10`; subway `0x01` → `0x1F`; subway `0x02` → `0x20`. Sets XBIT bit `0x20` on the tile ([xbit.md](xbit.md#bit-layout)), then fixes the shape (`0x44b8d0`) | **Confirmed (code)** |
| `0x440170` | Placing a subway station (XBLD `0xE9`): writes `0x23` and clears XBIT bit `0x20` | **Confirmed (code)** |
| `0x441b80` | Placing a subway-to-rail connection: writes `0x23`, sets XBLD `0x6C` + direction (`0x6C`–`0x6F`) and clears XBIT bit `0x20` | **Confirmed (code)** |
| `0x4382b0` | Placing a missile silo: XBLD `0xF9` and XUND `0x22` on a 3 × 3 area | **Confirmed (code)** |
| `0x4142c0` | Military-base placement only claims tiles whose XUND is `0x00` | **Confirmed (code)** |
| `0x465490`, `0x466660` | Remap every code on view rotation | **Confirmed (code)** |

### Relation to XBLD

What is built on the surface does not limit what lies underneath (**Verified (data)**):

| Surface (XBLD) | Pipe tiles | Subway tiles |
|---|---|---|
| buildings `0x70`–`0xFF` | 62 791 | 16 585 |
| roads `0x1D`–`0x2B` | 41 235 | 4 750 |
| crossings, bridges, highways `0x43`–`0x6F` | 11 205 | 1 337 |
| empty land `0x00` | 6 969 | 2 448 |
| rubble, trees, parks `0x01`–`0x0D` | 6 566 | 1 980 |
| power lines `0x0E`–`0x1C` | 2 965 | 183 |
| rails `0x2C`–`0x3E` | 1 050 | 365 |
| tunnel entrances `0x3F`–`0x42` | 4 | 0 |

- **Subway stations and connections** are the only surface codes tied to an XUND code: `0x23` lies under XBLD `0xE9` (1 797 tiles) and `0x6C`–`0x6F` (77 tiles). Only 5 `0x23` tiles lie elsewhere.
- **Road and rail tunnels are not in XUND.** 174 of the 178 tunnel-entrance tiles have XUND `0x00`. Where the tunnel itself is stored is described on the XBLD and XBIT pages.
- **Pipes and XBIT bit `0x20`.** 134 477 of 134 488 pipe and crossing tiles have XBIT bit `0x20` set. The bit is also set on many tiles without pipes, so it is not a pipe flag on its own ([xbit.md](xbit.md#bit-layout)).

## Rotation

Rotating the view moves every XUND byte to its new position and **replaces the code with its rotated equivalent**. The replacement comes from a lookup table for each direction (**Confirmed (code)**):

| Routine | Array move | Lookup table | Effect on codes |
|---|---|---|---|
| `0x465490` | `new[row][col] = old[col][127 − row]` | `0x4e90e8` | `01`↔`02`; `03`→`04`→`05`→`06`→`03`; `07`→`08`→`09`→`0A`→`07`; `0B`→`0C`→`0D`→`0E`→`0B`; pipes the same, +`0x0F`; `1F`↔`20` |
| `0x466660` | the inverse | `0x4e9258` | the inverse of the above |

Codes `0x00`, `0x0F`, `0x1E`, `0x21`, `0x22` and `0x23` map to themselves. Both tables have 36 entries (`0x00`–`0x23`), so a code of `0x24` or above would be replaced with an unrelated byte.

The remapping matches the shape table. For example, `new[row][col] = old[col][127 − row]` turns a piece that connects row−1 and row+1 into one that connects col−1 and col+1, which is `0x01` ↔ `0x02`. This is why the shape table holds for every compass value: the codes always describe the arrays as stored. To normalise a file to compass 0, rotate the arrays back ([conventions.md](../conventions.md#orientation)) and remap each code through the matching table.

## Evidence

| Claim | Code | Data (`sc2check.py --only xund`) |
|---|---|---|
| Only `0x00`–`0x20` and `0x23` occur | rotation tables end at `0x23` | 114/114 files; `0x21`, `0x22`, `0x24`+ in 0/114 |
| Subway / pipe shapes | `0x44bc40` with table `0x4e7c40` | 99.7% / 99.5% of tiles have no neighbour outside their shape |
| Slope piece = tile's XTER slope | `0x44bc40`, `0x44b8d0` with table `0x4e7c60` | 6 947 / 6 961 slope tiles |
| Higher end of slopes | — | ALTM agrees on 5 959, contradicts on 52, level on 949 |
| Crossings `0x1F`, `0x20` | `0x44b790`, `0x44bb20` | 1 374 of 1 703 crossing tiles have pipe and subway neighbours exactly as stated |
| `0x22` = missile silo | `0x4382b0` | 0/27 silo tiles carry it |
| `0x23` = station / connection | `0x440170`, `0x441b80` | 1 874 / 1 879 |
| `0x21` unused | no writer | 0/114 |
| Rotation remap | `0x465490`, `0x466660`; tables `0x4e90e8`, `0x4e9258` | — |

## Pitfalls for readers

- **XUND depends on the compass.** A shape code means a direction in the stored arrays. Two files of the same city saved at different compass values hold different codes.
- **Do not trust `0x22` to find missile silos.** The placement code writes it, but none of the silos in the corpus (27 tiles of XBLD `0xF9`) carry it. Something clears it again, or those silos were not placed through that path (see [open-questions.md](../open-questions.md)). Use XBLD `0xF9` instead.
- **A pipe or subway segment can end anywhere.** Line ends are not special pieces: a straight piece with only one neighbour is a dead end. 15 525 pipe tiles do not connect exactly as their shape says, mostly at line ends and under buildings.
- **Crossings belong to both networks.** When following the pipe network, treat `0x1F` as a pipe along the rows and `0x20` as a pipe along the cols. When following the subway, do the opposite.
- **Stations count as subway.** `0x23` (and `0x22`) count as subway tiles in the game's own statistics, so include them when you count subway length to match the game.
