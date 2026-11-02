# MISC: newspapers (words 916–999)

The state of the game's **newspapers**: how each of the six papers looks (name, layout, price wording, columns), and the queue of stories waiting to be printed. All values are small; most words hold a single byte.

## At a glance

| | Papers | Stories |
|---|---|---|
| Words | 916–945 (bytes `0xE50`–`0xEC7`) | 946–999 (bytes `0xEC8`–`0xF9F`) |
| Records | 6 × 5 words | 9 × 6 words |
| Loaded into | `0x4cad24` (5 bytes per paper) | `0x4ca1c0` (8 bytes per story) |
| Set up by | `0x479730` | `0x479730`, then `0x47b5c0` for each new story |

Related words elsewhere in MISC: the number of papers the city has (word 8), the paper the player last chose (word 1027) and the two newspaper menu switches (words 1025 and 1026) ([general.md](general.md)).

## Papers: words 916–945

Paper *p* (0–5) is words 916 + 5*p* … 920 + 5*p*. Each word holds one byte.

| Word | Field | Values | Meaning | Confidence |
|---|---|---|---|---|
| 916 + 5*p* | name | 0–5 | Picayune, Courier, Herald, Journal, Times, Chronicle (strings 360–365) | **Confirmed (code)** |
| 917 + 5*p* | layout | 0–2 | Which of three page layouts the paper uses | **Confirmed (code)** |
| 918 + 5*p* | price style | 0–2 | Which wording the price uses: "One Cent" / "A Single Penny" / a third set, by era (strings 377 and up) | **Confirmed (code)** |
| 919 + 5*p* | opinion column | 0–5 | Fred's Opinion, Editor's Corner, Commentary, Opinion Poll, Survey, MisSim's Advice (strings 354–359) | **Confirmed (code)** |
| 920 + 5*p* | weather column | 0–5 | Weather Corner, Weather Report, Today's Weather, Weather with Merle, Weather Forecast, Weather Talk (strings 347–352) | **Confirmed (code)** |

Each field is a shuffled column: across the six papers, the names, opinion columns and weather columns are each a permutation of 0–5, and the layouts and price styles each hold 0, 0, 1, 1, 2, 2. A new game sets them up once (`0x479730`) and nothing changes them afterwards.

The **Newspaper menu** (`0x406d70`) lists papers 0 … min(word 8, 5) by name. Paper *p*'s masthead is "The *name*" while *p* is below half of word 8, and "*city name* *name*" otherwise (`0x477c12`). The price wording also depends on the era: (founding year − 1900) / 50 + years elapsed / 50, at most 4.

## Stories: words 946–999

Story slot *s* (0–8) is words 946 + 6*s* … 951 + 6*s*.

| Word | Field | Type | Meaning | Confidence |
|---|---|---|---|---|
| 946 + 6*s* | type | int16 | Which story (see [§ Story types](#story-types)) | **Confirmed (code)** |
| 947 + 6*s* | priority | int16 | How newsworthy it still is; falls every month | **Confirmed (code)** |
| 948 + 6*s* | parameter | byte | A number the story text refers to (an index, a rate, a count) | **Confirmed (code)** |
| 949 + 6*s* … 951 + 6*s* | phrase choices | 3 bytes | Random choices made the first time the story is written out, so that it reads the same every time; 255 = not chosen yet | **Confirmed (code)** |

### The three kinds of slot

| Slots | Holds |
|---|---|
| 0–6 | The story queue, sorted by priority, highest first |
| 7 | The weather item: type 0, parameter = the weather (MISC word 27) |
| 8 | The opinion item: type 42–45 from the paper's opinion column (0 → 42, 1–2 → 43, 3–4 → 44, 5 → 45), parameter = the top problem in the latest opinion poll (`0x472510`; 0 traffic, 1 pollution, 2 crime, 3 taxes, 4 unemployment, 5 education, 6 health) |

Slots 7 and 8 are rewritten each time a paper is opened (`0x477880`), so they describe the last paper read. Their priority is 0.

### How the queue works

- **A new story** (`0x47b5c0`, called with a type and a parameter): its priority comes from a per-type table. It is inserted into slots 0–6 by priority, pushing the lower stories down; the story in slot 6 drops out. Its phrase choices are set to 255. If it is a major story (types 3, 4, 5 or `0x24`) and the newspaper delivery switch (word 1026) is on, the paper opens at once; type 2 always opens it.
- **Every month** (`0x479a70`, from `0x468150`): each story in slots 0–6 loses a per-type amount of priority (not below 0), and the slots are sorted again.
- **A new game** (`0x479730`): slot *s* gets type 11 + *s* with priority 0, parameter 0 and phrase choices 255. Types 11–19 are filler stories.
- **Printing** (`0x47a240`, which expands the story templates): the parameter is printed as a number or used to pick a phrase; phrase choices 255 (or out of range) are replaced by a random choice and kept.

The per-type priority and decay tables and the story templates are not in the city file; the game loads them from its data file (`DATA_USA.DAT`, records 1000–1005, via `0x4791d0`; **Inferred** from the file name passed).

### Story types

Which code creates which type. The texts are in the game's data file; the meanings here are **Inferred** from the caller.

| Type | Created by | Parameter |
|---|---|---|
| 0 | slot 7, when a paper is opened | weather |
| 1, 6, 8, `0x10`–`0x12`, `0x15`, `0x3D`–`0x3F`, `0x42` | monthly statistics `0x468150` | various (type 8: a value from that routine) |
| 2 | new city, after the terrain is made (`0x42e460`) | 0 |
| 3 | a new newspaper (`0x413520`, when population passes a threshold) | number of papers before |
| 4, 5 | an invention becomes available (`0x468150`) | technology index (type 5: index − 7) ([technology.md](technology.md)) |
| 7 | the national economy changes (`0x468d30`) | new trend (MISC word 23) |
| 9, 10 | the Fed Rate changes (`0x468d30`) | new rate (MISC word 22) |
| 11–19 | filler, from a new game (`0x479730`) | 0 |
| `0x19`–`0x22` | disasters (`0x45cf10`) | disaster detail |
| `0x24`–`0x26` | microsims (`0x44e390`) | for `0x24`: a microsim number |
| `0x27` | zone growth (`0x4358b0`) | 0 |
| `0x28` | the bulldozer (`0x443270`) | 0 |
| `0x29` | a random new ordinance (`0x4734a0`) | ordinance bit number |
| `0x2E` and up | disaster placement (`0x471bc0`) | 0 |
| 42–45 | slot 8, when a paper is opened | top poll problem |

## Evidence

| Claim | Code | Corpus (114 files) |
|---|---|---|
| Paper fields are shuffled columns | `0x479730` | names, opinion and weather columns a permutation of 0–5 in 114/114; layouts and price styles 0, 0, 1, 1, 2, 2 in 114/114 |
| Slots 0–6 sorted by priority | `0x47b5c0`, `0x479a70` | 114/114 |
| Slot 7 is the weather item | `0x477880` | type 0, priority 0 in 109/114; the other 5 still hold the filler types 18 and 19 (no paper opened yet) |
| Slot 8 is the opinion item | `0x477880` | type 42–45 in 109/114 (the same 5 files hold 19) |
| Phrase choices start at 255 | `0x47b5c0` | all three 255 in 777 of 798 queue slots |

## Pitfalls for readers

- **Most words hold a byte.** The saver writes the byte fields zero-extended, so they read as 0–255.
- **Slots 7 and 8 are not news.** They are scratch items for the last paper shown.
- **The story text is not in the file.** Only the type and the numbers that fill it in are.

## Links

- [MISC word index](README.md)
- [technology.md](technology.md): invention announcements
- [general.md](general.md): the newspaper count and menu switches
