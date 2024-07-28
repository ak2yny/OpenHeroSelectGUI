REM --configuration Release | uses the Release configuration by default for projects whose TargetFramework is set to net8.0 or a later
REM Doesn't support variables, such as PublishDir==bin\Release\$(TargetFramework)\win-x86\publish\
REM Only for .net 8 or newer. Previous .net versions used win10-x86

dotnet publish "OpenHeroSelectGUI.csproj" -r win-x86 -p:Platform=x86 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true -p:PublishTrimmed=false -p:PublishDir=bin\Release\net8.0\win-x86\publish\