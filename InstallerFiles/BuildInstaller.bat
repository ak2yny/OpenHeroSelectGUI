@echo off

REM Argument %1 is version number

set z=OHSGUI-%1
set zf=OHSGUI-%1-full

REM clean the publish folder
del %z%.7z %zf%.7z 2>nul
rmdir /q /s OHSGUI_new 2>nul
for /f "delims=" %%f in ('dir /a-d /b /s mua\*, xml2\* 2^>nul ^| find /i /v "team_bonus.engb.xml"') do del %%f
for /f "delims=" %%d in ('dir /ad /b /s mua\*, xml2\* 2^>nul') do rd %%d
mkdir OHSGUI_new

REM Add required files and make build
(
 echo rmdir /q /s OHSGUI
 echo ren OHSGUI_new OHSGUI
 echo mklink OpenHeroSelectGUI .\OHSGUI\OpenHeroSelectGUI.exe
 echo del MkLink.bat
) >MkLink.bat

for %%a in (
 OpenHeroSelectGUI.exe
 7z.exe
 *.dll
 *.json
 *.pri
 Assets
 en-us
 Microsoft.UI.Xaml
) do move %%a OHSGUI_new\
call :WriteCfg %1 "Please enter the OHS installation path:" >InstallerFiles\config.txt

call :BuildInstaller %z%

REM Add remaining GUI files and make full build with OHS
for %%a in (
 createdump.exe
 RestartAgent.exe
 OpenHeroSelectGUI.pdb
 *.winmd
) do move %%a OHSGUI_new\
for /d %%a in (*-*) do move %%a OHSGUI_new\

Powershell "$client = new-object System.Net.WebClient; $client.DownloadFile('https://github.com/TheRealPSV/OpenHeroSelect/releases/latest/download/OpenHeroSelect-32.7z','%CD%\OpenHeroSelect.7z')"
InstallerFiles\7z.exe x OpenHeroSelect.7z
for /d %%a in (OpenHeroSelect*) do robocopy "%%~fa" "%CD%" /S /XF "Source Code.txt" /MOVE & rd /s /q "%%~fa"
call :WriteCfg %1 "Please select a folder to install OHS and the GUI to:" >InstallerFiles\config.txt

call :BuildInstaller %zf% "help_files/ json2xmlb.exe OpenHeroSelect.exe LICENSE.txt"

del MkLink.bat

EXIT

:BuildInstaller
InstallerFiles\7z.exe a -t7z %1.7z OHSGUI_new/ stages/ mua/ xml2/ MkLink.bat %~2 -xr!"stages/.models/Super Team Stage Marvel Mods" -xr!"stages/.models/TeamStageCollection"
copy /b InstallerFiles\7zSD.sfx + InstallerFiles\config.txt + %1.7z %1.exe
EXIT /b

:WriteCfg
echo ;!@Install@!UTF-8!
echo Title="OpenHeroSelectGUI v%1"
echo ExtractPathText=%2
echo ExtractPathTitle="OpenHeroSelectGUI v%1"
echo ExtractTitle="Extracting..."
echo GUIFlags="128"
echo InstallPath="%%UserProfile%%\Desktop\OpenHeroSelect"
echo OverwriteMode="1"
echo RunProgram="MkLink.bat"
echo ;!@InstallEnd@!
EXIT /b
