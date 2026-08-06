[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][string]$PrinterName,
    [Parameter(Mandatory)][string[]]$AllowedNetworkRanges,
    [string]$InstallDirectory = "$env:ProgramFiles\E_POS\LocalPrintAgent",
    [string]$DataDirectory = "$env:ProgramData\E_POS\LocalPrintAgent",
    [int]$Port = 9101,
    [switch]$UseHttps,
    [string]$CertificatePath,
    [string]$AgentHostName,
    [switch]$ValidateOnly
)
$ErrorActionPreference = "Stop"
$serviceName = "E_POS.LocalPrintAgent"
$displayName = "E_POS Local Print Agent"
$firewallName = "E_POS Local Print Agent $Port"
$isAdministrator = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdministrator -and -not $ValidateOnly) {
    throw "Run this script from an elevated Administrator PowerShell."
}
$source = (Resolve-Path -LiteralPath $PublishDirectory).Path
$exe = Join-Path $source "E_POS.LocalPrintAgent.exe"
if (-not (Test-Path -LiteralPath $exe)) { throw "Published executable not found: $exe" }
if (-not (Get-Printer -Name $PrinterName -ErrorAction SilentlyContinue)) {
    throw "Windows printer queue '$PrinterName' was not found."
}
$cidr = '^(?:\d{1,3}\.){3}\d{1,3}/(?:[0-9]|[12][0-9]|3[0-2])$'
if ($AllowedNetworkRanges.Count -eq 0 -or
    ($AllowedNetworkRanges | Where-Object { $_ -notmatch $cidr })) {
    throw "AllowedNetworkRanges must contain explicit IPv4 CIDR values such as 192.168.18.0/24."
}
if ($UseHttps) {
    if (-not (Test-Path -LiteralPath $CertificatePath)) {
        throw "HTTPS certificate PFX was not found."
    }
    if ([string]::IsNullOrWhiteSpace($AgentHostName)) {
        throw "AgentHostName is required for HTTPS and must match the certificate SAN."
    }
}
if ($ValidateOnly) {
    Write-Host "Validation passed. Administrator at validation time: $isAdministrator. Installation still requires elevation."
    Write-Host "No service, files, secrets, ACLs, or firewall rules were changed."
    return
}
$apiKey = Read-Host "Enter Local Print Agent API key (minimum 24 characters)" -AsSecureString
$credential = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey)
$certificateCredential = [IntPtr]::Zero
try {
    $plainKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($credential)
    if ($plainKey.Length -lt 24) { throw "API key must contain at least 24 characters." }
    $scheme = "http"
    $healthHost = "localhost"
    $serviceEnvironment = @(
        "ASPNETCORE_ENVIRONMENT=Production",
        "PrintAgent__LocalApiKey=$plainKey"
    )
    if ($UseHttps) {
        $certificatePassword = Read-Host "Enter HTTPS certificate PFX password" -AsSecureString
        $certificateCredential = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($certificatePassword)
        $plainCertificatePassword =
            [Runtime.InteropServices.Marshal]::PtrToStringBSTR($certificateCredential)
        $scheme = "https"
        $healthHost = $AgentHostName
    }
    New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $DataDirectory -Force | Out-Null
    if ($UseHttps) {
        $protectedCertificate = Join-Path $DataDirectory "print-agent.pfx"
        Copy-Item -LiteralPath $CertificatePath -Destination $protectedCertificate -Force
        $serviceEnvironment += @(
            "ASPNETCORE_Kestrel__Certificates__Default__Path=$protectedCertificate",
            "ASPNETCORE_Kestrel__Certificates__Default__Password=$plainCertificatePassword"
        )
    }
    Copy-Item -Path (Join-Path $source "*") -Destination $InstallDirectory -Recurse -Force
    $config = @{
        PrintAgent = @{
            ListenUrl = "${scheme}://0.0.0.0:$Port"
            PrinterName = $PrinterName
            PaperWidth = "80mm"
            AutoCut = $true
            FeedLinesBeforeCut = 5
            SpoolerTimeoutSeconds = 5
            LocalApiKey = ""
            IdempotencyDirectory = (Join-Path $DataDirectory "operations")
            OperationRetentionDays = 30
            AllowedNetworkRanges = $AllowedNetworkRanges
            RequestBodyLimit = 262144
            LoggingDirectory = (Join-Path $DataDirectory "logs")
            LogRetentionDays = 14
            MaxLogFileBytes = 10485760
            MinimumFreeDiskBytes = 104857600
            FailedAuthenticationLimit = 10
            FailedAuthenticationWindowMinutes = 5
        }
        Logging = @{ LogLevel = @{ Default = "Information"; "Microsoft.AspNetCore" = "Warning" } }
    }
    $config | ConvertTo-Json -Depth 6 |
        Set-Content -LiteralPath (Join-Path $InstallDirectory "appsettings.json") -Encoding UTF8
    & icacls $DataDirectory /inheritance:r /grant:r `
        "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "LOCAL SERVICE:(OI)(CI)M" | Out-Null
    if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
        throw "Service already exists. Use update-print-agent.ps1."
    }
    $installedExe = Join-Path $InstallDirectory "E_POS.LocalPrintAgent.exe"
    & sc.exe create $serviceName binPath= "`"$installedExe`"" `
        start= delayed-auto obj= "NT AUTHORITY\LocalService" DisplayName= $displayName | Out-Null
    & sc.exe description $serviceName "Private-LAN RAW receipt print agent for E_POS." | Out-Null
    & sc.exe failure $serviceName reset= 86400 `
        actions= restart/60000/restart/120000/restart/300000 | Out-Null
    & sc.exe failureflag $serviceName 1 | Out-Null
    $serviceKey = "HKLM:\SYSTEM\CurrentControlSet\Services\$serviceName"
    New-ItemProperty -Path $serviceKey -Name Environment -PropertyType MultiString -Force `
        -Value $serviceEnvironment | Out-Null
    Remove-NetFirewallRule -DisplayName $firewallName -ErrorAction SilentlyContinue
    New-NetFirewallRule -DisplayName $firewallName -Direction Inbound -Protocol TCP `
        -LocalPort $Port -Action Allow -Profile Private -RemoteAddress $AllowedNetworkRanges | Out-Null
    Start-Service -Name $serviceName
    $deadline = (Get-Date).AddSeconds(45)
    do {
        Start-Sleep -Seconds 2
        try {
            $ready = Invoke-RestMethod "${scheme}://${healthHost}:$Port/health/ready" -TimeoutSec 3
        } catch { $ready = $null }
    } until ($ready.ready -eq $true -or (Get-Date) -gt $deadline)
    if ($ready.ready -ne $true) { throw "Service installed but readiness did not become healthy." }
    Write-Host "Installed and verified '$serviceName'."
}
catch {
    if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
        & sc.exe delete $serviceName | Out-Null
    }
    Remove-NetFirewallRule -DisplayName $firewallName -ErrorAction SilentlyContinue
    throw
}
finally {
    if ($credential -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($credential)
    }
    if ($certificateCredential -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($certificateCredential)
    }
    $plainKey = $null
    $plainCertificatePassword = $null
}
