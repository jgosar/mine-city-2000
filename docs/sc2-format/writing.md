# Writing files (out of scope for now)

These pages concentrate on **reading**. Writing a file that the game accepts and simulates correctly is a bigger problem: the segments must agree with each other, and some stored values are trusted while others are recomputed.

This page collects what has turned up so far, so that work can start from it later. **It is not a complete guide.**

## Container

- Write `FORM`, the size (file size − 8, big-endian), and `SCDH`; then the segments with no padding ([container.md](container.md)).
- MISC word 0 must be 0x122 ([container.md § Version check](container.md#version-check)).
- Write all 20 city segments; CNAM is optional ([container.md § Missing, repeated and unknown segments](container.md#missing-repeated-and-unknown-segments)).
- Compressed segments must decode to exactly their decoded size with no input left over. Any mix of literal and repeat runs is accepted, but to match the game's own output avoid control bytes `0x00` and `0x80` ([compression.md](compression.md#what-the-game-writes)).
- Write scenario ids little-endian ([conventions.md § Byte order](conventions.md#byte-order)).

## Values the game recomputes on load

The game overwrites these after loading, so a writer does not need to get them right ([container.md § After the walk](container.md#after-the-walk)):

- MISC words 124–379 (tile count per XBLD code; military-zone tiles are not counted; [tile-counts.md](segments/misc/tile-counts.md#building-counts-words-124379));
- the totals `0x434b70` recounts (developed tiles, road connections, the sum of the bond rates), none of which is stored in MISC;
- XBIT `0x40` (powered), `0x10` (watered) and `0x08` (mask), which are rebuilt by `0x467860`, `0x464550` and the mask step, so they can be left 0 ([xbit.md § What to trust](segments/xbit.md#what-to-trust));
- the road neighbour connection count, recounted from the XTXT `0xFA` tiles ([xtxt.md](segments/xtxt.md#neighbour-connections-0xfa));
- the XGRP graph maxima (not stored in the file at all).

The game does **not** recompute XTER, ALTM, XZON, XUND, the eight overlays or XGRP on load, and it does not redraw subway or pipe shapes until a neighbouring piece is laid. A writer has to make them consistent itself.

## Map layers

- **Orientation.** All maps are stored as seen at the saved compass, and MISC word 2 must match how the arrays are turned. A writer that rotates a map must remap XBLD, XTER and XUND codes and the bridge and on-ramp flags with the same rules as a reader ([conventions.md § Orientation](conventions.md#orientation)). XTHG turns too: positions, directions and targets ([xthg.md § Rotation](segments/xthg.md#rotation)).
- **ALTM.** Stored raw and exactly 32 768 bytes. Keep bit 15 zero and the altitude at most 30.
- **Water.** For underwater and shore codes (`0x10`–`0x2D`) the ALTM water height must equal the sea level (MISC word 912), and a shore tile's altitude is sea level − 1. For surface water (`0x30`–`0x45`) the water height equals the altitude. XBIT `0x04` is set on water tiles and clear on land ([altm.md](segments/altm.md#water-height-bits-59)).
- **Slopes.** Write XTER slopes that match the neighbouring altitudes, ideally with the game's rule (`0x447020`, [xter.md](segments/xter.md#slope-and-altitude)).
- **Buildings.** Write the code on every tile of a multi-tile building and set the XZON corner bits for the current compass: corner *k* gets `0x10 << ((k + compass) & 3)`, one-tile buildings get `0xF0`, edge and interior tiles and all infrastructure get none ([xzon.md § Corner bits](segments/xzon.md#corner-bits)). The game draws a building only at the corner whose bit matches the compass, so wrong corner bits make it invisible or drawn twice (**Inferred** from `0x43c730`).
- **Highways.** Lay them in even-aligned 2 × 2 cells ([xbld.md § Highways](segments/xbld.md#highways)).
- **Military base.** Mark its land with XZON zone 7.
- **XUND.** Write only `0x00`–`0x23`; codes from `0x24` up break rotation. Slope pieces must match the tile's XTER slope; `0x23` belongs under XBLD `0xE9` and `0x6C`–`0x6F`; pipes go with XBIT `0x20` ([xund.md](segments/xund.md#values)).
- **XBIT.** `0x80` (conducts power) and `0x20` (carries water) are trusted: a building or pipe without them never gets power or water. Runways and piers store their `0x02` flag as "along row XOR odd compass"; bridges as "along row" ([xbit.md](segments/xbit.md#0x02-mirror-rotate)).
- **Codes to avoid.** XBLD `0x59` (raised drawbridge) never occurs in saved files.

## MISC

See [misc/README.md § Recomputed and trusted](segments/misc/README.md#recomputed-and-trusted) for what the game overwrites.

- **Layout.** Write word 0 = `0x122`, write 16-bit values sign-extended and 8-bit values zero-extended, and write 0 in words 1044–1199, as the Windows 95 saver does ([misc/README.md](segments/misc/README.md#how-the-words-are-stored)). Write booleans as 0 or 1: the game saves back whatever it loaded, so a 255 from another version survives.
- **A saved city** has word 1 = 1, word 28 = 0, word 1042 = 0 and word 1040 = −1 ([general.md](segments/misc/general.md#disasters)). Word 3 should be 1900, 1950, 2000 or 2050, to match the starting national figures; the game does not check.
- **Trusted counts.** The game never recounts the subway tiles (word 1018, keep it equal to the XUND count, [general.md § Counters](segments/misc/general.md#counters)) or the military counts (words 1002–1017, [tile-counts.md](segments/misc/tile-counts.md#military-counts-words-10021017)); military growth uses the latter.
- **Rewards.** Keep word 8 at least the number of milestones word 1035 has passed, and word 30's bits within the levels reached; otherwise rewards are handed out again or never ([general.md § Rewards](segments/misc/general.md#rewards)).
- **Population.** Keep the age groups' people adding up to word 1035, and word 1035 = 10 × word 380 ([population.md](segments/misc/population.md#age-groups)). Words 380–387 are rebuilt from the map every 25 days; `zone_population_from_map` in `sc2dump.py` gives the full-count values. Industry tax rates 0–20, demand 0–127, sizes ≥ 0.
- **Bonds.** Word 6 ≤ 50, slots 0 to word 6 − 1 of words 388–437 hold rates 1–25, and budget entry 4 holds the same count and the average rate (Σ rates × 10 000 ÷ count). Only the sum of the rates is recomputed on load, so write the average consistently ([population.md § Bonds](segments/misc/population.md#bonds)).
- **Budget.** Year to date = Σ count × rate over months 0 to the current month (all 12 if word 911 is set), and the current month's pair = count and rate. The counts of entries 3–15 are recounted at the start of each month and the tax bases on day 21; the rates are trusted: taxes 0–20, funding 0–100 in steps of 5, ordinances rate 1 ([budget.md](segments/misc/budget.md#layout)). Word 911 is 0 unless the city is saved between the month-11 update and the next month's start.
- **Ordinances.** Write only bits 0–19 of word 1000 ([ordinances.md](segments/misc/ordinances.md#bits)).
- **Neighbours.** Name ids 0–36, with 0 only for neighbour 0 on a coast map (word 913 set), and different non-zero ids; word 441 + 4*k* = 0 ([neighbours.md](segments/misc/neighbours.md#layout)).
- **Demand** −2000 … +2000; words 457–461 = 0. **Technology**: 0 or a future year; 0 everywhere gives the player everything ([technology.md](segments/misc/technology.md#layout)).
- **Newspapers.** Keep each column of words 916–945 a permutation as the game makes it (names, opinion and weather columns 0–5; layouts and price styles 0, 0, 1, 1, 2, 2); other values index string tables and are probably unsafe (**Inferred**). Story slot *s* = (type 11 + *s*, priority 0, parameter 0, 255, 255, 255) is the state of a new game ([newspapers.md](segments/misc/newspapers.md#stories-words-946999)).

## Text and labels

- **XTXT.** Write only 0, signs 1–50, microsim values that match XMIC records, and `0xFA` on transport tiles at the map edge. Never write `0x33`, `0xF1`–`0xF9` or the disaster markers `0xFB`–`0xFF`. Values `0xC9`–`0xF0` should match the XTHG records. 0 there is safe for every type except the dispatched units (7, 8, 14), which do not move and are removed only through their marker; clear their records too ([xthg.md](segments/xthg.md#the-xtxt-marker)) ([xtxt.md](segments/xtxt.md#values)).
- **Microsims.** Keep three things in step: value *v* on every tile of the building, XMIC record *v* − `0x33` whose byte 0 is the building's XBLD code, and a label in XLAB slot *v* ([xtxt.md § Microsims](segments/xtxt.md#microsims-0x3d0xc8)).
- **XLAB.** Length at most 23, padded with zeros. Slot 0 is the mayor, slots 52–60 the city systems (the game always has them), slots 251–255 the team names; slot 250 may be empty ([xlab.md § Label slots](segments/xlab.md#label-slots)).
- **CNAM.** Write `0x1F`, the name (at most 31 bytes, Windows ANSI), then zeros up to 32 bytes. That matches the Windows 95 writer without its leftover memory. Or leave CNAM out, and the game names the city after the file ([cnam.md](segments/cnam.md#layout)).
- **XMIC.** Record 0: byte 0 = `0xEC`, the rest 0. Records 1–9 hold `0xEC`, `0xED`, `0xE9`, `0xC8`, `0xC6` or `0xC7`, `0xD5`, `0xD4`, `0xF5`, `0xF8` even in an empty city. Use records 10–149 only for codes with a record of their own, and write byte 0 = 0 for free records ([xmic.md § Records](segments/xmic.md#records)). Give a new building its starting figures (a power plant's MW, an arcology's design capacity, the year built; [microsim-kinds-xmic.md](reference/microsim-kinds-xmic.md)); the rest may be 0, and the January update fills it in. Nothing in the simulation reads XMIC except that update. Keep MISC words 1032, 1038 and 1039 consistent with the records: they are recomputed only in January.
- **XTHG.** The simplest valid XTHG is 480 zero bytes, with no XTXT `0xC9`–`0xF0` on the map and MISC word 1040 = −1: the game spawns vehicles by itself. Otherwise keep every marker consistent with its record (XTXT at (byte 3, byte 4) = `0xC9` + record), byte 10 equal to the XTXT value the marker replaced, record 0 at type 0 or 4, and the train links. Do not write types 5, 6, 15 or crashing airplanes unless MISC says a disaster is under way: they keep one going ([xthg.md](segments/xthg.md)).
- **Rail and highway connections.** Their count (`0x4ca3f0`) is not recounted on load; where it is stored is not known yet.

## Overlays

- The game uses the stored overlays until the next simulation step 2 (step 19 for XTRF), and step 2 builds on the stored XPLT, XROG, XTRF, XPOP and XCRM ([minimaps.md § What to trust](segments/minimaps.md#what-to-trust)).
- Writing them all 0 is safe (11/114 corpus cities have all-zero 64 × 64 overlays), but the city then looks empty in the data views and grows as if land were worthless until step 2 runs.
- XPOP can be computed exactly from XBLD ([minimaps.md § XPOP](segments/minimaps.md#xpop-building-density)).
- XROG's neutral value is 127 or 128, not 0: an all-zero XROG reads as steep decline everywhere.
- Keep MISC words 10, 11 and 13 equal to the sums of XVAL, XCRM and XPLT, and word 12 at most the sum of XTRF. The game recomputes them at the next step anyway.

## XGRP

- XGRP is trusted as stored: on load the game derives only the graph maxima from it, which are not saved ([xgrp.md](segments/xgrp.md#graph-scale-not-stored)).
- A new city may write 832 zeros, as the game does (`0x40e250`); the blocks fill as the months pass.
- A tool that changes MISC words 18–22 or 1001 changes only the next monthly sample; the history stays as it is.
- When generating history, keep the block rules (slot 12 is an earlier slot 0, slot 32 an earlier slot 12); the game does not check them ([xgrp.md § When samples are taken](segments/xgrp.md#when-samples-are-taken)).

## Not yet known

- Which other stored values the game trusts without checking, and which combinations of segments it needs to be consistent (for example XBLD with XZON, XBIT and XTXT).
- What else the save routine (`0x432350`; `0x433030` writes the header) does differently from the loader. Known so far: it writes CNAM first ([cnam.md](segments/cnam.md#how-the-game-uses-it)).
