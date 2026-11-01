# XMIC microsim kinds

What the four figures of an [XMIC](../segments/xmic.md) record mean for each building code in byte 0, what the game sets them to when the building is placed, and how the yearly update recomputes them. Byte 1 is **#0**, the big-endian words at offsets 2, 4 and 6 are **#1**, **#2** and **#3**, as in the window templates.

Sources, all **Confirmed (code)** unless a row says otherwise:

- **Kind**: table `0x4e7c9a`, indexed by building code.
- **Window lines**: table `0x4e7da0` (five string ids − 900 per kind), drawn by `0x44d360`. The text is the US version's; the "aus", "fre" and "ger" builds replace some lines at start-up (`0x4501af`, not traced line by line).
- **Default name**: string 831 + (code − `0xC6`) of `SIMCITY.EXE`, copied into the XLAB slot by `0x44cf60`.
- **Placed**: `0x44ddc0`.
- **Yearly**: `0x44e390`, once a year in January ([xmic.md](../segments/xmic.md#when-the-figures-change)).

Notation in the formulas:

- *pop* = MISC word 1035 (people outside microsim buildings).
- **cap(*x*, *d*)** = the smaller of *x* and (*pop* + MISC word 1032 + arcology bonus) ÷ *d* (`0x44f7d0`); the arcology bonus is (*n* − 140) × 20 000 when *n* = arcology tiles ÷ 16 is above 140.
- **tiles(*c*)** = the number of tiles with XBLD code *c* (`0x4ca4c8`, the same counts as MISC words 124–379).
- **rand(*a*–*b*)** = a random number in that range. Several random sources are used (`0x48cdd8`, `0x47fc70`, `0x47faf0`, `0x47faa0`); a reader cannot predict them.
- **funding** = the budget's funding level for the department, in percent ([budget.md](../segments/misc/budget.md#entries)).
- **year** = the current year (`0x4ca5f4` + `0x4cb020`).

## Summary

| Code | Building | Kind | Record | Default name (string) | #0 byte 1 | #1 word 2 | #2 word 4 | #3 word 6 |
|---|---|---|---|---|---|---|---|---|
| `0xC9`–`0xCF` | [power plants](#power-plants-0xc90xcf) | 1 | own | Gas Power … Coal Power (834–840) | age, years | max output, MW | % of capacity used | — |
| `0xD0` | [City Hall](#city-hall-0xd0) | 2 | own | City Hall (841) | — | employees | year built | — |
| `0xD1` | [hospital](#hospital-0xd1) | 3 | own | Hospital (842) | grade | patients | doctors | "annual cost" |
| `0xD2` | [police station](#police-station-0xd2) | 4 | own | Police Station (843) | "annual cost" | officers | crimes reported | arrests |
| `0xD3` | [fire station](#fire-station-0xd3) | 5 | own | Fire Station (844) | "annual cost" | firefighters | fire engines | response time, minutes |
| `0xD6` | [school](#school-0xd6) | 6 | own | School (847) | grade | students | teachers | "annual cost" |
| `0xD7` | [stadium](#stadium-0xd7) | 7 | own | Stadium (848) | wins | attendance | sport | team name's XLAB slot |
| `0xD8` | [prison](#prison-0xd8) | 8 | own | Prison (849) | escapes | inmates | guards | % of capacity |
| `0xD9` | [college](#college-0xd9) | 9 | own | College (850) | grade | students ("Attendance") | teachers | "annual cost" |
| `0xDA` | [zoo](#zoo-0xda) | 10 | own | Zoo (851) | other dromedaries | Peruvian llamas | Andean llamas | alpacas |
| `0xDB` | [statue](#statue-0xdb) | 11 | own | Statue (852) | — | year built | pigeons perched | — |
| `0xF3` | [mayor's house](#mayors-house-0xf3) | 12 | own | Mayor's House (876) | doorstops | year built | approval rating, % | employees |
| `0xF4` | [water treatment](#water-treatment-0xf4-and-desalination-0xfa) | 13 | own | Water Treatment (877) | % of capacity | untreated filth, 1 000 ft³ | treated filth, 1 000 ft³ | — |
| `0xFA` | [desalination](#water-treatment-0xf4-and-desalination-0xfa) | 14 | own | Desalinization (883) | % of capacity | salt removed, tons | employees | — |
| `0xFB`–`0xFE` | [arcologies](#arcologies-0xfb0xfe) | 15 | own | Plymouth Arco, Forest Arco, Darco, Launch Arco (884–887) | conditions grade | design capacity, thousands | residents | year built |
| `0xFF` | [Llama Dome](#llama-dome-0xff) | 16 | own | Llama Dome (888) | weddings, bungee jumps | visitors | llama sightings | complaints |
| `0xEC` | [bus system](#bus-rail-and-subway-0xec-0xed-0xe9) | 17 | 1 | SimBus System (869) | — | bus stations | buses | passengers |
| `0xED` | [rail system](#bus-rail-and-subway-0xec-0xed-0xe9) | 18 | 2 | SimRail System (870) | — | rail stations | — | passengers |
| `0xE9` | [subway system](#bus-rail-and-subway-0xec-0xed-0xe9) | 19 | 3 | SimSubway (866) | — | subway stations | — | passengers |
| `0xC8` | [wind power](#wind-and-hydro-power-0xc8-0xc6-0xc7) | 20 | 4 | Wind Power (833) | — | wind generators | max output, MW | — |
| `0xC6`, `0xC7` | [hydro power](#wind-and-hydro-power-0xc8-0xc6-0xc7) | 21 | 5 | Hydro Power (831, 832) | — | hydro generators | max output, MW | — |
| `0xD5` | [parks](#parks-0xd5) | 22 | 6 | SimPark System (846) | — | attendance last year | acres | employees |
| `0xD4` | [museums](#museums-0xd4) | 23 | 7 | Museum (845) | — | attendance last year | exhibits | — |
| `0xF5` | [libraries](#libraries-0xf5) | 24 | 8 | Library System (878) | grade | attendance | books | — |
| `0xF8` | [marinas](#marinas-0xf8) | 25 | 9 | Marina (881) | — | boats | — | — |

"—" = not shown and not written by the yearly update. Kinds 1–16 ("own") get a record of their own from 10 up; kinds 17–25 use the fixed record kind − 16. Codes below `0xC6` have no record: `0x44cf60` refuses them. (Table `0x4e7c9a` has non-zero bytes for many codes below `0x66`; they belong to the neighbouring tables `0x4e7c70` and `0x4e7cb8` and are never used as kinds.) Codes `0xDC`–`0xE8`, `0xEA`, `0xEB`, `0xEE`–`0xF2`, `0xF6`, `0xF7` and `0xF9` are kind 0: no record, even though they have default names (Pump, Runway, … MilSilo).

**Grades** (`#G`, byte 1): 0 F, 1 D−, 2 D, 3 D+, 4 C−, 5 C, 6 C+, 7 B−, 8 B, 9 B+, 10 A−, 11 A, 12 A+.

**"Annual Cost"**: the hospital, police, fire, school and college windows show "Annual Cost : $" and a figure that the yearly update sets from the department's funding level, not from money (**Inferred**: the window shows the funding level as if it were dollars).

## Power plants (`0xC9`–`0xCF`)

Window: `Max. Output : #1 MWatts` · `Running at #2% of Capacity` · `Age(Max) : #0 yrs (50)`.

| Code | Plant | #1 max output (MW), set when placed | Rebuild cost |
|---|---|---|---|
| `0xC9` | Gas | 50 | 2 000 |
| `0xCA` | Oil | 220 | 6 600 |
| `0xCB` | Nuclear | 500 | 15 000 |
| `0xCC` | Solar | 50 | 1 300 |
| `0xCD` | Microwave | 1 600 | 28 000 |
| `0xCE` | Fusion | 2 500 | 40 000 |
| `0xCF` | Coal | 200 | 4 000 |

- **Placed**: #0 (age) = 0, #1 = the table's output, #2 = 0. **Verified (data)**: #1 equals the table in 281/281 plant records.
- **Yearly**: age + 1; #2 = the city's power percentage (`0x4caa50`) + rand(0–7).
- **Wear-out**: above 48 years a newspaper story warns (story type `0x24`). Above 50 the game finds the plant on the map (`0x44f890`) and:
  - with disasters on (MISC word 1024 = 0), **destroys** it (`0x458d40`), frees the record and scrolls the view to it if the auto-goto option is on;
  - with disasters off, rebuilds it if the treasury holds the rebuild cost (table `0x4dc1d0`, indexed by `0x4e7e24`): the cost is paid and the age goes back to 0. Otherwise it is destroyed.

## City Hall (`0xD0`)

Window: `Employees : #1` · `Built in : #2`.

- **Placed**: #1 = min(200, *pop* ÷ 900); #2 = year.
- **Yearly**: #1 recomputed the same way.

## Hospital (`0xD1`)

Window: `Beds : 1000` · `Patients : #1` · `Doctors : #2` · `Grade : #G` · `Annual Cost : $#3`.

- **Placed**: #0 = 6 (C+), the rest 0.
- **Yearly**:
  - #3 = health funding ÷ 2.
  - #1 patients = cap(min(*pop* ÷ max(1, tiles(`0xD1`) ÷ 9 × 25) + rand(0–15), about 1 000), 30).
  - #2 doctors = cap(max(0, health funding + rand(0–7) + 2 × #0 − 24), 120).
  - #0 grade: *r* = patients × 10 ÷ doctors; *r* < 50 → 12; *r* < 111 → (110 − *r*) ÷ 5; else 0.

## Police station (`0xD2`)

Window: `Officers : #1` · `Crimes Reported : #2` · `Arrests : #3` · — · `Annual Cost : $#0`.

- **Placed**: #1 = cap(police funding × 2, 90), as in the yearly update; the rest 0.
- **Yearly**:
  - #0 = police funding.
  - #1 officers = cap(police funding × 2, 90).
  - #2 crimes = MISC word 11 (sum of XCRM) ÷ tiles(`0xD2`).
  - #3 arrests = crimes ÷ max(1, 5 − MISC word 1039 of last year) + rand(0–15).
- The arrests of all stations, at most 65 535, become **MISC word 1038**.

## Fire station (`0xD3`)

Window: `Firefighters : #1` · `Fire Engines : #2` · `Response Time : #3 minutes` · — · `Annual Cost : $#0`.

- **Placed**: #1 = cap(fire funding ÷ 2, 70), as in the yearly update; #2 = 4.
- **Yearly**: #0 = fire funding; #1 = cap(fire funding ÷ 2, 70); #2 = #1 ÷ 16 + 1; #3 = rand(2–21). **Verified (data)**: #2 = #1 ÷ 16 + 1 holds in 95/114 files, fails in 3.

## School (`0xD6`)

Window: `Capacity : 1500` · `Students : #1` · `Teachers : #2` · `Grade : #G` · `Annual Cost : $#3`.

- **Placed**: #0 = 6 (C+), the rest 0.
- **Yearly**:
  - #3 = school funding ÷ 4; below 20 the newspaper reports bad schools (story type `0x26`).
  - #1 students = cap(min((people in age groups 1 and 2) ÷ max(1, tiles(`0xD6`) ÷ 9) + rand(0–15), about 1 500), 20).
  - #2 teachers = cap(max(0, rand(0–7) + school funding × 6 ÷ 10 + #0 − 12), 100).
  - #0 grade: *r* = students ÷ teachers; *r* < 15 → 12; *r* < 52 → (51 − *r*) ÷ 3; else 0. A grade below 4 also triggers story `0x26`.

## Stadium (`0xD7`)

Window: `Capacity : 25000` · `Attendance : #1` · `Local Team : #S` · `#T` · `Wins-Losses : #W`.

- **#2 sport** (0–4): Football, Baseball, Soccer, Cricket, Rugby (strings 786–790). **#3**: the XLAB slot holding the team's name, 251 + sport ([xlab.md](../segments/xlab.md#slots-251255-sports-teams)). **#W** shows #0 wins and 40 − #0 losses; nothing while #0 is 0.
- **Placed**: the game asks for the team (`0x47c970`, opened from `0x440170`) and sets the sport's bit in MISC word 1034. The dialog's rule was not decompiled (**Inferred**: one stadium per sport). Importing a SimCity Classic city (`0x483250`) gives the stadiums sports 0, 1, 2, 3, 4 in turn, skipping sports already in word 1034, then random ones.
- **Yearly**: #1 attendance = cap(min((*pop* + MISC word 1032 + arcology bonus) ÷ tiles(`0xD7`), 25 000 − rand), 5) + rand(0–255); #0 wins = rand(9–40).
- **Demolished** (`0x458d40`): the sport's bit is cleared in word 1034.
- **Verified (data)**: #3 = 251 + #2 in 142 of 156 stadium records; the other 14 (4 files) have #2 = #3 = 0. 150 of 156 have their sport's bit in word 1034.

## Prison (`0xD8`)

Window: `Capacity : 10000` · `Inmates : #1` · `Guards : #2` · `Escapes : #0` · `% of Capacity : #3`.

- **Placed**: all 0.
- **Yearly**:
  - #1 inmates = cap(min(#1 − #1 ÷ 4 + last year's MISC word 1038 ÷ max(1, tiles(`0xD8`) ÷ 16), 10 000 + rand), 20).
  - #2 guards = cap(police funding × 3, 120).
  - #3 = inmates ÷ 100 (percent of the 10 000 capacity). Above 105 the newspaper reports crowded prisons (story type `0x25`).
  - #0 escapes = 0 if #3 < 91, else a random number below (#3 − 90 + (100 − police funding) ÷ 10).
- **MISC word 1039** = 1 if there is at least one prison and the average #3 is at most 79, else 0.

## College (`0xD9`)

Window: `Capacity : 5000` · `Attendance : #1` · `Teachers : #2` · `Grade : #G` · `Annual Cost : $#3`.

- **Placed**: #0 = 6 (C+), the rest 0.
- **Yearly**:
  - #3 = college funding.
  - #1 students = cap(min((people in age group 3) ÷ max(1, tiles(`0xD9`) ÷ 16) + rand(0–31), about 5 000), 30).
  - #2 teachers = cap(max(0, rand(0–15) + college funding × 2 + #0 × 4 − 48), 100).
  - #0 grade: *r* = students × 4 ÷ teachers; *r* < 50 → 12; *r* < 111 → (110 − *r*) ÷ 5; else 0.

## Zoo (`0xDA`)

Window: `Peruvian LLamas : #1` · `Andean LLamas : #2` · `Alpacas : #3` · `Other Dromedaries : #0`.

- **Placed**: all 0. **Yearly**: each figure = rand(0–99).

## Statue (`0xDB`)

Window: `Height : 65 feet` · `Material : Bronze` · `Built in : #1` · `Pigeons perched : #2`.

- **Placed**: #1 = year. **Yearly**: #2 = rand(0–41).

## Mayor's house (`0xF3`)

Window: `Approval Rating : #2%` · `Built in : #1` · `Employees : #3` · `Doorstops : #0`.

- **Placed**: #1 = year, #2 = rand(10–39), #3 = rand(0–59).
- **Yearly**: #2 = the mayor rating (`0x4e9904`, from the opinion poll); if #3 is not 0, #3 − 1 and #0 + 1 (an employee a year turns into a doorstop).
- Clicking a microsim tile whose XTXT value is `0x6F` runs the poll and this update first (`0x414b40`; [xmic.md](../segments/xmic.md#when-the-figures-change)).

## Water treatment (`0xF4`) and desalination (`0xFA`)

Water treatment window: `Capacity : 5,000 cubic feet/minute` · — · `Untreated filth : #1,000 cubic feet` · `Treated filth : #2,000 cubic feet`. The ",000" is part of the text: the figures are in thousands.

Desalination window: `Running at #0% of Capacity` · `salt/tons removed : #1` · `Employees : #2`.

- **Placed**: all 0.
- **Yearly**, both codes: #0 = the city's water percentage (`0x4cc4b8`) + rand(0–7); #1 = rand(0–99); #2 = min(rand(135–166), *pop* ÷ 50). So "Running at #0%" is shown for the desalination plant only.

## Arcologies (`0xFB`–`0xFE`)

Window: `Design Capacity : #1000` · `Residents : #2` · `Built in : #3` · — · `Conditions : #G`. "#1000" is #1 followed by "000".

| Code | Arcology | #1 design capacity (× 1 000 people) |
|---|---|---|
| `0xFB` | Plymouth | 55 |
| `0xFC` | Forest | 30 |
| `0xFD` | Darco | 45 |
| `0xFE` | Launch | 65 |

- **Placed**: #1 = the table's value, #0 = 5 (C), #2 = 0, #3 = year. **Verified (data)**: #1 matches in all 1 253 arcology records.
- **Yearly**:
  - target = min(cap(#1 × 100, max(1, *n*) × 20), ((60 − R tax − C tax − I tax) ÷ 6 + #0) × 5 − 50) × 40), with *n* = arcology tiles ÷ 16 and the tax rates of the budget (`0x4cc4cc` + 100, + `0xD4`, + `0x144`).
  - #2 residents = min(target, #2 + #2 ÷ 50, #1 × 1 000) + rand(0–63) (`0x47fc70`).
  - The residents of all arcologies become **MISC word 1032**.
  - **Launch**: if there are more than 300 Launch arcologies (tiles(`0xFE`) ÷ 16 > 300) and word 1032 is above 6 000 000, every Launch arcology tile is destroyed, the treasury gets 100 000 per Launch arcology record, and messages `0x211` and `0x212` are shown.
- **#0 conditions** is set by zone growth (`0x4358b0`), not by the yearly update. At the arcology's tile with XZON corner bits `0x80` (top corner): (XVAL ÷ 32 − XCRM ÷ 32 − XPLT ÷ 32) + 12 for the overlay cell of that tile, halved if the tile is unpowered (XBIT `0x40` clear), halved again if it has no water (XBIT `0x10` clear), then kept within 0–12.
- The window plays sound `0x20E`, then `0x201` if #0 is above 9 or `0x200` if it is 3 or less (`0x427720`). Other microsims play one sound per code.
- **Verified (data)**: #2 ≤ #1 × 1 000 + 63 in 1 252 of 1 253 records; the exception (`SAM_STOW.SC2`, a Darco with 65 491) is from the other game version.

## Llama Dome (`0xFF`)

Window: `Weddings : #0` · `Visitors : #1` · `Llama Sightings : #2` · `Complaints : #3` · `Bungee Jumps : #0`. Weddings and bungee jumps show the same byte.

- **Placed**: #3 = year in the US build, a random number in others.
- **Yearly** (US): #0 = random; #1 = *pop* ÷ 8 + rand(0–1 023); #2 = rand(0–127) + #1 ÷ 8; #3 = rand(0–63) + #1 ÷ 16. The "aus" build uses another formula (not traced).

## Bus, rail and subway (`0xEC`, `0xED`, `0xE9`)

Windows: `Bus Stations : #1` · `Buses : #2` · `Passengers/day : #3`; `Rail Stations : #1` · `Passengers/day : #3`; `Subway Stations : #1` · `Passengers/day : #3`; each with "(statistics are for citywide … system)".

| Code | Record | Placed (per station) | #1 yearly | #2 yearly | #3 yearly |
|---|---|---|---|---|---|
| `0xEC` | 1 | #1 + 1 | tiles(`0xEC`) ÷ 4 | tiles(`0xEC`) | bus trips (`0x4c85a0`) |
| `0xED` | 2 | #1 + 1 | tiles(`0xED`) ÷ 4 | — | rail trips (`0x4c85a4`) |
| `0xE9` | 3 | #1 + 1 | tiles(`0xE9`) | — | subway trips (`0x4c8600`) |

The trip counters are increased by the trip routine (`0x457620`) and reset to 0 after the yearly update and after loading (`0x434b70`). So "passengers/day" is the number of trips counted since the last January (**Inferred** unit). **Verified (data)**: the subway count matches the tile count in 112/114 files, the bus counts in 111/114.

## Wind and hydro power (`0xC8`, `0xC6`, `0xC7`)

Windows: `Wind generators : #1` · `Max. system Output : #2 MWatts`; `Hydro generators : #1` · `Max. system Output : #2 MWatts`.

- **Placed**: wind (record 4): #1 + 1, #2 + 4. Hydro (record 5): #1 + 1, #2 + 20. Byte 0 is rewritten with the code placed, so record 5 shows `0xC6` or `0xC7`.
- **Yearly**: wind #1 = tiles(`0xC8`), #2 = 4 × #1. Hydro #1 = tiles(`0xC6`) + tiles(`0xC7`), #2 = 20 × #1.
- **Verified (data)**: hydro #2 = 20 × #1 in 114/114, wind #2 = 4 × #1 in 113/114.

## Parks (`0xD5`)

Window: `Attendance : #1 (last year)` · `Acres : #2 (citywide)` · `Employees : #3`.

- **Placed**: #2 + 9.
- **Yearly**: #1 = min(old #2 × 412, *pop* ÷ 6), each part at most 65 000; #2 = tiles(`0x0D`, small park) + tiles(`0xD5`); #3 = cap(#2 ÷ 9, 120).
- **Verified (data)**: #2 matches the tile counts in 108/114 files.

## Museums (`0xD4`)

Window: `Attendance : #1 (last year)` · `Exhibits : #2`.

- **Placed**: no change (the placement code would set #0 = 100, but only for buildings with their own record, which the museum is not).
- **Yearly**: #1 = cap(tiles(`0xD4`) × college funding × 4, 20); #2 = (college funding ÷ 10) × tiles(`0xD4`).
- **Unknown**: some files have #0 and #3 set (values such as 12 and 25), which this version never writes.

## Libraries (`0xF5`)

Window: `Attendance : #1` · `Books : #2` · `Grade : #G`.

- **Placed**: no change.
- **Yearly**: #1 = cap(tiles(`0xF5`) × school funding × 4, 18); #2 + (school funding − 50) × tiles(`0xF5`), kept only if the result is within 0–32 000; #0 = min(12, tiles(`0xF5`) × school funding × 300 ÷ *pop*).

## Marinas (`0xF8`)

Window: `Boats : #1`.

- **Placed**: no change. **Yearly**: #1 = cap(rand(0–19) + tiles(`0xF8`) × 8, 150).

## Links

- [xmic.md](../segments/xmic.md): the segment
- [tile-codes-xbld.md](tile-codes-xbld.md): the building codes
- [general.md § Counters](../segments/misc/general.md#counters): MISC words 1032, 1034, 1038, 1039
- [budget.md](../segments/misc/budget.md#entries): funding levels and tax rates
