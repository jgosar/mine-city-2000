# XBLD tile codes

Every value an [XBLD](../segments/xbld.md) byte can hold, with the game's own name, footprint and what happens to it when the view turns.

## How to read this table

| Column | Meaning | Source and confidence |
|---|---|---|
| Code | The XBLD byte | — |
| Game name | The name the game's query tool uses, exactly as stored in `SIMCITY.EXE` (typos included: "Plymoth Archology", "Hanger", "Grocery story") | Text: **Confirmed (code)**, string table ids 594–753. Which string belongs to which code: **Inferred** (see below) |
| Category | Grouping used on these pages | Ours |
| Size | Footprint in tiles | Codes `0x70`–`0xFF`: **Confirmed (code)**, size table at `0x4e85d8`. Codes `0x61`–`0x6B`: **Confirmed (code)**, anchor finder `0x45b1c0`. Everything below `0x70` else: **Confirmed (code)**, 1×1 |
| Files | In how many of the 114 corpus files the code appears at least once | **Verified (data)** |
| Notes | Shape, variant, and the code it **turns into** after one view turn by `0x465490` (the turn that decreases the compass by one) | Turns: **Confirmed (code)**, remap table `0x4e8fa0`. Shapes: **Inferred**, from the older documents |

**Directions** ("left-right", "rising to the top", "curve bottom-right") are the older documents' screen-relative terms for the view the file was saved in. How rows and columns map to the screen is on [conventions.md](../conventions.md#orientation). The rotation cycles confirm that the shapes within each group run in one consistent turning order.

**Game names.** The string table holds one name per *group* of codes below `0x7C` (for example a single "Power Lines" for all of `0x0E`–`0x1C`) and one name per code from `0x7C` to `0xFF` (string id = code + 491). The 132 strings for `0x7C`–`0xFF` line up one to one with the older documents' names, and the 21 group strings line up with their ranges, but the lookup code that picks the string was not found, so the assignment is **Inferred**. Code `0x00` uses string 751, "Clear terrain".

**Shape patterns** shared by power lines (`0x0E`–`0x1C`), roads (`0x1D`–`0x2B`) and rails (`0x2C`–`0x3A`): offset 0–1 straight, 2–5 slope, 6–9 curve, 10–13 T-junction, 14 crossing.

## Codes

| Code | Game name | Category | Size | Files | Notes |
|---|---|---|---|---|---|
| `0x00` | Clear terrain | clear | 1×1 | 114 |  |
| `0x01` | Rubble | rubble | 1×1 | 27 | variant 1 |
| `0x02` | Rubble | rubble | 1×1 | 24 | variant 2 |
| `0x03` | Rubble | rubble | 1×1 | 33 | variant 3 |
| `0x04` | Rubble | rubble | 1×1 | 24 | variant 4 |
| `0x05` | Radioactive Waste | radioactive waste | 1×1 | 1 |  |
| `0x06` | Trees | trees | 1×1 | 100 | density 1 of 7 |
| `0x07` | Trees | trees | 1×1 | 96 | density 2 of 7 |
| `0x08` | Trees | trees | 1×1 | 94 | density 3 of 7 |
| `0x09` | Trees | trees | 1×1 | 94 | density 4 of 7 |
| `0x0A` | Trees | trees | 1×1 | 95 | density 5 of 7 |
| `0x0B` | Trees | trees | 1×1 | 97 | density 6 of 7 |
| `0x0C` | Trees | trees | 1×1 | 97 | density 7 of 7 |
| `0x0D` | Park | park | 1×1 | 83 | small park |
| `0x0E` | Power Lines | power line | 1×1 | 90 | left-right; turns into 0x0F |
| `0x0F` | Power Lines | power line | 1×1 | 93 | top-bottom; turns into 0x0E |
| `0x10` | Power Lines | power line | 1×1 | 81 | top-bottom, rising to the top; turns into 0x11 |
| `0x11` | Power Lines | power line | 1×1 | 77 | left-right, rising to the right; turns into 0x12 |
| `0x12` | Power Lines | power line | 1×1 | 73 | top-bottom, rising to the bottom; turns into 0x13 |
| `0x13` | Power Lines | power line | 1×1 | 75 | left-right, rising to the left; turns into 0x10 |
| `0x14` | Power Lines | power line | 1×1 | 60 | curve bottom-right; turns into 0x15 |
| `0x15` | Power Lines | power line | 1×1 | 56 | curve bottom-left; turns into 0x16 |
| `0x16` | Power Lines | power line | 1×1 | 57 | curve left-top; turns into 0x17 |
| `0x17` | Power Lines | power line | 1×1 | 60 | curve top-right; turns into 0x14 |
| `0x18` | Power Lines | power line | 1×1 | 44 | T: top, right, bottom; turns into 0x19 |
| `0x19` | Power Lines | power line | 1×1 | 50 | T: left, bottom, right; turns into 0x1A |
| `0x1A` | Power Lines | power line | 1×1 | 47 | T: top, left, bottom; turns into 0x1B |
| `0x1B` | Power Lines | power line | 1×1 | 43 | T: top, left, right; turns into 0x18 |
| `0x1C` | Power Lines | power line | 1×1 | 39 | crossing (all four sides) |
| `0x1D` | Road | road | 1×1 | 107 | left-right; turns into 0x1E |
| `0x1E` | Road | road | 1×1 | 106 | top-bottom; turns into 0x1D |
| `0x1F` | Road | road | 1×1 | 83 | top-bottom, rising to the top; turns into 0x20 |
| `0x20` | Road | road | 1×1 | 80 | left-right, rising to the right; turns into 0x21 |
| `0x21` | Road | road | 1×1 | 78 | top-bottom, rising to the bottom; turns into 0x22 |
| `0x22` | Road | road | 1×1 | 82 | left-right, rising to the left; turns into 0x1F |
| `0x23` | Road | road | 1×1 | 102 | curve bottom-right; turns into 0x24 |
| `0x24` | Road | road | 1×1 | 104 | curve bottom-left; turns into 0x25 |
| `0x25` | Road | road | 1×1 | 102 | curve left-top; turns into 0x26 |
| `0x26` | Road | road | 1×1 | 104 | curve top-right; turns into 0x23 |
| `0x27` | Road | road | 1×1 | 102 | T: top, right, bottom; turns into 0x28 |
| `0x28` | Road | road | 1×1 | 101 | T: left, bottom, right; turns into 0x29 |
| `0x29` | Road | road | 1×1 | 101 | T: top, left, bottom; turns into 0x2A |
| `0x2A` | Road | road | 1×1 | 100 | T: top, left, right; turns into 0x27 |
| `0x2B` | Road | road | 1×1 | 101 | crossing (all four sides) |
| `0x2C` | Railway | rail | 1×1 | 46 | left-right; turns into 0x2D |
| `0x2D` | Railway | rail | 1×1 | 45 | top-bottom; turns into 0x2C |
| `0x2E` | Railway | rail | 1×1 | 20 | top-bottom, rising to the top; turns into 0x2F |
| `0x2F` | Railway | rail | 1×1 | 20 | left-right, rising to the right; turns into 0x30 |
| `0x30` | Railway | rail | 1×1 | 20 | top-bottom, rising to the bottom; turns into 0x31 |
| `0x31` | Railway | rail | 1×1 | 18 | left-right, rising to the left; turns into 0x2E |
| `0x32` | Railway | rail | 1×1 | 40 | curve bottom-right; turns into 0x33 |
| `0x33` | Railway | rail | 1×1 | 42 | curve bottom-left; turns into 0x34 |
| `0x34` | Railway | rail | 1×1 | 39 | curve left-top; turns into 0x35 |
| `0x35` | Railway | rail | 1×1 | 35 | curve top-right; turns into 0x32 |
| `0x36` | Railway | rail | 1×1 | 18 | T: top, right, bottom; turns into 0x37 |
| `0x37` | Railway | rail | 1×1 | 19 | T: left, bottom, right; turns into 0x38 |
| `0x38` | Railway | rail | 1×1 | 17 | T: top, left, bottom; turns into 0x39 |
| `0x39` | Railway | rail | 1×1 | 21 | T: top, left, right; turns into 0x36 |
| `0x3A` | Railway | rail | 1×1 | 6 | crossing (all four sides) |
| `0x3B` | Railway | rail | 1×1 | 19 | top-bottom, level piece before a slope rising to the top; turns into 0x3C |
| `0x3C` | Railway | rail | 1×1 | 19 | left-right, level piece before a slope rising to the right; turns into 0x3D |
| `0x3D` | Railway | rail | 1×1 | 18 | top-bottom, level piece before a slope rising to the bottom; turns into 0x3E |
| `0x3E` | Railway | rail | 1×1 | 14 | left-right, level piece before a slope rising to the left; turns into 0x3B |
| `0x3F` | Tunnel Entrance | tunnel entrance | 1×1 | 18 | road tunnel opening to the top; turns into 0x40 |
| `0x40` | Tunnel Entrance | tunnel entrance | 1×1 | 17 | road tunnel opening to the right; turns into 0x41 |
| `0x41` | Tunnel Entrance | tunnel entrance | 1×1 | 18 | road tunnel opening to the bottom; turns into 0x42 |
| `0x42` | Tunnel Entrance | tunnel entrance | 1×1 | 17 | road tunnel opening to the left; turns into 0x3F |
| `0x43` | Road | crossing | 1×1 | 103 | road left-right, power top-bottom; turns into 0x44 |
| `0x44` | Road | crossing | 1×1 | 101 | road top-bottom, power left-right; turns into 0x43 |
| `0x45` | Railway | crossing | 1×1 | 40 | road left-right, rail top-bottom; turns into 0x46 |
| `0x46` | Railway | crossing | 1×1 | 40 | road top-bottom, rail left-right; turns into 0x45 |
| `0x47` | Railway | crossing | 1×1 | 42 | rail left-right, power top-bottom; turns into 0x48 |
| `0x48` | Railway | crossing | 1×1 | 37 | rail top-bottom, power left-right; turns into 0x47 |
| `0x49` | Highway | highway | 1×1 | 48 | left-right; one of four tiles of a 2×2 highway cell; turns into 0x4A |
| `0x4A` | Highway | highway | 1×1 | 47 | top-bottom; one of four tiles of a 2×2 highway cell; turns into 0x49 |
| `0x4B` | Highway | highway | 1×1 | 44 | left-right, road top-bottom crossing; one of four tiles of a 2×2 highway cell; turns into 0x4C |
| `0x4C` | Highway | highway | 1×1 | 41 | top-bottom, road left-right crossing; one of four tiles of a 2×2 highway cell; turns into 0x4B |
| `0x4D` | Highway | highway | 1×1 | 17 | left-right, rail top-bottom crossing; one of four tiles of a 2×2 highway cell; turns into 0x4E |
| `0x4E` | Highway | highway | 1×1 | 17 | top-bottom, rail left-right crossing; one of four tiles of a 2×2 highway cell; turns into 0x4D |
| `0x4F` | Highway | highway | 1×1 | 42 | left-right, power top-bottom crossing; one of four tiles of a 2×2 highway cell; turns into 0x50 |
| `0x50` | Highway | highway | 1×1 | 35 | top-bottom, power left-right crossing; one of four tiles of a 2×2 highway cell; turns into 0x4F |
| `0x51` | Suspension Bridge | bridge | 1×1 | 41 | suspension bridge piece 1 of 5; turns into 0x55 |
| `0x52` | Suspension Bridge | bridge | 1×1 | 41 | suspension bridge piece 2 of 5; turns into 0x54 |
| `0x53` | Suspension Bridge | bridge | 1×1 | 41 | suspension bridge piece 3 of 5 |
| `0x54` | Suspension Bridge | bridge | 1×1 | 41 | suspension bridge piece 4 of 5; turns into 0x52 |
| `0x55` | Suspension Bridge | bridge | 1×1 | 41 | suspension bridge piece 5 of 5; turns into 0x51 |
| `0x56` | Bridge | bridge | 1×1 | 32 | raising bridge tower |
| `0x57` | Bridge | bridge | 1×1 | 63 | causeway pylon |
| `0x58` | Bridge | bridge | 1×1 | 32 | raising bridge middle (lowered) |
| `0x59` | Bridge | bridge | 1×1 | 0 | raising bridge middle (raised); never seen in the corpus; the raised state is only drawn while a ship passes (Inferred) |
| `0x5A` | Bridge | bridge | 1×1 | 19 | rail bridge pylon |
| `0x5B` | Bridge | bridge | 1×1 | 19 | rail bridge |
| `0x5C` | Power Lines | power line | 1×1 | 48 | elevated power line (over water) |
| `0x5D` | Highway Entrance | highway on-ramp | 1×1 | 45 | direction from this code and the XBIT rotate flag; remapped by the rotation post-pass, not the table |
| `0x5E` | Highway Entrance | highway on-ramp | 1×1 | 47 | direction from this code and the XBIT rotate flag; remapped by the rotation post-pass, not the table |
| `0x5F` | Highway Entrance | highway on-ramp | 1×1 | 44 | direction from this code and the XBIT rotate flag; remapped by the rotation post-pass, not the table |
| `0x60` | Highway Entrance | highway on-ramp | 1×1 | 43 | direction from this code and the XBIT rotate flag; remapped by the rotation post-pass, not the table |
| `0x61` | Highway | highway (2×2) | 2×2 | 14 | top-bottom, rising to the top; turns into 0x62 |
| `0x62` | Highway | highway (2×2) | 2×2 | 14 | left-right, rising to the right; turns into 0x63 |
| `0x63` | Highway | highway (2×2) | 2×2 | 14 | top-bottom, rising to the bottom; turns into 0x64 |
| `0x64` | Highway | highway (2×2) | 2×2 | 12 | left-right, rising to the left; turns into 0x61 |
| `0x65` | Highway | highway (2×2) | 2×2 | 29 | curve bottom-right; turns into 0x66 |
| `0x66` | Highway | highway (2×2) | 2×2 | 31 | curve bottom-left; turns into 0x67 |
| `0x67` | Highway | highway (2×2) | 2×2 | 33 | curve left-top; turns into 0x68 |
| `0x68` | Highway | highway (2×2) | 2×2 | 32 | curve top-right; turns into 0x65 |
| `0x69` | Highway | highway (2×2) | 2×2 | 27 | cloverleaf (all four sides) |
| `0x6A` | Highway Bridge | highway bridge (2×2) | 2×2 | 7 | reinforced highway bridge; direction from the XBIT rotate flag |
| `0x6B` | Highway Bridge | highway bridge (2×2) | 2×2 | 7 | reinforced highway bridge; direction from the XBIT rotate flag |
| `0x6C` | Subway to Rail | subway/rail connection | 1×1 | 9 | rail on the bottom; turns into 0x6D |
| `0x6D` | Subway to Rail | subway/rail connection | 1×1 | 17 | rail on the left; turns into 0x6E |
| `0x6E` | Subway to Rail | subway/rail connection | 1×1 | 9 | rail on the top; turns into 0x6F |
| `0x6F` | Subway to Rail | subway/rail connection | 1×1 | 9 | rail on the right; turns into 0x6C |
| `0x70` | Lower-class homes | residential | 1×1 | 94 |  |
| `0x71` | Lower-class homes | residential | 1×1 | 95 |  |
| `0x72` | Lower-class homes | residential | 1×1 | 94 |  |
| `0x73` | Lower-class homes | residential | 1×1 | 93 |  |
| `0x74` | Middle-class homes | residential | 1×1 | 100 |  |
| `0x75` | Middle-class homes | residential | 1×1 | 97 |  |
| `0x76` | Middle-class homes | residential | 1×1 | 99 |  |
| `0x77` | Middle-class homes | residential | 1×1 | 98 |  |
| `0x78` | Luxury Homes | residential | 1×1 | 94 |  |
| `0x79` | Luxury Homes | residential | 1×1 | 94 |  |
| `0x7A` | Luxury Homes | residential | 1×1 | 94 |  |
| `0x7B` | Luxury Homes | residential | 1×1 | 96 |  |
| `0x7C` | Gas Station | commercial | 1×1 | 97 |  |
| `0x7D` | Bed & Breakfast Inn | commercial | 1×1 | 97 |  |
| `0x7E` | Convience Store | commercial | 1×1 | 97 |  |
| `0x7F` | Gas Station | commercial | 1×1 | 97 |  |
| `0x80` | Small office building | commercial | 1×1 | 97 |  |
| `0x81` | Office building | commercial | 1×1 | 96 |  |
| `0x82` | Warehouse | commercial | 1×1 | 95 |  |
| `0x83` | Cassidy's Toy Store | commercial | 1×1 | 95 |  |
| `0x84` | Warehouse | industrial | 1×1 | 98 |  |
| `0x85` | Chemical Storage | industrial | 1×1 | 99 |  |
| `0x86` | Warehouse | industrial | 1×1 | 99 |  |
| `0x87` | Industrial substation | industrial | 1×1 | 97 |  |
| `0x88` | Construction | construction | 1×1 | 19 |  |
| `0x89` | Construction | construction | 1×1 | 21 |  |
| `0x8A` | Abandoned building | abandoned | 1×1 | 97 |  |
| `0x8B` | Abandoned building | abandoned | 1×1 | 99 |  |
| `0x8C` | Cheap apartments | residential | 2×2 | 88 |  |
| `0x8D` | Apartments | residential | 2×2 | 88 |  |
| `0x8E` | Apartments | residential | 2×2 | 90 |  |
| `0x8F` | Nice Apartments | residential | 2×2 | 89 |  |
| `0x90` | Nice Apartments | residential | 2×2 | 91 |  |
| `0x91` | Condominium | residential | 2×2 | 94 |  |
| `0x92` | Condominium | residential | 2×2 | 92 |  |
| `0x93` | Condominium | residential | 2×2 | 93 |  |
| `0x94` | Shopping center | commercial | 2×2 | 55 |  |
| `0x95` | Grocery story | commercial | 2×2 | 61 |  |
| `0x96` | Office building | commercial | 2×2 | 55 |  |
| `0x97` | Resort hotel | commercial | 2×2 | 50 |  |
| `0x98` | Office building | commercial | 2×2 | 52 |  |
| `0x99` | Office / Retail | commercial | 2×2 | 86 |  |
| `0x9A` | Office building | commercial | 2×2 | 89 |  |
| `0x9B` | Office building | commercial | 2×2 | 85 |  |
| `0x9C` | Office building | commercial | 2×2 | 87 |  |
| `0x9D` | Office building | commercial | 2×2 | 85 |  |
| `0x9E` | Warehouse | industrial | 2×2 | 48 |  |
| `0x9F` | Chemical processing | industrial | 2×2 | 45 |  |
| `0xA0` | Factory | industrial | 2×2 | 44 |  |
| `0xA1` | Factory | industrial | 2×2 | 58 |  |
| `0xA2` | Factory | industrial | 2×2 | 91 |  |
| `0xA3` | Factory | industrial | 2×2 | 91 |  |
| `0xA4` | Factory | industrial | 2×2 | 93 |  |
| `0xA5` | Factory | industrial | 2×2 | 88 |  |
| `0xA6` | Construction | construction | 2×2 | 55 |  |
| `0xA7` | Construction | construction | 2×2 | 54 |  |
| `0xA8` | Construction | construction | 2×2 | 77 |  |
| `0xA9` | Construction | construction | 2×2 | 82 |  |
| `0xAA` | Abandoned building | abandoned | 2×2 | 59 |  |
| `0xAB` | Abandoned building | abandoned | 2×2 | 62 |  |
| `0xAC` | Abandoned building | abandoned | 2×2 | 65 |  |
| `0xAD` | Abandoned building | abandoned | 2×2 | 60 |  |
| `0xAE` | Large apartment building | residential | 3×3 | 49 |  |
| `0xAF` | Large apartment building | residential | 3×3 | 54 |  |
| `0xB0` | Condominium | residential | 3×3 | 50 |  |
| `0xB1` | Condominium | residential | 3×3 | 54 |  |
| `0xB2` | Office park | commercial | 3×3 | 44 |  |
| `0xB3` | Office tower | commercial | 3×3 | 36 |  |
| `0xB4` | Mini-mall | commercial | 3×3 | 45 |  |
| `0xB5` | Theatre Square | commercial | 3×3 | 38 |  |
| `0xB6` | Drive-in theater | commercial | 3×3 | 42 |  |
| `0xB7` | Office tower | commercial | 3×3 | 41 |  |
| `0xB8` | Office tower | commercial | 3×3 | 41 |  |
| `0xB9` | Parking lot | commercial | 3×3 | 46 |  |
| `0xBA` | Historic office building | commercial | 3×3 | 43 |  |
| `0xBB` | Corporate headquarters | commercial | 3×3 | 42 |  |
| `0xBC` | Chemical processing | industrial | 3×3 | 74 |  |
| `0xBD` | Large factory | industrial | 3×3 | 67 |  |
| `0xBE` | Industrial thingamajig | industrial | 3×3 | 77 |  |
| `0xBF` | Factory | industrial | 3×3 | 76 |  |
| `0xC0` | Large warehouse | industrial | 3×3 | 77 |  |
| `0xC1` | Warehouse | industrial | 3×3 | 80 |  |
| `0xC2` | Construction | construction | 3×3 | 41 |  |
| `0xC3` | Construction | construction | 3×3 | 29 |  |
| `0xC4` | Abandoned building | abandoned | 3×3 | 20 |  |
| `0xC5` | Abandoned building | abandoned | 3×3 | 17 |  |
| `0xC6` | Hydroelectric power | power plant | 1×1 | 32 | variant 1 |
| `0xC7` | Hydroelectric power | power plant | 1×1 | 30 | variant 2 |
| `0xC8` | Wind power | power plant | 1×1 | 32 |  |
| `0xC9` | Natural gas power plant | power plant | 4×4 | 9 |  |
| `0xCA` | Oil power plant | power plant | 4×4 | 9 |  |
| `0xCB` | Nuclear power plant | power plant | 4×4 | 18 |  |
| `0xCC` | Solar power plant | power plant | 4×4 | 10 |  |
| `0xCD` | Microwave power plant | power plant | 4×4 | 23 |  |
| `0xCE` | Fusion power plant | power plant | 4×4 | 49 |  |
| `0xCF` | Coal power plant | power plant | 4×4 | 19 |  |
| `0xD0` | City Hall | civic | 3×3 | 84 |  |
| `0xD1` | Hospital | civic | 3×3 | 98 |  |
| `0xD2` | Police station | civic | 3×3 | 102 |  |
| `0xD3` | Fire station | civic | 3×3 | 101 |  |
| `0xD4` | Museum | civic | 3×3 | 70 |  |
| `0xD5` | Park | civic | 3×3 | 87 |  |
| `0xD6` | School | civic | 3×3 | 99 |  |
| `0xD7` | Stadium | civic | 4×4 | 80 |  |
| `0xD8` | Prison | civic | 4×4 | 42 |  |
| `0xD9` | College | civic | 4×4 | 82 |  |
| `0xDA` | Zoo | civic | 4×4 | 67 |  |
| `0xDB` | Statue | civic | 1×1 | 57 |  |
| `0xDC` | Water pump | utility | 1×1 | 99 |  |
| `0xDD` | Runway | airport / seaport / military | 1×1 | 69 | straight runway; direction from the XBIT rotate flag |
| `0xDE` | Runway | airport / seaport / military | 1×1 | 30 | runway crossing |
| `0xDF` | Pier | airport / seaport / military | 1×1 | 66 | pier; direction from the XBIT rotate flag |
| `0xE0` | Crane | airport / seaport / military | 1×1 | 66 |  |
| `0xE1` | Control tower | airport / seaport / military | 1×1 | 68 | civilian control tower |
| `0xE2` | Control tower | airport / seaport / military | 1×1 | 12 | military control tower |
| `0xE3` | Warehouse | airport / seaport / military | 1×1 | 70 |  |
| `0xE4` | Building | airport / seaport / military | 1×1 | 66 | airport building |
| `0xE5` | Buiding | airport / seaport / military | 1×1 | 67 | airport building |
| `0xE6` | Tarmac | airport / seaport / military | 1×1 | 68 |  |
| `0xE7` | F-15b | airport / seaport / military | 1×1 | 12 |  |
| `0xE8` | Hanger | airport / seaport / military | 1×1 | 3 | small hangar (1×1); military count index 13 |
| `0xE9` | Subway Station | transport | 1×1 | 48 |  |
| `0xEA` | Radar | airport / seaport / military | 1×1 | 68 |  |
| `0xEB` | Water tower | utility | 2×2 | 42 |  |
| `0xEC` | Bus station | transport | 2×2 | 55 |  |
| `0xED` | Rail station | transport | 2×2 | 49 |  |
| `0xEE` | Parking lot | transport | 2×2 | 54 | civilian parking lot |
| `0xEF` | Parking lot | airport / seaport / military | 2×2 | 11 | military parking lot |
| `0xF0` | Loading bay | airport / seaport / military | 2×2 | 62 |  |
| `0xF1` | Top Secret | airport / seaport / military | 2×2 | 4 |  |
| `0xF2` | Cargo yard | airport / seaport / military | 2×2 | 58 |  |
| `0xF3` | man | reward | 2×2 | 75 | the mayor's house (game string: "man") |
| `0xF4` | Water treatment plant | utility | 2×2 | 57 |  |
| `0xF5` | Library | civic | 2×2 | 78 |  |
| `0xF6` | Hangar | airport / seaport / military | 2×2 | 62 | large hangar (2×2); military count index 14 |
| `0xF7` | Church | civic | 2×2 | 99 |  |
| `0xF8` | Marina | seaport | 3×3 | 73 |  |
| `0xF9` | Missile silo | military | 2×2 | 2 |  |
| `0xFA` | Desalinization plant | utility | 3×3 | 26 |  |
| `0xFB` | Plymoth Archology | arcology | 4×4 | 17 | Plymouth arcology |
| `0xFC` | Forest Arcology | arcology | 4×4 | 16 | Forest arcology |
| `0xFD` | Darco Arcology | arcology | 4×4 | 12 | Darco arcology |
| `0xFE` | Lanch Archology | arcology | 4×4 | 24 | Launch arcology |
| `0xFF` | Braun Llama-dome | reward | 4×4 | 48 | reward building |

## Groups at a glance

| Codes | Category | Size |
|---|---|---|
| `0x00` | clear | 1×1 |
| `0x01`–`0x0D` | rubble, radioactive waste, trees, small park | 1×1 |
| `0x0E`–`0x48` | power lines, roads, rails, tunnel entrances, crossings | 1×1 |
| `0x49`–`0x50` | highways (one tile of a 2×2 highway cell) | 1×1 |
| `0x51`–`0x5C` | bridges, elevated power line | 1×1 |
| `0x5D`–`0x60` | highway on-ramps | 1×1 |
| `0x61`–`0x6B` | sloped, curved and cloverleaf highways; highway bridges | 2×2 |
| `0x6C`–`0x6F` | subway/rail connections | 1×1 |
| `0x70`–`0x8B` | 1×1 zone buildings (R, C, I, construction, abandoned) | 1×1 |
| `0x8C`–`0xAD` | 2×2 zone buildings | 2×2 |
| `0xAE`–`0xC5` | 3×3 zone buildings | 3×3 |
| `0xC6`–`0xCF` | power plants (hydro and wind 1×1, the rest 4×4) | 1×1 / 4×4 |
| `0xD0`–`0xDB` | civic buildings | 3×3 / 4×4 / 1×1 |
| `0xDC`–`0xFA` | utilities, ports, military, transport, special | 1×1 / 2×2 / 3×3 |
| `0xFB`–`0xFF` | arcologies, Braun Llama-dome | 4×4 |

## Evidence

| Claim | Code | Data |
|---|---|---|
| Names | string table ids 594–753 in `SIMCITY.EXE` | — |
| Sizes of `0x70`–`0xFF` | 16-bit table at `0x4e85d8` + 2 × code, read by `0x45b1c0` | in 108/114 files every anchored footprint is well-formed ([xbld.md](../segments/xbld.md#footprints-and-anchors)) |
| 2×2 highways | `0x45b1c0` treats `0x49`–`0x50` and `0x61`–`0x6B` as size 2 on an even grid | [xbld.md § Highways](../segments/xbld.md#highways) |
| Turns | remap tables `0x4e8fa0` (`0x465490`) and `0x4e9110` (`0x466660`, the inverse) | — |
| Frequencies | — | `sc2check.py --only xbld_codes` |
| Military count index | `0x441f00` | — |
