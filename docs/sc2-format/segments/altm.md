# ALTM: Altitude map

The height of every tile, plus two more per-tile values packed into the same word: the height of the water surface on the tile, and the depth of a road tunnel beneath it.

## At a glance

| | |
|---|---|
| Tag | `ALTM` |
| Storage | **raw**, not compressed ([container.md § Segment table](../container.md#segment-table)) |
| Size | 32 768 bytes (114/114 files) |
| Shape | 128 × 128 tiles, one **big-endian uint16** per tile |
| Game buffer | `0x4cae10` (128 row pointers) |
| Fix-up on load | every word byte-swapped (`0x42a880`) |

## Layout

Tiles are stored row by row ([conventions.md § The city map](../conventions.md#the-city-map-128--128-tiles)):

```
word(row, col) = u16be(ALTM, (row * 128 + col) * 2)
```

**Confirmed (code)**: the loader reads the segment raw and swaps every 2-byte word. Every routine indexes it as `altm[row][col]`.

## Bit layout

```
 bit  15 | 14  13  12  11  10 |  9   8   7   6   5 |  4   3   2   1   0
      ---+--------------------+--------------------+-------------------
       0 |   tunnel depth     |   water height     |     altitude
```

| Bits | Mask | Field | Values | Confidence |
|---|---|---|---|---|
| 0–4 | `0x001F` | [**Altitude**](#altitude-bits-04): height of the tile's lowest corner | 0–30 | **Confirmed (code)**; **Verified (data)** 114/114 never exceed 30 |
| 5–9 | `0x03E0` | [**Water height**](#water-height-bits-59): height of the water surface on a water tile | 0–31; meaningful only on water tiles | **Confirmed (code)**; **Verified (data)** 114/114 |
| 10–14 | `0x7C00` | [**Tunnel depth**](#tunnel-depth-bits-1014): 0 = no tunnel; otherwise how far a tunnel runs below the surface, plus one | 0–31 (seen 0–20) | **Confirmed (code)**; **Verified (data)** 26/114 files have tunnels |
| 15 | `0x8000` | Unused | always 0 | **Unused**: no code reads it; **Verified (data)** 114/114 |

A reader decodes a word `w` as:

```
altitude     = w        & 0x1F
water_height = (w >> 5)  & 0x1F
tunnel_depth = (w >> 10) & 0x1F
```

The repository's reader takes "the second byte mod 32", which is the same thing as `w & 0x1F`.

### Altitude (bits 0–4)

The height of the tile in **levels**. The tile's corners are at `altitude` or `altitude + 1`; which ones are raised is given by the slope shape in [XTER](xter.md#values). One level is drawn 12 pixels high: the tile renderer (`0x43c730`) shifts a tile up by `height × 12`.

- **Range 0–30.** The terrain "raise" tool (`0x4441c0`) refuses to raise a tile at 30 or above. **Verified (data)**: the largest altitude in the corpus is 30 (114/114).
- On **underwater and shore tiles** the altitude is the sea floor. The water surface is in [bits 5–9](#water-height-bits-59).
- A new, empty map (`0x40e1c0`) sets every word to `0x0042`: altitude 2, water height 2.
- Neighbouring tiles usually differ by at most one level, but there is no hard rule: steps of 2 occur in 82/114 files and steps of 3 in 12/114 (**Verified (data)**).

### Water height (bits 5–9)

The height of the water surface on the tile. The game uses it **only for tiles that hold water**: XTER code `0x10` or higher, or XBIT bit 2 set ([xbit.md](xbit.md#bit-layout)). On dry land the field holds a leftover value that the game ignores. **Confirmed (code)**: the renderer takes the tile height from bits 0–4 when XTER < `0x10` and from bits 5–9 otherwise (`0x43c730`, `0x43d620`, `0x43e550`). It draws the water column (from altitude up to water height) only for tiles with XBIT bit 2 set.

What the value is, by kind of tile (**Verified (data)** 114/114 each):

| Tile (XTER) | Water height | Set by |
|---|---|---|
| Underwater `0x10`–`0x1D` | = sea level, and > altitude | terraforming `0x447020`, flooding `0x4460b0` |
| Shore `0x20`–`0x2D` | = sea level = altitude + 1 | same |
| Surface water `0x30`–`0x45` | = altitude | the water tool `0x446650` |
| Dry land `0x00`–`0x0D` | leftover: 0 on 76% of land tiles, equal to the altitude on 15%, anything else on the rest | ignore |

### Tunnel depth (bits 10–14)

Non-zero on the tiles a road tunnel passes under, 0 everywhere else.

- **Written** when a tunnel is dug (`0x44bea0`): for each tile from one portal to the other, `tile altitude − tunnel altitude + 1`. **Confirmed (code)**. The tunnel altitude is that of its portals (**Inferred**: tunnel portal tiles, XBLD `0x3F`–`0x42`, always have depth 1, **Verified (data)** in all 26 files that have tunnels).
- **Cleared** (together with bit 15, by masking with `0x3FF`) when the tunnel is destroyed (`0x458d40`). **Confirmed (code)**.
- **Read** in the underground view: the bulldozer (`0x443270`) treats depth 1 as something to demolish, and an underground placement check (`0x448f50`) rejects tiles where the depth is 1 or 2. **Confirmed (code)**.
- The surface above a tunnel is unaffected. In the corpus, tiles with a tunnel depth carry empty land, trees, roads and buildings alike.

## Sea level

The city's sea level is **MISC word 912** (bytes 3648–3651, big-endian), which the game loads into the variable `0x4ca818` (**Confirmed (code)**, MISC loader `0x431330`). The full MISC layout is described separately.

| | |
|---|---|
| Range in the corpus | 0–16; 4 in 66/114 files, 5 in 22/114 |
| Relation to ALTM | every underwater and shore tile has water height = sea level (**Verified (data)** 114/114) |
| Below sea level, dry | Dry land tiles one level below sea level exist (37/114 files, 192 tiles, always exactly sea level − 1). The game does not flood them by itself |

A tile becomes sea when terraforming (`0x447020`) leaves its altitude below the sea level. The game then sets XBIT bit 2, sets the water height to the sea level, and gives the tile the underwater or shore version of its slope ([xter.md § Water](xter.md#water)).

## How the game uses it

| Routine | What it does with ALTM |
|---|---|
| `0x43c730` and friends (tile drawing) | Tile height = altitude, or water height for water tiles; draws water columns and map-edge sides |
| `0x4441c0`, `0x445b50`, `0x447c70` (terraforming tools) | Raise or lower altitude by one level, up to 30 |
| `0x447020` (re-shape after terraforming) | Recomputes the XTER slope from the 8 neighbours' altitudes and applies the sea level |
| `0x4460b0` (flood to a level) | Sets the water height on every tile below a given level |
| `0x446650` (place water) | Water height = altitude for new surface water |
| `0x462ed0`, `0x463f60` (highways) | Levels a 2 × 2 highway footprint |
| `0x45b930` (bridges) | Raises each bridge end by one level |
| `0x44bea0`, `0x458d40` (tunnels) | Write and clear the tunnel depth |

## Rotation

When the player rotates the view, ALTM words move with their tiles, but their **values do not change** (**Confirmed (code)**: `0x465490` and `0x466660` swap the words of four tiles at a time, without any remapping). Altitude, water height and tunnel depth have no direction, so nothing needs adjusting. How tile positions move is described in [conventions.md § Orientation](../conventions.md#orientation).

## Evidence

| Claim | Code | Data (`sc2check.py --only altm`) |
|---|---|---|
| Field masks `0x1F`, `0x3E0`, `0x7C00` | 228, 24 and 4 uses of these masks in the decompile | — |
| Altitude ≤ 30 | raise tool `0x4441c0` stops at 30 | 114/114 |
| Bit 15 unused | no reader; cleared only by `0x458d40` | never set, 114/114 |
| Water height rules | `0x447020`, `0x4460b0`, `0x446650`; renderer `0x43c730` | 114/114 each |
| Tunnel depth | written `0x44bea0`, cleared `0x458d40`, read `0x443270`, `0x448f50` | portals depth 1 in 26/26 files with tunnels |
| Sea level = MISC word 912 | `0x431330` → `0x4ca818`; used by `0x447020` | 114/114 in 0..31 |

## Pitfalls for readers

- **Bit numbering.** OpenCity2k numbers bits from the most significant end: its "bits 11–15 altitude, 6–10 water level, 0–5 tunnel?" are bits 0–4, 5–9 and 10–15 here.
- **Bit 7 is not a water flag.** It is part of the water height. With the common sea level of 4 it happens to be set on every water tile, which is why the 1995 description took it for a flag. Decide whether a tile holds water from XTER (code ≥ `0x10`) or XBIT bit 2, not from ALTM.
- **Do not read the water height on dry land.** It is leftover data.
- **Altitude below sea level does not mean water.** Dry tiles one level below sea level exist (37/114 files).
- **The tile's top is not always its altitude.** A sloped tile, or a tile with XTER `0x0D`, has corners one level higher ([xter.md § Slope and altitude](xter.md#slope-and-altitude)).
