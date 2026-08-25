[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDirectory = "$PSScriptRoot\..\..\artifacts\local-print-agent\publish",
    [bool]$SelfContained = $true
)
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot\..\..").Path
$project = Join-Path $repo "tools\E_POS.LocalPrintAgent\E_POS.LocalPrintAgent.csproj"
$tests = Join-Path $repo "tests\E_POS.LocalPrintAgent.Tests\E_POS.LocalPrintAgent.Tests.csproj"
$output = [IO.Path]::GetFullPath($OutputDirectory)

dotnet clean $project --configuration $Configuration
dotnet restore $project --runtime $Runtime
dotnet restore $tests
dotnet build $project --configuration $Configuration --runtime $Runtime --no-restore
dotnet test $tests --configuration $Configuration --no-restore -m:1
if (Test-Path -LiteralPath $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}
dotnet publish $project --configuration $Configuration --runtime $Runtime `
    --self-contained:$($SelfContained.ToString().ToLowerInvariant()) `
    --no-restore --output $output `
    -p:DebugType=None -p:DebugSymbols=false `
    -p:PublishSingleFile=false

Get-ChildItem -LiteralPath $output -Recurse -Force |
    Where-Object {
        $_.Name -in @(
            ".git", "data", "appsettings.Development.json",
            "launchSettings.json"
        ) -or $_.FullName -match '\\Properties\\'
    } |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
Write-Host "Published Local Print Agent to $output"
Write-Host "Self-contained=$SelfContained Runtime=$Runtime"
Write-Host "Install with scripts/local-print-agent/install-print-agent.ps1 (not dotnet run)."
