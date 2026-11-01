# XUND underground codes

Every value an [XUND](../segments/xund.md) byte can hold. Directions are array directions (row−1, row+1, col−1, col+1), as explained in [xund.md § Piece shapes](../segments/xund.md#piece-shapes). The rotation columns give the code a tile gets when the view is rotated by `0x465490` (array move `new[row][col] = old[col][127 − row]`, table `0x4e90e8`) or by `0x466660` (the inverse, table `0x4e9258`).

"Tiles / files" counts the corpus: 114 distinct files, 1 867 776 tiles in total.

| Code | Contents | Connects to | Higher end | → `0x465490` | → `0x466660` | Tiles / files | Confidence |
|---|---|---|---|---|---|---|---|
| `0x00` | nothing | — | — | `0x00` | `0x00` | 1 703 761 / 114 | **Confirmed (code)** |
| `0x01` | subway, straight | col−1, col+1 | — | `0x02` | `0x02` | 10 618 / 48 | **Confirmed (code)**, **Verified (data)** |
| `0x02` | subway, straight | row−1, row+1 | — | `0x01` | `0x01` | 9 851 / 48 | **Confirmed (code)**, **Verified (data)** |
| `0x03` | subway, slope | row−1, row+1 | row−1 | `0x04` | `0x06` | 301 / 31 | **Confirmed (code)**, **Verified (data)** |
| `0x04` | subway, slope | col−1, col+1 | col−1 | `0x05` | `0x03` | 337 / 40 | **Confirmed (code)**, **Verified (data)** |
| `0x05` | subway, slope | row−1, row+1 | row+1 | `0x06` | `0x04` | 452 / 32 | **Confirmed (code)**, **Verified (data)** |
| `0x06` | subway, slope | col−1, col+1 | col+1 | `0x03` | `0x05` | 232 / 31 | **Confirmed (code)**, **Verified (data)** |
| `0x07` | subway, bend | row+1, col−1 | — | `0x08` | `0x0A` | 1 247 / 46 | **Confirmed (code)**, **Verified (data)** |
| `0x08` | subway, bend | row+1, col+1 | — | `0x09` | `0x07` | 1 038 / 47 | **Confirmed (code)**, **Verified (data)** |
| `0x09` | subway, bend | row−1, col+1 | — | `0x0A` | `0x08` | 1 286 / 47 | **Confirmed (code)**, **Verified (data)** |
| `0x0A` | subway, bend | row−1, col−1 | — | `0x07` | `0x09` | 1 050 / 46 | **Confirmed (code)**, **Verified (data)** |
| `0x0B` | subway, T-junction | row−1, row+1, col−1 | — | `0x0C` | `0x0E` | 303 / 38 | **Confirmed (code)**, **Verified (data)** |
| `0x0C` | subway, T-junction | row+1, col−1, col+1 | — | `0x0D` | `0x0B` | 271 / 32 | **Confirmed (code)**, **Verified (data)** |
| `0x0D` | subway, T-junction | row−1, row+1, col+1 | — | `0x0E` | `0x0C` | 299 / 35 | **Confirmed (code)**, **Verified (data)** |
| `0x0E` | subway, T-junction | row−1, col−1, col+1 | — | `0x0B` | `0x0D` | 276 / 33 | **Confirmed (code)**, **Verified (data)** |
| `0x0F` | subway, crossroads | all four | — | `0x0F` | `0x0F` | 87 / 21 | **Confirmed (code)**, **Verified (data)** |
| `0x10` | pipe, straight | col−1, col+1 | — | `0x11` | `0x11` | 50 076 / 98 | **Confirmed (code)**, **Verified (data)** |
| `0x11` | pipe, straight | row−1, row+1 | — | `0x10` | `0x10` | 48 509 / 100 | **Confirmed (code)**, **Verified (data)** |
| `0x12` | pipe, slope | row−1, row+1 | row−1 | `0x13` | `0x15` | 1 575 / 71 | **Confirmed (code)**, **Verified (data)** |
| `0x13` | pipe, slope | col−1, col+1 | col−1 | `0x14` | `0x12` | 1 584 / 70 | **Confirmed (code)**, **Verified (data)** |
| `0x14` | pipe, slope | row−1, row+1 | row+1 | `0x15` | `0x13` | 1 167 / 71 | **Confirmed (code)**, **Verified (data)** |
| `0x15` | pipe, slope | col−1, col+1 | col+1 | `0x12` | `0x14` | 1 313 / 71 | **Confirmed (code)**, **Verified (data)** |
| `0x16` | pipe, bend | row+1, col−1 | — | `0x17` | `0x19` | 2 068 / 91 | **Confirmed (code)**, **Verified (data)** |
| `0x17` | pipe, bend | row+1, col+1 | — | `0x18` | `0x16` | 1 951 / 95 | **Confirmed (code)**, **Verified (data)** |
| `0x18` | pipe, bend | row−1, col+1 | — | `0x19` | `0x17` | 2 137 / 94 | **Confirmed (code)**, **Verified (data)** |
| `0x19` | pipe, bend | row−1, col−1 | — | `0x16` | `0x18` | 1 968 / 96 | **Confirmed (code)**, **Verified (data)** |
| `0x1A` | pipe, T-junction | row−1, row+1, col−1 | — | `0x1B` | `0x1D` | 2 585 / 85 | **Confirmed (code)**, **Verified (data)** |
| `0x1B` | pipe, T-junction | row+1, col−1, col+1 | — | `0x1C` | `0x1A` | 2 634 / 88 | **Confirmed (code)**, **Verified (data)** |
| `0x1C` | pipe, T-junction | row−1, row+1, col+1 | — | `0x1D` | `0x1B` | 2 600 / 88 | **Confirmed (code)**, **Verified (data)** |
| `0x1D` | pipe, T-junction | row−1, col−1, col+1 | — | `0x1A` | `0x1C` | 2 673 / 87 | **Confirmed (code)**, **Verified (data)** |
| `0x1E` | pipe, crossroads | all four | — | `0x1E` | `0x1E` | 9 945 / 96 | **Confirmed (code)**, **Verified (data)** |
| `0x1F` | crossing: pipe along the rows (row−1, row+1), subway along the cols (col−1, col+1) | both | — | `0x20` | `0x20` | 799 / 40 | **Confirmed (code)**, **Verified (data)** |
| `0x20` | crossing: pipe along the cols, subway along the rows | both | — | `0x1F` | `0x1F` | 904 / 39 | **Confirmed (code)**, **Verified (data)** |
| `0x21` | never used | — | — | `0x21` | `0x21` | 0 / 0 | **Unused**: no code writes it |
| `0x22` | missile silo (under XBLD `0xF9`) | counts as subway | — | `0x22` | `0x22` | 0 / 0 | **Confirmed (code)** `0x4382b0`; absent from the corpus |
| `0x23` | subway station (under XBLD `0xE9`) or subway-to-rail connection (under XBLD `0x6C`–`0x6F`) | counts as subway | — | `0x23` | `0x23` | 1 879 / 49 | **Confirmed (code)** `0x440170`, `0x441b80`; **Verified (data)** |
| `0x24`–`0xFF` | never used; beyond the end of the rotation tables | — | — | undefined | undefined | 0 / 0 | **Unused** |

"Counts as subway" means the game includes the code in its subway tile count and treats the tile as a subway neighbour when it picks shapes (`0x442370`, `0x44bc40`).
