# Overview: what is in a `.sc2` file

A city file is an IFF container (`FORM` … `SCDH`) holding a list of **segments**. Each segment has a four-letter tag. Twenty segments describe the city. Scenario files (`.scn`) add three or four more segments that hold the briefing, the picture and the win conditions.

The container, segment headers and walk order are described in [container.md](container.md). Most segments are run-length encoded ([compression.md](compression.md)). Once decoded, every segment has a fixed size.

## Anatomy of a typical file

```mermaid
flowchart LR
    subgraph hdr["File header (12 bytes)"]
        F["'FORM'"] --> S["size"] --> T["'SCDH'"]
    end
    subgraph city["City segments (any order; this is the order the game writes)"]
        CNAM --> MISC --> ALTM --> XTER --> XBLD --> XZON --> XUND --> XTXT --> XLAB --> XMIC --> XTHG --> XBIT --> XTRF --> XPLT --> XVAL --> XCRM --> XPLC --> XFIR --> XPOP --> XROG --> XGRP
    end
    subgraph scn["Scenario segments (.scn only)"]
        TEXT1["TEXT id 0x80"] --> TEXT2["TEXT id 0x81"] --> SCEN --> PICT --> TMPL["TMPL (5 files)"]
    end
    hdr --> city --> scn
```

Where CNAM sits varies. It is first in 68/114 files, last in 39/114 and missing in 7/114 (**Verified (data)**). The game does not care about order ([container.md § Segment order](container.md#segment-order)).

## Segment table

"Decoded size" is the size the game decodes the segment into. It is fixed, and every file in the corpus matches it exactly (**Confirmed (code), Verified (data)** 114/114; see [container.md § Segment table](container.md#segment-table)). "Byte order fix-up" is what the loader does to the decoded bytes before use. Where the Page column says *planned*, the purpose column still summarises the older documents; it gets confirmed when that page is written.

| Tag | Stored | Decoded size | Shape | Byte order fix-up | Purpose | Page |
|---|---|---|---|---|---|---|
| `CNAM` | raw | 32 bytes in every file that has it | string | none | City name | planned |
| `MISC` | RLE | 4800 | 1200 × int32 | each 4-byte word big-endian | General city state: date, funds, budget, tax rates, statistics, settings | planned |
| `ALTM` | raw | 32768 | 128 × 128 × uint16 | each 2-byte word big-endian | Altitude, water surface height and tunnel depth of each tile | [altm.md](segments/altm.md) |
| `XTER` | RLE | 16384 | 128 × 128 × byte | none | Terrain shape (slopes), sea floor, shore and surface water | [xter.md](segments/xter.md) |
| `XBLD` | RLE | 16384 | 128 × 128 × byte | none | What is built on each tile | [xbld.md](segments/xbld.md) |
| `XZON` | RLE | 16384 | 128 × 128 × byte | none | Zone type, and building-corner bits | [xzon.md](segments/xzon.md) |
| `XUND` | RLE | 16384 | 128 × 128 × byte | none | Underground: pipes, subways, subway stations | [xund.md](segments/xund.md) |
| `XTXT` | RLE | 16384 | 128 × 128 × byte | none | Per-tile index of a sign label or microsim | planned |
| `XLAB` | RLE | 6400 | 256 × 25-byte string | none (see note) | Label texts (signs, city name, …) | planned |
| `XMIC` | RLE | 1200 | 150 × 8-byte record | per record: bytes 0–1 as is, then three big-endian uint16 | Microsimulation records (stadiums, …) | planned |
| `XTHG` | RLE | 480 | records | none | Moving things: planes, ships, monsters, … | planned |
| `XBIT` | RLE | 16384 | 128 × 128 × byte | none | Per-tile flags: power, water, salt, sprite mirror | [xbit.md](segments/xbit.md) |
| `XTRF` | RLE | 4096 | 64 × 64 × byte | none | Traffic density | planned |
| `XPLT` | RLE | 4096 | 64 × 64 × byte | none | Pollution | planned |
| `XVAL` | RLE | 4096 | 64 × 64 × byte | none | Land value | planned |
| `XCRM` | RLE | 4096 | 64 × 64 × byte | none | Crime | planned |
| `XPLC` | RLE | 1024 | 32 × 32 × byte | none | Police coverage | planned |
| `XFIR` | RLE | 1024 | 32 × 32 × byte | none | Fire coverage | planned |
| `XPOP` | RLE | 1024 | 32 × 32 × byte | none | Population density | planned |
| `XROG` | RLE | 1024 | 32 × 32 × byte | none | Rate of growth | planned |
| `XGRP` | RLE | 3328 | 16 × 52 × int32 | each 4-byte word big-endian | Graph history | planned |
| `TEXT` | raw | 4-byte id + text | string | id is **little-endian** | Scenario briefing texts (two per file) | planned |
| `SCEN` | raw | 4-byte id + record | record | id is **little-endian** | Scenario goals, disaster, time limit | planned |
| `PICT` | raw | 4-byte id + image | image | id is **little-endian** | Scenario picture | planned |
| `TMPL` | raw | varies | records | — | Field names for `SCEN`; **never read** by the game | planned |

Notes:

- **Shapes** are fixed by the decoded sizes and by how the loader byte-swaps them (**Confirmed (code)**, loader `0x4302e0`). XGRP is 16 series of 52 values: the post-load routine `0x431fd0` walks it that way. The 25-byte XLAB records are **Confirmed (code)** by the label routine `0x430c00`. The record layout of XTHG is not settled yet.
- **XLAB** is not byte-swapped. Each 25-byte record starts with a length byte, which the game converts to a C string after loading ([container.md § After the walk](container.md#after-the-walk)).
- **TMPL** appears in 5 of the 18 scenario files and is not referenced anywhere in `SIMCITY.EXE` (**Confirmed (code)**: no code or data refers to the tag).

## How the parts fit together

```mermaid
flowchart TB
    subgraph tiles["Per-tile layers, 128 × 128"]
        ALTM & XTER & XBLD & XZON & XUND & XTXT & XBIT
    end
    subgraph mini["Overlays (coarser grids)"]
        m64["64 × 64: XTRF XPLT XVAL XCRM"]
        m32["32 × 32: XPLC XFIR XPOP XROG"]
    end
    subgraph tables["Tables"]
        XLAB["XLAB: 256 labels"]
        XMIC["XMIC: 150 microsims"]
        XTHG["XTHG: moving things"]
        XGRP["XGRP: 16 graphs"]
        MISC["MISC: 1200 values"]
    end
    XTXT -- "label index" --> XLAB
    XTXT -- "microsim index" --> XMIC
    tiles -. "each cell covers 2×2 / 4×4 tiles" .- mini
```

The relations are summarised from the older documents. Each one is confirmed on the page of the segment involved, and they are all brought together in `cross-segment.md` (planned).
