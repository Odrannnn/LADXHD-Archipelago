#========================================================================================================================================
# PARAMETERS
#========================================================================================================================================

param ([switch]$RemovePublishPath)
$Host.UI.RawUI.WindowTitle = "LADXHD: Launcher Packing Script"

#========================================================================================================================================
# SET BASE PATHS
#========================================================================================================================================

Set-Location (Split-Path $script:MyInvocation.MyCommand.Path)
Set-Location ..
$RootPath   = Get-Location
$LauncherPath = Join-path $RootPath ("\ladxhd_launcher_source_code")
$PublishPath  = Join-path $LauncherPath ("\_Publish")
$PatcherPath  = Join-path $RootPath ("\ladxhd_patcher_source_code")
$ResourcePath = Join-path $PatcherPath ("\Resources")

#========================================================================================================================================
# LAUNCHER ZIP FILES
#========================================================================================================================================

$pubpath_linux_x86 = Join-path $PublishPath ("\Linux-x64")
$pubpath_linux_arm = Join-path $PublishPath ("\Linux-arm64")
$pubpath_macos_x86 = Join-path $PublishPath ("\macOS-x64")
$pubpath_macos_arm = Join-path $PublishPath ("\macOS-arm64")
$pubpath_window_64 = Join-path $PublishPath ("\Windows")

$publish_linux_x86 = Join-path $pubpath_linux_x86 ("\launcher_linux_x86.zip")
$publish_linux_arm = Join-path $pubpath_linux_arm ("\launcher_linux_arm64.zip")
$publish_macos_x86 = Join-path $pubpath_macos_x86 ("\launcher_macos_x86.zip")
$publish_macos_arm = Join-path $pubpath_macos_arm ("\launcher_macos_arm64.zip")
$publish_window_64 = Join-path $pubpath_window_64 ("\launcher_windows.zip")

$launcher_linux_x86 = Join-path $ResourcePath ("\launcher_linux_x86.zip")
$launcher_linux_arm = Join-path $ResourcePath ("\launcher_linux_arm64.zip")
$launcher_macos_x86 = Join-path $ResourcePath ("\launcher_macos_x86.zip")
$launcher_macos_arm = Join-path $ResourcePath ("\launcher_macos_arm64.zip")
$launcher_window_64 = Join-path $ResourcePath ("\launcher_windows.zip")

#========================================================================================================================================
# COMPRESS LAUNCHER BUILDS
#========================================================================================================================================

Compress-Archive -Path "$pubpath_linux_x86\*" -DestinationPath $publish_linux_x86 -Force
Compress-Archive -Path "$pubpath_linux_arm\*" -DestinationPath $publish_linux_arm -Force
Compress-Archive -Path "$pubpath_macos_x86\*" -DestinationPath $publish_macos_x86 -Force
Compress-Archive -Path "$pubpath_macos_arm\*" -DestinationPath $publish_macos_arm -Force
Compress-Archive -Path "$pubpath_window_64\*" -DestinationPath $publish_window_64 -Force

#========================================================================================================================================
# REMOVE OLD FILES
#========================================================================================================================================

Remove-Item -Path $launcher_linux_x86 -Force -ErrorAction SilentlyContinue | Out-Null
Remove-Item -Path $launcher_linux_arm -Force -ErrorAction SilentlyContinue | Out-Null
Remove-Item -Path $launcher_macos_x86 -Force -ErrorAction SilentlyContinue | Out-Null
Remove-Item -Path $launcher_macos_arm -Force -ErrorAction SilentlyContinue | Out-Null
Remove-Item -Path $launcher_window_64 -Force -ErrorAction SilentlyContinue | Out-Null

#========================================================================================================================================
# MOVE ZIP FILES TO RESOURCES
#========================================================================================================================================

Move-Item -Path $publish_linux_x86 -Destination $launcher_linux_x86 -Force
Move-Item -Path $publish_linux_arm -Destination $launcher_linux_arm -Force
Move-Item -Path $publish_macos_x86 -Destination $launcher_macos_x86 -Force
Move-Item -Path $publish_macos_arm -Destination $launcher_macos_arm -Force
Move-Item -Path $publish_window_64 -Destination $launcher_window_64 -Force

#========================================================================================================================================
# REMOVE PUBLISH FOLDER
#========================================================================================================================================

if ($RemovePublishPath) 
{
    Remove-Item -Path $PublishPath -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
}