[CmdletBinding()]
param(
    [string]$OutputDirectory = ".local/build/android-arm64",
    [switch]$SkipRestore,
    [switch]$SkipSmoke,
    [switch]$SkipPublicSourceGuard
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$builderImage = "ladxhd-android-builder:net9"
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$outputFull = if ([IO.Path]::IsPathRooted($OutputDirectory)) {
    [IO.Path]::GetFullPath($OutputDirectory)
} else {
    [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
}
$repoPrefix = $repoRoot.TrimEnd(
    [IO.Path]::DirectorySeparatorChar,
    [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $outputFull.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must stay inside the repository: $outputFull"
}
$outputRelative = [IO.Path]::GetRelativePath($repoRoot, $outputFull).Replace('\', '/')
$nugetCache = Join-Path $repoRoot ".local/nuget-packages"
$androidProject = "ladxhd_game_source_code/ProjectZ.Android/ProjectZ.Android.csproj"
$smokeProject = "ladxhd_game_source_code/ProjectZ.Archipelago.SmokeTests/ProjectZ.Archipelago.SmokeTests.csproj"

function Invoke-DockerStep {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [string[]]$Command
    )
    Write-Host "==> $Name"
    $arguments = @(
        "run", "--rm",
        "-v", "${repoRoot}:/src",
        "-v", "${nugetCache}:/root/.nuget/packages",
        "-w", "/src",
        $builderImage
    ) + $Command
    & docker @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

function Invoke-PythonTool {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [string[]]$Arguments
    )
    Write-Host "==> $Name"
    if (Get-Command python -ErrorAction SilentlyContinue) {
        & python @Arguments
    } elseif (Get-Command py -ErrorAction SilentlyContinue) {
        & py -3 @Arguments
    } else {
        throw "Python 3 is required for $Name."
    }
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try {
    if ($SkipRestore -and -not $SkipSmoke) {
        throw "-SkipRestore can only be used with -SkipSmoke because the smoke and Android graphs share obj assets."
    }
    Write-Host "==> Verify pinned builder image"
    & docker image inspect $builderImage *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Pinned builder image '$builderImage' is unavailable. Do not substitute another SDK."
    }

    New-Item -ItemType Directory -Force $nugetCache, $outputFull | Out-Null

    # Remove only the two exact products owned by this script. If any later
    # step fails, no APK from an older invocation remains available to sign.
    $packagedApk = Join-Path $outputFull "com.zelda.ladxhd.archipelago-Signed.apk"
    $unsignedApk = Join-Path $outputFull "com.zelda.ladxhd.archipelago-framework.apk"
    Remove-Item -LiteralPath $packagedApk, $unsignedApk -Force -ErrorAction SilentlyContinue

    if (-not $SkipPublicSourceGuard) {
        Invoke-PythonTool "Verify public source boundary" @("tools/verify_public_source.py")
    }

    if (-not $SkipRestore) {
        if (-not $SkipSmoke) {
            Invoke-DockerStep "Restore smoke-test graph" @(
                "dotnet", "restore", $smokeProject
            )
            Invoke-DockerStep "Run Archipelago and wallpaper regressions" @(
                "dotnet", "run", "--project", $smokeProject,
                "-c", "Release", "--no-restore"
            )
        }
        # Core and the vendored AP client share obj/project.assets.json with
        # the Android graph. Run smoke first, then restore Android last so its
        # net9.0/android-arm64 targets remain in place for build and publish.
        Invoke-DockerStep "Restore Android ARM64 graph" @(
            "dotnet", "restore", $androidProject,
            "-r", "android-arm64",
            "-p:RuntimeIdentifiers=android-arm64"
        )
    }

    # This step is deliberately separate from Core and smoke compilation. It
    # catches Android binding/API visibility errors before the slower publish.
    Invoke-DockerStep "Compile Android ARM64 platform surface" @(
        "dotnet", "build", $androidProject,
        "-c", "Release", "-r", "android-arm64", "--no-restore",
        "-p:RuntimeIdentifiers=android-arm64"
    )

    Invoke-DockerStep "Publish assetless Android ARM64 APK" @(
        "dotnet", "publish", $androidProject,
        "-c", "Release", "-r", "android-arm64", "--no-restore",
        "-p:RuntimeIdentifiers=android-arm64",
        "-p:PublishDir=/src/$outputRelative/"
    )

    # .NET Android calls its framework-packaged output "-Signed.apk" even
    # though it has not been signed with this project's release lineage. Move
    # it to an intentionally unambiguous name before it can be consumed by the
    # separate release-signing step.
    if (-not (Test-Path -LiteralPath $packagedApk -PathType Leaf)) {
        throw "Publish completed without the expected framework-packaged APK: $packagedApk"
    }
    Move-Item -LiteralPath $packagedApk -Destination $unsignedApk -Force
    Invoke-PythonTool "Verify assetless APK contents" @(
        "tools/verify_assetless_apk.py", $unsignedApk
    )

    $hash = (Get-FileHash -LiteralPath $unsignedApk -Algorithm SHA256).Hash
    Write-Host "ARM64 framework package verified (not release-lineage signed)."
    Write-Host "APK: $unsignedApk"
    Write-Host "SHA-256: $hash"
    Write-Output $unsignedApk
} finally {
    Pop-Location
}
