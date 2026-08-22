@echo off
cd /d "%~dp0"
Title LADXHD: Game Publish Script

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Configuration
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

set RunCreatePatches=true
set BuildDirectX12=false
set BuildVulkan=false

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

echo Publishing game builds...
echo.

echo Restoring game projects...
dotnet restore ProjectZ.Content\ProjectZ.Content.csproj
if %errorlevel% neq 0 ( echo Restore failed ^(Content Builder^)! & pause & exit /b 1 )
dotnet restore ProjectZ.WindowsDX11\ProjectZ.WindowsDX11.csproj
if %errorlevel% neq 0 ( echo Restore failed ^(ProjectZ.WindowsDX11^)! & pause & exit /b 1 )
dotnet restore ProjectZ.DesktopGL\ProjectZ.DesktopGL.csproj
if %errorlevel% neq 0 ( echo Restore failed ^(ProjectZ.DesktopGL^)! & pause & exit /b 1 )
dotnet restore ProjectZ.Android\ProjectZ.Android.csproj
if %errorlevel% neq 0 ( echo Restore failed ^(ProjectZ.Android^)! & pause & exit /b 1 )

if [%BuildDirectX12%]==[true] (
    dotnet restore ProjectZ.WindowsDX12\ProjectZ.WindowsDX12.csproj
    if %errorlevel% neq 0 ( echo Restore failed ^(ProjectZ.WindowsDX12^)! & pause & exit /b 1 )
)
if [%BuildVulkan%]==[true] (
    dotnet restore ProjectZ.DesktopVK\ProjectZ.DesktopVK.csproj
    if %errorlevel% neq 0 ( echo Restore failed ^(ProjectZ.DesktopVK^)! & pause & exit /b 1 )
)

echo Build Content Builder App...
dotnet build --nologo ProjectZ.Content\ProjectZ.Content.csproj -c Release --no-restore
if %errorlevel% neq 0 ( echo Content builder build failed! & pause & exit /b 1 )

echo Building Windows ^(DirectX 11^)...
dotnet build --nologo ProjectZ.WindowsDX11\ProjectZ.WindowsDX11.csproj -c Release --no-restore -p:PublishProfile=FolderProfile_Windows-DX
if %errorlevel% neq 0 ( echo DX prebuild failed! & pause & exit /b 1 )

echo Publishing Windows ^(DirectX 11^)...
dotnet publish ProjectZ.WindowsDX11\ProjectZ.WindowsDX11.csproj -c Release --no-restore -p:PublishProfile=FolderProfile_Windows-DX
if %errorlevel% neq 0 ( echo DX publish failed! & pause & exit /b 1 )

echo Building Windows ^(OpenGL^)...
dotnet build --nologo ProjectZ.DesktopGL\ProjectZ.DesktopGL.csproj -c Release -r win-x64 --no-restore -p:PublishProfile=FolderProfile_Windows-GL
if %errorlevel% neq 0 ( echo GL prebuild failed! & pause & exit /b 1 )

echo Publishing Windows ^(OpenGL^)...
dotnet publish ProjectZ.DesktopGL\ProjectZ.DesktopGL.csproj -c Release -r win-x64 --no-restore -p:PublishProfile=FolderProfile_Windows-GL
if %errorlevel% neq 0 ( echo GL publish failed! & pause & exit /b 1 )

echo Publishing Android APK...
dotnet publish ProjectZ.Android\ProjectZ.Android.csproj -c Release --no-restore -p:PublishProfile=FolderProfile_Android
if %errorlevel% neq 0 ( echo Android build failed! & pause & exit /b 1 )

echo Publishing Linux x64 ^(OpenGL^)...
dotnet publish ProjectZ.DesktopGL\ProjectZ.DesktopGL.csproj -c Release -r linux-x64 --no-restore -p:PublishProfile=FolderProfile_Linux-x86_64
if %errorlevel% neq 0 ( echo Linux x86_64 build failed! & pause & exit /b 1 )

echo Publishing Linux Arm64 ^(OpenGL^)...
dotnet publish ProjectZ.DesktopGL\ProjectZ.DesktopGL.csproj -c Release -r linux-arm64 --no-restore -p:PublishProfile=FolderProfile_Linux-Arm64
if %errorlevel% neq 0 ( echo Linux Arm64 build failed! & pause & exit /b 1 )

echo Publishing MacOS arm64 ^(OpenGL^)...
dotnet publish ProjectZ.DesktopGL\ProjectZ.DesktopGL.csproj -c Release -r osx-arm64 --no-restore -p:PublishProfile=FolderProfile_MacOS-Arm64
if %errorlevel% neq 0 ( echo MacOS Arm64 build failed! & pause & exit /b 1 )

echo Publishing MacOS x64 ^(OpenGL^)...
dotnet publish ProjectZ.DesktopGL\ProjectZ.DesktopGL.csproj -c Release -r osx-x64 --no-restore -p:PublishProfile=FolderProfile_MacOS-x86_64
if %errorlevel% neq 0 ( echo MacOS x86_64 build failed! & pause & exit /b 1 )

if [%BuildDirectX12%]==[true] (

    echo Building Windows ^(DirectX 12^)...
    dotnet build --nologo ProjectZ.WindowsDX12\ProjectZ.WindowsDX12.csproj -c Release --no-restore -p:PublishProfile=FolderProfile_Windows-DX12
    if %errorlevel% neq 0 ( echo DX prebuild failed! & pause & exit /b 1 )

    echo Publishing Windows ^(DirectX 12^)...
    dotnet publish ProjectZ.WindowsDX12\ProjectZ.WindowsDX12.csproj -c Release --no-restore -p:PublishProfile=FolderProfile_Windows-DX12
    if %errorlevel% neq 0 ( echo DX publish failed! & pause & exit /b 1 )
)

if [%BuildVulkan%]==[true] (

    echo Building Windows ^(Vulkan^)...
    dotnet build --nologo ProjectZ.DesktopVK\ProjectZ.DesktopVK.csproj -c Release --no-restore -p:PublishProfile=FolderProfile_Windows-VK
    if %errorlevel% neq 0 ( echo DX prebuild failed! & pause & exit /b 1 )

    echo Publishing Windows ^(Vulkan^)...
    dotnet publish ProjectZ.DesktopVK\ProjectZ.DesktopVK.csproj -c Release --no-restore -p:PublishProfile=FolderProfile_Windows-VK
    if %errorlevel% neq 0 ( echo DX publish failed! & pause & exit /b 1 )
    
    echo Publishing Linux x64 ^(Vulkan^)...
    dotnet publish ProjectZ.DesktopVK\ProjectZ.DesktopVK.csproj -c Release -r linux-x64 --no-restore -p:PublishProfile=FolderProfile_Linux-x86_64
    if %errorlevel% neq 0 ( echo Linux x86_64 build failed! & pause & exit /b 1 )

    echo Publishing Linux Arm64 ^(Vulkan^)...
    dotnet publish ProjectZ.DesktopVK\ProjectZ.DesktopVK.csproj -c Release -r linux-arm64 --no-restore -p:PublishProfile=FolderProfile_Linux-Arm64
    if %errorlevel% neq 0 ( echo Linux Arm64 build failed! & pause & exit /b 1 )

    echo Publishing MacOS arm64 ^(Vulkan^)...
    dotnet publish ProjectZ.DesktopVK\ProjectZ.DesktopVK.csproj -c Release -r osx-arm64 --no-restore -p:PublishProfile=FolderProfile_MacOS-Arm64
    if %errorlevel% neq 0 ( echo MacOS Arm64 build failed! & pause & exit /b 1 )

    echo Publishing MacOS x64 ^(Vulkan^)...
    dotnet publish ProjectZ.DesktopVK\ProjectZ.DesktopVK.csproj -c Release -r osx-x64 --no-restore -p:PublishProfile=FolderProfile_MacOS-x86_64
    if %errorlevel% neq 0 ( echo MacOS x86_64 build failed! & pause & exit /b 1 )
)

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Clean up unnecessary files
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

echo.
echo Cleaning up junk files...
for /r "%~dp0_Publish" %%f in (nfd.lib nfd.pdb sosdocsunix.txt com.zelda.ladxhd.archipelago.apk _Microsoft.Android.Resource.Designer.dll) do (
  if exist "%%f" (
    echo Deleting: %%f
    del "%%f"
  )
)

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Create Patches
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

if [%RunCreatePatches%]==[true] (
    echo Running CreatePatches.ps1...
    powershell -ExecutionPolicy Bypass -File "%~dp0..\ladxhd_patcher_source_code\CreatePatches.ps1"
    if %errorlevel% neq 0 ( echo CreatePatches failed! & pause & exit /b 1 )
)

::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────
:: Finish
::───────────────────────────────────────────────────────────────────────────────────────────────────────────────────

echo.
if [%RunCreatePatches%]==[true] (
    echo Done! Game built, patches created, and launcher published.
) else (
    echo Done! Builds are in the Publish folder.
    pause >nul
)
