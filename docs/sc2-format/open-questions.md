# Open questions

Everything these pages tag **Inferred** or **Unknown**, with what has been tried. When one is settled, move the answer to the page it belongs on and delete it here.

## Container and compression

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Does the game clear the buffers of segments missing from a file before loading? | Unknown | The loader itself does not ([container.md](container.md#missing-repeated-and-unknown-segments)) | Read the callers of `0x4302e0` (new city / open city paths) |
| Is a non-IFF file of 27 248 bytes a SimCity Classic city? | Inferred | `0x482850` checks for exactly that size; Classic `.cty` files have that size | Read what `0x482850`'s caller does when it returns 1 |
| Does a decoding error abort the load? | Inferred | `0x430dc0` shows an error when input is left over; Ghidra does not show its return value clearly | Check the return register in the disassembly of `0x430dc0` |
| Is the character set always the saving machine's ANSI code page? | Inferred | Code page 1252 is **Verified (data)** for the corpus ([conventions.md § Strings](conventions.md#strings)); the game converts nothing on load or save | Find a city saved on a non-Western system |

## Conventions

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Which screen edge does the game's compass rose call "north" at compass 0? | Open (not needed for reading) | Compass 0 = the view at creation; row runs to the bottom-right, col to the bottom-left ([conventions.md](conventions.md#orientation)) | Find the code that draws the compass rose |
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
| Why do one prison, one water treatment plant and one empty tile carry `0x02`? | Unknown | 3 files | Look for other writers of `0x02` (`0x4417b0` sets it) |
| What does `0x43c730` draw when `XBIT & 0xC0 == 0x80` on a building? | Inferred: the "no power" symbol | The condition is confirmed | Identify the sprite id |
| Why are some powered or watered tiles not reachable from a source (3 and 1 files)? | Inferred: saved in the middle of a flood | The floods yield to Windows every 5000 tiles | Check whether saving is possible in the middle of a simulation step |

## Text and labels

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Which disaster does each of the XTXT markers `0xFB`, `0xFD` and `0xFE` belong to? | Inferred | `0xFF` is fire and `0xFC` flood (code); `0xFD`/`0xFE` move along roads like rioters | Map the cases 1–18 of the dispatcher `0x45cf10` to the disaster names (strings 33210 and up) |
| Why do 27 moving-object tiles in 12 files not match their XTHG record's position? | Open | 1 048 of 1 075 marker tiles match XTHG bytes 3 and 4 | XTHG page (M4): look for a second position field, or stale markers |
| Why do 53 sign labels have no sign on the map? | Inferred: a disaster marker overwrote the sign | `0x461140` and `0x461190` write their markers over signs and leave the label ([xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff)) | Check whether the sign tool's edit path `0x44d7c0` can leave a label behind too |
| Which routine fills XLAB slots 251–255 (team names), and where are they shown? | Open | The defaults are Llamas, Alpacas, Camels, Dromedaries and Army ants | Search for writes to the label buffer `0x4ca198` at + 251 × 25 |
| Where are the four neighbour city names (`0x4cad58`, 4 × 32 bytes) stored? | Open | Not in XLAB or CNAM | MISC pages (M3) |
| Which version writes CNAM with the name length in byte 0, and which puts CNAM last? | Open | 57/107 files; 39 have CNAM last ([cnam.md](segments/cnam.md#which-files-have-which-form)) | Compare saves from the DOS, Mac and Network editions |
| Does a 31-character city name get a terminator? | Inferred | The segment has none; the loader copies 32 bytes into a larger buffer | Read the CNAM branch of `0x4302e0` |

## Overlays

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| What exactly do the land-value scores start from? | Inferred | Step 2 reuses a scratch grid (`0x4cc6f8`) that is not fully cleared first, so leftovers of the pollution pass leak in | Trace the scratch grid through `0x46a840` in a debugger |
| Which disaster starts at the most polluted cell (case 4 of `0x471bc0`)? | Inferred: the monster | It picks the largest XPLT above 149 | Match the case numbers to the disaster menu strings |
| What are the ordinance bits `0x4`, `0x10`, `0x800` and `0x80000` of `0x4caa40`? | Inferred | Step 2 changes crime (+16), fire coverage (+2 per tile), police coverage (+2 per tile) and how fast pollution fades | MISC pages (M3): find where the ordinance dialog sets them |
| How does the fire code use XFIR? | Inferred | `0x45f760` uses XFIR + 8 as a random threshold | Read `0x45f760` fully |

## XGRP

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Why do 17/109 files break the five-year rule and 4/109 the half-year rule? | Unknown | 11 and 3 of them are scenarios | Compare with saves from other versions; check the scenarios' MISC word 3 (founding year) |
| Which three counts does the Traffic graph divide by? | Inferred: road, highway and bridge tiles | Entries 10–12 of the table at `0x4cc4cc`, field + `0x60` | Find the writers of that table |
| What unit is GNP in? | Unknown | MISC word 21; the help text gives none | Look for GNP display strings in the Industry and budget windows |
| Is month index 0 January? | Inferred | The calendar code only uses the index | Find the month-name table of the date display |
| Why are the Health and Education floors swapped between a new city and a loaded one? | Unknown (probably a game bug) | `0x4e6750` has 90 and 150, `0x4e72b0` 150 and 90 | — |
| Why do old Power % samples go negative in 3 files? | Inferred: an earlier version had no 100 cap | `CAPE.SC2`, `PETER2.SC2`, `CHICAGO.SCN`: −5 to −8 | — |
| What is MISC word 1032? | Inferred: people living in large microsim buildings | The sum of the XMIC words at offset 4 of building types `0xEC`, `0xED`, `0xF3`–`0xF5`, `0xF8`, `0xFA`–`0xFE` (`0x44e390`) | XMIC page (M4) |
