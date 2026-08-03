[CmdletBinding()]
param(
    [string]$Version = "1.0.0",
    [string]$ArtifactsDirectory = "$PSScriptRoot\..\..\artifacts\local-print-agent"
)
$ErrorActionPreference = "Stop"
$artifacts = [IO.Path]::GetFullPath($ArtifactsDirectory)
$publish = Join-Path $artifacts "publish"
& "$PSScriptRoot\publish-print-agent.ps1" -OutputDirectory $publish
$stage = Join-Path $artifacts "package"
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null
Copy-Item -LiteralPath $publish -Destination (Join-Path $stage "app") -Recurse
Copy-Item -LiteralPath $PSScriptRoot -Destination (Join-Path $stage "scripts") -Recurse
$archive = Join-Path $artifacts "E_POS.LocalPrintAgent-$Version-win-x64.zip"
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $archive -CompressionLevel Optimal
Write-Host "Created package $archive"
