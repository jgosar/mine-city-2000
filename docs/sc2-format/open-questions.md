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

## XBIT

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Why do one prison, one water treatment plant and one empty tile carry `0x02`? | Unknown | 3 files | Look for other writers of `0x02` (`0x4417b0` sets it) |
| What does `0x43c730` draw when `XBIT & 0xC0 == 0x80` on a building? | Inferred: the "no power" symbol | The condition is confirmed | Identify the sprite id |
| Why are some powered or watered tiles not reachable from a source (3 and 1 files)? | Inferred: saved in the middle of a flood | The floods yield to Windows every 5000 tiles | Check whether saving is possible in the middle of a simulation step |

## Text and labels

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Which disaster does each of the XTXT markers `0xFB`, `0xFD` and `0xFE` belong to? | Inferred | `0xFF` is fire and `0xFC` flood (code); `0xFD`/`0xFE` move along roads like rioters. The disaster numbers have names ([general.md § Disasters](segments/misc/general.md#disasters)), most of them from OpenCity2k | Follow the dispatcher `0x45cf10` from each disaster number to the marker it writes |
| Why do 27 moving-object tiles in 12 files not match their XTHG record's position? | Open | 1 048 of 1 075 marker tiles match XTHG bytes 3 and 4 | XTHG page (M4): look for a second position field, or stale markers |
| Why do 53 sign labels have no sign on the map? | Inferred: a disaster marker overwrote the sign | `0x461140` and `0x461190` write their markers over signs and leave the label ([xtxt.md](segments/xtxt.md#disaster-markers-0xfb0xff)) | Check whether the sign tool's edit path `0x44d7c0` can leave a label behind too |
| Which routine fills XLAB slots 251–255 (team names), and where are they shown? | Open | The defaults are Llamas, Alpacas, Camels, Dromedaries and Army ants | Search for writes to the label buffer `0x4ca198` at + 251 × 25 |
| Which version writes CNAM with the name length in byte 0, and which puts CNAM last? | Open | 57/107 files; 39 have CNAM last ([cnam.md](segments/cnam.md#which-files-have-which-form)). The same files have data in MISC words 1044–1070 ([general.md](segments/misc/general.md#words-10441199)) | Compare saves from the DOS, Mac and Network editions |
| Does a 31-character city name get a terminator? | Inferred | The segment has none; the loader copies 32 bytes into a larger buffer | Read the CNAM branch of `0x4302e0` |

## Overlays

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| What exactly do the land-value scores start from? | Inferred | Step 2 reuses a scratch grid (`0x4cc6f8`) that is not fully cleared first, so leftovers of the pollution pass leak in | Trace the scratch grid through `0x46a840` in a debugger |
| Which disaster starts at the most polluted cell (case 4 of `0x471bc0`)? | Inferred: the toxic spill | It picks the largest XPLT above 149; 4 is the toxic spill in OpenCity2k's disaster list | Match the disaster numbers to the disaster menu strings |
| How does the fire code use XFIR? | Inferred | `0x45f760` uses XFIR + 8 as a random threshold | Read `0x45f760` fully |

## XGRP

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Why do 17/109 files break the five-year rule and 4/109 the half-year rule? | Unknown | 11 and 3 of them are scenarios | Compare with saves from other versions; check the scenarios' MISC word 3 (founding year) |
| What unit is GNP in? | Unknown | MISC word 21; the help text gives none | Look for GNP display strings in the Industry and budget windows |
| Is month index 0 January? | Inferred | The calendar and budget code only use the index (the budget months are the same index) | Find the month-name table (`0x4c71f8`, used by `0x4731e0` for the Budget window title) |
| Why are the Health and Education floors swapped between a new city and a loaded one? | Unknown (probably a game bug) | `0x4e6750` has 90 and 150, `0x4e72b0` 150 and 90 | — |
| Why do old Power % samples go negative in 3 files? | Inferred: an earlier version had no 100 cap | `CAPE.SC2`, `PETER2.SC2`, `CHICAGO.SCN`: −5 to −8 | — |
| What is MISC word 1032? | Inferred: people living in large microsim buildings | The sum of the XMIC words at offset 4 of building types `0xEC`, `0xED`, `0xF3`–`0xF5`, `0xF8`, `0xFA`–`0xFE` (`0x44e390`) | XMIC page (M4) |

## MISC

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Which version wrote MISC words 1044–1070, and what are words 1045–1047, 1050–1052, 1056, 1065, 1066, 1069 and 1070? | Unknown | Exactly the 60 files with a non-Windows-95 CNAM; 1048 = power %, 1049 = water %, 1067 = developed tiles, 1044 = connection tiles in 27/60 ([general.md](segments/misc/general.md#words-10441199)) | Compare with a DOS or Mac save; check 1050–1052 against the scenarios' goals (SCEN page) |
| Which version writes 255 for "true" in the option words, and non-zero words 380–387 in steps 0–2? | Unknown | 255 in 36/114 files (25 from the other version, 11 re-saved by Windows 95); non-zero 380–387 at steps 0–2 in 15/16 files of the other version | Same |
| Which half of word 1028 is the row? | Inferred | Low 7 bits and high byte; both 10–127 in the corpus | Read `0x412570`, or set the view in the game and save |
| Who writes military base value 4 (navy)? | Inferred | No direct write found; 3 corpus files have it | Search for indirect writes to `0x4cc4e4`, or read the seaport code |
| What do the weather types and the second index of the weather table (`0x4cb3e8`) mean? | Inferred (names from OpenCity2k) | The targets fit the names; 10 hurricane and 11 tornado are confirmed by the disaster code | Read the strings of the ids in `0x4e98d4`; find the writers of `0x4cb3e8` |
| What exactly is word 1038? | Inferred: total arrests | Set by the police-station pass of `0x44e390` and fed into the next pass | Read the police part of `0x44e390` (XMIC page) |
| Which industries does each demand adjustment in `0x468d30` touch? | Inferred | They depend on Pollution Controls, on this month's population growth and on the education quotient | Read the disassembly of `0x468d30` around the floating-point stores the decompile loses |
| What exactly changes the R, C, I demand each month? | Inferred | The tax-rate table `0x4e9328` and the ordinances are confirmed; the floating-point part of `0x468150` is only partly readable | Read the disassembly of `0x468150` around the demand loop |
| Does the Industry window show these words exactly? | Inferred | Its drawing code was not found | Find the window's paint routine (an indirect call) |
| How is a bond repaid, and which one? | Open | No code was found that lowers word 6; 21/114 files have stale rates after the last bond | Find the Budget window's repay handler |
| Where do the −15 532 bond rates come from? | Unknown | Bond slots 0–2 of 6 rich cities (`EM`, `FLOATING`, `ISLAND`, `LINCOLN`, `SIMCITY`, `ZUTHERLA`), the value `0xC354` read as 16 bits | Probably a money cheat or an editor; compare with a known cheat |
| Do Parking Fines (bit 3) and Junior Sports (bit 7) do anything besides their amount? | Open | No test of bits `0x8` or `0x80` was found | Search for loops over all bits other than `0x42bca0` and `0x4194b0` |
| Which power plant does Nuclear Free Zone (bit 17) remove? | Inferred: nuclear | It keeps flag `0x10` out of the available-plant mask (`0x46a080`) | Map the mask bits to the power plant menu |
| What is story type `0x29`, queued when the game enacts a random ordinance? | Open | Queued by `0x4734a0` with the ordinance number | Decode the story templates (next row) |
| What are the newspaper story types 1, 6, 8, `0x10`–`0x12`, `0x15`, `0x3D`–`0x3F`, `0x42`, and what do the story templates and per-type tables hold? | Inferred: monthly statistics news | Posted by `0x468150`; templates loaded by `0x4791d0` from records 1000–1005 of the `DATA` file | Parse `DATA_USA.DAT` and its index |
| Does anything use the neighbours' economy figure? | Inferred: nothing | Only `0x468d30` updates it | — |
| Which ratios does military growth keep, and why do 9 files have military counts that differ from the base on the map? | Inferred / Open | `0x4358b0` compares the military counts per base type; the counts are not recomputed on load | Read `0x4358b0` cases 2–5; check whether those files were edited or saved by the other version |
