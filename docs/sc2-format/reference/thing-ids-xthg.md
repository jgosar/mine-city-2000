# XTHG thing types

Every value of byte 0 of an [XTHG](../segments/xthg.md) record: what the object is, where it comes from, how it moves and how it ends. Field numbers are record bytes ([layout](../segments/xthg.md#layout)). Directions are the 8-way steps of [Directions](../segments/xthg.md#directions) unless a row says otherwise.

The names are ours, matched to the sprites and behaviour; OpenCity2k's list (types 1–15) agrees except where noted. "Limit" is how many the game lets exist; it counts them during each animation pass (`0x450890`), and files from the other game version can hold more ([xthg.md](../segments/xthg.md#pitfalls-for-readers)).

## Summary

| Type | Object | Created by | Limit | Updated by | Speed | Sprite base | Corpus records / files |
|---|---|---|---|---|---|---|---|
| 0 | free record | — | — | — | — | — | — |
| 1 | [airplane](#1-airplane) | `0x452830` (airport), `0x45e7d0` (disaster) | 2 | `0x453980` | 16 | 359 | 99 / 60 |
| 2 | [helicopter](#2-helicopter) | `0x452ba0` (airport) | 1 | `0x4540c0` | 8 | 364 | 72 / 69 |
| 3 | [cargo ship](#3-cargo-ship) | `0x452d00` (seaport) | 1 | `0x456760` | 4 | 369 | 68 / 67 |
| 4 | [bulldozer](#4-bulldozer) | `0x442a30`, `0x442db0` (tools) | record 0 only | — | — | 390 | record 0: 107 files |
| 5 | [monster](#5-monster) | `0x45ee10` (disaster 8) | 1 | `0x4548c0` | 8 | 490 (body parts 478–489) | 0 |
| 6 | [explosion](#6-explosion) | `0x453280`; crashing airplanes and helicopters turn into one | — | `0x4546f0` | — | 387 | 0 |
| 7 | [police unit](#7-8-14-emergency-units) | `0x44fb50` (dispatch tool) | police stations | not animated | — | 382 | 0 |
| 8 | [fire unit](#7-8-14-emergency-units) | `0x44fd60` (dispatch tool) | fire stations | not animated | — | 383 | 30 / 8 |
| 9 | [sailboat](#9-sailboat) | `0x453460` (marina) | 4 | `0x4562e0` | 1 tile per move | 380 | 218 / 73 |
| 10 | [train engine](#1013-trains) | `0x453600` (rail station) | — | `0x455500` | 1 tile per move | 374 | 199 / 47 |
| 11 | [train car](#1013-trains) | `0x453600` | 2 per engine | moved by its engine | — | 374 | 378 / 45 |
| 12 | [subway train engine](#1013-trains) | a type 10 that enters a subway | — | `0x455500` | 1 tile per move | 374, not drawn | 11 / 7 |
| 13 | [subway train car](#1013-trains) | a type 11 behind it | — | moved by its engine | — | 374, not drawn | 42 / 12 |
| 14 | [military unit](#7-8-14-emergency-units) | `0x44ff70` (dispatch tool) | by base type | not animated | — | 384 | 0 |
| 15 | [tornado](#15-tornado) | `0x45f090` (disaster 7) | 1 | `0x454ea0` | 8 | 497 | 0 |
| 16 | [flying rescuer](#16-flying-rescuer) | `0x452fa0` (any disaster) | 1 | `0x455020` | 16 | 495 | 2 / 2 |

All **Confirmed (code)**: the creators write the type, the animation loop `0x450890` dispatches the updates, the speeds are table `0x4e81f8`, the sprite bases table `0x4e8080` (monster and trains drawn by `0x451f10` and `0x451200`). Corpus counts are **Verified (data)** over records 1–39 of the 114 files.

## 1 airplane

- **Created** at an airport: the simulation (`0x4358b0`) picks a powered runway tile (XBLD `0xDD`, XBIT `0x40`) with a 1-in-30 chance; 60% of those make an airplane, 40% a helicopter. None is made while a monster is out. Half the airplanes **arrive** (`0x452830`): they start at a random point 10–109 along a map edge, 16 high, heading into the map, with state 3 and the target (bytes 8, 9) 16 tiles before the runway. The other half **take off** from the runway, height 0, state 0, target (20, 20). Byte 2's bits 4–6 hold the runway heading passed by the caller.
- **Disaster 18** (`0x45e7d0`) places an airplane on a tile in the middle of the map, 16 high, in state 7.
- **States** (low 4 bits of byte 2, `0x453980`): 0 take-off, climbing to 14 → 2 cruise, turning at random → 3 approach to the target → 4 line-up with the runway → 1 landing, descending to 0: removed if it lands on a runway, otherwise it crashes. 7 crashing: spirals down, then explodes.
- **Crashes** (turns into an explosion with byte 2 = 5, the air-crash disaster, and byte 11 = 1 to start fires) when it flies into an arcology (XBLD `0xFB`–`0xFE`) or into a building taller than three times its height. It makes a helicopter near it crash (`0x454650`). While a monster is out, every airplane goes into state 7.
- **Corpus**: heights 16 (52 of 99) and 14 (37); low nibble of byte 2 (state) 3 in 51, 2 in 36, others 12; the heading bits are always even (0, 2, 4, 6).

## 2 helicopter

- **Created** at an airport with the airplane rule above (`0x452ba0`), on the runway tile, state 0, a random target.
- **States** (byte 2, `0x4540c0`): 0 take-off, climbing to 10 → 2 flying to the target. On arrival it picks a new target within 32 tiles of the centre of the buildings (MISC words 1030, 1031), and half the time it lands if the tile below is empty, flat land: 3 descending → 4 on the ground, taking off again with a 1-in-20 chance per update. 5 crashing: spins down, then explodes as disaster 17 (helicopter crash), with fires.
- Over an XTRF cell (2 × 2 tiles) with traffic above 169 it plays the traffic report sound, at most every 5 seconds.
- Airplanes and helicopters steer around arcologies (`0x454520`). While a monster is out, every helicopter goes into state 5.
- **Corpus**: height 10 and state 2 in 69 of 72.

## 3 cargo ship

- **Created** at a seaport (`0x452d00`): a crane tile (XBLD `0xE0`) in steps 3–18 with a 1-in-4 chance. It starts on deep water (XTER `0x10`) two tiles inside a random map edge, height 1. The start point is kept for the way back in variables that are not saved (`0x4e80cc`, `0x4e80d0`).
- **States** (byte 2, `0x456760`): 0 sailing toward the target (bytes 8, 9), turning around obstacles (1, 2); 3 moored next to a pier (XBLD `0xDF`), leaving with a 1-in-30 chance per update (with a sound): the target becomes the start point → 4 sailing back. A ship that can turn nowhere is removed; one that is off the water (XBIT `0x04`) or off the map turns into an explosion without fires.
- Where the target of state 0 comes from is **Unknown**: the spawn routine does not set bytes 8, 9.
- **Corpus**: state 0 (39 of 68) or 4 (23); 9 stand on the map edge without their XTXT marker.

## 4 bulldozer

Only in record 0. While the player drags the bulldozer or the level-terrain tool, record 0 is type 4 and follows the cursor tile, marked with XTXT `0xC9` ([xthg.md § Record 0](../segments/xthg.md#record-0)). OpenCity2k: "unknown". The sprite (390) is assumed to be a bulldozer (**Inferred**).

## 5 monster

- **Created** by disaster 8 (`0x45ee10`) at the disaster spot. MISC word 1040 holds its record ([general.md](../segments/misc/general.md#disasters)). Byte 11 is its **variant**: 0 (it sets fires) or, with a 50% chance unless a scenario is running (`0x4cad44`, [scen.md](../segments/scen.md#how-the-game-uses-it)), 1–3.
- **States** (byte 2, `0x4548c0`): 0 walking toward the centre of the buildings while its height (byte 5) drops from 15 to 8; 1 rampaging in random directions; 2 rising to height 15; then either back to 0 or 3 leaving, removed with a 1-in-100 chance per update.
- **Rampage**: on each tile it steps on, variant 0 starts a fire (`0x460e10`) if the XBLD code is above 5. Variants 1–3 destroy the tile (`0x458d40`) and leave: trees (XBLD 9–12) for variant 1, water for variant 2 (`0x446650`), and for variant 3 a call of `0x440c50` with 200 (**Unknown** effect).
- A military unit (type 14) next to it makes it leave. While it exists, all airplanes and helicopters crash.
- **Bytes 8 and 9 are animation bits** for the drawing (`0x451f10`); bit 7 of byte 8 marks "destroyed something this step". They are not a target, although the rotation code turns them like one.

## 6 explosion

- **Created** when an airplane or helicopter crashes (the record changes type), when a train derails, and when the flying rescuer defeats the monster or tornado (`0x453280`).
- Byte 1 counts the frames; after 2 the record is removed (`0x4546f0`).
- If byte 11 is non-zero and disasters are on, it tries to start fires on 4 random tiles within 2 of its tile. If one starts, the pending disaster (MISC word 28) becomes byte 2 (1, fire, when byte 2 is 0) at that spot. Byte 2 is 5 for an airplane, 17 for a helicopter, 0 for a train.

## 7, 8, 14 emergency units

- **Placed** by the player with the dispatch tool during a disaster (`0x43f220` → `0x44fb50` police, `0x44fd60` fire, `0x44ff70` military), on a dry tile without an XTXT marker. They do not move: the animation loop skips them.
- **How many**: police and fire units up to the police and fire station tiles ÷ 8 (XBLD `0xD2`, `0xD3`); military units by the base in MISC word 915: army 5, air force 2, navy 3. A city with none of the three gets one military unit (**Inferred**: the National Guard). Placing more moves the oldest unit (position tables `0x4c7da0` … `0x4c8608`, not saved) (`0x44f910`).
- Fire and military units, and sometimes police units, fight the fires and riots next to them (`0x45f760`).
- At the start of a dispatch (`0x44f910`) and when the disaster ends (`0x45cf10`) the game removes every unit **through its XTXT marker**. A unit whose marker was lost stays in XTHG.
- **Corpus**: 30 fire units in 8 files, all without a marker: stale (**Inferred**).

## 9 sailboat

- **Created** next to a powered marina (XBLD `0xF8`) in steps 3–18 (`0x453380` → `0x453460`), on an adjacent water tile with no XTXT value, while there are fewer than 4 sailboats and fewer than the marina tiles ÷ 9. Byte 1 is a 4-way direction (0–2 at creation), bytes 6, 7 = 4, 4.
- **Moves** (`0x4562e0`) one tile at a time in its direction; with a 1-in-4 chance per update it turns by −1, 0 or +1. It is removed when it reaches a marina, leaves the water, or more than 4 exist.
- **Byte 2 = 1** (a 1-in-4000 chance at each turn, with a sound): drawn with another, animated sprite (379); then removed with a 1-in-5 chance per update. OpenCity2k: "turns the boat into Nessie" (**Inferred**: not checked in the game). **Verified (data)**: 0 in all 218 sailboats.
- A turned view gives it a direction of 4–7 ([xthg.md § Rotation](../segments/xthg.md#rotation)).

## 10–13 trains

- **Created** next to a powered rail station (XBLD `0xED`) in steps 3–18 (`0x453590` → `0x453600`) while there are fewer engines than rail-station tiles ÷ 4 and at most 4, on a straight rail tile (XBLD `0x2C`–`0x35`) without a marker. One engine (10) and two cars (11), all on the start tile.
- **Fields**: byte 1 is the 4-way direction; byte 2 of the engine is the record of the first car, and the first car's byte 2 the record of the second; bytes 6, 7 the next tile; byte 8 the sprite direction (low 3 bits; not set at creation). Bytes 9 and 11 are never written (stale).
- **Moving** (`0x455500`, engines only): the engine moves to its next tile and each car takes the tile of the one in front (`0x455ef0`). It sometimes waits at a station, picks a direction where the track allows, and reverses when it cannot go on (`0x455fc0`).
- **Subways**: on a subway-to-rail tile (XBLD `0x6C`–`0x70`) the engine switches between 10 and 12; each car takes the engine's kind (11 or 13) as it follows. Types 12 and 13 are underground and not drawn.
- **Derailing**: if the engine's tile is no longer track (rail, rail bridge, crossing, subway connection; or subway underneath for type 12), all three records are removed and an explosion is made (`0x455dc0`).
- **Verified (data)**: engine → car → car links are valid for 209 of 210 engines.

## 15 tornado

- **Created** by disaster 7 (`0x45f090`) at the disaster spot. Byte 5 is the altitude of the ground below (ALTM). MISC word 1040 holds its record.
- **Moves** (`0x454ea0`) one tile per update in a randomly wobbling direction (± 2), and a second time when the tile it stood on has an XBLD code below `0x0D`. It destroys the tile under it when the XBLD code is above 5 (`0x458d40`). Each move has a 1-in-256 chance of ending it.

## 16 flying rescuer

- **Created** (`0x452fa0`) when a disaster starts, if the city refused the military base or had no site for it (MISC word 915 = 1), with a 1-in-4 chance. It appears 16 tiles from the disaster spot (MISC words of `0x4caa58`, not saved), flies over the ground (byte 5 = ground altitude + its state), and the view jumps to it.
- **Byte 11 is its target**, set from the disaster number ([general.md](../segments/misc/general.md#disasters)): the XTXT marker `0xFF` (fire) for disasters 1, 5, 6, 9, 10, 11, 12; `0xFC` (flood) for 2 and 14; `0xFD` or `0xFE` (riots) for 3 and 13; `0xFB` (toxic) for 4 and 15; the record number of the monster or tornado (MISC word 1040) for 7 and 8. **Confirmed (code)**: the switch in `0x452fa0`. This ties each XTXT disaster marker to its disasters ([xtxt.md](../segments/xtxt.md)).
- **Update** (`0x455020`): it flies toward the target (bytes 8, 9 for a marker, or the target record's tile). Next to a marker it clears it with a 50% chance; when none is left at the target it searches 32 tiles around for another. Next to the monster or tornado it removes it with a 1-in-4 chance, leaving an explosion that starts fires. Its height is the ground altitude plus byte 2.
- OpenCity2k does not list type 16. The name is ours; the object is known among players as "Maxis Man" (**Inferred**: no name for it in the game's text).
- **Corpus**: 2 records, in `HILTON.SC2` and `2008.sc2`, both state 0.

## Links

- [XTHG](../segments/xthg.md): layout, markers, rotation
- [XTXT values `0xC9`–`0xF0`](../segments/xtxt.md#moving-objects-0xc90xf0)
- [MISC: disasters](../segments/misc/general.md#disasters)
- [game-internals.md](../game-internals.md)
