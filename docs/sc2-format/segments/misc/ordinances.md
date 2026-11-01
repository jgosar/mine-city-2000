# MISC word 1000: ordinances

One MISC word records which of the game's 20 **ordinances** the city has enacted, one bit per ordinance. Each ordinance brings in or costs a monthly amount that the game books on the ordinances line of the [budget](budget.md), and most of them change the simulation somewhere.

## At a glance

| | |
|---|---|
| Word | 1000 (bytes `0xFA0`–`0xFA3`), part of [MISC](README.md) |
| Element | 32-bit big-endian bit set; bits 0–19 used |
| Game variable | `0x4caa40` (**Confirmed (code)**: MISC loader `0x431330`, saver `0x433a40`) |
| Unused bits | 20–31: never tested, clear in 114/114 files |

## Bits

Bit *n* is mask `1 << n`. The Ordinances window groups them in fours, in this order (**Confirmed (code)**: the window `0x4194b0` totals bit *n* into group *n* / 4, whose labels are Finance, Safety & Health, Education, Promotional and Other). The names are the window's own labels. Which name belongs to which bit within a group follows from what each bit does in the code (last column) and from its amount (next section); every bit has at least one such tie (**Confirmed (code)**).

| Bit | Mask | Group | Ordinance | Monthly amount | Other effects |
|---|---|---|---|---|---|
| 0 | `0x1` | Finance | 1% Sales Tax | + C | Commercial tax counts 1 point higher in the demand calculation (`0x468150`); the advisor suggests dropping it when commercial demand is low (`0x41a490`) |
| 1 | `0x2` | Finance | 1% Income Tax | + R | Residential tax counts 1 point higher (`0x468150`); advisor (`0x41a490`) |
| 2 | `0x4` | Finance | Legalized Gambling | + 2 × C | More crime: + 16 in XCRM (step 2, `0x46a840`; [minimaps.md](../minimaps.md#xcrm-crime)); crime advisor |
| 3 | `0x8` | Finance | Parking Fines | + R / 2 | none found |
| 4 | `0x10` | Safety & Health | Volunteer Fire Dept. | − R / 3 | Fire coverage + 2 per tile (step 2, [minimaps.md](../minimaps.md#xfir-fire-coverage)) |
| 5 | `0x20` | Safety & Health | Public Smoking Ban | − C / 6 | Adds 5 to a base value of the health calculation (`0x468d30`); health advisor |
| 6 | `0x40` | Safety & Health | Free Clinics | − R / 2 | Adds R / 400 to the hospital capacity in the health calculation (`0x468d30`); health advisor |
| 7 | `0x80` | Safety & Health | Junior Sports | − R / 4 | none found |
| 8 | `0x100` | Education | Pro-Reading Campaign | − R / 6 | Without it, everyone moving up an age group loses 1 education point (`0x468d30`, [population.md](population.md#how-the-age-groups-change)) |
| 9 | `0x200` | Education | Anti-Drug Campaign | − R / 5 | Raises the base value of the health calculation from 85 to 90 (`0x468d30`); crime advisor |
| 10 | `0x400` | Education | CPR Training | − R / 6 | Adds 5 to the same base value (`0x468d30`); health advisor |
| 11 | `0x800` | Education | Neighborhood Watch | − R / 3 | Police coverage + 2 per tile (step 2, [minimaps.md](../minimaps.md#xplc-police-coverage)); crime advisor |
| 12 | `0x1000` | Promotional | Tourist Advertising | − C | Commercial tax counts 1 point lower (`0x468150`) |
| 13 | `0x2000` | Promotional | Business Advertising | − I | Industrial tax counts 1 point lower (`0x468150`) |
| 14 | `0x4000` | Promotional | City Beautification | − R / 4 | Residential tax counts 1 point lower (`0x468150`) |
| 15 | `0x8000` | Promotional | Annual Carnival | − C / 3 | Commercial tax counts 1 point lower (`0x468150`) |
| 16 | `0x10000` | Other | Energy Conservation | − (MISC 1032 + MISC 1035) | Power plants supply 1/12 more (`0x4679b0`); utilities advisor |
| 17 | `0x20000` | Other | Nuclear Free Zone | 0 | Leaves one power plant type out of the build menu (`0x46a080`; **Inferred**: the nuclear plant) |
| 18 | `0x40000` | Other | Homeless Shelter | − R / 2 | Commercial tax counts 1 point lower (`0x468150`) |
| 19 | `0x80000` | Other | Pollution Controls | − I | Industrial tax counts 1 point higher (`0x468150`); pollution fades faster (step 2, [minimaps.md](../minimaps.md#xplt-pollution)); changes the industry calculation (`0x468d30`); pollution advisor |

R, C and I are the residential, commercial and industrial tax bases: the counts of [budget](budget.md#entries) entries 0, 1 and 2. MISC word 1032 is the microsim population and word 1035 the city population × 10 ([general.md](general.md)).

"Counts 1 point higher/lower" means the tax rate the zone demand calculation uses, not the stored rate: the ordinance changes how the zone reacts, not what it pays.

### Amounts

`0x42bca0` gives each ordinance's monthly amount from the formula in the table (**Confirmed (code)**), with C integer division. Positive amounts are income, negative ones costs. The ordinances' total is the count of [budget entry 3](budget.md#entries):

```
amount(n) = the formula of bit n in the table above
ordinances.count = sum of amount(n) over the bits n that are set in word 1000
```

The game recomputes the total at the start of each month (`0x4734a0`) and whenever the Ordinances window opens or an ordinance is switched (`0x4194b0`, `0x419680`). The window shows each ordinance as amount / 75: the dollars per year ([budget.md](budget.md#entries)).

## How the game uses it

- The player switches a bit in the Ordinances window (`0x419680` flips the bit and updates the total).
- **The game can enact ordinances by itself.** At the start of a month, if disasters are enabled (MISC word 1024 = 0), with a chance of 1 in 8 and when the funds exceed $50 000 plus a random amount up to $65 535, `0x4734a0` sets a random bit 0–19 and queues news item `0x29` with its number (**Confirmed (code)**). A bit that is already set stays set.
- The other effects are listed in the table. Bits 3 and 7 have no effect beyond their amount in the code read so far.

## Evidence

| Claim | Code | Data (`sc2check.py --only misc_`) |
|---|---|---|
| Word 1000 is the ordinance bit set | `0x431330`, `0x433a40`, `0x419680` | bits 20–31 clear in 114/114 |
| Groups of four, labels | `0x4194b0`; Ordinances dialog resource | — |
| Bit ↔ name | effects in `0x468150`, `0x46a840`, `0x468d30`, `0x4679b0`, `0x46a080`, advisor `0x41a490` | — |
| Amount formulas | `0x42bca0` | budget entry 3 equals the sum in 91/91 files saved before day 21 |
| Usage | — | at least one ordinance in 74/96 cities and 14/18 scenarios |

## Pitfalls for readers

- **Bit 0 is the first Finance ordinance**: OpenCity2k's "rightmost bits" are the low bits.
- **The amounts depend on the tax bases**, which change during the month. To reproduce the stored total, use the tax bases from the same file and expect a mismatch if it was saved on day 21 or later ([budget.md § Every month](budget.md#every-month)).
- **An enacted ordinance may not be the player's choice**: the game enacts some by itself.

## Links

- [budget.md](budget.md) (entry 3 holds the ordinances' total)
- [minimaps.md](../minimaps.md) (crime, police, fire and pollution effects)
- [MISC overview](README.md)
