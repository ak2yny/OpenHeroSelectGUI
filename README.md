# OpenHeroSelect GUI
 GUI to modify the herostat in Marvel Ultimate Alliance based on MUAOpenHeroSelect by adamatti, now using the similarly named [OpenHeroSelect](https://github.com/TheRealPSV/OpenHeroSelect) as the compiler.
 
 With the motivation to be open source for everyone to contribute.
 
# [Download](https://github.com/ak2yny/OpenHeroSelectGUI/releases/latest)
Note: The source code has drastically changed, but it's not ready for release yet. The readme does not reflect the latest release at the moment.
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
- Based on [MUAOpenHeroSelect](https://code.google.com/archive/p/muaopenheroselect/) by [@adamatti](https://github.com/adamatti) 2008-2009
- Using code samples from helpful developers on sites like StackOverflow
- Using a lot of samples from the WinUI and Windows App SDK tutorials from [Microsoft](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- Using a few assets from X-Men Legends and Ultimate Alliance by Activision ( & Marvel)
- Using the MarvelMods logo by Outsider, based on Marvel's classic comic logo.
<br/><br/>

## Requirements
- [OpenHeroSelect](https://github.com/TheRealPSV/OpenHeroSelect) and its requirements (Windows 10+)
Version 0.1.1 and earlier:
- .Net Framework 3.5. [(Windows 7+)](https://learn.microsoft.com/en-us/dotnet/framework/install/dotnet-35-windows) [(Earlier Windows)](https://www.microsoft.com/en-us/download/details.aspx?id=21)
<br/><br/>

## Features
- Support for OHS configurations and herostats
- Support for XML2 and MUA
- Supports custom stages, including preview images and a config file with their locations and layout coordinates for the GUI.
- Supports infinte characters
  (setup the locations in the stage layouts)
- Support for the unlocked characters and starting characters feature of OHS.
- Create teams based on folder structure
- Just "replace" a character
- Install herostats through drag & drop.
- Save and load selection
- Create a random roster
- Free-up saves for the selected roster
- Modern UI with support for Windows 11's dark mode and Windows 10+'s accent colours.

 Version 0.1:
- Log messages
<br/><br/>

## Usage Instructions

 The archive's content must be extracted to the same location as "OpenHeroSelect.exe".

 Run "OpenHeroSelectGUI.exe". Navigate to the games and settings on the left pane (navigation view). For MUA, select a character and a menulocation to assign it or drag the the character on a menulocation or the selected list (and drop it). For XML2, drag the character on the selected list (more methods will be added in the future). Alternatively, populate the selected list with a random, default, or custom roster loaded from a file. Remove assigned heroes with the `Clear` button or by selecting them in the list and pressing the Delete key.
 Click `Run OHS` to run OHS and let it build herostat.engb and other files.
 
 Use the controls to adjust the settings for OHS and the GUI.
 
 
 Version 0.1:
 Assign a character to the menulocation by double-clicking on its name on the left list. If the number is black, it means that it is already assigned. Remove assigned heroes with the `Remove All` button or by selecting them in the list and pressing the Delete key.
 For XML2, just double-click on characters on the left list. If you want to add more than 21 characters, you must manually enter a higher number in the "Current Position" field.
 Click `Run Open Hero Select` to run OHS and let it build herostat.engb and other files.
 To add different locations (for 50RH etc.), type the number in the "Current Position" field and double-click on the hero you want to place there.

#### How can I add more characters?
 Place the herostats in the herostatFolder (by default `mua/xml` for MUA). Rename the herostat. All files in this folder and its sub-folders will be available in the program.
 
 You can use the reload button to update the list when Open Hero Select GUI is running.
 
 Alternatively, drag & drop a herostat (e.g. herostat.txt) on the available character list (tree view).
 
 Examples:
 - Creating `xml/Ant-Man.txt` will make `Ant-Man` available without tree-structure.
 - Creating `xml/MCU/Ant-Man.txt` will make `Ant-Man` available with a tree-structure, in the `MCU` sub-menu.

#### How can I change the path to MUA/XML2?
 The path can be browsed in the settings tab.
 Or use the OpenHeroSelect.exe set-up dialogue. Paste the path with right-click, when prompted.
 
#### What are Save Slots?
 Version 0.1:
 They're supposed to create and restore save backups, so you have free saves for your new roster. I haven't tested them, though. The feature only becomes available, if OHS is set-up to run the game.
<br/><br/>

## Coding Instructions (v. 0.0.3)
- [WinUI3 projects with Windows App SDK (non-UWP)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/create-your-first-winui3-app)
- The language is C# and Xaml.

 Version 0.1:
- [Tree View](https://www.c-sharpcorner.com/article/treeview-control-in-C-Sharp/)
- [Read INI Files](https://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C)
- [Work with Events](https://web.archive.org/web/20080215231303/http://www.csharphelp.com/archives/archive253.html)
- ["Order by" functions](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listview.sort?redirectedfrom=MSDN&view=windowsdesktop-7.0#System_Windows_Forms_ListView_Sort)
<br/><br/>

## Build Instructions
- Use Visual Studio and install .Net with its installer dialogue (I used .Net 6, Framework 3.5 in V0.1). Install Windows App SDK in the same dialogue (it's not possible to build a WinUI project with another application).
- Make sure to add the dependencies before building, as always.
- I recommend to leave the project as self contained (no dependencies), since WinUI is contained anyway.
- A Windows App SDK can be built unpackaged or packaged (MSIX). As a WinUI3 project, it can be easily re-targeted to a UWP project, but it lacks permission (file access, running OHS) and signature details.
- Theoretically the project can be made cross platform through .Net MAUI or the UNO platform, but OHS is currently Windows 10+ exclusive.
 
 Version 0.1:
- Requires Newtonsoft.JOSN. The DLL file can be added to the built project.
- Use MSBuild.
- Originally developed with [Visual Studio C# Express Edition(VSCEE)](https://visualstudio.microsoft.com/vs/express/) and .Net framework 2.0.
<br/><br/>

## Planned Features

#### Original Plans by adamatti
- Change items based on the roster (by modifying `data/items.engb`)
- (Make a tutorial and/or presentation video)

#### Updated Plans by ak2yny
- ~~Change teams to ini files and~~ add support for `data/team_bonus.engb` in XML format, using json2xmlb.exe to compile.
- Add support for custom order (currently only supports sorted by menulocation and character name ascending and descending).
- Add function to install herostats with a browse button. If possible add function to install from archive.
- Add support for Mod Organizer 2.
- Add more installer functions for skins and other mods.
- Add features that ak2yny's Stage & Herostat Helper has:
  - Effect duplication
  - EXE hex-editing (removal of effects in the F12 pause menu and changing the Spidey upside-down arrow)
  - etc.
<br/><br/>

## Changelog

 |07.07.2023|Alpha 0.2.0: New UI
 - Complete overhaul, re-creating the project using the WinUI3 in a Windows App SDK project
 - New functions: Drag & drop, Stage selection for MUA, controls for OHS settings, etc.
 - Use of OHS's latest additions, such as unlock characters.

 |15.02.2023|Beta 0.1.1: Added support for XML2
 - Different icon
 - A few bugs fixed

 |09.02.2023|Beta 0.1.0: Changed from xmlb-compile to OpenHeroSelect
 - Remove files that are no longer required (.svn folders)
 - A few touch ups

 |19.09.2009|Beta 0.0.3: Implemented capability to index save slots and roosters and fixed issues
 
 |02.09.2009|Beta 0.0.2: Implemented import/export
 
 |31.08.2008|First version
