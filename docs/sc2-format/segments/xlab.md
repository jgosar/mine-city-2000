# XLAB: label texts

256 short strings: the mayor's name, the player's signs, the names of microsims (buildings and city systems with their own statistics) and the names of the sports teams. Most of them are reached through an index in [XTXT](xtxt.md); a few slots have a fixed meaning.

## At a glance

| | |
|---|---|
| Size | 6400 bytes: 256 records of 25 bytes |
| Storage | RLE-compressed ([compression.md](../compression.md)) |
| Element | 25-byte record: length byte, then up to 24 characters |
| Game buffer | `0x4ca198` |
| Character set | Windows ANSI ([conventions.md](../conventions.md#strings)) |
| Changes with the compass? | No |

## Layout

Record *s* (slot *s*, 0–255) is bytes `s * 25` to `s * 25 + 24`.

| Byte | Field | Confidence |
|---|---|---|
| 0 | Length *n* of the text. The game uses at most 23 | **Confirmed (code)** `0x430c00`; **Verified (data)**: 0–24 occur |
| 1 … *n* | The characters, no terminator | **Confirmed (code)** `0x430c00`, `0x430c90` |
| *n* + 1 … 24 | Not part of the text: zeros or leftovers of an older, longer text | **Confirmed (code)**: the save conversion writes only the length and the text; **Verified (data)** |

A record is **empty** when its length byte is 0.

In memory the game keeps each label as a zero-terminated string. After loading, `0x430c00` turns every record into one, capping the length at 23. Before saving, `0x430c90` turns it back into length + characters (**Confirmed (code)**). Neither writes byte 24, and neither clears the bytes after the text. So these bytes hold whatever was there before: in the corpus, 4 170 non-empty records have non-zero bytes after their text, and byte 24 is 0 in 15 623 records, `0xCD` in 1 232, 1 in 611, `0xFF` in 605 and `0x0A` in 402 (**Verified (data)**).

If XLAB is missing, the loader clears all 256 labels (**Confirmed (code)** `0x430d50`; [container.md](../container.md#missing-repeated-and-unknown-segments)).

## Label slots

| Slot | Contents | Reached through | Files with the slot non-empty | Confidence |
|---|---|---|---|---|
| 0 | **mayor's name** | fixed | 108/114 | **Confirmed (code)** `0x44cb60`; **Verified (data)** |
| 1–50 | **signs** | XTXT value = slot | per sign | **Confirmed (code)** `0x44d7c0`; **Verified (data)** |
| 51 | never used | (XTXT `0x33`, unused) | 0/114 | **Unused** |
| 52–60 | names of the nine **built-in microsims** (city systems) | XTXT `0x34`–`0x3C` | 114/114 | **Confirmed (code)** `0x44cf60`; **Verified (data)** |
| 61–200 | names of **microsims** (single buildings) | XTXT `0x3D`–`0xC8` | fewer for higher slots | **Confirmed (code)** `0x44cf60`; **Verified (data)** |
| 201–249 | never used | — | 0/114 | **Unused**: no XTXT value reaches them |
| 250 (`0xFA`) | **scratch buffer** for the neighbour connection sign | built by the renderer | 81/114 | **Confirmed (code)** `0x44d9a0` |
| 251–255 (`0xFB`–`0xFF`) | names of the five **sports teams** | fixed | 106–110/114 | **Verified (data)**; use **Inferred** |

### Slot 0: the mayor

When a new city is started, `0x44cb60` fills slot 0 from the registry value `REGISTRATION\MAYOR_NAME`, with the default "Mayor McSim" (**Confirmed (code)**). Examples from the corpus: "Jason Shankel", "Daniel Scroggins", "Blackbeard". This is **not the city name**: that is in [CNAM](cnam.md), and the two differ in every one of the 107 files that have both (**Verified (data)**).

### Slots 1–50: signs

The sign tool stores the text in the slot whose number is the XTXT value it placed ([xtxt.md](xtxt.md#signs-0x010x32)). A slot can hold text with no sign on the map: **53 sign labels in the corpus have no matching XTXT value**. A disaster marker can overwrite a sign's tile and leave its label behind ([xtxt.md](xtxt.md#disaster-markers-0xfb0xff)) (**Inferred** as the main cause).

### Slots 52–60: city systems

| Slot | XTXT value | Default name | Files with the default |
|---|---|---|---|
| 52 | `0x34` | SimBus System | 98 |
| 53 | `0x35` | SimRail System | 95 |
| 54 | `0x36` | SimSubway | 94 |
| 55 | `0x37` | Wind Power | 110 |
| 56 | `0x38` | Hydro Power | 106 |
| 57 | `0x39` | SimPark System | 97 |
| 58 | `0x3A` | Museum | 92 |
| 59 | `0x3B` | Library System | 93 |
| 60 | `0x3C` | Marina | 95 |

These slots are filled in every file, even when the city has none of the buildings. The player can rename them; the other files hold custom names.

### Slots 61–200: microsims

When a microsim building is placed, its default name ("Police Station", "Fire Station", "School", "Launch Arco", …) is written into slot XMIC record + `0x33`, which is also the XTXT value of its tiles (**Confirmed (code)** `0x44cf60`). The player can rename it.

The three places that describe a microsim agree in almost every case (**Verified (data)**, all 140 slots × 114 files):

| Label non-empty | XMIC record in use | Value on the map | Slot instances |
|---|---|---|---|
| yes | yes | yes | 6 669 |
| yes | yes | no | 24 |
| yes | no | no | 5 |
| no | yes | yes | 1 |
| no | no | no | 9 261 |

The 5 labels without a record include slots 192–195 of `BAYVIEW.SC2`, which hold non-text bytes: garbage, not names.

### Slot 250: connection sign scratch

The renderer builds the sign of a neighbour connection here every time it draws one: the neighbour's name, a space and a digit, such as "Cats Corner 5" or "Oak Creek 6" ([xtxt.md](xtxt.md#the-connection-sign)). The saved slot holds whichever sign was drawn last. **It is not a label of the city.**

### Slots 251–255: sports teams

| Slot | Sport | Default (string) | Files non-empty |
|---|---|---|---|
| 251 | football | Llamas (401) | 110 |
| 252 | baseball | Alpacas (402) | 109 |
| 253 | soccer | Camels (403) | 106 |
| 254 | cricket | Dromedaries (404) | 106 |
| 255 | rugby | Army Ants (405) | 108 |

The sports are as named by djm and OpenCity2k (**Inferred**: not traced in the code).

The defaults match strings 401–405 of `SIMCITY.EXE`. Some cities use other names (for example "Giants" or "Mets"). Which routine fills these slots and where the names are shown is not traced (**Inferred**: the newspaper's sports stories).

## How the game uses it

| Use | Function | What it does with XLAB |
|---|---|---|
| Load | `0x430c00` | Length-prefixed → zero-terminated, length capped at 23 |
| Load without XLAB | `0x430d50` | Clears all 256 labels |
| Save | `0x430c90` | Zero-terminated → length-prefixed; writes bytes 0 … *n* only |
| New city | `0x44cb60` | Sets slot 0 to the mayor's name, clears slots 1–255 |
| Sign tool | `0x44d7c0` | Writes slots 1–50 |
| Placing a microsim | `0x44cf60` | Writes a default or remembered name into slot record + `0x33` |
| Drawing | `0x44d9a0` | Draws slots 1–50 for signs (when labels are on, `0x4e9e54`); builds slot 250 for connection signs |
| Message text | `0x47a240` | Expands message templates; converts to the OEM character set only for its own processing (`CharToOemBuffA` / `OemToCharA`) |

## Character set

The text is in the **Windows ANSI** code page of the machine that saved it (code page 1252 on Western systems).

- **Verified (data)**: the labels of `FLARANGE.SC2` decode correctly as code page 1252 ("Rédalenc Castle", "Côm L'átou Field") and wrongly as the DOS code page 437. Seven files have labels with bytes ≥ `0x80`.
- **Confirmed (code)**: the game stores the text as typed through the ANSI Windows API and converts to OEM only temporarily inside `0x47a240`.

## Evidence

| Claim | Code | Data (`sc2check.py --only xlab`) |
|---|---|---|
| 25-byte records, length byte first | `0x430c00`, `0x430c90` | 6400 bytes in 114/114 |
| Length capped at 23 | `0x430c00` | length 24 in 12 records, 7/114 files |
| Slot 0 = mayor | `0x44cb60` | 108/114 non-empty; differs from CNAM in 107/107 |
| Slots 1–50 = signs | `0x44d7c0` | every sign on the map has its label, 66/66 files |
| Slot 51 and 201–249 unused | — | empty in 114/114 |
| Slots 52–60 = city systems | `0x44cf60`, table `0x4e7c9a` | all nine non-empty in 114/114 |
| Slot 250 = connection sign scratch | `0x44d9a0` | non-empty in 81/114 |
| Slots 251–255 = teams | strings 401–405 | all five non-empty in 106/114 |

## Pitfalls for readers

- **Read at most 23 characters.** A length byte of 24 occurs (12 records); the game shows only 23.
- **Stop at the length, not at a zero byte.** The bytes after the text are often leftovers of a longer, older name.
- **Not every non-empty slot is in use.** Sign labels can be orphaned (53), slot 250 is scratch, and a few unused slots hold garbage. To list the labels a player sees, start from the XTXT values on the map.
- **Slot 0 is the mayor, not the city.** The city name is in [CNAM](cnam.md).
- **Decode as Windows ANSI**, not UTF-8 and not the DOS code page.
