#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$InstallDirectory = "$env:ProgramFiles\E_POS\LocalPrintAgent",
    [string]$DataDirectory = "$env:ProgramData\E_POS\LocalPrintAgent",
    [int]$Port = 9101,
    [switch]$DeleteOperationalData
)
$ErrorActionPreference = "Stop"
$serviceName = "E_POS.LocalPrintAgent"
Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    & sc.exe delete $serviceName | Out-Null
}
Remove-NetFirewallRule -DisplayName "E_POS Local Print Agent $Port" -ErrorAction SilentlyContinue
if (Test-Path -LiteralPath $InstallDirectory) {
    Remove-Item -LiteralPath $InstallDirectory -Recurse -Force
}
if ($DeleteOperationalData -and (Test-Path -LiteralPath $DataDirectory)) {
    Remove-Item -LiteralPath $DataDirectory -Recurse -Force
    Write-Warning "Operational idempotency and log data was explicitly deleted."
} else {
    Write-Host "Operational data preserved at $DataDirectory"
}
Write-Host "Service removed. Windows printer driver and printer queue were not modified."
