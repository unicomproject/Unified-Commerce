$src = "C:\ProgramData\E_POS\LocalPrintAgent\logs"
$dst = "C:\Users\User\Downloads\EPOS\.cursor-agent-drawer-safe.log"
Remove-Item $dst -Force -ErrorAction SilentlyContinue
Get-ChildItem $src -Filter "*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object {
  Select-String -Path $_.FullName -Pattern "bytesHex|Drawer pulse|drawer_pulse_accepted|e530389b|6e28fa63" | ForEach-Object { $_.Line } | Set-Content $dst
}
if (-not (Test-Path $dst)) { "NO_MATCHES" | Set-Content $dst }
