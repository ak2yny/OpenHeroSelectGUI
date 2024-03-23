# OpenHeroSelect GUI
 GUI to modify the herostat in Marvel Ultimate Alliance based on MUAOpenHeroSelect by adamatti, now using the similarly named [OpenHeroSelect](https://github.com/TheRealPSV/OpenHeroSelect) as the compiler.
 
 With the motivation to be open source for everyone to contribute.
 
# [Download](https://github.com/ak2yny/OpenHeroSelectGUI/releases/latest)

## Table of contents:

* [Credits](#credits)
* [Requirements](#requirements)
* [Features](#features)
* [Usage Instructions](#usage-instructions-v-010)
  * [How can I add more characters?](#how-can-i-add-more-characters)
  * [How can I change the path to MUA/XML2/MO2?](#how-can-i-change-the-path-to-muaxml2mo2)
  * [What are Save Slots?](#what-are-save-slots)
* [Coding Instructions](#coding-instructions)
* [Build Instructions](#build-instructions)
* [Planned Features](#planned-features)
* [Changelog](#changelog)
<br/><br/>

## Credits
- Inspired by [HeroSelect](http://marvelmods.com/forum/index.php?topic=732) from Norrin Radd
- Based on [MUAOpenHeroSelect](https://code.google.com/archive/p/muaopenheroselect/) by [@adamatti](https://github.com/adamatti) 2008-2009
- Using [7-zip](https://7-zip.org/) by Igor Pavlov & contributors (open source under the GNU LGPL licence, but source not included)
- Using code samples from helpful developers on sites like StackOverflow
- Using a lot of samples from the WinUI and Windows App SDK tutorials from [Microsoft](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- Using a few assets from X-Men Legends and Ultimate Alliance by Raven Software (Activision, Marvel)
- Using the MarvelMods logo by Outsider, based on Marvel's classic comic logo.

 Stage Creators:
- Outsider
- Emanuel(MUAXP)
- Nuhverah
- Tony Stark aka Hyperman360
- Overload
- UltraMegaMagnus (models & layouts)
- nikita488 (layouts)
- harpua1982 (33RH model)
- ak2yny (layouts & previews)

 Effects research:
- Bobooooo
- Norrin Radd
- ak2yny

 Thanks to testers:
- Nuhverah
- [@butsukdanila](https://github.com/butsukdanila)
<br/><br/>

## Requirements
- [OpenHeroSelect](https://github.com/TheRealPSV/OpenHeroSelect) and its requirements (Windows 10+)
<br/><br/>

## Features
- Support for OHS configurations and herostats
- Support for XML2 and MUA
- Supports custom stages, including preview images and a config file with their locations and layout coordinates for the GUI
- Supports effect duplication for MUA mannequins
- Supports infinte characters
  (setup the locations in the stage layouts)
- Support for the unlocked and starting characters feature of OHS
- Support for custom order
- Just "replace" a character
- Create teams based on folder structure
- Modify team bonuses through team_bonus directly in the GUI
- Skin editor and installer
- Install herostats and mods through drag & drop or with a browse button
- Automatic and manual roster saving and restoring
- Load the default roster or create a random roster
- Free-up saves for the selected roster
- Change the location of the upside-down arrow in the CSS
- Supports Mod Organizer 2
- Notifies character number clashes and provides a button to fix
- Modern UI with support for Windows 11's dark mode and Windows 10+'s accent colours
- Information bars with success and fail messages
<br/><br/>

## Usage Instructions

 [Full installation and usage instructions](https://github.com/ak2yny/OpenHeroSelectGUI/wiki/Instructions)

 Run OpenHeroSelectGUI. Navigate to the games and settings on the left pane (navigation view). For MUA, select a character and a menulocation to assign it or drag the the character on a menulocation or the selected list (and drop it). For XML2, drag the character on the selected list or double-click on a character to add it at the end. Alternatively, populate the selected list with a random, default, or custom roster loaded from a file. Remove assigned heroes with the `Clear` button or by selecting them in the list and pressing the Delete key.
 Click `Run OHS` to run OHS and let it build herostat.engb and other files.
 
 Use the controls to adjust the settings for OHS and the GUI.

#### How can I add more characters?
 Place the herostats in the herostatFolder (by default `mua/xml` for MUA).
 
 Find detailed instructions [here](https://github.com/ak2yny/OpenHeroSelectGUI/wiki/Instructions#add-characters).

#### How can I change the path to MUA/XML2/MO2?
 The path can be browsed in the settings tab.
 Or use the OpenHeroSelect.exe set-up dialogue and paste the path with right-click, when prompted.
<br/><br/>

## Coding Instructions
- [WinUI3 projects with Windows App SDK (non-UWP)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/create-your-first-winui3-app)
- The language is C# and Xaml/XML.
<br/><br/>

## Build Instructions
- Use Visual Studio and install .Net with its installer dialogue (.Net 8). Install Windows App SDK in the same dialogue (it's not possible to build a WinUI project with another application).
- Make sure to add the dependencies before building, as always.
- I recommend to leave the project as self contained (no dependencies), since WinUI is contained anyway.
- A Windows App SDK can be built [unpackaged](https://github.com/microsoft/WindowsAppSDK-Samples/tree/f1a30c2524c785739fee842d02a1ea15c1362f8f/Samples/SelfContainedDeployment/cs-winui-unpackaged) or [packaged](https://github.com/microsoft/WindowsAppSDK-Samples/tree/f1a30c2524c785739fee842d02a1ea15c1362f8f/Samples/SelfContainedDeployment/cs-winui-packaged) (MSIX). As a WinUI3 project, it can be easily re-targeted to a UWP project, but it lacks permission (file access, running OHS) and signature details.
- Theoretically, the project can be made cross platform through [.Net MAUI](https://dotnet.microsoft.com/en-us/apps/maui) or other platforms, like [UNO](https://platform.uno/), but OHS is currently Windows 10+ exclusive.
<br/><br/>

## Planned Features

#### Original Plans by adamatti
- Change items based on the roster (by modifying `data/items.engb`)
- (Make a tutorial and/or presentation video)

#### Updated Plans by ak2yny
- [Planned Features #14](https://github.com/user/repo/issues/14)
<br/><br/>

## Changelog

 |22.03.2024|1.0.3: Added clash resolver, fixed bugs

 |12.03.2024|1.0.2: Fixed bugs

 |07.03.2024|1.0.0: Install mods and herostats from archives

 |30.01.2024|Beta 0.3.2: Fixed bugs, New settings layout

 |15.01.2024|Beta 0.3.1: Fixed bugs

 |10.01.2023|Beta 0.3.0: New features, few bugs fixed, better messages
 - Added stage selection with thumbnails
 - Added team bonus editor
 - Added clash notifications

 |01.08.2023|Beta 0.2.5: Added installer, fixed bugs

 |30.07.2023|Beta 0.2.4: Fixed bugs, added effects and skin installer

 |14.07.2023|Beta 0.2.3: Fixed bugs (file picker, skin editor), added stages

 |14.07.2023|Beta 0.2.2: Added Skin Editor

 |07.12.2023|Beta 0.2.1: Better banner, fixed bugs

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

 |19.09.2009|Beta 0.0.3: Implemented capability to index save slots and roosters and fixed issues|by adamatti
 
 |02.09.2009|Beta 0.0.2: Implemented import/export|by adamatti
 
 |31.08.2008|First version|by adamatti
