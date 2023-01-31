# OpenHeroSelect GUI
GUI to modify the herostat in Marvel Ultimate Alliance based on MUAOpenHeroSelect by adamatti
The goal is to eventually change this GUI to use [OpenHeroSelect](https://github.com/TheRealPSV/OpenHeroSelect) and add features.
This is heavily influenced by HeroSelect by Norrin Radd.


## Original Source
 Developed by @adamatti 2008-2009
 https://code.google.com/archive/p/muaopenheroselect/
 Developed with [Visual Studio C# Express Edition(VSCEE)](https://visualstudio.microsoft.com/vs/express/) and .Net framework 2.0 (this version of Dotnet is outdated but the framework is included in Windows).
 With the motivation to be an alternative open source to everyone contribute (and more).


## Features
- Capability to config all using config files
- Create groups based on folder structure
- Just "replace" a character
- See better log messages
- Import/Export Selection
- Capability to index save games + roosters


## Usage Instructions (v. 0.0.3)

 Run "WFA_MUA.exe" and click a menulocation (white or black numbers). Then assign a character to that location by double-clicking on its name on the left list. If the number is black, it means that it is already assigned. Remove assigned heroes with the `Remove All` button (I couldn't get removing single characters working on Windows 11).
 Click `Run Marvel Ultimate Alliance` to build the herostat.engb. Note: this currently displays and error, if the Game.exe isn't in the location we defined. The error can be ignored (Continue).
 You can select save slots, but I haven't tested these. I'm not sure if they're connected to the save files or not.
 Other features will hopefully be changed, so I'm not mentioning these.

#### How can I add more characters?
 There is a folder called `chars` in the MUAOpenHeroSelect folder. You can create a new .txt file with the character's herostat (or simply copy and rename the herostat.txt file) and this character will be available in the program.
 You can use the reload button to update the list when MUA Open Hero Select is running.
 Examples:
 - Creating `chars/Ant-Man.txt` will make `Ant-Man` available without tree-structure.
 - Creating `chars/MCU/Ant-Man.txt` will make `Ant-Man` available with a tree-structure, in the `MCU` sub-menu.

#### How can I change the path to MUA?
 There is a file called "Config.ini" in "sys" folder. You can change paths to MUA and the herostat files in the `[mua]` section.
 - `path` changes the path to MUA
 - `chars` changes the path to the herostats (`chars` is the default)
 Paths can be absolute (eg. `C:\Program Files (x86)\Activision\Marvel - Ultimate Alliance`) or relative (eg. `herostats`). Relative paths are sub-folders to the executable (WFA_MUA.exe).


## Coding Instructions (v. 0.0.3)
- [Tree View](https://www.c-sharpcorner.com/article/treeview-control-in-C-Sharp/)
- [Read INI File? ](https://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C)
- [Work with Events](https://web.archive.org/web/20080215231303/http://www.csharphelp.com/archives/archive253.html)
- ["Order by" functions](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listview.sort?redirectedfrom=MSDN&view=windowsdesktop-7.0#System_Windows_Forms_ListView_Sort)


## Build Instructions
 I have no idea, sorry.
 It probably has something to do with [SVN](https://subversion.apache.org/) and/or [VSCEE](https://visualstudio.microsoft.com/vs/express/)


## Planned Features

#### Original Plans by adamatti
- Change items based on the roster (by modifying `data/items.engb`)
- Randomizer?
- (Make a tutorial and/or presentation video)

#### Updated Plans by ak2yny
- Remove files that are no longer required (.svn folders?) (should remove duplicates).
- Change herostat files to the OHS structure, possibly remove all herostat files, remove all OCP herostats for sure. (should remove duplicates).
- Change to use the OHS files (menulocation.cfg, roster.cfg, config.ini, and its subfolder structure).
- Change teams to ini files and add support for `data/team_bonus.engb` (should remove duplicates).
- Update/remove (some) wiki files (should remove duplicates).
- Add support for the unlocked characters and starting characters feature of OHS.
- Add support for Mod Organizer 2.
- Add support for custom stages, including support for a preview image and an ini file with their locations and layout coordinates for the GUI.
- Possibly add features that ak2yny's Stage & Herostat Helper has:
  - Effect duplication
  - EXE hex-editing (removal of effects in the F12 pause menu and changing the Spidey upside-down arrow)
  - etc.


## Changelog
 |19.09.2009|Beta 0.0.3: Implemented capability to index save slots and roosters and fixed issues|
 |02.09.2009|Beta 0.0.2: Implemented import/export |
 |31.08.2008|First version |