# MISC: technology years (words 462–478)

When each of 17 inventions becomes available: newer power plants, the airport, highways, buses, subways, water treatment, desalination and the arcologies. Each word holds the year the invention will appear, or 0 once it is available.

## At a glance

| | |
|---|---|
| Words | 462–478 (bytes `0x738`–`0x77B`) |
| Entries | 17 × int16 |
| Loaded into | `0x4cb430` |
| 0 means | available now |
| Non-zero means | the year it becomes available |

## Layout

| Index | Word | Byte | Invention | Base year | Unlocks (tool menu) |
|---|---|---|---|---|---|
| 0 | 462 | `0x738` | Gas power | 1940 | Power: gas power plant |
| 1 | 463 | `0x73C` | Nuclear power | 1950 | Power: nuclear power plant (not while the Nuclear Free Zone ordinance is on) |
| 2 | 464 | `0x740` | Solar power | 1980 | Power: solar power plant |
| 3 | 465 | `0x744` | Wind power | 1970 | Power: wind power |
| 4 | 466 | `0x748` | Microwave power | 2020 | Power: microwave power plant |
| 5 | 467 | `0x74C` | Fusion power | 2050 | Power: fusion power plant |
| 6 | 468 | `0x750` | Airport | 1920 | Ports: airport |
| 7 | 469 | `0x754` | Highways | 1920 | Roads: highway and on-ramp |
| 8 | 470 | `0x758` | Buses | 1910 | Roads: bus depot |
| 9 | 471 | `0x75C` | Subways | 1900 | Rails: subway, subway station, subway-to-rail |
| 10 | 472 | `0x760` | Water treatment | 1925 | Water: treatment plant |
| 11 | 473 | `0x764` | Desalination | 1980 | Water: desalination plant |
| 12 | 474 | `0x768` | Plymouth arcology | 1990 | Rewards: arcologies (see below) |
| 13 | 475 | `0x76C` | Forest arcology | 2040 | Rewards: arcologies |
| 14 | 476 | `0x770` | Darco arcology | 2090 | Rewards: arcologies |
| 15 | 477 | `0x774` | Launch arcology | 2140 | Rewards: arcologies |
| 16 | 478 | `0x778` | — | 2190 | nothing (**Unused**) |

**Confirmed (code)**: `0x46a080` rebuilds the tool menus from this table. For each entry that is 0 it sets one or more menu bits; the base years are the table at `0x4e99e8`. The invention names follow from which menu items those bits enable: the bits run in the order of the submenu items (strings 96–99 and 129–133 for power, 134–138 for water, 148–152 for roads, 153–157 for rails, 158–159 for ports), which is **Inferred** to be the bit order. The arcology entries do not enable a fixed item: `0x46a080` counts how many of entries 12–15 are 0, and that many arcology types are offered. Entry 16 sets nothing.

Coal, hydro and oil power, roads, rails, the seaport, pipes, pumps and water towers are always available; they have no entry.

## Values

- **0**: the invention is available.
- **A year**: the invention appears in that year or soon after. It is the base year plus a random 0–19, fixed when the city is created.

The current year is the founding year (MISC word 3) plus the day counter (MISC word 4) divided by 300 ([general.md](general.md)).

## How the game uses it

- **New city** (`0x42e460`): every entry gets its base year plus a random 0–19. Entries earlier than the founding year are set to 0, so a city founded in 2050 starts with most inventions.
- **SimCity Classic import** (`0x484cd0`): the same, compared with the current year.
- **Every month** (`0x468150`, step 21 of the simulation cycle): with a chance of 1 in 8, the first entry whose year has come is announced in the newspaper (story type 4 with the index for entries 0–6, type 5 with index − 7 for entries 7–16; [newspapers.md](newspapers.md#story-types)), set to 0, and the menus are rebuilt. So an invention can arrive a few months after its year, and only one per month.
- **Cheat** (`0x412290`, called from the keyboard cheat handler `0x40ca30`, **Inferred**): sets all 17 entries to 0, adds 500 000 to the funds and sets MISC word 8 (newspapers) to 6.
- **After loading**, the menus are rebuilt from the table, so a reader can tell what the player could build.

## Evidence

| Claim | Code | Corpus (114 files) |
|---|---|---|
| Non-zero values are base year + 0…19 | `0x42e460`, `0x4e99e8` | all 315 non-zero entries |
| Non-zero years lie in the future | `0x468150` | 313 of 315; the other 2 have just come due and wait for the 1-in-8 chance |
| Entry 16 unlocks nothing | `0x46a080` | non-zero in 45/114 files (year 2190–2209 not yet reached) |

## Pitfalls for readers

- **0 means "available", not "unknown".**
- **A year that has passed is still "not yet available"** until the monthly 1-in-8 check fires.
- **Entry 16 does nothing.** It is a 17th table slot with a base year of 2190.
- **The arcology entries are counted, not named.** If entries 12 and 14 were 0 but 13 not, the game would still offer two arcology types (the first two). With base years 50 years apart this does not happen in a normal game.

## Links

- [MISC word index](README.md)
- [newspapers.md](newspapers.md): the announcements
- [tile-codes-xbld.md](../../reference/tile-codes-xbld.md): the buildings themselves
