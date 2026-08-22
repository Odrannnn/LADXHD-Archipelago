@echo off
cd /d "%~dp0"
Title LADXHD: Launcher Publish Script

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Configuration
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

set Copy_to_Resources=true
set RemovePublishPath=true

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Clean Previous Builds
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

if exist "%~dp0_Publish" (
    echo Cleaning previous builds...
    rd /s /q "%~dp0_Publish"
)

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Publish all Builds
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

echo Building Windows x64...
dotnet publish LADXHD_Launcher.csproj -r win-x64 /p:PublishProfile=Windows
if %errorlevel% neq 0 ( echo Windows build failed! & pause & exit /b 1 )

echo Building Linux x64...
dotnet publish LADXHD_Launcher.csproj -r linux-x64 /p:PublishProfile=Linux-x64
if %errorlevel% neq 0 ( echo Linux x64 build failed! & pause & exit /b 1 )

echo Building Linux Arm64...
dotnet publish LADXHD_Launcher.csproj -r linux-arm64 /p:PublishProfile=Linux-arm64
if %errorlevel% neq 0 ( echo Linux Arm64 build failed! & pause & exit /b 1 )

echo Building MacOS x64...
dotnet publish LADXHD_Launcher.csproj -r osx-x64 /p:PublishProfile=macOS-x64
if %errorlevel% neq 0 ( echo MacOS x64 build failed! & pause & exit /b 1 )

echo Building MacOS Arm64...
dotnet publish LADXHD_Launcher.csproj -r osx-arm64 /p:PublishProfile=macOS-arm64
if %errorlevel% neq 0 ( echo MacOS Arm64 build failed! & pause & exit /b 1 )

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Clean up unnecessary files
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

echo.
echo Cleaning up junk files...
for /r "%~dp0_Publish" %%f in (nfd.lib nfd.pdb Launcher.dll.config) do (
  if exist "%%f" (
    echo Deleting: %%f
    del "%%f"
  )
)

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Package Builds
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

if [%Copy_to_Resources%]==[true] (
    echo Packaging builds...
    if [%RemovePublishPath%]==[true] (
        powershell -ExecutionPolicy Bypass -File "%~dp0zip_resources.ps1" -RemovePublishPath
    ) else (
        powershell -ExecutionPolicy Bypass -File "%~dp0zip_resources.ps1"
    )
    if %errorlevel% neq 0 ( echo Packaging failed! & pause & exit /b 1 )
)

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Finish
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

echo.
if [%Copy_to_Resources%]==[true] (
    echo Done! Builds were zipped and copied to the patcher "Resources" folder.
) else (
    echo Done! Builds can be found in the Publish folder.
    pause >nul
)