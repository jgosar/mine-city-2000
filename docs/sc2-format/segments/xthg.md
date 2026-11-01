# XTHG: moving objects

The vehicles and other objects that move over the city: airplanes, helicopters, ships, sailboats, trains, the monster, the tornado and the units the player dispatches during a disaster. XTHG holds 40 records of 12 bytes, one per object. An object also marks the tile it stands on in [XTXT](xtxt.md#moving-objects-0xc90xf0) with the value `0xC9` + its record number. The objects are animation: nothing in the simulation depends on them, and a city loads and runs with all 40 records empty.

## At a glance

| | |
|---|---|
| Size | 480 bytes: 40 records × 12 bytes |
| Storage | RLE-compressed ([compression.md](../compression.md)); no byte swapping |
| Element | 12 single bytes per record ([Layout](#layout)) |
| Game buffer | `0x4ca434` (a pointer to the 480 bytes) |
| Link | record *n* ↔ XTXT value `0xC9` + *n* on the object's tile |
| Changes with the compass? | Yes: positions and directions turn ([Rotation](#rotation)) |
| Every object type | [XTHG thing types](../reference/thing-ids-xthg.md) |

**Confirmed (code)**: the loader reads XTHG with `0x430dc0` into 480 bytes (`0x1E0`) and does not swap it (`0x4302e0`); the save routine writes the buffer as it is (`0x432350`). All 114 corpus files have 480 bytes.

## Layout

Record *n* (0–39) starts at byte 12 × *n*. All fields are single unsigned bytes.

| Byte | Field | Meaning | Confidence |
|---|---|---|---|
| 0 | type | 0 = free record; 1–16 = the kind of object ([thing types](../reference/thing-ids-xthg.md)) | **Confirmed (code)**: the animation loop `0x450890` dispatches on it |
| 1 | direction | Heading. Most types: 0–7 ([Directions](#directions)). Trains: 0–3. Sailboats: 0–3. Explosions: animation frame | **Confirmed (code)**: `0x4516f0`, `0x455500`, `0x456600` |
| 2 | state | What the object is doing, per type (take-off, cruise, landing, docked, …). For a train engine or car: the record number of the next car | **Confirmed (code)** |
| 3 | row | Tile row of the object | **Confirmed (code)**, **Verified (data)** |
| 4 | col | Tile col of the object | **Confirmed (code)**, **Verified (data)** |
| 5 | height | Height above the ground, in the units of the object's sprite (airplane up to 16, helicopter 10, ship 1, sailboat 0) | **Confirmed (code)**: drawing `0x450bb0` lifts the sprite by it |
| 6, 7 | sub-tile position | Position inside the tile along the row and col, 0–16 (8 = centre). **Trains**: the (row, col) of the next tile | **Confirmed (code)**: `0x4516f0`; trains `0x455500` |
| 8, 9 | target | (row, col) the object heads for. **Monster**: animation bits. **Trains**: byte 8 is the sprite direction (low 3 bits), byte 9 is unused | **Confirmed (code)** |
| 10 | XTXT under | The XTXT value the object's marker replaced on its tile | **Confirmed (code)**: `0x450690`, `0x4516f0` |
| 11 | extra | Per type: start fires (explosion), variant (monster), target (flying rescuer). Not written for other types | **Confirmed (code)** |

The field names are ours. OpenCity2k's labels for bytes 6–11 ("px", "py", "dx", "dy", "label", "goal") fit this layout.

### Directions

The 8-way direction used by most types is one step on the map (**Confirmed (code)**: tables `0x4e80e0` / `0x4e80f0`):

| Direction | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|---|
| Row step | 0 | +1 | +1 | +1 | 0 | −1 | −1 | −1 |
| Col step | −1 | −1 | 0 | +1 | +1 | +1 | 0 | −1 |

Trains and sailboats use 4 directions: 0 = col − 1, 1 = row + 1, 2 = col + 1, 3 = row − 1 (tables `0x4e82e0` / `0x4e82f0` and `0x4e8360` / `0x4e8370`).

A moving object advances its sub-tile position by its speed (per type, table `0x4e81f8`: airplane and flying rescuer 16, helicopter, monster and tornado 8, ship 4) times the direction's step. Past 16 or below 0 it moves to the next tile (`0x4516f0`).

## Record 0

Record 0 is not a vehicle. The bulldozer and level-terrain tools use it while the player drags the mouse: it becomes type 4, follows the cursor tile, and XTXT `0xC9` marks the tile (`0x442db0`, `0x442a30`, **Confirmed (code)**). When the drag ends the marker is cleared, but the record keeps its contents.

- The record search for new objects (`0x450850`) starts at record 1, and so do the animation loop and the rotation code. Record 0 is never animated or turned.
- **Verified (data)**: record 0 is type 4 in 107/114 files, 0 in 6. One file (`UTOPIA.SC2`) has a train car in record 0: the train code takes a car record without checking that one was free, and `0x450850` returns 0 when all are taken (**Inferred** cause).

A reader should **ignore record 0**.

## The XTXT marker

Every object except record 0 marks its tile in XTXT with `0xC9` + its record number, so the city view finds and draws it there (`0x44d9a0` → `0x450bb0`).

- A marker is only written on a tile whose XTXT value is below `0xC9`. Before writing it, the game saves the old value (a sign or microsim number, usually 0) in **byte 10**, and puts it back when the object moves on (`0x4516f0`). An object that would step onto another object's tile skips ahead to the next tile in the same direction (**Confirmed (code)**).
- Removing an object (`0x4507d0`) writes 0 on its tile instead of byte 10. Deleting a sign under an object clears byte 10, so the sign does not come back (`0x44ced0`) (**Confirmed (code)**).
- When the city view draws a marker whose record is no longer on that tile, it clears the marker. Train engines and cars (types 10, 11) are exempt (`0x450bb0`, **Confirmed (code)**).

**Verified (data)**, over the 1 119 used records 1–39 in the corpus:

| Records | Marker at (byte 3, byte 4) | Why not |
|---|---|---|
| airplanes, helicopters, sailboats, subway cars (13), flying rescuers | all 433 | — |
| ships | 59/68 | the other 9 are on the map edge, arriving or leaving |
| train engines and cars above ground (10, 11), subway engines (12) | 554/588 | two cars stacked on one tile (a new train starts with its cars on the engine's tile); engine and first car with swapped markers (saved halfway through a move); engines on a connection tile (`0xFA`), which keeps its value |
| fire units | 0/30 | units left over after a disaster: the cleanup removes units through their markers, so a unit whose marker was lost (for example under a fire) stays (**Inferred**) |

## Allocation

A new object takes the first free record from 1 to 39 (`0x450850`). If all are taken it is not created; in disaster mode (MISC word 1 = 2) a dispatched unit takes record 39 instead (`0x450690`). Each type has its own spawn rules and limits ([thing types](../reference/thing-ids-xthg.md)).

A free record has type 0. **Its other bytes are not cleared** (96/114 files have stale bytes in free records), so test byte 0 only. A new game clears all 40 records (`0x44cb60`).

## How the game uses it

| What | Code |
|---|---|
| Animation loop: updates each object by type | `0x450890` |
| Movement and marker bookkeeping | `0x4516f0` |
| Creation per type | see [thing types](../reference/thing-ids-xthg.md) |
| Removal | `0x4507d0` |
| Drawing | `0x450bb0` (called by `0x44d9a0` for a tile whose XTXT value is `0xC9`–`0xF0`) |
| Disaster in progress while a monster (5), tornado (15), crashing airplane (1, state 7) or explosion (6) exists | `0x45cf10` |
| MISC word 1040: record of the current monster or tornado, −1 when none | `0x45ee10`, `0x45f090`, `0x45cf10` ([general.md](misc/general.md#disasters)) |
| A tile picture shows the sailboat sprite on a tile with a sailboat (**Inferred**: the query window) | `0x427c10` |
| Emergency units (7, 8, 14) fight fires and riots next to them | `0x45f760` |

Nothing outside these routines reads XTHG. The simulation (growth, traffic, power) never does. **Verified (data)**: MISC word 1040 is −1 in 114/114 files (no file was saved during a monster or tornado).

## Rotation

When the view turns clockwise, `0x466410` changes every used record from 1 to 39 as follows (**Confirmed (code)**; `0x467610` is the exact inverse):

| Types | Changes |
|---|---|
| all | tile (row, col) → (127 − col, row), like the maps ([conventions.md](../conventions.md#orientation)) |
| 1–9, 14–16 | direction → (direction + 2) & 7; target (byte 8, byte 9) → (127 − byte 9, byte 8) |
| 1 (airplane) | also the runway heading in bits 4–6 of byte 2 → + 2 (bit 7 is cleared) |
| 10–13 (trains) | direction → (direction + 1) & 3; next tile (byte 6, byte 7) → (127 − byte 7, byte 6); byte 8 → (byte 8 + 2) & 7 |

The sub-tile position (bytes 6, 7 for non-trains) is not turned. Record 0 is left alone. The rotation also turns position tables of the dispatched units that are not saved.

Sailboats move in 4 directions but are turned like the 8-direction types, so a turn gives them a direction of 4–7, which has no step; the boat drifts again after its next random turn. **Verified (data)**: 22 of 218 sailboats have a direction of 4–7, in 12/114 files.

**Verified (data)**: `test-rotated.sc2` (compass 3) turned three times with these rules equals `test.sc2` (compass 0) in all 40 records, except byte 8 of one train engine, which differs only in bits 3–7 (stored `0x6B`, turned `0x03`): the engine was created without setting byte 8, and a turn keeps only its low 3 bits.

## Evidence

| Claim | Code | Data (`sc2check.py --only xthg`) |
|---|---|---|
| 40 records × 12 bytes, not swapped | `0x4302e0`, `0x432350`, `0x466410` (loop to `0x28`, stride `0xC`) | 114/114 |
| Types 1–16 | `0x450890`, `0x450bb0` | 114/114 have no other type |
| Marker = `0xC9` + record at (byte 3, byte 4) | `0x450690`, `0x4516f0` | see [The XTXT marker](#the-xtxt-marker) |
| Byte 10 = XTXT value under the object | `0x450690`, `0x4516f0`, `0x44ced0` | 15 records hold sign or microsim numbers (25–115) |
| Record 0 = tool bulldozer | `0x442a30`, `0x442db0` | type 4 in 107/114 |
| Train links engine → car → car | `0x453600`, `0x455500` | 209/210 engines |
| Rotation rules | `0x466410`, `0x467610` | `test-rotated.sc2` → `test.sc2` |

## Pitfalls for readers

- **Test byte 0 only.** Free records keep old data, and record 0 holds the tool bulldozer.
- **An object hides what is under it.** On a tile with an object marker, the real XTXT value (a sign, a microsim) is in the object's byte 10. To list signs or microsims, put byte 10 back on each marked tile first.
- **Markers and records disagree in a few places** (trains, ships on the edge, stale fire units). Trust the record's (byte 3, byte 4) for the position and ignore a marker whose record is free.
- **Bytes 6–9 mean different things for trains** (next tile and sprite direction) than for everything else (sub-tile position and target).
- **Normalising to compass 0 must turn XTHG** with the rules in [Rotation](#rotation), or the objects stand on the wrong tiles.
- **Files from the other game version** (see [general.md § Words 1044–1199](misc/general.md#words-10441199)) hold more objects than the Windows 95 spawn limits allow (up to 10 trains); do not rely on the limits.

## Links

- [Thing types](../reference/thing-ids-xthg.md): every type, how it spawns, moves and ends
- [XTXT](xtxt.md#moving-objects-0xc90xf0): the markers
- [MISC word 1040](misc/general.md#disasters): the monster or tornado record
- [game-internals.md](../game-internals.md): the routines cited here
