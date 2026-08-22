#========================================================================================================================================
# LINK'S AWAKENING DX HD: VCDIFF PATCH CREATOR FOR GAME PATCHER
# By: Bighead
#========================================================================================================================================
# PURPOSE
#========================================================================================================================================
<#

  The purpose of this script is to generate patches from v1.0.0 of Link's Awakening DX HD and whatever the latest version is. 

#>
#========================================================================================================================================
# INSTRUCTIONS
#========================================================================================================================================
<#

  Information:
  - Generate vcdiff patches to update v1.0.0 or v1.1.4+ to the latest build.
  - VCDiff patches must share a name with the file they are patching + ".vcdiff" extension.
  - For example, the file "musicOverworld.data" the patch should be "musicOverworld.data.vcdiff"
  - This script can be automatically ran from "publish.bat" in "ladxhd_game_source_code" folder.
  - It can automatically run the launcher "publish.bat" script in "ladxhd_launcher_source_code" folder.

  Requirements:
  - .NET 9.0 SDK for building game/launcher.
  - Original v1.0.0 of the game.
  - New builds of the game.
  - Both must be fully built and playable.
  - VCDiff CLI App (source code is in LADXHD-Updated repository)
  - 7-Zip (for Android build).

  Configuration:
  - $OldGamePath : Root path where the original v1.0.0 is released.
  - $SevenZipExe : The path to 7-zip. Required to pack Android ".apk" file.
  - $PubLauncher : Publish/pack launcher and move to "Resources" folder.
  - $CreateXXXXX : Create patches for the build described by "XXXXX".

  How to use:
  - Publish the games via "publish.bat" in "ladxhd_game_source_code" folder.
  - This script will automatically retrieve the published game paths.
  - Version 1.0.0 should be set to "OldGamePath".
  - The new builds should be set to their respective folders.
  - Right click this script, select "Run with PowerShell".
  - Generated patches can be found in the "Resources" folder.
  - Obviously, the vcdiff patches can be found in this folder.

  What to do with patches:
  - Open "LADXHD_Patcher.sln" in Visual Studio 2022.
  - In Solution Explorer, go to "Properties >> Resources.resx"
  - Double click "Resources.resx" to open it in a window.
  - Select all "vcdiff patches" currently in Resources.resx and delete them.
  - Drag and drop all the new patches from the "patches" folder in "Resources.resx".
  - For easier identification and sorting later, set Neutral Comment to "vcdiff patch".

  Now what?:
  - Edit the version number in "Program >> Config" to set the new version of the game.
  - Build the project. This will create a new patcher. All patches are handled automatically.

#>
#========================================================================================================================================
# CONFIGURATION
#========================================================================================================================================

$OldGamePath = "H:\Projects\Zelda Link's Awakening\original"
$SevenZipExe = "C:\Program Files\7-Zip\7z.exe"
$PubLauncher = $true
$MakePatcher = $true
$LitePatcher = $true

$CreateWinDX = $true
$CreateWinGL = $true
$CreateDroid = $true
$CreateLix86 = $true
$CreateLiArm = $true
$CreateMcx86 = $true
$CreateMcArm = $true

#========================================================================================================================================
# SET BASE PATHS
#========================================================================================================================================

$Host.UI.RawUI.WindowTitle = "LADXHD: Patch Creation Script"
Set-Location (Split-Path $script:MyInvocation.MyCommand.Path)
Set-Location ..
$BaseFolder   = Get-Location
$GameFolder   = Join-path $BaseFolder ("\ladxhd_game_source_code")
$PublishPath  = Join-path $GameFolder ("\_Publish")
$PatcherPath  = Join-path $BaseFolder ("\ladxhd_patcher_source_code")
$PatcherLite  = Join-path $BaseFolder ("\ladxhd_patcher_lite_source_code")
$ResourcePath = Join-path $PatcherPath ("\Resources")

#========================================================================================================================================
# PUBLISHED PATHS
#========================================================================================================================================

$WinDXInPath = Join-path $PublishPath ("\Windows-DX")
$WinGLInPath = Join-path $PublishPath ("\Windows-GL")
$DroidInPath = Join-path $PublishPath ("\Android")
$Linux86Path = Join-path $PublishPath ("\Linux-x86_64")
$LinuxArPath = Join-path $PublishPath ("\Linux-Arm64")
$MacOS86Path = Join-path $PublishPath ("\MacOS-x86_64")
$MacOSArPath = Join-path $PublishPath ("\MacOS-Arm64")

#========================================================================================================================================
# SETUP VCDIFF & OUTPUTS
#========================================================================================================================================

$VCDiffPath  = Join-Path $PatcherPath ("\vcdiff.exe")
$PatchesPath = Join-Path $PatcherPath ("\Patches")

$WinDXPatches = Join-Path $PatchesPath ("\LADXHD (Win-DX) Patches")
$WinGLPatches = Join-Path $PatchesPath ("\LADXHD (Win-GL) Patches")
$DroidPatches = Join-Path $PatchesPath ("\LADXHD (Android) Patches")
$Lix86Patches = Join-Path $PatchesPath ("\LADXHD (Linux-x86) Patches")
$LiArmPatches = Join-Path $PatchesPath ("\LADXHD (Linux-Arm64) Patches")
$Mcx86Patches = Join-Path $PatchesPath ("\LADXHD (MacOS-x86) Patches")
$McArmPatches = Join-Path $PatchesPath ("\LADXHD (MacOS-Arm64) Patches")

#========================================================================================================================================
# CREATE PATCHES FOLDER
#========================================================================================================================================

if ($CreateWinDX -and (!(Test-Path $WinDXPatches))) {
    New-Item -Path $WinDXPatches -ItemType Directory | Out-Null
}
if ($CreateWinGL -and (!(Test-Path $WinGLPatches))) {
    New-Item -Path $WinGLPatches -ItemType Directory | Out-Null
}
if ($CreateDroid -and (!(Test-Path $DroidPatches))) {
    New-Item -Path $DroidPatches -ItemType Directory | Out-Null
}
if ($CreateLix86 -and (!(Test-Path $Lix86Patches))) {
    New-Item -Path $Lix86Patches -ItemType Directory | Out-Null
}
if ($CreateLiArm -and (!(Test-Path $LiArmPatches))) {
    New-Item -Path $LiArmPatches -ItemType Directory | Out-Null
}
if ($CreateMcx86 -and (!(Test-Path $Mcx86Patches))) {
    New-Item -Path $Mcx86Patches -ItemType Directory | Out-Null
}
if ($CreateMcArm -and (!(Test-Path $McArmPatches))) {
    New-Item -Path $McArmPatches -ItemType Directory | Out-Null
}

#========================================================================================================================================
# MISCELLANEOUS
#========================================================================================================================================
$host.UI.RawUI.WindowTitle = "Link's Awakening DX HD: VCDiff Patch Generation Script"

function PauseBeforeClose
{
    Write-Host "Press any key to close this window."
    [void][System.Console]::ReadKey()
    Exit
}

#========================================================================================================================================
# SPECIAL CASES
#========================================================================================================================================

$langFiles  = @("chn.lng", "deu.lng", "esp.lng", "fre.lng", "ind.lng", "ita.lng", "por.lng", "pte.lng", "rus.lng", "swe.lng")
$langDialog = @("dialog_chn.lng", "dialog_deu.lng", "dialog_esp.lng", "dialog_fre.lng", "dialog_ind.lng", "dialog_ita.lng", "dialog_por.lng", "dialog_pte.lng", "dialog_rus.lng", "dialog_swe.lng")
$langAchiev = @("achieve_chn.lng", "achieve_deu.lng", "achieve_eng.lng", "achieve_esp.lng", "achieve_fre.lng", "achieve_ind.lng", "achieve_ita.lng", "achieve_por.lng", "achieve_pte.lng", "achieve_rus.lng", "achieve_swe.lng")
$smallFonts = @("smallFont_redux.xnb", "smallFont_vwf.xnb", "smallFont_vwf_redux.xnb", "smallFont_chn.xnb", "smallFont_chn_0.xnb", "smallFont_chn_redux.xnb", "smallFont_chn_redux_0.xnb")
$backGround = @("menuBackgroundB.xnb", "menuBackgroundC.xnb", "sgb_border.xnb")
$lighting   = @("mamuLight.xnb")
$linkImages = @("link1.png", "weapons.png")
$linkAtlas  = @("weapons.atlas")
$npcImages  = @("npcs_redux.png")
$itemImages = @("items_chn.png", "items_deu.png", "items_esp.png", "items_fre.png", "items_ind.png", "items_ita.png", "items_por.png", "items_rus.png", "items_swe.png", "items_redux.png", 
                "items_redux_chn.png", "items_redux_deu.png", "items_redux_esp.png", "items_redux_fre.png", "items_redux_ind.png", "items_redux_ita.png", "items_redux_por.png", "items_redux_rus.png", "items_redux_swe.png")
$introImage = @("intro_chn.png", "intro_deu.png", "intro_esp.png", "intro_fre.png", "intro_ind.png", "intro_ita.png", "intro_por.png", "intro_rus.png", "intro_swe.png")
$introAtlas = @("intro_chn.atlas")
$miniMapImg = @("minimap_chn.png", "minimap_deu.png", "minimap_esp.png", "minimap_fre.png", "minimap_ind.png", "minimap_ita.png", "minimap_por.png", "minimap_rus.png", "minimap_swe.png")
$objectsImg = @("objects_chn.png", "objects_deu.png", "objects_esp.png", "objects_fre.png", "objects_ind.png", "objects_ita.png", "objects_por.png", "objects_rus.png", "objects_swe.png")
$photograph = @("photos_chn.png", "photos_deu.png", "photos_esp.png", "photos_fre.png", "photos_ind.png", "photos_ita.png", "photos_por.png", "photos_rus.png", "photos_swe.png", "photos_redux.png", 
                "photos_redux_chn.png", "photos_redux_deu.png", "photos_redux_esp.png", "photos_redux_fre.png", "photos_redux_ind.png", "photos_redux_ita.png", "photos_redux_por.png", "photos_redux_rus.png", "photos_redux_swe.png" )
$uiImages   = @("ui_chn.png", "ui_deu.png", "ui_esp.png", "ui_fre.png", "ui_ind.png", "ui_ita.png", "ui_por.png", "ui_rus.png", "ui_swe.png")
$musicTile  = @("musicOverworldClassic.data")
$dungeon3M  = @("dungeon3.map")
$dungeon3D  = @("dungeon3.map.data")
$newBridgeM = @("bridge_l_castle.map", "bridge_r_castle.map")
$newBridgeD = @("bridge_l_castle.map.data", "bridge_r_castle.map.data")
$bowwowanim = @("bowwow_water.ani")
$dungeonani = @("mapDungeon.ani", "mapManboPond.ani", "mapTeleporter.ani")
$boomerang  = @("boomerangOrig.ani")
$effectani  = @("bushExplosion.ani", "fireballDeath.ani")
$shaderFile = @("GBCColorCorrection.xnb", "PixelGrid.xnb")

$FileTargets = @{
    "eng.lng"             = $langFiles + $langAchiev
    "dialog_eng.lng"      = $langDialog
    "smallFont.xnb"       = $smallFonts
    "menuBackground.xnb"  = $backGround
    "ligth room.xnb"      = $lighting
    "link0.png"           = $linkImages
    "link0.atlas"         = $linkAtlas
    "npcs.png"            = $npcImages
    "items.png"           = $itemImages
    "intro.png"           = $introImage
    "intro.atlas"         = $introAtlas
    "minimap.png"         = $miniMapImg
    "objects.png"         = $objectsImg
    "photos.png"          = $photograph
    "ui.png"              = $uiImages
    "musicOverworld.data" = $musicTile
    "dungeon3_1.map"      = $dungeon3M
    "dungeon3_1.map.data" = $dungeon3D
    "bridge.map"          = $newBridgeM
    "bridge.map.data"     = $newBridgeD
    "BowWow.ani"          = $bowwowanim
    "mapPlayer.ani"       = $dungeonani
    "boomerang.ani"       = $boomerang
    "explosion0.ani"      = $effectani
    "ShockEffect.xnb"     = $shaderFile
}

function Build-ReverseMap($Targets)
{
    $Reverse = @{}
    foreach ($Key in $Targets.Keys) 
    {
        $ShortName = $Key
        $LongNames = $Targets[$Key]
        foreach ($LongName in $LongNames) 
        {
            $Reverse[$LongName.ToLower()] = $ShortName
        }
    }
    return $Reverse
}
$ReverseFileTargets = Build-ReverseMap -Targets $FileTargets


function GetOldFilePath([object]$File, [string]$GamePath, [string]$RelativePath, [string]$Platform)
{
    if (($File.Name -eq "Link's Awakening DX HD") -and (($Platform -like "Linux_*") -or ($Platform -like "MacOS_*")))
    {
        return Join-Path $OldGamePath ($RelativePath + ".exe")
    }
    if ($ReverseFileTargets.ContainsKey($File.Name.ToLower()))
    {
        $relativeDir = $File.DirectoryName.Substring($GamePath.Length).TrimStart('\','/')
        $mappedName  = $ReverseFileTargets[$File.Name.ToLower()]

        if ([string]::IsNullOrWhiteSpace($relativeDir))
        {
            return Join-Path $OldGamePath $mappedName
        }
        return Join-Path $OldGamePath (Join-Path $relativeDir $mappedName)
    }
    return Join-Path $OldGamePath $RelativePath
}

#========================================================================================================================================
# ANDROID EXTRA FILES
#========================================================================================================================================

function PrepareAndroid([string]$GamePath)
{
    $WorkDir = Join-Path $GamePath ("_Temp")
    $APKFile = Join-Path $GamePath ("com.zelda.ladxhd.archipelago-Signed.apk")
    $TempZIP = Join-Path $WorkDir ("com.zelda.ladxhd.archipelago-Signed.zip")
    $MetaInf = Join-Path $WorkDir ("META-INF")

    Remove-Item -Path $WorkDir -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
    New-Item -Path $WorkDir -ItemType Directory | Out-Null
    Copy-Item -LiteralPath $APKFile -Destination $TempZIP -Force
    Expand-Archive -Path $TempZIP -DestinationPath $WorkDir -Force

    Remove-Item -Path $MetaInf -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
    Remove-Item -Path $TempZIP -Force -ErrorAction SilentlyContinue | Out-Null

    $TempPath = Join-Path $WorkDir ("com.zelda.ladxhd")
    New-Item -Path $TempPath -ItemType Directory | Out-Null

    foreach ($Item in Get-ChildItem -LiteralPath $WorkDir)
    {
        if ($Item.Name -eq "com.zelda.ladxhd") { continue }
        Move-Item -LiteralPath $Item.FullName -Destination $TempPath -Force
    }
    $ContPath = Join-Path (Join-Path $TempPath "assets") ("Content")
    $DataPath = Join-Path (Join-Path $TempPath "assets") ("Data")
    $BaseAPK = Join-Path $ResourcePath ("android_base.apk")

    Move-Item -LiteralPath $ContPath -Destination $GamePath -Force
    Move-Item -LiteralPath $DataPath -Destination $GamePath -Force
    Remove-Item -Path $BaseAPK -Force -ErrorAction SilentlyContinue | Out-Null

    $TempAPK = Join-Path $GamePath ("com.zelda.ladxhd.archipelago-temp.zip")
    Copy-Item -LiteralPath $APKFile -Destination $TempAPK -Force
    & $SevenZipExe "d" $TempAPK "META-INF\*" "assets\Content\*" "assets\Data\*" "-r"
    Copy-Item -LiteralPath $TempAPK -Destination $BaseAPK -Force

    Remove-Item -Path $TempAPK -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
}

#========================================================================================================================================
# MACOS EXTRA FILES
#========================================================================================================================================

$MacOSExtraFiles = @("libopenal.dylib", "libSDL2-2.0.0.dylib")

function CreateMacOSExtraFilesZip([bool]$CreatePatches, [string]$GamePath, [string]$Platform)
{
    if ((!$CreatePatches) -or ($Platform -notlike "MacOS_*")) { return }

    $ZipName = $Platform.ToLower() + "_files.zip"
    $ZipFile = Join-Path $ResourcePath $ZipName

    $TempPath = Join-Path ([System.IO.Path]::GetTempPath()) ("ladxhd_" + $Platform.ToLower() + "_extra_files")
    $AllFiles = Get-ChildItem -LiteralPath $GamePath -File

    Remove-Item -Path $ZipFile -Force -ErrorAction SilentlyContinue | Out-Null
    New-Item -Path $TempPath -ItemType Directory | Out-Null

    foreach ($FileName in $MacOSExtraFiles)
    {
        $SourceFile = $AllFiles | Where-Object { $_.Name -eq $FileName } | Select-Object -First 1
        Copy-Item -LiteralPath $SourceFile.FullName -Destination (Join-Path $TempPath $FileName) -Force
    }
    if ((Get-ChildItem -LiteralPath $TempPath -File | Measure-Object).Count -gt 0)
    {
        Write-Host ('Generating "' + $ZipName + '" for patcher program.')
        Write-Host ""
        Compress-Archive -Path (Join-Path $TempPath "*") -DestinationPath $ZipFile | Out-Null
    }
    Remove-Item -Path $TempPath -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
}

#========================================================================================================================================
# PUBLISH LAUNCHER AND PACK INTO ZIP IN "RESOURCES" FOLDER
#========================================================================================================================================

function PublishAndPackLauncher()
{
    if (!$PubLauncher) { return }
  
    Write-Host "------------------------------------------------------------------------------------------"
    Write-Host ""
    Write-Host "Publishing Launcher and packing into ZIP files:"
    Write-Host ""

    $LauncherBat = Join-Path $BaseFolder ("ladxhd_launcher_source_code\publish.bat")

    if (!(Test-Path $LauncherBat))
    {
        Write-Host "Could not find publish.bat at: $LauncherBat"
        return
    }
    & cmd.exe /c "`"$LauncherBat`""
    
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Launcher publish failed with exit code: $LASTEXITCODE"
    }
    else
    {
        Write-Host "Launcher published and packed successfully."
    }
    Write-Host ""
}

#========================================================================================================================================
# PUBLISH LAUNCHER AND PACK INTO ZIP IN "RESOURCES" FOLDER
#========================================================================================================================================

function BuildAllPatchers()
{
    Write-Host "------------------------------------------------------------------------------------------"
    Write-Host ""
    Write-Host "Building all Patchers:"
    Write-Host ""

    $PatcherMainBat = Join-Path $PatcherPath ("publish.bat")
    $PatcherLiteBat = Join-Path $PatcherLite ("publish.bat")

    if (!(Test-Path $PatcherMainBat))
    {
        Write-Host "Could not find publish.bat at: $PatcherMainBat"
        return
    }
    & cmd.exe /c "`"$PatcherMainBat`""
    
    if (!(Test-Path $PatcherLiteBat))
    {
        Write-Host "Could not find publish.bat at: $PatcherLiteBat"
        return
    }
    & cmd.exe /c "`"$PatcherLiteBat`""

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Patchers publish failed with exit code: $LASTEXITCODE"
    }
    else
    {
        Write-Host "Patchers published successfully."
    }
    Write-Host ""
}

#========================================================================================================================================
# VERIFICATION
#========================================================================================================================================

function VerifyOriginal()
{
    if (!(Test-Path (Join-Path $OldGamePath ("Link's Awakening DX HD.exe"))))
    {
        Write-Host "Invalid path or missing executable for original game (OldGamePath)."
        return $false
    }
    return $true
}

function VerifyVCDiff()
{
    if (!(Test-Path $VCDiffPath)) 
    {
        Write-Host 'Missing "vcdiff.exe" in "Resources" folder.'
        return $false
    }
    return $true
}

function VerifyExecutable([string]$Platform, [string]$GamePath)
{
    switch -wildcard ($Platform)
    {
        "Win_*"   { if (!(Test-Path (Join-Path $GamePath ("Link's Awakening DX HD.exe")))) { return $false } }
        "Linux_*" { if (!(Test-Path (Join-Path $GamePath ("Link's Awakening DX HD")))) { return $false } }
        "MacOS_*" { if (!(Test-Path (Join-Path $GamePath ("Link's Awakening DX HD")))) { return $false } }
        "Android" { return $true }
    }
    return $true
}

#========================================================================================================================================
# GENERATE PATCHES
#========================================================================================================================================

function GeneratePatches([bool]$CreatePatches, [string]$GamePath, [string]$PatchOutput, [string]$Platform)
{
    if ((!$CreatePatches) -or (!(VerifyExecutable -Platform $Platform -GamePath $GamePath))) { return }

    if ($Platform -eq "Android") { PrepareAndroid -GamePath $GamePath }

    Write-Host "------------------------------------------------------------------------------------------"
    Write-Host ""
    Write-Host ("Generating " + $Platform + " patches for Link's Awakening DX HD ...")
    Write-Host ""

    $ManifestLines = [System.Collections.Generic.List[string]]::new()

    foreach ($file in Get-ChildItem -LiteralPath $GamePath -Recurse -File) 
    {
        $RelativePath = $file.FullName.Substring($GamePath.Length).TrimStart('\')
        $OldFilePath  = GetOldFilePath -File $file -GamePath $GamePath -RelativePath $RelativePath -Platform $Platform
        $NewFilePath  = $file.FullName

        if (!(Test-Path -LiteralPath $OldFilePath)) { continue }

        $OldMD5 = (Get-FileHash -Path $OldFilePath -Algorithm MD5).Hash
        $NewMD5 = (Get-FileHash -Path $NewFilePath -Algorithm MD5).Hash

        if ($OldMD5 -ne $NewMD5) 
        {
            $PatchFile = Join-Path $PatchOutput ($file.Name + ".vcdiff")
            $ManifestLines.Add("$OldFilePath|$NewFilePath|$PatchFile")
        }
    }

    if ($ManifestLines.Count -gt 0)
    {
        $ManifestFile = Join-Path $PatchOutput ("patch_manifest.txt")
        $ManifestLines | Set-Content -Path $ManifestFile -Encoding UTF8
        Write-Host ""
        & $VCDiffPath -manifest $ManifestFile
        Remove-Item -Path $ManifestFile -Force -ErrorAction SilentlyContinue | Out-Null
    }
    else
    {
        Write-Host "No changed files found."
    }
    Write-Host ""
    Write-Host ('Generating "patches_' + $Platform.ToLower() + '.zip" for patcher program.')
    Write-Host ""

    $ZipPath = Join-Path $PatchOutput ("\*")
    $ZipFile = Join-Path $ResourcePath ("\patches_" + $Platform.ToLower() + ".zip")
    Remove-Item -Path $ZipFile -Force -ErrorAction SilentlyContinue | Out-Null
    Compress-Archive -Path $ZipPath -DestinationPath $ZipFile | Out-Null

    CreateMacOSExtraFilesZip -CreatePatches $CreatePatches -GamePath $GamePath -Platform $Platform
}

if ((VerifyOriginal) -and (VerifyVCDiff))
{
    GeneratePatches -CreatePatches $CreateWinDX -GamePath $WinDXInPath -PatchOutput $WinDXPatches -Platform "Win_DX"
    GeneratePatches -CreatePatches $CreateWinGL -GamePath $WinGLInPath -PatchOutput $WinGLPatches -Platform "Win_GL"
    GeneratePatches -CreatePatches $CreateDroid -GamePath $DroidInPath -PatchOutput $DroidPatches -Platform "Android"
    GeneratePatches -CreatePatches $CreateLix86 -GamePath $Linux86Path -PatchOutput $Lix86Patches -Platform "Linux_x86"
    GeneratePatches -CreatePatches $CreateLiArm -GamePath $LinuxArPath -PatchOutput $LiArmPatches -Platform "Linux_Arm64"
    GeneratePatches -CreatePatches $CreateMcx86 -GamePath $MacOS86Path -PatchOutput $Mcx86Patches -Platform "MacOS_x86"
    GeneratePatches -CreatePatches $CreateMcArm -GamePath $MacOSArPath -PatchOutput $McArmPatches -Platform "MacOS_Arm64"

    PublishAndPackLauncher

    if ($MakePatcher)
    {
        BuildAllPatchers
    }
    else
    {
        Write-Host "------------------------------------------------------------------------------------------"
        Write-Host ""
        Write-Host "Patch generation complete. Patches can be found in folder:"
        Write-Host $PatchFolder
        Write-Host ""
        Write-Host "------------------------------------------------------------------------------------------"
    }
}

Write-Host ""

PauseBeforeClose
