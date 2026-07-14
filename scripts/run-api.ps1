# Runs the API from a trusted Windows path.
# Windows Application Control blocks DLLs when the build is started from Desktop.
param(
    [switch]$SyncOnly,
    [switch]$Migrate,
    [string]$Urls = 'http://0.0.0.0:5150'
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$runRoot = Join-Path $env:USERPROFILE 'source\nytroz-pos-api-run'
$apiDir = Join-Path $runRoot 'src\E_POS.Api'
$apiProject = Join-Path $apiDir 'E_POS.Api.csproj'
$apiDll = Join-Path $apiDir 'bin\Debug\net10.0\E_POS.Api.dll'
$infraProject = Join-Path $runRoot 'src\E_POS.Infrastructure\E_POS.Infrastructure.csproj'

function Stop-ExistingApi {
    Write-Host 'Stopping any existing API process...'

    Get-NetTCPConnection -LocalPort 5150 -ErrorAction SilentlyContinue |
        ForEach-Object {
            Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
        }

    Get-Process -Name 'E_POS.Api' -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue

    Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object {
            $_.CommandLine -like '*nytroz-pos-api-run*' -or
            $_.CommandLine -like '*E_POS.Api.dll*'
        } |
        ForEach-Object {
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }

    Start-Sleep -Seconds 1
}

Write-Host 'Nytroz POS API launcher'
Write-Host "  Source: $repoRoot"
Write-Host "  Run at: $runRoot"
Write-Host ''

Stop-ExistingApi

New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

Write-Host 'Syncing project files...'
robocopy $repoRoot $runRoot /MIR /XD bin obj .git /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "Robocopy failed with exit code $LASTEXITCODE"
}

Get-ChildItem -Path $runRoot -Recurse -File -ErrorAction SilentlyContinue |
    Unblock-File -ErrorAction SilentlyContinue

if ($SyncOnly) {
    Write-Host 'Sync complete.'
    exit 0
}

Push-Location $apiDir
try {
    Write-Host 'Building API...'
    dotnet build $apiProject
    if ($LASTEXITCODE -ne 0) {
        throw 'Build failed.'
    }

    if ($Migrate) {
        Write-Host 'Applying database migrations...'
        dotnet ef database update --project $infraProject --startup-project $apiProject
        if ($LASTEXITCODE -ne 0) {
            throw 'Database migration failed.'
        }
    }

    if (-not (Test-Path $apiDll)) {
        throw "API build output not found: $apiDll"
    }

    Write-Host ''
    Write-Host "Starting API at $Urls"
    Write-Host 'Press Ctrl+C to stop.'
    Write-Host ''

    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = $Urls
    dotnet exec $apiDll
}
finally {
    Pop-Location
}

