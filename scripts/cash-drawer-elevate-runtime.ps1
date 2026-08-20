#Requires -RunAsAdministrator
$ErrorActionPreference = 'Continue'
$ok = 'C:\Users\User\Downloads\EPOS\.cursor-cash-drawer-runtime-ok.txt'
Remove-Item $ok -Force -ErrorAction SilentlyContinue
Write-Host 'Stopping listeners on 5150...'
Get-NetTCPConnection -LocalPort 5150 -State Listen -ErrorAction SilentlyContinue | ForEach-Object {
  Write-Host ("Kill PID {0}" -f $_.OwningProcess)
  Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
  & taskkill.exe /PID $_.OwningProcess /F /T 2>&1 | Out-Host
}
Start-Sleep -Seconds 2
$still = Get-NetTCPConnection -LocalPort 5150 -State Listen -ErrorAction SilentlyContinue
if ($still) {
  "STILL_OCCUPIED pid=$($still.OwningProcess)" | Set-Content $ok
  exit 1
}
# Also refresh LocalPrintAgent from published artifacts (preserve config via Force install script pattern)
$publish = 'C:\artifacts\local-print-agent\publish'
$installDir = "$env:ProgramFiles\E_POS\LocalPrintAgent"
if (Test-Path $publish) {
  Write-Host 'Stopping LocalPrintAgent service...'
  Stop-Service -Name 'E_POS.LocalPrintAgent' -Force -ErrorAction SilentlyContinue
  Start-Sleep -Seconds 2
  Write-Host 'Copying published agent binaries...'
  robocopy $publish $installDir /E /XO /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
  Start-Service -Name 'E_POS.LocalPrintAgent' -ErrorAction SilentlyContinue
  Start-Sleep -Seconds 3
}
# Start CURRENT api on 5150 from artifacts (built from Unified-Commerce)
$dll = 'C:\artifacts\epos-api-current\E_POS.Api.dll'
if (-not (Test-Path $dll)) { "MISSING_DLL" | Set-Content $ok; exit 2 }
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://0.0.0.0:5150'
$p = Start-Process -FilePath 'dotnet' -ArgumentList @('exec',$dll) -WorkingDirectory 'C:\artifacts\epos-api-current' -PassThru -WindowStyle Minimized
Start-Sleep -Seconds 8
$conn = Get-NetTCPConnection -LocalPort 5150 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $conn) { "API_NOT_LISTENING" | Set-Content $ok; exit 3 }
$cmd = (Get-CimInstance Win32_Process -Filter "ProcessId=$($conn.OwningProcess)").CommandLine
@(
  "OK",
  "pid=$($conn.OwningProcess)",
  "cmdline=$cmd",
  "source=C:\artifacts\epos-api-current (built from Unified-Commerce)",
  "agentService=$((Get-Service E_POS.LocalPrintAgent).Status)"
) | Set-Content $ok
Write-Host (Get-Content $ok -Raw)
exit 0
