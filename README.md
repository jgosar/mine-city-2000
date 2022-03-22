# mine-city-2000
MineCity 2000 is a program that converts SimCity 2000 cities into Minecraft (Java Edition) worlds

From this:

![](https://raw.githubusercontent.com/jgosar/mine-city-2000/master/screens/mc2k-a.PNG)

Into this:

![](https://raw.githubusercontent.com/jgosar/mine-city-2000/master/screens/mc2k-b.PNG)

## Overview

The program works by reading a *.sc2 city file created by SimCity 2000. It then maps the terrain and places all the buildings into a Minecraft world.

See it in action:

https://www.youtube.com/watch?v=R6RW6WouGbE

https://www.youtube.com/watch?v=Z3FkM7GLxKo

## Opening generated worlds on the current versions of Minecraft

Unfortunately, since this project was started quite some time ago (2014), it only generates Minecraft Java Edition worlds.
I currently don't have the time to change the inner workings of the project in order to support Minecraft for Windows 10.
See the instructions for converting old worlds into new ones here: https://github.com/jgosar/mine-city-2000/wiki/How-to-open-generated-worlds-on-the-current-versions-of-Minecraft


## Projects

The repository contains these projects:
- SimCityReader: Reads information about the city map from a *.sc2 city file
- AnvilFile: Contains utility classes for working with Minecraft's Anvil file format
- MinecraftEditor: Contains classes for creating and editing a Minecraft world
- MineCity2000: Creates the Minecraft world with terrain and buildings
- MineCity2000-GUI: A rudimentary GUI for running the program

You need .NET Framework 4 in order for MineCity 2000 to run. I have checked that the project compiles and runs in Visual Studio 2010 and Visual Studio 2019.

This is a development version, so do not be surprised if something doesn't work.

At the current stage, not all buildings are supported, so there will be some holes between the buildings in a generated world.

Stuff that does NOT work yet:
- Subways
- Highways (mostly)
- Underwater pipes?
- ???

## Running the compiled program

Run the compiled program using MineCity2000.exe.

With the first browse button, choose your *.sc2 city file. A good place to look for it would be "C:\Program Files\Maxis\SimCity 2000\Cities". But if you can't find it, open it in SimCity 2000, click "File->Save city as" and put it in a place where you will find it.

With the second browse button, choose your Minecraft install directory. It might be "C:\Users\[username]\AppData\Roaming\.minecraft" or something similar. A sure way of finding it is if you right-click the shortcut you use to run Minecraft, click Properties and see where the shortcut points.

After you choose both of these, click "Convert!". And when the processing is done, you can close this window, open Minecraft and you should see your city among the saved games.

Your antivirus program might think that MineCity 2000 is a virus. It's not.

## If you would like to help

Let me know about any bugs or other problems you encounter.

If you know a community that might be interested in this project, tell them about it.

If you have the time, patience and skills, you can design a few more of the missing buildings. I have a very specific process that i follow to design them, but i am not going to describe it here unless somebody volunteers to help. 

## Changelog:

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
- Abandoned building 1
- Abandoned building 2
- Big Park
- Small Appartments 2
- Small Appartments 3
- Statue

## Contact

<minecity.2000@gmail.com>