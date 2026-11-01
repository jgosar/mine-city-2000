# XTXT text codes

Every value an [XTXT](../segments/xtxt.md) byte can hold. Long ranges are collapsed into one row where the rule is the same for every value in them. The values never change with the compass: rotation moves them without remapping (`0x465490`, `0x466660`).

"Tiles / files" counts the corpus: 114 distinct files, 1 867 776 tiles in total.

| Value | Meaning | XLAB slot | Other target | Tiles / files | Confidence |
|---|---|---|---|---|---|
| `0x00` | nothing | — | — | 1 762 462 / 114 | **Confirmed (code)** |
| `0x01`–`0x32` | sign 1–50 | value | — | 793 / 66 | **Confirmed (code)** `0x44d7c0`, **Verified (data)** |
| `0x33` | — | (51, unused) | (XMIC record 0, unused) | 0 / 0 | **Unused** |
| `0x34` | bus stations (XBLD `0xEC`) | 52 | XMIC record 1 | 4 416 / 55 | **Confirmed (code)** `0x44cf60`, **Verified (data)** |
| `0x35` | rail stations (`0xED`) | 53 | XMIC record 2 | 1 428 / 49 | **Confirmed (code)**, **Verified (data)** |
| `0x36` | subway stations (`0xE9`) | 54 | XMIC record 3 | 1 787 / 48 | **Confirmed (code)**, **Verified (data)** |
| `0x37` | wind power (`0xC8`) | 55 | XMIC record 4 | 645 / 33 | **Confirmed (code)**, **Verified (data)** |
| `0x38` | hydroelectric power (`0xC6`, `0xC7`) | 56 | XMIC record 5 | 3 709 / 34 | **Confirmed (code)**, **Verified (data)** |
| `0x39` | parks (`0xD5`) | 57 | XMIC record 6 | 9 441 / 87 | **Confirmed (code)**, **Verified (data)** |
| `0x3A` | museums (`0xD4`) | 58 | XMIC record 7 | 2 818 / 70 | **Confirmed (code)**, **Verified (data)** |
| `0x3B` | libraries (`0xF5`) | 59 | XMIC record 8 | 2 505 / 78 | **Confirmed (code)**, **Verified (data)** |
| `0x3C` | marinas (`0xF8`) | 60 | XMIC record 9 | 4 176 / 73 | **Confirmed (code)**, **Verified (data)** |
| `0x3D`–`0xC8` | microsim of one building; covers its whole footprint | value (61–200) | XMIC record value − `0x33` (10–149), byte 0 = the building's XBLD code | 71 643 / 104 | **Confirmed (code)** `0x44cf60`, **Verified (data)** 96.7% of tiles |
| `0xC9`–`0xF0` | moving object | — | [XTHG](../segments/xthg.md) record value − `0xC9` (0–39); bytes 3, 4 = (row, col); record 0 = the tools' cursor | 1 075 / 92 | **Confirmed (code)** `0x44d9a0`, **Verified (data)** 97.5% of tiles |
| `0xF1`–`0xF9` | — | — | — | 0 / 0 | **Unused**: no writer found |
| `0xFA` | neighbour connection on a road, rail or highway at the map edge | (250 used as scratch when drawn) | neighbour name at `0x4cad58` | 878 / 76 | **Confirmed (code)** `0x449bb0`, `0x449f50`, `0x4621f0`, **Verified (data)** |
| `0xFB` | disaster marker on zoned buildings: toxic spill, pollution accident (disasters 4, 15; `0x452fa0`) | — | — | 0 / 0 | **Confirmed (code)** `0x45e590`, `0x461190`; never saved |
| `0xFC` | flood water | — | — | 0 / 0 | **Confirmed (code)** `0x461000`; never saved |
| `0xFD` | trail behind a front moving along roads: riots (disasters 3, 13; `0x452fa0`) | — | — | 0 / 0 | **Confirmed (code)** `0x461140`, `0x4612d0`; never saved |
| `0xFE` | front moving along roads: riots (disasters 3, 13; `0x452fa0`) | — | — | 0 / 0 | **Confirmed (code)** `0x461140`, `0x4612d0`; never saved |
| `0xFF` | fire | — | — | 0 / 0 | **Confirmed (code)** `0x460e10`, `0x4611e0`; never saved |

Details and the code behind each range: [xtxt.md § Values](../segments/xtxt.md#values). The labels are described on [xlab.md](../segments/xlab.md#label-slots).

## Microsim building codes

The XBLD codes that get their own record (values `0x3D`–`0xC8`) when placed, from the table at `0x4e7c9a` (**Confirmed (code)**). Names: [XBLD tile codes](tile-codes-xbld.md).

| XBLD codes | Buildings |
|---|---|
| `0xC9`–`0xCF` | power plants |
| `0xD0`–`0xD3` | city hall, hospital, police station, fire station |
| `0xD6`–`0xDB` | school, stadium, prison, college, zoo, statue |
| `0xF3` | mayor's house |
| `0xF4` | water treatment plant |
| `0xFA` | desalination plant |
| `0xFB`–`0xFE` | arcologies |
| `0xFF` | Llama dome |
