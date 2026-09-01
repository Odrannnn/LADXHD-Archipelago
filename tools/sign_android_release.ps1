[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$InputApk,
    [Parameter(Mandatory)] [string]$OutputApk,
    [Parameter(Mandatory)] [string]$BuildToolsDirectory,
    [Parameter(Mandatory)] [string]$LegacyKeystore,
    [Parameter(Mandatory)] [string]$PermanentPkcs12,
    [Parameter(Mandatory)] [string]$PermanentStorePasswordFile,
    [Parameter(Mandatory)] [string]$PermanentKeyPasswordFile,
    [Parameter(Mandatory)] [string]$SigningLineage,
    [string]$LegacyAlias = "androiddebugkey",
    [string]$ExpectedPermanentCertificateSha256 =
        "05459e510a84c042ad2ded1587c7f6ff0817bf63e397bdaa50a2d8054145039d"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RequiredFile([string]$Path, [string]$Label) {
    $resolved = [IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "$Label does not exist: $resolved"
    }
    return $resolved
}

function Invoke-Checked([string]$Name, [string]$Program, [string[]]$Arguments) {
    Write-Host "==> $Name"
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

$inputFull = Resolve-RequiredFile $InputApk "Input APK"
$legacyFull = Resolve-RequiredFile $LegacyKeystore "Legacy keystore"
$permanentFull = Resolve-RequiredFile $PermanentPkcs12 "Permanent PKCS#12 keystore"
$storePasswordFull = Resolve-RequiredFile $PermanentStorePasswordFile "Permanent store-password file"
$keyPasswordFull = Resolve-RequiredFile $PermanentKeyPasswordFile "Permanent key-password file"
$lineageFull = Resolve-RequiredFile $SigningLineage "Signing lineage"
$outputFull = [IO.Path]::GetFullPath($OutputApk)
if ([StringComparer]::OrdinalIgnoreCase.Equals($inputFull, $outputFull)) {
    throw "InputApk and OutputApk must be different files."
}
$outputParent = [IO.Path]::GetDirectoryName($outputFull)
if ([string]::IsNullOrWhiteSpace($outputParent)) {
    throw "Output APK must have a parent directory."
}
New-Item -ItemType Directory -Force $outputParent | Out-Null

$toolsFull = [IO.Path]::GetFullPath($BuildToolsDirectory)
$zipalign = Join-Path $toolsFull "zipalign.exe"
$apksigner = Join-Path $toolsFull "apksigner.bat"
if (-not (Test-Path -LiteralPath $zipalign -PathType Leaf) -or
    -not (Test-Path -LiteralPath $apksigner -PathType Leaf)) {
    throw "BuildToolsDirectory must contain zipalign.exe and apksigner.bat: $toolsFull"
}

$temporaryAligned = Join-Path ([IO.Path]::GetTempPath()) (
    "ladxhd-" + [Guid]::NewGuid().ToString("N") + "-aligned-unsigned.apk")
$temporarySigned = Join-Path ([IO.Path]::GetTempPath()) (
    "ladxhd-" + [Guid]::NewGuid().ToString("N") + "-verified-signed.apk")
try {
    Invoke-Checked "Align unsigned APK" $zipalign @(
        "-P", "16", "-f", "4", $inputFull, $temporaryAligned
    )
    Invoke-Checked "Verify alignment" $zipalign @(
        "-c", "-P", "16", "4", $temporaryAligned
    )
    Invoke-Checked "Sign with compatibility lineage" $apksigner @(
        "sign", "--out", $temporarySigned,
        "--v1-signing-enabled", "false",
        "--v2-signing-enabled", "true",
        "--v3-signing-enabled", "true",
        "--v4-signing-enabled", "false",
        "--lineage", $lineageFull,
        "--rotation-min-sdk-version", "28",
        "--ks", $legacyFull,
        "--ks-key-alias", $LegacyAlias,
        "--ks-pass", "pass:android",
        "--key-pass", "pass:android",
        "--next-signer",
        "--ks", $permanentFull,
        "--ks-type", "PKCS12",
        "--ks-pass", "file:$storePasswordFull",
        "--key-pass", "file:$keyPasswordFull",
        $temporaryAligned
    )
    Invoke-Checked "Verify Android 7-8.1 signature" $apksigner @(
        "verify", "--verbose", "--min-sdk-version", "24",
        "--max-sdk-version", "27", $temporarySigned
    )
    Invoke-Checked "Verify Android 9+ rotated signature" $apksigner @(
        "verify", "--verbose", "--print-certs",
        "--min-sdk-version", "28", "--max-sdk-version", "35", $temporarySigned
    )

    $certificateOutput = & $apksigner verify --print-certs `
        --min-sdk-version 28 --max-sdk-version 35 $temporarySigned 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Certificate fingerprint verification failed."
    }
    $normalized = ($certificateOutput -join "`n").ToLowerInvariant()
    if (-not $normalized.Contains($ExpectedPermanentCertificateSha256.ToLowerInvariant())) {
        throw "Signed APK does not expose the expected permanent certificate for API 28+."
    }

    # Promote only an already verified file. A failed attempt can never leave a
    # stale or partially signed requested output masquerading as the new APK.
    Move-Item -LiteralPath $temporarySigned -Destination $outputFull -Force
    $hash = (Get-FileHash -LiteralPath $outputFull -Algorithm SHA256).Hash
    Write-Host "Signed APK verified."
    Write-Host "APK: $outputFull"
    Write-Host "SHA-256: $hash"
    Write-Output $outputFull
} finally {
    if (Test-Path -LiteralPath $temporaryAligned) {
        Remove-Item -LiteralPath $temporaryAligned -Force
    }
    if (Test-Path -LiteralPath $temporarySigned) {
        Remove-Item -LiteralPath $temporarySigned -Force
    }
}
