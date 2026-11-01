# TEXT: scenario texts

The two texts of a scenario: a short description shown in the scenario list, and a longer briefing shown when the scenario starts. Only scenario files (`.scn`) have them. Each text is a separate TEXT segment, told apart by its id.

## At a glance

| | |
|---|---|
| Segments | two per scenario: id `0x80` and id `0x81` (18/18 scenarios) |
| Storage | raw, not compressed ([container.md](../container.md#segment-table)) |
| Body | 4-byte id (**little-endian**), then the text, with no terminator |
| Length the game reads | at most 768 bytes after the id (`0x42ae10`); 92–566 in the corpus |
| Character set | plain ASCII in the corpus; the game converts bytes from `0x80` up as if they were Mac Roman ([below](#character-set)) |
| Line breaks | `\r`, turned into spaces by the game ([below](#line-breaks)) |
| Found by | tag and id, wherever the segment is ([container.md § Scenario segments](../container.md#scenario-segments)) |

## Layout

| Bytes | Field | Confidence |
|---|---|---|
| 0–3 | Id, little-endian: `0x80` = the description in the scenario list, `0x81` = the briefing | **Confirmed (code)**: `0x426180` asks for `TEXT` `0x80`, `0x42f500` and `0x42f820` for `TEXT` `0x81`. **Verified (data)** 18/18 |
| 4 … | The text. Its length is the segment length − 4; there is no length byte and no terminator | **Confirmed (code)** `0x42ae10`. **Verified (data)**: no zero byte in any of the 36 texts |

The game reads at most 768 bytes of text and puts a zero byte after them (`0x42ae10`). A longer text is cut at 768 bytes; a zero byte inside the text ends it early (**Confirmed (code)**). The order of the two segments does not matter: in `FLINT.SCN` the `0x81` text comes first.

Example, `ATLANTA.SCN`, id `0x80` (126 bytes after the id):

```
Atlanta - UFO Invasion!!  We don't know if it thinks it's \r
being friendly or what, but it sure is making a mess of \r
downtown!\r
```

## The two texts

| Id | Shown | Function | Corpus |
|---|---|---|---|
| `0x80` | In the scenario list, to the right of the [picture](pict.md), one entry per `.scn` file | `0x425e40` (draws a list entry) via `0x426180` | 92–201 bytes |
| `0x81` | In a message window when the scenario starts, after the city is loaded | `0x42f500`, `0x42f820` (the two scenario start paths), shown by `0x42dc20` | 179–566 bytes |

If the `0x81` text cannot be read, the scenario does not start: the game shows message `0xEF` instead (`0x42f500`, `0x42f820`; **Confirmed (code)**). The list does not check whether the `0x80` text was found.

17 of the 18 briefings begin with the words `Extended Description: ` (the exception is `OAKLAND.SCN`). The game does not remove them, so the player sees them (**Confirmed (code)**: nothing between the read and the message window changes the start of the text).

## Line breaks

Both texts are stored with hard line breaks: a `\r` (`0x0D`) after roughly 55–60 characters, as typed into an editor. Three briefings and no description contain a tab (`0x09`). There is no `\n` anywhere (**Verified (data)** 36/36 texts).

The game replaces every `\r` and every tab with a space before it shows the text, and lets the window wrap the text (**Confirmed (code)**: `0x425e40`, `0x42f500`, `0x42f820`). The stored line breaks therefore never appear, and a text ending in `\r` simply ends in a space. To show a text the way the game does, replace `\r` and `\t` with spaces.

## Character set

All 36 texts in the corpus are plain ASCII (bytes `0x20`–`0x7E`, `\r` and tab) (**Verified (data)**), so for them the character set does not matter.

For other bytes the game does more than for the other strings in the file ([conventions.md § Strings](../conventions.md#strings)). Before showing either text, `0x42a080` converts it from the OEM code page to Windows ANSI (`OemToCharA`), then replaces 31 of the resulting characters with others, and removes a `-` that stands directly before `!`, `:`, `?`, `;`, `,` or `.` (**Confirmed (code)**). With the US OEM code page 437, the effect is that the texts are read as **Mac Roman** text, the character set of the Macintosh version (**Inferred** from the table below; the code does not name it):

| Byte | 80 | 81 | 82 | 83 | 84 | 85 | 86 | 87 | 88 | 89 | 8A | 8B | 8C | 8D | 8E | 8F |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Shown | Ä | Å | Ç | É | Ñ | Ö | Ü | á | à | â | ä | ã | å | ç | é | è |

| Byte | 90 | 91 | 92 | 93 | 94 | 95 | 96 | 97 | 98 | 99 | 9A | 9C | 9F | A7 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Shown | ê | ë | í | ì | î | ï | ñ | ó | ò | ô | ö | ú | ü | § |

Every entry except `0xA7` is the Mac Roman character (Mac Roman `0xA7` is `ß`). Other bytes from `0x80` up come out as `OemToCharA` maps them, which for most is not the Mac Roman character. The `-` rule fits Mac Roman too: its non-breaking space `0xCA`, which French text puts before `!`, `?`, `:` and `;`, is a box-drawing character in code page 437 that Windows turns into `-` (**Inferred**). One more replacement (Windows `0xA6` → `0xC3`) cannot be tied to a source byte without knowing how `OemToCharA` maps the box-drawing characters (**Unknown**).

**For a reader:** decode the text as Mac Roman. That is exact for the corpus, and it is what the game intends for accented letters. `sc2dump.py` does this.

## How the game uses it

| Use | Function | What it does |
|---|---|---|
| Find | `0x42ae10` | Walks the file for a `TEXT` segment with the wanted id; reads at most 768 bytes and adds a zero byte |
| Scenario list | `0x425e40` → `0x426180` | For each `.scn` file in the `[PATHS] SCENARIOS` folder: reads `PICT` `0x80` and `TEXT` `0x80`, converts the text (`0x42a080`), turns `\r` and tabs into spaces and draws it beside the picture |
| Scenario start | `0x42f500`, `0x42f820` | Loads the city and its `SCEN` goals, reads `TEXT` `0x81`, turns `\r` and tabs into spaces, converts the text (`0x42a080`) and shows it in a message window (`0x42dc20`) |

The texts stay in the scenario file: the save routine `0x432350` writes no TEXT segment, so a scenario saved as a city has none (**Confirmed (code)**: no save code refers to the tag).

## Evidence

| Claim | Code | Data (`sc2check.py --only text_`) |
|---|---|---|
| Ids `0x80` and `0x81`, one segment each | `0x426180`, `0x42f500`, `0x42f820` | 18/18 scenarios |
| At most 768 bytes read | `0x42ae10` | all 36 texts are shorter (92–566 bytes) |
| `\r` and tab shown as spaces | `0x425e40`, `0x42f500`, `0x42f820` | `\r` in 14/18 descriptions and 15/18 briefings; no `\n` or zero byte in any |
| OEM → ANSI conversion and fix-ups | `0x42a080` | no byte from `0x80` up in any text (18/18) |
| `Extended Description: ` prefix | — | 17/18 briefings |

## Pitfalls for readers

- **Find the texts by id, not by order.** `FLINT.SCN` stores `0x81` before `0x80`.
- **The id is little-endian**, unlike every other number in the file ([conventions.md § Byte order](../conventions.md#byte-order)).
- **Do not keep the `\r` as line breaks** if you want the game's look: it reflows the text.
- **Do not decode as Windows ANSI or UTF-8.** Accented letters, if a text has any, follow Mac Roman.
- **Cut at 768 bytes** and at the first zero byte, as the game does.

## Links

- [pict.md](pict.md): the picture shown next to the `0x80` text
- [container.md § Scenario segments](../container.md#scenario-segments): how the game finds scenario segments
- [conventions.md § Strings](../conventions.md#strings): the character set of the other strings
