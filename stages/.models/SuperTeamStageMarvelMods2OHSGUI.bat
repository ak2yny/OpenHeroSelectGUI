@echo off

set "MainDir=%cd%"

if exist m_team_stage0.IGB goto RenameStageModels

for /l %%n in (1,1,2) do cd "%MainDir%" && call :FindTSC %%n
goto EOF

:FindTSC
if %1==1 (
  set sts=Super Team Stage Marvel Mods
) else (
  set sts=SUPER TEAM STAGE\ui\models\
)
cd "%sts%" 2>nul
if exist m_team_stage1.IGB goto RenameStageModels
if %1==2 EXIT /b
set /a i+=1
if %i%==4 set /a i=0 & EXIT /b
goto FindTSC

:RenameStageModel
if not exist m_team_stage%2.IGB EXIT /b
set stf="%MainDir%\Super Team Stage Marvel Mods\%1\%2"
mkdir %stf% 2>nul
copy %2.png %stf% >nul
copy m_team_stage%2.IGB %stf% >nul
echo %1.%2
EXIT /b

:RenameStageModels
for /l %%n in (0,1,9) do call :RenameStageModel %1 %%n
EXIT /b

:EOF