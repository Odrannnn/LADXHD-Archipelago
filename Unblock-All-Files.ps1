$BaseFolder = Split-Path $script:MyInvocation.MyCommand.Path

foreach ($File in Get-Childitem -LiteralPath $BaseFolder -Recurse)
{
    Unblock-File -Path $File.FullName
    Write-Host ("Unblocking file: " + $File.Name)
}
Write-Host ""
Write-Host "Finished unblocking all files. Press any key to close this window."
[void][System.Console]::ReadKey($true)