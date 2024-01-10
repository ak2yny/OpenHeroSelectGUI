@echo off

REM Argument %1 is version number

set z=OHSGUI-%1
set zf=OHSGUI-%1-full

del %z%.7z %zf%.7z 2>nul
rmdir /q /s OHSGUI 2>nul
for /f "delims=" %%f in ('dir /a-d /b /s mua\*, xml2\* 2^>nul ^| find /i /v "team_bonus.engb.xml"') do del %%f
for /f "delims=" %%d in ('dir /ad /b /s mua\*, xml2\* 2^>nul') do rd %%d
mkdir OHSGUI

for %%a in (
 OpenHeroSelectGUI.exe
 *.dll
 *.json
 *.pri
 Assets
 en-us
 Microsoft.UI.Xaml
) do move %%a OHSGUI\
(
 echo ;!@Install@!UTF-8!
 echo Title="OpenHeroSelectGUI v%1"
 echo ExtractPathText="Please enter the OHS installation path:"
 echo ExtractPathTitle="OpenHeroSelectGUI v%1"
 echo ExtractTitle="Extracting..."
 echo GUIFlags="128"
 echo InstallPath="%%UserProfile%%\Desktop\OpenHeroSelect"
 echo OverwriteMode="0"
 echo RunProgram="MkLink.bat"
 echo ;!@InstallEnd@!
) >InstallerFiles\config.txt
(
 echo mklink OpenHeroSelectGUI .\OHSGUI\OpenHeroSelectGUI.exe
 echo del MkLink.bat
) >MkLink.bat

call :BuildInstaller %z%

for %%a in (
 createdump.exe
 OpenHeroSelectGUI.pdb
 *.winmd
) do move %%a OHSGUI\
for /d %%a in (*-*) do move %%a OHSGUI\

call :BuildInstaller %zf%

del MkLink.bat

EXIT

:BuildInstaller
InstallerFiles\7z.exe a -t7z %1.7z OHSGUI/ stages/ mua/ xml2/ MkLink.bat -xr!"stages/.models/Super Team Stage Marvel Mods" -xr!"stages/.models/TeamStageCollection"
copy /b InstallerFiles\7zSD.sfx + InstallerFiles\config.txt + %1.7z %1.exe
EXIT /b
