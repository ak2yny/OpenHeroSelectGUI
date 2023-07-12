@echo off

set "MainDir=%cd%"

if exist Visuals goto RenameStageModels

for /l %%n in (1,1,4) do cd "%MainDir%" && call :FindTSC %%n
goto EOF

:FindTSC
cd "Team Stage Collection %1" 2>nul
if exist Visuals goto RenameStageModels
set /a i+=1
if %i%==4 set /a i=0 & EXIT /b
goto FindTSC

:RenameStageModel
set f=%1
set f=%f:m_team_stage_=%
mkdir "%MainDir%\TeamStageCollection\%f%" 2>nul
copy %3\%1%2 "%MainDir%\TeamStageCollection\%f%\m_team_stage%2" >nul
echo %f%
EXIT /b

:RenameStageModels
for %%p in (Visuals/*) do call :RenameStageModel %%~np  %%~xp Visuals
for %%p in (ui/models/*.igb) do call :RenameStageModel %%~np  %%~xp ui\models
EXIT /b

:EOF