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

https://www.youtube.com/watch?v=EhIF_1SuZLs

## Download link for the compiled app

This is a link to the compiled app, so you don't have to build it from the source code yourself:
https://raw.githubusercontent.com/jgosar/mine-city-2000/master/release/mc2k-release.rar

You can download a smaller version here if you only want to use MineCity 2000 CLI (Command-line interface)
https://raw.githubusercontent.com/jgosar/mine-city-2000/master/release/mc2k-release.rar

If for some reason the app above doesn't work, please try the old version:
https://raw.githubusercontent.com/jgosar/mine-city-2000/master/release/mc2k-release-legacy.rar

## Running the compiled app

Run the compiled app using MineCity2000-GUI.exe.

With the first browse button, choose your *.sc2 city file. A good place to look for it would be `C:\Program Files\Maxis\SimCity 2000\Cities`. But if you can't find it, open it in SimCity 2000, click "File->Save city as" and put it in a place where you will find it.

With the second browse button, choose your Minecraft install directory. It might be `C:\Users\[username]\AppData\Roaming\.minecraft` or something similar. A sure way of finding it is if you right-click the shortcut you use to run Minecraft, click Properties and see where the shortcut points.

If you want the underground area to be filled instead of empty (So you don't fall into the ground after digging for a bit), select the "Fill underground area" checkbox. But be warned that this also increases the conversion time and RAM requirements.

If you want Minecraft to automatically generate its own terrain around the city instead of having an obsidian wall, select the "Generate terrain around the city" option.

After you choose both of these, click "Convert!". And when the processing is done, you can close this window, open Minecraft and you should see your city among the saved games.

Your antivirus might think that MineCity 2000 is a virus. It's not.

## Running the CLI (Command-line interface)

You can also run MineCity 2000 as a CLI, which can be useful if you want to call it from a script or from another app, here are the instructions: https://github.com/jgosar/mine-city-2000/wiki/CLI-usage


## Opening generated worlds in Minecraft Bedrock Edition

This project was started quite some time ago (2014), so it only generates Minecraft Java Edition worlds.

But fortunately Microsoft has released a converter that can transform Minecraft Java Edition worlds to Bedrock Edition, it's accessible here: https://chunker.app/.
I have tested it with a world that was generated by MineCity 2000 and it works almost perfectly.
The only difference is that you can't fly around as fast, because that was my custom setting in the generated Java Edition world.
But as you can see in the screenshot below, the draw distance in Bedrock Edition can be much much greater (Click for larger images):

<a target="_blank" rel="noopener noreferrer nofollow" href="https://user-images.githubusercontent.com/36840705/206261457-3b6266d3-07da-4b7a-b6c6-0c6c4264a4aa.png"><img src="https://user-images.githubusercontent.com/36840705/206261457-3b6266d3-07da-4b7a-b6c6-0c6c4264a4aa.png" alt="image" width=45%></a>
<a target="_blank" rel="noopener noreferrer nofollow" href="https://user-images.githubusercontent.com/36840705/206260351-4b5b4f91-d983-4505-ab00-50c6c76d537a.jpg"><img src="https://user-images.githubusercontent.com/36840705/206260351-4b5b4f91-d983-4505-ab00-50c6c76d537a.jpg" alt="mc2k-bedrock-4K" width=45%></a>

Here's a video from this city in Minecraft Bedrock Edition: https://youtu.be/w_fU_Cy6xCA


## Projects

The repository contains these projects:
- SimCityReader: Reads information about the city map from a *.sc2 city file
- AnvilFile: Contains utility classes for working with Minecraft's Anvil file format
- MinecraftEditor: Contains classes for creating and editing a Minecraft world
- MineCity2000: Creates the Minecraft world with terrain and buildings
- MineCity2000-GUI: A rudimentary GUI for running the program

For building the app from the source code you need .NET Core framework 8.0. The projects are set up for development with Visual Studio Code.
In order to develop the GUI, you will also need the dotnet MAUI workload.
If you open the project with Visual Studio Code, you can launch it using the "Launch GUI" or "Launch CLI" commands under the "Run and Debug" menu.

This is a development version, so do not be surprised if something doesn't work. Please open an issue on GitHub if you encounter any problems.

At the current stage, not all buildings are supported, so there will be some holes between the buildings in a generated world.

Stuff that does NOT work yet:
- Subways
- Highways (mostly)
- Underwater pipes?
- ???

## If you would like to help

Let me know about any bugs or other problems you encounter.

If you know a community that might be interested in this project, tell them about it.

If you have the time, patience and skills, you can design a few more of the missing buildings. The instructions are available here: https://github.com/jgosar/mine-city-2000/wiki/How-to-design-and-add-new-buildings-to-the-project. Please let me know if anything is unclear.

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
