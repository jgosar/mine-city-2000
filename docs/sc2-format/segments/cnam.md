# CNAM: city name

A 32-byte, uncompressed segment holding the city's name as a zero-terminated string from byte 1. Byte 0 is not used by the game. CNAM is optional: without it the game names the city after the file.

## At a glance

| | |
|---|---|
| Size | 32 bytes in every file that has it (107/107) |
| Storage | raw, not compressed ([container.md](../container.md#segment-table)) |
| Element | byte 0, then a zero-terminated string |
| Game buffer | `0x4ca1a0` (name), `0x4ca1a4` (its length) |
| Character set | Windows ANSI ([conventions.md](../conventions.md#strings)) |
| Position | first (68/114) or last (39/114) segment, or absent (7/114) ([container.md](../container.md#segment-order)) |

## Layout

| Byte | Field | Confidence |
|---|---|---|
| 0 | `0x1F` (the number of bytes after it) **or** the length of the name. The game ignores it | **Confirmed (code)**: the Windows 95 writer `0x432990` writes the constant `0x1F`; the loader `0x4302e0` starts at byte 1. **Verified (data)**: both forms occur ([below](#which-files-have-which-form)) |
| 1 … *n* | The name, *n* characters | **Confirmed (code)** `0x4302e0`, `0x432990` |
| *n* + 1 | `0x00`, the end of the name | **Confirmed (code)** |
| *n* + 2 … 31 | Zeros, or leftover bytes from the game's memory | **Confirmed (code)**: the writer copies 31 bytes from the name's buffer, whatever follows the name. **Verified (data)** |

**The name is the C string starting at byte 1.** Read up to the first zero byte or the end of the segment, whichever comes first.

### Which files have which form

| CNAM position | Byte 0 | Bytes after the name | Files |
|---|---|---|---|
| first | `0x1F` | leftover bytes | 50 |
| first | name length | leftover bytes | 18 |
| last | name length | leftover bytes | 27 |
| last | name length | zeros | 12 |
| absent | — | — | 7 (all `.sc2`) |

(**Verified (data)**, 114 files.) The `0x1F` form is what the Windows 95 version writes: its save routine `0x432350` writes CNAM first, through `0x432990` (**Confirmed (code)**). The other forms come from another version or edition of the game: the same files, and only they, have data in MISC words 1044–1070, which the Windows 95 saver writes as zeros (**Verified (data)**, [general.md § Words 1044–1199](misc/general.md#words-10441199)). Which version that is, is **Unknown**. Name lengths in the corpus are 3 to 25 characters.

Example, `0x1F` form (city "Flarange"): `1F` `46 6C 61 72 61 6E 67 65` `00`, then 22 bytes of whatever was in memory.

## How the game uses it

| Use | Function | What it does |
|---|---|---|
| Load | `0x4302e0` | Reads `length` bytes into a buffer one byte longer; length 0 gives an empty name. The name is the C string at byte 1 |
| No name | `0x431ee0` | If CNAM is missing or the name is empty, the city is named after the file: the file name without its directory, upper-cased (`CharUpperA`) ([container.md](../container.md#after-the-walk)). Whether the extension is part of it depends on what the caller passes (**Unknown**, [open-questions.md](../open-questions.md#text-and-labels)) |
| Save | `0x432350` → `0x432990` | Writes the tag, length 32, byte `0x1F`, then the first 31 bytes starting at the name (`Left(name, 31)` via `0x49e9d8`, which does not clear what follows the name) |

The mayor's name is not here: it is XLAB slot 0 ([xlab.md](xlab.md#slot-0-the-mayor)).

## Character set

Windows ANSI, as for XLAB. **Verified (data)**: one file in the corpus (`žabnca.sc2`) has a non-ASCII city name, "Žabnca", with `Ž` stored as `0x8E`: the code for `Ž` in Windows code pages 1250 and 1252.

## Evidence

| Claim | Code | Data (`sc2check.py --only cnam`) |
|---|---|---|
| Name = C string from byte 1 | `0x4302e0` | 107/107 files give a readable name |
| Byte 0 = `0x1F` from the Windows 95 writer | `0x432990` | 50/107, all with CNAM first |
| Byte 0 = name length in other files | — | 57/107 |
| Leftover bytes after the name | `0x432990` | 95/107; zeros in 12/107 |
| Missing CNAM → file name | `0x431ee0` | 7/114 files have no CNAM |
| Always 32 bytes | `0x432990` | 107/107 |

## Pitfalls for readers

- **Do not use byte 0 as the length.** In 50 files it is `0x1F`, whatever the name's length.
- **Do not read the whole 31 bytes as the name.** The bytes after the terminator are often fragments of other strings from the game's memory.
- **CNAM may be missing.** Fall back to the file name without its directory, upper-cased, as the game does.
- **Do not decode as UTF-8.** A byte ≥ `0x80` is a Windows ANSI character.
- **A 31-character name has no terminator** in the segment. The loader's extra buffer byte probably ends it (**Inferred**); a reader should simply stop at byte 31.
