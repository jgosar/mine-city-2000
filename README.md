# mine-city-2000
MineCity 2000 is a program that converts SimCity 2000 cities into Minecraft worlds

Original discussion topic:
http://www.minecraftforum.net/topic/2669597-making-a-simcity-2000-to-minecraft-converter-volunteers-needed

See it in action:
https://www.youtube.com/watch?v=R6RW6WouGbE
https://www.youtube.com/watch?v=Z3FkM7GLxKo

The repository contains these projects:
- SimCityReader: Reads information about the city map from a *.sc2 city file
- AnvilFile: Contains utility classes for working with Minecraft's Anvil file format
- MinecraftEditor: Creates the Minecraft world with terrain and buildings
- MineCity2000: A rudimentary GUI for running the program

First of all, you need .NET Framework 4 in order for MineCity 2000 to run.

This is a development version, so do not be surprised if something doesn't work.
If you come across any interesting bugs, tell me about them.

At the current stage, not all buildings are supported, so there will be some holes between the buildings in a generated world.

Stuff that does NOT work yet:
- Subways
- Highways
- Underwater pipes?
- ???


Run the program using MineCity2000.exe.

With the first browse button, choose your *.sc2 city file. A good place to look for it would be "C:\Program Files\Maxis\SimCity 2000\Cities". But if you can't find it, open it in SimCity 2000, click "File->Save city as" and put it in a place where you will find it.

With the second browse button, choose your Minecraft install directory. It might be "C:\Users\[username]\AppData\Roaming\.minecraft" or something similar. A sure way of finding it is if you right-click the shortcut you use to run Minecraft, click Properties and see where the shortcut points.

After you choose both of these, click "Convert!". And when the processing is done, you can close this window, open Minecraft and you should see your city among the saved games.


Your antivirus program might think that MineCity 2000 is a virus. It's not.


Changelog:
The minecraftforums version:
v0.1:
Buildings:
- Upper class homes 1-4
- Medium Condominiums 2
- Police Station
- Water Pump
- Middle class homes 5

Features:
- Power lines
- Roads
- Ground water
- Trees


v0.1-v0.2:
New buildings:
- Middle class homes 2
- Medium Condominiums 1
- Medium Condominiums 3
- Middle class homes 4
- Middle class homes 3
- Small warehouse 1
- Small warehouse 2
- Gas station 1
- Small office building 2
- Chemical storage
- Gas station 2
- Toy store
- Industrial substation
- Convinience store
- Small Factory 3
- Small Factory 4
- Small Factory 5
- Small Factory 6
- Medium office 5
- Medium office 6
- Medium Appartments 2

New features:
- Tunnels
- Railways
- The file selectors on the GUI automatically open at the most probable location for SimCity and Minecraft installs

Fixed bugs:
- Water level is read correctly from SimCity file
- Program does not crash if an object needs to be at a height greater than 255


v0.2-v0.3:
New buildings:
- Office/Retail
- Medium office 3
- Medium office 4
- B&B Inn
- Warehouse
- Small office 1
- Port warehouse
- Large warehouse 1
- Small Park
- Lower class homes 1
- Lower class homes 2
- Lower class homes 3
- Church
- Fire Department
- Large Factory
- Medium Factory
- School


The GitHub version:
- Refactored code

New buildings:
- Runway
- Pier
- Middle class homes 1
- Hospital
- Medium Appartments 1
- Small Appartments 1
- Corporate Headquarters
