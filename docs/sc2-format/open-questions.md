# Open questions

Everything these pages tag **Inferred** or **Unknown**, with what has been tried. When one is settled, move the answer to the page it belongs on and delete it here.

## Container and compression

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Does the game clear the buffers of segments missing from a file before loading? | Unknown | The loader itself does not ([container.md](container.md#missing-repeated-and-unknown-segments)) | Read the callers of `0x4302e0` (new city / open city paths) |
| Is a non-IFF file of 27 248 bytes a SimCity Classic city? | Inferred | `0x482850` checks for exactly that size; Classic `.cty` files have that size | Read what `0x482850`'s caller does when it returns 1 |
| Does a decoding error abort the load? | Inferred | `0x430dc0` shows an error when input is left over; Ghidra does not show its return value clearly | Check the return register in the disassembly of `0x430dc0` |
| Character set of strings | Inferred | ANSI code page of the saving machine | Find a city saved on a non-Western system |

## Conventions

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Which screen edge does the game's compass rose call "north" at compass 0? | Open (not needed for reading) | Compass 0 = the view at creation; row runs to the bottom-right, col to the bottom-left ([conventions.md](conventions.md#orientation)) | Find the code that draws the compass rose |
| 32 × 32 overlay cell ↔ tile mapping (4 × 4) | Inferred | The grid size is confirmed; the 64 × 64 mapping is confirmed | Minimap page (M2) |
| Signedness of MISC values | Inferred | Some are copied into 8- or 16-bit variables | MISC pages (M3) |

## ALTM and XTER

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Why do some water tiles lack XBIT `0x04`? | Unknown | 127 underwater tiles in 26 files and 1 848 surface-water tiles in 10 files; none over a tunnel, mostly empty | Find which routine clears `0x04` without changing XTER (writes of `& 0xFB` to the XBIT buffer `0x4cab10`) |
| How do dry tiles one level below sea level arise? | Unknown | 192 tiles in 37 files, always exactly sea level − 1, mostly empty | Same approach; check `0x45b930` (bridges) and the dam code |
| Is the tunnel altitude the portals' altitude? | Inferred | Portals always have tunnel depth 1 | Read where `0x44bea0` gets its altitude parameter |
| What is XTER `0x2E`? | Inferred | Handled like `0x3E`, drawn with the waterfall sprite, never created | Search for writes of `0x2E` through pointers |
| Who calls `0x4460b0` (flood to a level)? | Inferred: the terrain editor's sea-level control | — | Follow its callers |

## XBLD

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| How does the game draw and simulate fragment tiles (multi-tile codes outside any complete footprint)? | Unknown | 37/114 files have them, mostly 1–5 tiles, mostly without corner bits | Read the renderer `0x43c730` for codes `0x70` and up, and the bulldozer; or load such a city and query a fragment |
| Which code picks the query tool's name string? | Inferred | Strings 594–753 match the code ranges in order; `0x42b520` (category 2000 → 593 + index) is the likely entry, but no caller passing 2000 was found | Find indirect callers of `0x42b520`, or the code → index table of the query dialog |
| Do seaport and airport pieces `0xF0` / `0xF2` use other corner bits? | Open | JUNETOWN, LAKEVIEW and SIMCITY have 2 × 2 port blocks whose corner bits break the compass pattern | Read the seaport and airport growth code |
| Which way do the power line, road and rail shapes run? | Inferred | From djm's screen descriptions; the rotation cycles confirm their order | Render a test city at compass 0 |

## XZON and XUND

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Why does no missile silo in the corpus have XUND `0x22`? | Open | `0x4382b0` writes `0x22` when it places XBLD `0xF9`; 0 of the 27 silo tiles in the corpus carry it | Find other code paths that place `0xF9` (military base generation `0x4142c0` and its callers) and any code that clears XUND later |
| What would zone values 10–15 do? | Unknown | Never in the corpus; the renderer picks ground sprites by base + zone | Try in the game, or count the sprites from `0x50A` / `0x316` |
| Why do 9 files have corner bits off their footprint (144 tiles)? | Open | Mostly `FLOATING.SC2`, `PETER3.SC2`, `RETREAT.SC2` | Compare with what the game does when bulldozing; possibly hand-edited files |
| Is the subway tile count (`0x4ca41c`) stored in MISC? | Open | Maintained by `0x442370` | MISC pages (M3) |

## XBIT

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| What does the `0x08` mask feed in simulation step 2 (`0x46a840`)? | Inferred | A 64 × 64 mask of developed 2 × 2 blocks, the same grid as XPLT and XVAL | Read the parts of `0x46a840` that test `0x08` |
| Why do one prison, one water treatment plant and one empty tile carry `0x02`? | Unknown | 3 files | Look for other writers of `0x02` (`0x4417b0` sets it) |
| What does `0x43c730` draw when `XBIT & 0xC0 == 0x80` on a building? | Inferred: the "no power" symbol | The condition is confirmed | Identify the sprite id |
| Why are some powered or watered tiles not reachable from a source (3 and 1 files)? | Inferred: saved in the middle of a flood | The floods yield to Windows every 5000 tiles | Check whether saving is possible in the middle of a simulation step |
