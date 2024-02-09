using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.SimCityReader
{
    public class BuildingType
    {
        private byte _code;
        private String _name;
        private byte _size;

        public byte getCode()
        {
            return _code;
        }

        public String getName()
        {
            return _name;
        }

        public byte getSize()
        {
            return _size;
        }

        public override String ToString()
        {
            return _code + "\t" + _size + "\t" + _name;
        }

        private BuildingType(byte code, String name, byte size)
        {
            _code = code;
            _name = name;
            _size = size;
        }

        public static BuildingType getByCode(byte code)
        {
            //00: Clear terrain (empty)
            if (code == 0)
            {
                return new BuildingType(code, "Empty", 1);
            }
            //01-04: Rubble
            else if (code >= 1 && code <=4)
            {
                return new BuildingType(code, "Rubble "+code, 1);
            }
            //05: Radioactive waste
            else if (code == 5)
            {
                return new BuildingType(code, "Radioac", 1);
            }
            //06-0C: Trees (density increases as code increases)
            else if (code == 6)
            {
                return new BuildingType(code, "1 Tree", 1);
            }
            else if (code >= 7 && code <=12)
            {
                return new BuildingType(code, "Trees "+(code-6), 1);
            }
            //0D: Small park (set XZON as for a 1x1 building)
            else if (code == 13)
            {
                return new BuildingType(code, "SmallPa", 1);
            }
            //0E-1C: Power lines (various directions, slopes)
            //   The difference X between the code and 0E, the first code, tells 
            //   what direction(s) and slope the power line takes.
            //   X (in hex)  Direction
            //   0           Left-right [for definition of directions, see note with ALTM]
            //   1           Top-bottom
            //   2           Top-bottom; slopes upwards towards top
            //   3           Left-right; slopes upwards towards right 
            //   4           Top-bottom; slopes upwards towards bottom
            //   5           Left-right; slopes upwards towards left
            //   6           From bottom side to right side
            //   7           Bottom to left
            //   8           Left to top
            //   9           Top to right
            //   A           T junction between top, right and bottom
            //   B           T between left, bottom and right
            //   C           T between top, left and bottom
            //   D           T between top, left and right
            //   E           Intersection connecting top, left, bottom, and right
            else if (code >=14 && code <=28)
            {
                return new BuildingType(code, "Power "+(code-13), 1);
            }
            //1D-2B: Roads (various directions, slopes; same coding as for 0E-1C)
            else if (code >= 29 && code <= 43)
            {
                return new BuildingType(code, "Road " + (code - 28), 1);
            }
            //2C-3A: Rails (various directions, slopes; same coding as for 0E-1C)
            //3B-3E: More sloping rails.  These are used as preparation before ascending.
            //The 2C-3A rail codes are used on the actual sloping code.  This is why
            //rails don't look right when ascending a 1:1 grade.
            //  3B: Top-bottom; slopes upwards towards top
            //  3C: Left-right; slopes upwards towards right
            //  3D: Top-bottom; slopes upwards towards bottom
            //  3E: Left-right; slopes upwards towards left
            else if (code >= 44 && code <= 62)
            {
                return new BuildingType(code, "Rail " + (code - 43), 1);
            }
            //3F-42: Tunnel entrances
            //  3F: Tunnel to the top
            //  40: Tunnel to the right
            //  41: Tunnel to the bottom
            //  42: Tunnel to the left
            else if (code >= 63 && code <= 66)
            {
                return new BuildingType(code, "Tunnel " + (code - 62), 1);
            }
            //43-44: Crossovers (roads/power lines)
            //  43: Road left-right, power top-bottom
            else if (code == 67)
            {
                return new BuildingType(code, "RoadPower", 1);
            }
            //  44: Road top-bottom, power left-right
            else if (code == 68)
            {
                return new BuildingType(code, "PowerRoad", 1);
            }
            //45-46: Crossovers (roads/rails)
            //  45: Road left-right, rails top-bottom
            else if (code == 69)
            {
                return new BuildingType(code, "RoadRail", 1);
            }
            //  46: Road top-bottom, rails left-right
            else if (code == 70)
            {
                return new BuildingType(code, "RailRoad", 1);
            }
            //47-48: Crossovers (rails/power lines)
            //  47: Rails left-right, power lines top-bottom
            else if (code == 71)
            {
                return new BuildingType(code, "RailPower", 1);
            }
            //  48: Rails top-bottom, power lines left-right
            else if (code == 72)
            {
                return new BuildingType(code, "PowerRail", 1);
            }
            //49-4A: Highways (set XZON as for a 1x1 building)
            //  49: Highway left-right
            //  4A: Highway top-bottom
            else if (code == 73 || code == 74)
            {
                return new BuildingType(code, "Hwy " + (code - 72), 1);
            }
            //4B-4C: Crossovers (roads/highways; set XZON as for a 1x1 building)
            //  4B: Highway left-right, road top-bottom
            else if (code == 75)
            {
                return new BuildingType(code, "RoadHwy", 1);
            }
            //  4C: Highway top-bottom, road left-right
            else if (code == 76)
            {
                return new BuildingType(code, "HwyRoad", 1);
            }
            //4D-4E: Crossovers (rails/highways; set XZON as for a 1x1 building)
            //  4D: Highway left-right, rails top-bottom
            else if (code == 77)
            {
                return new BuildingType(code, "HwyRail", 1);
            }
            //  4E: Highway top-bottom, rails left-right
            else if (code == 78)
            {
                return new BuildingType(code, "RailHwy", 1);
            }
            //4F-50: Crossovers (highways/power lines; set XZON as for a 1x1 building)
            //  4F: Highway left-right, power lines top-bottom
            else if (code == 79)
            {
                return new BuildingType(code, "HwyPower", 1);
            }
            //  50: Highway top-bottom, power lines left-right
            else if (code == 80)
            {
                return new BuildingType(code, "PowerHwy", 1);
            }
            //51-55: Suspension bridge pieces
            else if (code >= 81 && code <= 85)
            {
                return new BuildingType(code, "SBridge "+(code-80), 1);
            }
            //56-59: Other road bridge pieces
            else if (code >= 86 && code <= 89)
            {
                return new BuildingType(code, "Bridge " + (code - 85), 1);
            }
            //5A-5B: Rail bridge pieces
            else if (code >= 90 && code <= 91)
            {
                return new BuildingType(code, "RBridge " + (code - 89), 1);
            }
            //5C: Elevated power lines
            else if (code == 92)
            {
                return new BuildingType(code, "PowerBridge", 1);
            }
            //5D-60: Highway entrances (on-ramps)
            //  5D: Highway at top, road at left OR highway at right, road at bottom
            //  5E: H right, R top OR H top, R right
            //  5F: R right, H bottom OR H left, R top
            //  60: R left, H bottom OR H left, R bottom
            else if (code >= 93 && code <= 96)
            {
                return new BuildingType(code, "HwyRamp "+(code-92), 1);
            }
            //61-69: Highways (various directions, slopes; 2x2 tiles; XZON should be set
            //                 as for a 2x2 building)
            //  61: Highway top-bottom, slopes up to the top
            //  62: Highway left-right, slopes up to the right
            //  63: Highway top-bottom, slopes up to the bottom
            //  64: Highway left-right, slopes up to the left
            //  65: Highway joining the bottom to the right
            //  66: Highway joining the bottom to the left
            //  67: Highway joining the left to the top
            //  68: Highway joining the top to the right
            //  69: Cloverleaf intersection connecting top, left, bottom and right
            else if (code >= 97 && code <= 105)
            {
                return new BuildingType(code, "Hwy " + (code - 94), 2);
            }
            //6A-6B: Highway bridges (2x2 tiles; set XZON as for a 2x2 building.)  This 
            //       is a reinforced bridge.  Use 49/4A for the `Hiway' bridge.
            else if (code >= 106 && code <= 107)
            {
                return new BuildingType(code, "HBridge " + (code - 105), 2);
            }
            //6C-6F: Sub/rail connections (set XZON as for a 1x1 building)
            //  6C: Sub/rail connection, rail at bottom
            //  6D: Sub/rail connection, rail at left
            //  6E: Sub/rail connection, rail at top
            //  6F: Sub/rail connection, rail at right
            else if (code >= 108 && code <= 111)
            {
                return new BuildingType(code, "SubRail " + (code - 107), 1);
            }

            //Buildings:

            //  Residential, 1x1:
            //    70-73: Lower-class homes
            else if (code >= 112 && code<=114)
            {
                return new BuildingType(code, "LC Home "+(code-111), 1);
            }
            //    74-77: Middle-class homes
            else if (code >= 115 && code <= 119)
            {
                return new BuildingType(code, "MC Home " + (code - 114), 1);
            }
            //    78-7B: Luxury homes
            else if (code >= 120 && code <= 123)
            {
                return new BuildingType(code, "UC Home " + (code - 119), 1);
            }
            //  Commercial, 1x1:
            //    7C: Gas station
            else if (code == 124)
            {
                return new BuildingType(code, "Gas 1", 1);
            }
            //    7D: Bed & breakfast inn
            else if (code == 125)
            {
                return new BuildingType(code, "B&B Inn", 1);
            }
            //    7E: Convenience store
            else if (code == 126)
            {
                return new BuildingType(code, "ConvSto", 1);
            }
            //    7F: Gas station
            else if (code == 127)
            {
                return new BuildingType(code, "Gas 2", 1);
            }
            //    80: Small office building
            else if (code == 128)
            {
                return new BuildingType(code, "S Office 1", 1);
            }
            //    81: Office building
            else if (code == 129)
            {
                return new BuildingType(code, "S office 2", 1);
            }
            //    82: Warehouse
            else if (code == 130)
            {
                return new BuildingType(code, "Warehouse", 1);
            }
            //    83: Cassidy's Toy Store
            else if (code == 131)
            {
                return new BuildingType(code, "Toy Stor", 1);
            }
            //  Industrial, 1x1:
            //    84: Warehouse
            else if (code == 132)
            {
                return new BuildingType(code, "S Whse 1", 1);
            }
            //    85: Chemical storage
            else if (code == 133)
            {
                return new BuildingType(code, "Chem St", 1);
            }
            //    86: Warehouse
            else if (code == 134)
            {
                return new BuildingType(code, "S Whse 2", 1);
            }
            //    87: Industrial substation
            else if (code == 135)
            {
                return new BuildingType(code, "IndStati", 1);
            }
            //  Miscellaneous, 1x1:
            //    88-89: Construction
            else if (code == 136)
            {
                return new BuildingType(code, "Constr 7", 1);
            }
            else if (code == 137)
            {
                return new BuildingType(code, "Constr 8", 1);
            }
            //    8A-8B: Abandoned building
            else if (code == 138)
            {
                return new BuildingType(code, "Aband 1", 1);
            }
            else if (code == 139)
            {
                return new BuildingType(code, "Aband 2", 1);
            }
            //  Residential, 2x2:
            //    8C: Cheap apartments
            //    8D-8E: Apartments
            else if (code >= 140 && code <=142)
            {
                return new BuildingType(code, "S Apts "+(code-139), 2);
            }
            //    8F-90: Nice apartments
            else if (code >= 143 && code <= 144)
            {
                return new BuildingType(code, "M Apts " + (code - 142), 2);
            }
            //    91-93: Condominium
            else if (code >= 145 && code <= 147)
            {
                return new BuildingType(code, "M Condo " + (code - 144), 2);
            }
            //  Commercial, 2x2:
            //    94: Shopping center
            else if (code == 148)
            {
                return new BuildingType(code, "Shop Ct", 2);
            }
            //    95: Grocery store
            else if (code == 149)
            {
                return new BuildingType(code, "Grocery", 2);
            }
            //    96: Office building
            else if (code == 150)
            {
                return new BuildingType(code, "M Office 1", 2);
            }
            //    97: Resort hotel
            else if (code == 151)
            {
                return new BuildingType(code, "Res Hot", 2);
            }
            //    98: Office building
            else if (code == 152)
            {
                return new BuildingType(code, "M Office 2", 2);
            }
            //    99: Office / Retail
            else if (code == 153)
            {
                return new BuildingType(code, "Office-R", 2);
            }
            //    9A-9D: Office building
            else if (code >= 154 && code <= 157)
            {
                return new BuildingType(code, "M Office "+(code-151), 2);
            }
            //  Industrial, 2x2:
            //    9E: Warehouse
            else if (code == 158)
            {
                return new BuildingType(code, "M Whse", 2);
            }
            //    9F: Chemical processing
            else if (code == 159)
            {
                return new BuildingType(code, "Chemic 2", 2);
            }
            //    A0-A5: Factory
            else if (code >= 160 && code <= 165)
            {
                return new BuildingType(code, "Factory " + (code - 159), 2);
            }
            //  Miscellaneous, 2x2:
            //    A6-A9: Construction
            else if (code >= 166 && code <= 169)
            {
                return new BuildingType(code, "Constr " + (code - 163), 2);
            }
            //    AA-AD: Abandoned building
            else if (code >= 170 && code <= 173)
            {
                return new BuildingType(code, "Aband " + (code - 167), 2);
            }
            //  Residential, 3x3:
            //    AE-AF: Large apartment building
            else if (code >= 174 && code <= 175)
            {
                return new BuildingType(code, "L Apts " + (code - 173), 3);
            }
            //    B0-B1: Condominium
            else if (code >= 176 && code <= 177)
            {
                return new BuildingType(code, "L Condo " + (code - 175), 3);
            }
            //  Commercial, 3x3:
            //    B2: Office park
            else if (code == 178)
            {
                return new BuildingType(code, "O Park ", 3);
            }
            //    B3: Office tower
            else if (code == 179)
            {
                return new BuildingType(code, "Tower 1", 3);
            }
            //    B4: Mini-mall
            else if (code == 180)
            {
                return new BuildingType(code, "Mini Mal ", 3);
            }
            //    B5: Theater code
            else if (code == 181)
            {
                return new BuildingType(code, "Theater", 3);
            }
            //    B6: Drive-in theater
            else if (code == 182)
            {
                return new BuildingType(code, "Drive In", 3);
            }
            //    B7-B8: Office tower
            else if (code == 183)
            {
                return new BuildingType(code, "Tower 2", 3);
            }
            else if (code == 184)
            {
                return new BuildingType(code, "Tower 3", 3);
            }
            //    B9: Parking lot
            else if (code == 185)
            {
                return new BuildingType(code, "Parking", 3);
            }
            //    BA: Historic office building
            else if (code == 186)
            {
                return new BuildingType(code, "Hist Offic", 3);
            }
            //    BB: Corporate headquarters
            else if (code == 187)
            {
                return new BuildingType(code, "Corp Hq", 3);
            }
            //  Industrial, 3x3:
            //    BC: Chemical processing
            else if (code == 188)
            {
                return new BuildingType(code, "Chemic 1", 3);
            }
            //    BD: Large factory
            else if (code == 189)
            {
                return new BuildingType(code, "L Facto", 3);
            }
            //    BE: Industrial thingamajig
            else if (code == 190)
            {
                return new BuildingType(code, "Industri", 3);
            }
            //    BF: Factory
            else if (code == 191)
            {
                return new BuildingType(code, "M Facto", 3);
            }
            //    C0: Large warehouse
            else if (code == 192)
            {
                return new BuildingType(code, "L Whse 1", 3);
            }
            //    C1: Warehouse
            else if (code == 193)
            {
                return new BuildingType(code, "L Whse 2", 3);
            }
            //  Miscellaneous, 3x3:
            //    C2-C3: Construction
            else if (code == 194)
            {
                return new BuildingType(code, "Constr 1", 3);
            }
            else if (code == 195)
            {
                return new BuildingType(code, "Constr 2", 3);
            }
            //    C4-C5: Abandoned building
            else if (code == 196)
            {
                return new BuildingType(code, "Aband 1", 3);
            }
            else if (code == 197)
            {
                return new BuildingType(code, "Aband 2", 3);
            }
            //  Power plants:
            //    C6-C7: Hydroelectric power (1x1)
            else if (code == 198)
            {
                return new BuildingType(code, "Hydro 1", 1);
            }
            else if (code == 199)
            {
                return new BuildingType(code, "Hydro 2", 1);
            }
            //    C8: Wind power (1x1)
            else if (code == 200)
            {
                return new BuildingType(code, "Wind", 1);
            }
            //    C9: Natural gas power plant (4x4)
            else if (code == 201)
            {
                return new BuildingType(code, "Gas", 4);
            }
            //    CA: Oil power plant (4x4)
            else if (code == 202)
            {
                return new BuildingType(code, "Oil", 4);
            }
            //    CB: Nuclear power plant (4x4)
            else if (code == 203)
            {
                return new BuildingType(code, "Nuclear", 4);
            }
            //    CC: Solar power plant (4x4)
            else if (code == 204)
            {
                return new BuildingType(code, "Solar", 4);
            }
            //    CD: Microwave power receiver (4x4)
            else if (code == 205)
            {
                return new BuildingType(code, "Microwa", 4);
            }
            //    CE: Fusion power plant (4x4)
            else if (code == 206)
            {
                return new BuildingType(code, "Fusion", 4);
            }
            //    CF: Coal power plant (4x4)
            else if (code == 207)
            {
                return new BuildingType(code, "Coal", 4);
            }
            //  City services:
            //    D0: City hall
            else if (code == 208)
            {
                return new BuildingType(code, "City Hall", 3);
            }
            //    D1: Hospital
            else if (code == 209)
            {
                return new BuildingType(code, "Hospital", 3);
            }
            //    D2: Police station
            else if (code == 210)
            {
                return new BuildingType(code, "Police", 3);
            }
            //    D3: Fire station
            else if (code == 211)
            {
                return new BuildingType(code, "Fire Dep", 3);
            }
            //    D4: Museum
            else if (code == 212)
            {
                return new BuildingType(code, "Museum", 3);
            }
            //    D5: Park (big)
            else if (code == 213)
            {
                return new BuildingType(code, "BigPark", 3);
            }
            //    D6: School
            else if (code == 214)
            {
                return new BuildingType(code, "School", 3);
            }
            //    D7: Stadium
            else if (code == 215)
            {
                return new BuildingType(code, "Stadium", 4);
            }
            //    D8: Prison
            else if (code == 216)
            {
                return new BuildingType(code, "Prison", 4);
            }
            //    D9: College
            else if (code == 217)
            {
                return new BuildingType(code, "College", 4);
            }
            //    DA: Zoo
            else if (code == 218)
            {
                return new BuildingType(code, "Zoo", 4);
            }
            //    DB: Statue
            else if (code == 219)
            {
                return new BuildingType(code, "Statue", 1);
            }
            //  Seaports, airports, transportation, military bases, and more city services:
            //    DC: Water pump
            else if (code == 220)
            {
                return new BuildingType(code, "Water P", 1);
            }
            //    DD-DE: Runway
            else if (code == 221)
            {
                return new BuildingType(code, "Runway 1", 1);
            }
            else if (code == 222)
            {
                return new BuildingType(code, "Runway 2", 1);
            }
            //    DF: Pier
            else if (code == 223)
            {
                return new BuildingType(code, "Pier", 1);
            }
            //    E0: Crane
            else if (code == 224)
            {
                return new BuildingType(code, "Crane", 1);
            }
            //    E1-E2: Control tower
            else if (code == 225)
            {
                return new BuildingType(code, "Civ Ctl T", 1);
            }
            else if (code == 226)
            {
                return new BuildingType(code, "Mil Ctl T", 1);
            }
            //    E3: Warehouse (for seaport)
            else if (code == 227)
            {
                return new BuildingType(code, "Warehouse", 1);
            }
            //    E4-E5: Building (for airport)
            else if (code == 228)
            {
                return new BuildingType(code, "Building 1", 1);
            }
            else if (code == 229)
            {
                return new BuildingType(code, "Building 2", 1);
            }
            //    E6: Tarmac
            else if (code == 230)
            {
                return new BuildingType(code, "Tarmac", 1);
            }
            //    E7: F-15b
            else if (code == 231)
            {
                return new BuildingType(code, "F-15b", 1);
            }
            //    E8: Hangar
            else if (code == 232)
            {
                return new BuildingType(code, "Hangar 1", 1);
            }
            //    E9: Subway station
            else if (code == 233)
            {
                return new BuildingType(code, "SubStat", 1);
            }
            //    EA: Radar
            else if (code == 234)
            {
                return new BuildingType(code, "Radar", 1);
            }
            //    EB: Water tower
            else if (code == 235)
            {
                return new BuildingType(code, "W Towe", 2);
            }
            //    EC: Bus station
            else if (code == 236)
            {
                return new BuildingType(code, "BusStat", 2);
            }
            //    ED: Rail station
            else if (code == 237)
            {
                return new BuildingType(code, "RailStat", 2);
            }
            //    EE-EF: Parking lot
            else if (code == 238)
            {
                return new BuildingType(code, "CivParki", 2);
            }
            else if (code == 239)
            {
                return new BuildingType(code, "MilParki", 2);
            }
            //    F0: Loading bay
            else if (code == 240)
            {
                return new BuildingType(code, "Load Ba", 2);
            }
            //    F1: Top secret
            else if (code == 241)
            {
                return new BuildingType(code, "Top Sec", 2);
            }
            //    F2: Cargo yard
            else if (code == 242)
            {
                return new BuildingType(code, "Cargo Y", 2);
            }
            //    F3: man (aka the mayor's house)
            else if (code == 243)
            {
                return new BuildingType(code, "Mayor H", 2);
            }
            //    F4: Water treatment plant
            else if (code == 244)
            {
                return new BuildingType(code, "Water T", 2);
            }
            //    F5: Library
            else if (code == 245)
            {
                return new BuildingType(code, "Library", 2);
            }
            //    F6: Hangar
            else if (code == 246)
            {
                return new BuildingType(code, "Hangar 2", 2);
            }
            //    F7: Church
            else if (code == 247)
            {
                return new BuildingType(code, "Church", 2);
            }
            //    F8: Marina
            else if (code == 248)
            {
                return new BuildingType(code, "Marina", 3);
            }
            //    F9: Missile silo
            else if (code == 249)
            {
                return new BuildingType(code, "Silo", 2);
            }
            //    FA: Desalination plant
            else if (code == 250)
            {
                return new BuildingType(code, "Desalin", 3);
            }
            //  Arcologies:
            //    FB: Plymouth arcology
            //    FC: Forest arcology
            //    FD: Darco arcology
            //    FE: Launch arcology
            else if (code >= 251 && code <= 254)
            {
                return new BuildingType(code, "Arco " + (code - 250), 4);
            }
            //  Braun Llama-dome:
            //    FF: Braun Llama-dome
            else if (code == 255)
            {
                return new BuildingType(code, "Dome", 4);
            }
            else
            {
                throw new Exception("Undefined code: " + code);
            }
        }

        public static bool isRotatable(byte code)
        {
            return isBridgePart(code) || code == 221 || code == 223;
        }

        // Returns true if other code is part of a bridge AND both squares are roads, rails or power lines
        public static bool isSameKindOfBridgePart(byte thisCode, byte otherCode)
        {
            if (isBridgePart(otherCode))
            {
                return isSameKindOfPart(thisCode, otherCode);
            }
            else
            {
                return false;
            }
        }

        // Returns true if both squares are roads, rails or power lines
        public static bool isSameKindOfPart(byte thisCode, byte otherCode)
        {
            return (isRoad(thisCode) && isRoad(otherCode)) || (isPowerline(thisCode) && isPowerline(otherCode)) || (isRail(thisCode) && isRail(otherCode));
        }

        // Returns true, if building is part of a road or a road bridge
        public static bool isRoad(byte code)
        {
            return (code >= 29 && code <= 43) || // roads
                (code >= 63 && code <= 66) || // tunnel entrances
                (code >= 67 && code <= 68) || // road-power crossings
                (code >= 69 && code <= 70) || // road-rail crossings
                (code >= 81 && code <= 89); // road bridges
        }

        // Returns true, if building is part of a power line or raised wires
        public static bool isPowerline(byte code)
        {
            return (code >= 14 && code <= 28) || // power lines
                (code >= 67 && code <= 68) || // road-power crossings
                (code >= 71 && code <= 72) || // rail-power crossings
                (code == 92); // raised wires
        }

        // Returns true, if building is part of a rail track or a rail bridge
        public static bool isRail(byte code)
        {
            return  (code >= 44 && code <= 62) || // rails
                (code >= 69 && code <= 70) || // road-rail crossings
                (code >= 71 && code <= 72) || // rail-power crossings
                (code >= 90 && code <= 91); // rail bridges
        }

        public static bool isBridgePart(byte code)
        {
            // 81-85 suspension bridge
            // 86-89 normal bridge, raising bridge
            // 90-91 rail bridge
            // 92 raised power lines
            return code >= 81 && code <= 92;
        }

        public static Boolean isSloped(byte code)
        {
            // 16-19: sloped power lines
            // 31-34: sloped roads
            // 46-49: sloped rails
            return (code >= 16 && code <= 19) || (code >= 31 && code <= 34) || (code >= 46 && code <= 49);

        }

        public static bool isSlopedToTop(byte code)
        {
            return code == 16 || code == 31 || code == 46;
        }

        public static bool isSlopedToRight(byte code)
        {
            return code == 17 || code == 32 || code == 47;
        }

        public static bool isSlopedToBottom(byte code)
        {
            return code == 18 || code == 33 || code == 48;
        }

        public static bool isSlopedToLeft(byte code)
        {
            return code == 19 || code == 34 || code == 49;
        }

        public static bool isTunnelEntrance(byte code)
        {
            return code >= 63 && code <= 66;
        }
    }
}
