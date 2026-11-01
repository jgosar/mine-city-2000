# MISC: residential, commercial and industrial demand (words 454–461)

The three values behind the game's **RCI demand indicator**: how much the city wants new residential, commercial and industrial zones. Five more words follow that the game loads but never uses.

## At a glance

| | |
|---|---|
| Words | 454–461 (bytes `0x718`–`0x737`) |
| Entries | 8 × int16, loaded into `0x4cc8f8` |
| Used | entries 0–2 |
| Range | −2000 to +2000 |

## Layout

| Word | Byte | Type | Name | Meaning | Confidence |
|---|---|---|---|---|---|
| 454 | `0x718` | int16 | residential demand | −2000 … +2000; positive means residential zones will grow | **Confirmed (code)** |
| 455 | `0x71C` | int16 | commercial demand | the same for commercial zones | **Confirmed (code)** |
| 456 | `0x720` | int16 | industrial demand | the same for industrial zones | **Confirmed (code)** |
| 457–461 | `0x724`–`0x737` | int16 | — | loaded and saved, never read | **Unused** |

## How the game uses it

- **Updated every month** by `0x468150` (step 21 of the simulation cycle). Each value moves by an amount taken from a table (`0x4e9328`) indexed by the zone's tax rate (from the budget table, [budget.md](budget.md)): +200 at 0 % tax, 0 at 7 %, −600 at 20 %. Some ordinances shift the index by one first (for example the bit `0x2` and `0x4000` ordinances for residential). The result is clamped to −2000 … +2000. The surrounding floating-point code is only partly readable, so other inputs are possible (**Inferred**).
- **Zone growth** (`0x4358b0`): the chance that a zone tile develops depends on demand + 2000 for its zone type (light and dense residential use entry 0, commercial entry 1, industrial entry 2).
- **The demand indicator** (`0x422f10`) draws each bar as value × half the bar height ÷ 2000.
- **Advisors** (`0x41a490`) comment when residential or commercial demand is below −666 while certain ordinances are on.
- **New city** (`0x40e250`): entries 0–2 start at 0. The SimCity Classic import (`0x4829f0`) sets entries 0–2 from the imported city.

Nothing refers to entries 3–7 (**Confirmed (code)**: no code addresses `0x4cc8fe` or beyond, apart from the load and save loops).

## Evidence

| Claim | Code | Corpus (114 files) |
|---|---|---|
| Range −2000 … +2000 | `0x468150` | 114/114 |
| Entries 3–7 unused | `0x431330`, `0x433a40` | 0 in 114/114 |
| Negative values stored sign-extended | `0x433a40` | negative demand in 28 files, always as a negative 32-bit word |

## Pitfalls for readers

- **Demand is signed.** Read the words as signed 32-bit (or take the low 16 bits as signed).
- **The demand indicator scale is ±2000**, not percent.

## Links

- [MISC word index](README.md)
- [budget.md](budget.md): the tax rates
