@echo off
setlocal enabledelayedexpansion

for %%I in ("%~dp0.") do set "Root=%%~fI"
cd /d "%Root%"
Title LADXHD: Patcher-Lite Publish Script

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
dotnet publish LADXHD_Patcher_Lite.csproj -r win-x64 /p:PublishProfile=Windows
if %errorlevel% neq 0 ( echo Windows build failed! & pause & exit /b 1 )

echo Building Linux x64...
dotnet publish LADXHD_Patcher_Lite.csproj -r linux-x64 /p:PublishProfile=Linux-x64
if %errorlevel% neq 0 ( echo Linux x64 build failed! & pause & exit /b 1 )

echo Building Linux Arm64...
dotnet publish LADXHD_Patcher_Lite.csproj -r linux-arm64 /p:PublishProfile=Linux-arm64
if %errorlevel% neq 0 ( echo Linux Arm64 build failed! & pause & exit /b 1 )

echo Building MacOS x64...
dotnet publish LADXHD_Patcher_Lite.csproj -r osx-x64 /p:PublishProfile=macOS-x64
if %errorlevel% neq 0 ( echo MacOS x64 build failed! & pause & exit /b 1 )

echo Building MacOS Arm64...
dotnet publish LADXHD_Patcher_Lite.csproj -r osx-arm64 /p:PublishProfile=macOS-arm64
if %errorlevel% neq 0 ( echo MacOS Arm64 build failed! & pause & exit /b 1 )

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Rename output "Launcher" executables to "LADXHD.Patcher.v#.#.#".
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

echo.
echo Renaming output executables...
if exist "%Root%\_Publish\Windows\Patcher-Lite.exe" (
    echo Renaming: "%Root%\_Publish\Windows\Patcher-Lite.exe" to "LADXHD-Patcher-Lite.exe"
    ren "%Root%\_Publish\Windows\Patcher-Lite.exe" "LADXHD-Patcher-Lite.exe"
)
if exist "%Root%\_Publish\Linux-x64\Patcher-Lite" (
    echo Renaming: "%Root%\_Publish\Linux-x64\Patcher-Lite" to "LADXHD-Patcher-Lite.bin"
    ren "%Root%\_Publish\Linux-x64\Patcher-Lite" "LADXHD-Patcher-Lite.bin"
)
if exist "%Root%\_Publish\Linux-arm64\Patcher-Lite" (
    echo Renaming: "%Root%\_Publish\Linux-arm64\Patcher-Lite" to "LADXHD-Patcher-Lite.bin"
    ren "%Root%\_Publish\Linux-arm64\Patcher-Lite" "LADXHD-Patcher-Lite.bin"
)
if exist "%Root%\_Publish\macOS-x64\Patcher-Lite.app" (
    echo Renaming: "%Root%\_Publish\macOS-x64\Patcher-Lite.app" to "LADXHD-Patcher-Lite.app"
    ren "%Root%\_Publish\macOS-x64\Patcher-Lite.app" "LADXHD-Patcher-Lite.app"
)
if exist "%Root%\_Publish\macOS-arm64\Patcher-Lite.app" (
    echo Renaming: "%Root%\_Publish\macOS-arm64\Patcher-Lite.app" to "LADXHD-Patcher-Lite.app"
    ren "%Root%\_Publish\macOS-arm64\Patcher-Lite.app" "LADXHD-Patcher-Lite.app"
)

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Clean up unnecessary files
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

echo.
echo Cleaning up junk files...
for /r "%~dp0_Publish" %%f in (nfd.lib nfd.pdb) do (
  if exist "%%f" (
    echo Deleting: %%f
    del "%%f"
  )
)

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Sign the MacOS Launcher
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Uses a personal signing key. Users building for MacOS would need to generate their own key using "rcodesign.exe". 
:: This can be done with command: rcodesign generate-self-signed-certificate --person-name NAME > \path\to\NAME.pem

set CodeSignApp="%Root%\publish\rcodesign.exe"
set CodeSignKey="%USERPROFILE%\LADXHD\Bighead.pem"

echo.
echo Signing MacOS-x64 executable...
%CodeSignApp% sign --pem-source %CodeSignKey% "%Root%\_Publish\macOS-x64\LADXHD-Patcher-Lite.app\Contents\MacOS\Patcher-Lite"
copy "%Root%\publish\fix-macos-permissions.command" "%Root%\_Publish\macOS-x64\fix-permissions.command"

echo.
echo Signing MacOS-Arm64 executable...
%CodeSignApp% sign --pem-source %CodeSignKey% "%Root%\_Publish\macOS-arm64\LADXHD-Patcher-Lite.app\Contents\MacOS\Patcher-Lite"
copy "%Root%\publish\fix-macos-permissions.command" "%Root%\_Publish\macOS-arm64\fix-permissions.command"

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Finish
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

echo.
echo Done! Builds can be found in the Publish folder.
pause >nul