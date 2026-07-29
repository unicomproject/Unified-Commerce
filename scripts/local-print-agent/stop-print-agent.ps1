#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"
Stop-Service -Name "E_POS.LocalPrintAgent"
Get-Service -Name "E_POS.LocalPrintAgent"
