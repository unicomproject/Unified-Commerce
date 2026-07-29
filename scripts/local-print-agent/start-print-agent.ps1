#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"
Start-Service -Name "E_POS.LocalPrintAgent"
Get-Service -Name "E_POS.LocalPrintAgent"
