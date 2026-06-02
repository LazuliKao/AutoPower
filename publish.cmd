@echo off
REM NativeAOT publish script - two-step to avoid NETSDK1207 on source generator projects
REM The issue: dotnet publish -p:PublishAot=true passes PublishAot during restore,
REM which fails for netstandard2.0 source generator projects.

echo [1/2] Restoring...
dotnet restore src/AutoPower/AutoPower.csproj -r win-x64
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo [2/2] Publishing with NativeAOT...
dotnet publish src/AutoPower/AutoPower.csproj -c Release -r win-x64 -p:PublishAot=true --no-restore
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo Done.
