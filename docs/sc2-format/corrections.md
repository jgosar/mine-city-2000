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
| MISC words 124–379 (building counts) | djm: tile counts per building code | Correct, but the game **recomputes** them from XBLD and XZON on every load, so the stored values are never used | [container.md § After the walk](container.md#after-the-walk) |
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
