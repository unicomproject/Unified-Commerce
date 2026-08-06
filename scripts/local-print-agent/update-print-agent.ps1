#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [string]$InstallDirectory = "$env:ProgramFiles\E_POS\LocalPrintAgent",
    [string]$BackupDirectory = "$env:ProgramData\E_POS\LocalPrintAgent\backups",
    [string]$HealthBaseUrl = "http://localhost:9101"
)
$ErrorActionPreference = "Stop"
$serviceName = "E_POS.LocalPrintAgent"
$source = (Resolve-Path -LiteralPath $PublishDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $source "E_POS.LocalPrintAgent.exe"))) {
    throw "Published executable is missing."
}
$backup = Join-Path $BackupDirectory (Get-Date -Format "yyyyMMdd-HHmmss")
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Stop-Service -Name $serviceName
Copy-Item -LiteralPath $InstallDirectory -Destination $backup -Recurse
try {
    $savedConfig = Get-Content -Raw -LiteralPath (Join-Path $InstallDirectory "appsettings.json")
    Copy-Item -Path (Join-Path $source "*") -Destination $InstallDirectory -Recurse -Force
    $savedConfig | Set-Content -LiteralPath (Join-Path $InstallDirectory "appsettings.json") -Encoding UTF8
    Start-Service -Name $serviceName
    $deadline = (Get-Date).AddSeconds(45)
    do {
        Start-Sleep -Seconds 2
        try { $ready = Invoke-RestMethod "$HealthBaseUrl/health/ready" -TimeoutSec 3 } catch { $ready = $null }
    } until ($ready.ready -eq $true -or (Get-Date) -gt $deadline)
    if ($ready.ready -ne $true) { throw "Updated service did not become ready." }
    Write-Host "Update verified. Backup: $backup"
}
catch {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $InstallDirectory -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $backup (Split-Path $InstallDirectory -Leaf)) `
        -Destination $InstallDirectory -Recurse
    Start-Service -Name $serviceName
    throw "Update failed and previous application files were restored. $($_.Exception.Message)"
}
