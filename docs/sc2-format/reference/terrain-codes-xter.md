# XTER terrain codes

Every value of an [XTER](../segments/xter.md) byte. The segment page explains the kinds, the [slope shapes](../segments/xter.md#slope-shapes) and the [surface-water patterns](../segments/xter.md#surface-water). This page is the lookup table.

Columns:

- **Shape or meaning**: for land, underwater and shore codes, which corners are one level above the tile's altitude ([corner names](../segments/xter.md#slope-shapes)). For surface water, which sides border land.
- **Files**: in how many of the 114 corpus files the code occurs at least once (**Verified (data)**).
- **Sprite**: the ground sprite the game draws, `1000 + ` the 16-bit entry of the table at `0x4e7628` (**Confirmed (code)**, `0x43c730`). "—" means the entry is 0.
- **Rotate −1 / +1**: the code after the view is rotated by `0x465490` (compass − 1, table `0x4e90a0`) or `0x466660` (compass + 1, table `0x4e9210`) (**Confirmed (code)**).

Codes the game's own routines create are `0x00`–`0x0D`, `0x10`–`0x1D`, `0x20`–`0x2D`, `0x30`–`0x3E` and `0x40`–`0x45` (**Confirmed (code)**, [xter.md § Water](../segments/xter.md#water)). No file in the corpus uses any other value (114/114).

| Code | Kind | Shape or meaning | Files | Sprite | Rotate −1 | Rotate +1 |
|---|---|---|---|---|---|---|
| `00` | Land | flat | 114/114 | 1256 | `00` | `00` |
| `01` | Land | up towards row − 1 | 103/114 | 1257 | `02` | `04` |
| `02` | Land | up towards col − 1 | 103/114 | 1258 | `03` | `01` |
| `03` | Land | up towards row + 1 | 100/114 | 1259 | `04` | `02` |
| `04` | Land | up towards col + 1 | 104/114 | 1260 | `01` | `03` |
| `05` | Land | three up, (row+, col+) low | 88/114 | 1261 | `06` | `08` |
| `06` | Land | three up, (row−, col+) low | 86/114 | 1262 | `07` | `05` |
| `07` | Land | three up, (row−, col−) low | 87/114 | 1263 | `08` | `06` |
| `08` | Land | three up, (row+, col−) low | 90/114 | 1264 | `05` | `07` |
| `09` | Land | (row−, col−) up | 87/114 | 1265 | `0A` | `0C` |
| `0A` | Land | (row+, col−) up | 88/114 | 1266 | `0B` | `09` |
| `0B` | Land | (row+, col+) up | 88/114 | 1267 | `0C` | `0A` |
| `0C` | Land | (row−, col+) up | 89/114 | 1268 | `09` | `0B` |
| `0D` | Land | all four up (flat, one level higher) | 74/114 | 1269 | `0D` | `0D` |
| `0E` | — | not used | 0 | 1269 | `00` | `00` |
| `0F` | — | not used | 0 | — | `00` | `00` |
| `10` | Underwater | flat | 98/114 | 1270 | `10` | `10` |
| `11` | Underwater | up towards row − 1 | 93/114 | 1270 | `12` | `14` |
| `12` | Underwater | up towards col − 1 | 94/114 | 1270 | `13` | `11` |
| `13` | Underwater | up towards row + 1 | 90/114 | 1270 | `14` | `12` |
| `14` | Underwater | up towards col + 1 | 92/114 | 1270 | `11` | `13` |
| `15` | Underwater | three up, (row+, col+) low | 90/114 | 1270 | `16` | `18` |
| `16` | Underwater | three up, (row−, col+) low | 92/114 | 1270 | `17` | `15` |
| `17` | Underwater | three up, (row−, col−) low | 89/114 | 1270 | `18` | `16` |
| `18` | Underwater | three up, (row+, col−) low | 90/114 | 1270 | `15` | `17` |
| `19` | Underwater | (row−, col−) up | 86/114 | 1270 | `1A` | `1C` |
| `1A` | Underwater | (row+, col−) up | 88/114 | 1270 | `1B` | `19` |
| `1B` | Underwater | (row+, col+) up | 84/114 | 1270 | `1C` | `1A` |
| `1C` | Underwater | (row−, col+) up | 84/114 | 1270 | `19` | `1B` |
| `1D` | Underwater | all four up (flat, one level higher) | 38/114 | 1270 | `1D` | `1D` |
| `1E` | — | not used | 0 | 1270 | `00` | `00` |
| `1F` | — | not used | 0 | 1270 | `00` | `00` |
| `20` | Shore | flat | 86/114 | 1270 | `20` | `20` |
| `21` | Shore | up towards row − 1 | 96/114 | 1271 | `22` | `24` |
| `22` | Shore | up towards col − 1 | 94/114 | 1272 | `23` | `21` |
| `23` | Shore | up towards row + 1 | 95/114 | 1273 | `24` | `22` |
| `24` | Shore | up towards col + 1 | 96/114 | 1274 | `21` | `23` |
| `25` | Shore | three up, (row+, col+) low | 94/114 | 1275 | `26` | `28` |
| `26` | Shore | three up, (row−, col+) low | 94/114 | 1276 | `27` | `25` |
| `27` | Shore | three up, (row−, col−) low | 95/114 | 1277 | `28` | `26` |
| `28` | Shore | three up, (row+, col−) low | 95/114 | 1278 | `25` | `27` |
| `29` | Shore | (row−, col−) up | 92/114 | 1279 | `2A` | `2C` |
| `2A` | Shore | (row+, col−) up | 91/114 | 1280 | `2B` | `29` |
| `2B` | Shore | (row+, col+) up | 90/114 | 1281 | `2C` | `2A` |
| `2C` | Shore | (row−, col+) up | 89/114 | 1282 | `29` | `2B` |
| `2D` | Shore | all four up (flat, one level higher) | 5/114 | 1283 | `2D` | `2D` |
| `2E` | — | never seen; the water tool treats it like `0x3E`, rotation keeps it, drawn with the waterfall sprite | 0 | 1284 | `2E` | `2E` |
| `2F` | — | not used | 0 | — | `00` | `00` |
| `30` | Surface water | open water | 76/114 | 1270 | `30` | `30` |
| `31` | Surface water | bank on row − 1 side | 86/114 | 1271 | `32` | `34` |
| `32` | Surface water | bank on col − 1 side | 85/114 | 1272 | `33` | `31` |
| `33` | Surface water | bank on row + 1 side | 87/114 | 1273 | `34` | `32` |
| `34` | Surface water | bank on col + 1 side | 83/114 | 1274 | `31` | `33` |
| `35` | Surface water | banks on row − 1 and col − 1 sides | 91/114 | 1275 | `36` | `38` |
| `36` | Surface water | banks on col − 1 and row + 1 sides | 92/114 | 1276 | `37` | `35` |
| `37` | Surface water | banks on row + 1 and col + 1 sides | 92/114 | 1277 | `38` | `36` |
| `38` | Surface water | banks on row − 1 and col + 1 sides | 91/114 | 1278 | `35` | `37` |
| `39` | Surface water | land at diagonal (row−, col−) | 58/114 | 1279 | `3A` | `3C` |
| `3A` | Surface water | land at diagonal (row+, col−) | 54/114 | 1280 | `3B` | `39` |
| `3B` | Surface water | land at diagonal (row+, col+) | 52/114 | 1281 | `3C` | `3A` |
| `3C` | Surface water | land at diagonal (row−, col+) | 53/114 | 1282 | `39` | `3B` |
| `3D` | Surface water | isolated pond, land on all sides | 53/114 | 1283 | `3D` | `3D` |
| `3E` | Surface water | waterfall (water on a slope) | 69/114 | 1284 | `3E` | `3E` |
| `3F` | — | not used | 0 | — | `00` | `00` |
| `40` | Surface water | channel along the row (water at col ± 1) | 83/114 | 1285 | `41` | `41` |
| `41` | Surface water | channel along the column (water at row ± 1) | 80/114 | 1286 | `40` | `40` |
| `42` | Surface water | dead end, water only at row + 1 | 68/114 | 1287 | `43` | `45` |
| `43` | Surface water | dead end, water only at col + 1 | 78/114 | 1288 | `44` | `42` |
| `44` | Surface water | dead end, water only at row − 1 | 74/114 | 1289 | `45` | `43` |
| `45` | Surface water | dead end, water only at col − 1 | 75/114 | 1290 | `42` | `44` |
| `46` | — | not used | 0 | — | `00` | `00` |
| `47` | — | not used | 0 | — | `00` | `00` |
| `48`–`FF` | — | not used; outside the rotation tables (rotating would read unrelated data) | 0 | — | — | — |

## Notes

- `0x0E`, `0x1E` and `0x1F` have sprite entries (the shape-`D` sprite and the flat-water sprite) but no routine creates them, and rotating turns them into `0x00`. Treat them as unused.
- `0x2E` is never created by the game's routines, yet the water tool (`0x446650`) and rotation handle it like the waterfall `0x3E`, and it shares the waterfall sprite. **Inferred**: a leftover "waterfall at the shore" code.
- Shore `0x20 + n` and surface water `0x30 + n` (n = 1 to `D`) share sprites: both are drawn as water with land showing on the sides given by `n`.
