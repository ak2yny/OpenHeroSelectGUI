@echo off

cd cs_source\bin\Release\net6.0-windows10.0.22000.0\win10-x86\publish

for /f "tokens=2 delims=/" %%v in ('findstr /i "OpenHeroSelectGUI/" ^<OpenHeroSelectGUI.deps.json') do set version=%%v
set version=%version:~,-4%

del OHSGUI-%version%.7z OHSGUI-%version%-full.7z

"C:\Program Files\7-Zip\7z.exe" a -t7z OHSGUI-%version%-full.7z * -x!*.7z -x!*.bat -xr!"stages/.models/Super Team Stage Marvel Mods" -xr!"stages/.models/TeamStageCollection"

"C:\Program Files\7-Zip\7z.exe" a -t7z OHSGUI-%version%.7z * ^
 -x!*.7z^
 -x!*.bat^
 -x!createdump.exe^
 -x!*.pdb^
 -x!*.winmd^
 -x!WindowsAppRuntime.png^
 -xr!af-ZA^
 -xr!ar-SA^
 -xr!az-Latn-AZ^
 -xr!bg-BG^
 -xr!bs-Latn-BA^
 -xr!ca-ES^
 -xr!cs-CZ^
 -xr!cy-GB^
 -xr!da-DK^
 -xr!de-DE^
 -xr!el-GR^
 -xr!en-GB^
 -xr!es-ES^
 -xr!es-MX^
 -xr!et-EE^
 -xr!eu-ES^
 -xr!fa-IR^
 -xr!fi-FI^
 -xr!fr-CA^
 -xr!fr-FR^
 -xr!gl-ES^
 -xr!he-IL^
 -xr!hi-IN^
 -xr!hr-HR^
 -xr!hu-HU^
 -xr!id-ID^
 -xr!is-IS^
 -xr!it-IT^
 -xr!ja-JP^
 -xr!ka-GE^
 -xr!kk-KZ^
 -xr!ko-KR^
 -xr!lt-LT^
 -xr!lv-LV^
 -xr!ms-MY^
 -xr!nb-NO^
 -xr!nl-NL^
 -xr!nn-NO^
 -xr!pl-PL^
 -xr!pt-BR^
 -xr!pt-PT^
 -xr!ro-RO^
 -xr!ru-RU^
 -xr!sk-SK^
 -xr!sl-SI^
 -xr!sq-AL^
 -xr!sr-Cyrl-RS^
 -xr!sr-Latn-RS^
 -xr!sv-SE^
 -xr!th-TH^
 -xr!tr-TR^
 -xr!uk-UA^
 -xr!vi-VN^
 -xr!zh-CN^
 -xr!zh-TW^
 -xr!"stages/.models/Super Team Stage Marvel Mods"^
 -xr!"stages/.models/TeamStageCollection"
