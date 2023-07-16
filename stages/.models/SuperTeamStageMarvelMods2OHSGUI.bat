@echo off

set "MainDir=%cd%"

if exist m_team_stage0.IGB goto RenameStageModels

for /l %%n in (1,1,2) do cd "%MainDir%" && call :FindTSC %%n
goto EOF

:FindTSC
cd "Super Team Stage Marvel Mods" 2>nul
if exist m_team_stage0.IGB goto RenameStageModels
set /a i+=1
if %i%==4 set /a i=0 & EXIT /b
goto FindTSC

:RenameStageModel
mkdir "%MainDir%\Super Team Stage Marvel Mods\%1" 2>nul
copy %1.png "%MainDir%\Super Team Stage Marvel Mods\%1\" >nul
copy m_team_stage%1.IGB "%MainDir%\Super Team Stage Marvel Mods\%1\" >nul
echo %1
EXIT /b

:RenameStageModels
for /l %%n in (0,1,9) do call :RenameStageModel %%n
EXIT /b

:EOF