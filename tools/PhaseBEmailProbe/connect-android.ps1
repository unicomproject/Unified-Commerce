param([string]$Serial)
$ErrorActionPreference='Stop'
$adb=Join-Path $env:LOCALAPPDATA 'Android/Sdk/platform-tools/adb.exe'
if(!(Test-Path -LiteralPath $adb)){throw 'Android SDK adb is unavailable.'}
$devices=@(& $adb devices | Select-Object -Skip 1 | Where-Object {$_ -match '\sdevice$'} | ForEach-Object {($_ -split '\s+')[0]})
if(!$Serial){if($devices.Count -ne 1){throw 'Connect and authorize one Android device, or specify -Serial.'};$Serial=$devices[0]}
if($devices -notcontains $Serial){throw 'Selected Android device is not connected and authorized.'}
& $adb -s $Serial reverse tcp:4200 tcp:4200
if($LASTEXITCODE -ne 0){throw 'Invitation bridge port forwarding failed.'}
& $adb -s $Serial reverse tcp:5151 tcp:5151
if($LASTEXITCODE -ne 0){throw 'API port forwarding failed.'}
$apk=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../Tenantadmin/Nytroz-POS-App/build/app/outputs/flutter-apk/app-debug.apk'))
if(!(Test-Path -LiteralPath $apk)){throw 'Build the development APK first.'}
& $adb -s $Serial install -r $apk
if($LASTEXITCODE -ne 0){throw 'APK install failed; no uninstall or data deletion was attempted.'}
Write-Output 'Development phone connected. Open the existing email invitation and tap Open app.'
