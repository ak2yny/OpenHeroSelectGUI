# OpenHeroSelect GUI
 GUI to modify the herostat in Marvel Ultimate Alliance based on MUAOpenHeroSelect by adamatti.
 
 The goal is to eventually use more of [OpenHeroSelect's](https://github.com/TheRealPSV/OpenHeroSelect) features.
 
 With the motivation to be open source for everyone to contribute.
 
# [Download](https://github.com/ak2yny/OpenHeroSelectGUI/releases/latest)
<br/><br/>

## Table of contents:

* [Credits](#credits)
* [Requirements](#requirements)
* [Features](#features)
* [Usage Instructions](#usage-instructions-v-010)
  * [How can I modify rosters for the 50 roster hack?](#how-can-i-modify-rosters-for-the-50-roster-hack)
  * [How can I add more characters?](#how-can-i-add-more-characters)
  * [How can I change the path to MUA?](#how-can-i-change-the-path-to-mua)
  * [What are Save Slots?](#what-are-save-slots)
* [Coding Instructions (v. 0.0.3)](#coding-instructions-v-003)
* [Build Instructions](#build-instructions)
* [Planned Features](#planned-features)
* [Changelog](#changelog)
<br/><br/>

## Credits
- Inspired by [HeroSelect](http://marvelmods.com/forum/index.php?topic=732) from Norrin Radd
- [Original Source](https://code.google.com/archive/p/muaopenheroselect/) by [@adamatti](https://github.com/adamatti) 2008-2009
<br/><br/>

## Requirements
- .Net Framework 3.5. [(Windows 7+)](https://learn.microsoft.com/en-us/dotnet/framework/install/dotnet-35-windows) [(Earlier Windows)](https://www.microsoft.com/en-us/download/details.aspx?id=21)
- [OpenHeroSelect](https://github.com/TheRealPSV/OpenHeroSelect) and its requirements
<br/><br/>

## Features
- Support for OHS configurations and herostats (settings controls are planned)
- Support for XML2 and MUA (both still limited)
- Supports infinte characters
  (manual location setups for non-OCP CSS required - better support is planned)
- Create teams based on folder structure
- Just "replace" a character
- Log messages
- Save and load selection
- Capability to select saves to free-up for the selected roster (currently buggy)
<br/><br/>

## Usage Instructions (v. 0.1.0)

 "OpenHeroSelectGUI.exe" and "Newtonsoft.JSON.dll" must be extracted to the same location as "OpenHeroSelect.exe".

 Run "OpenHeroSelectGUI.exe" and click a menulocation (white or black numbers). Then assign a character to that location by double-clicking on its name on the left list. If the number is black, it means that it is already assigned. Remove assigned heroes with the `Remove All` button or by selecting them in the list and pressing the Delete key.
 For XML2, just double-click on characters on the left list. If you want to add more than 21 characters, you must manually enter a higher number in the "Current Position" field.
 
 Click `Run Open Hero Select` to run OHS and let it build herostat.engb and other files.
 
#### How can I modify rosters for the 50 roster hack?
 To add different locations (for 50RH etc.), type the number in the "Current Position" field and double-click on the hero you want to place there.

#### How can I add more characters?
 Place the herostats in the herostatFolder (by default `mua/xml` for MUA). Rename the herostat. All files in this folder and its sub-folders will be available in the program.
 
 You can use the reload button to update the list when Open Hero Select GUI is running.
 
 Examples:
 - Creating `xml/Ant-Man.txt` will make `Ant-Man` available without tree-structure.
 - Creating `xml/MCU/Ant-Man.txt` will make `Ant-Man` available with a tree-structure, in the `MCU` sub-menu.

#### How can I change the path to MUA/XML2?
 Use the OpenHeroSelect.exe set-up dialogue. Paste the path with right-click, when prompted.
 
#### What are Save Slots?
 They're supposed to create and restore save backups, so you have free saves for your new roster. I haven't tested them, though. The feature only becomes available, if OHS is set-up to run the game.
<br/><br/>

## Coding Instructions (v. 0.0.3)
- [Tree View](https://www.c-sharpcorner.com/article/treeview-control-in-C-Sharp/)
- [Read INI Files](https://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C)
- [Work with Events](https://web.archive.org/web/20080215231303/http://www.csharphelp.com/archives/archive253.html)
- ["Order by" functions](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listview.sort?redirectedfrom=MSDN&view=windowsdesktop-7.0#System_Windows_Forms_ListView_Sort)
<br/><br/>

## Build Instructions
- Currently using .NET Framework 3.5. I recommend to use Visual Studio and install it with its installer dialogue. It may be possible to re-target the project.
- Requires Newtonsoft.JOSN. The DLL file can be added to the built project.
- Use MSBuild.
- The language is C# (.cs files).
- Originally developed with [Visual Studio C# Express Edition(VSCEE)](https://visualstudio.microsoft.com/vs/express/) and .Net framework 2.0.
<br/><br/>

## Planned Features

#### Original Plans by adamatti
- Change items based on the roster (by modifying `data/items.engb`)
- Randomizer?
- (Make a tutorial and/or presentation video)

#### Updated Plans by ak2yny
- ~~Change teams to ini files and~~ add support for `data/team_bonus.engb`
- Add support for the unlocked characters and starting characters feature of OHS.
- Add support for Mod Organizer 2.
- Add support for custom stages, including support for a preview image and an ini file with their locations and layout coordinates for the GUI.
- Add support for custom order (currently only supports sorted by menulocation and character name ascending and descending).
- Change to different (modern?) schemes. The current one has icon style menus, but no icons defined, which doesn't look good. There are other issues as well.
- Possibly add features that ak2yny's Stage & Herostat Helper has:
  - Effect duplication
  - EXE hex-editing (removal of effects in the F12 pause menu and changing the Spidey upside-down arrow)
  - etc.
<br/><br/>

## Changelog

 |15.02.2023|Beta 0.1.1: Added support for XML2
 - Different icon
 - A few bugs fixed

 |09.02.2023|Beta 0.1.0: Changed from xmlb-compile to OpenHeroSelect
 - Remove files that are no longer required (.svn folders)
 - A few touch ups

 |19.09.2009|Beta 0.0.3: Implemented capability to index save slots and roosters and fixed issues
 
 |02.09.2009|Beta 0.0.2: Implemented import/export
 
 |31.08.2008|First version
