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
    [switch]$Force,
    [switch]$ValidateOnly,
    [switch]$SkipFirewall
)
$ErrorActionPreference = "Stop"
$serviceName = "E_POS.LocalPrintAgent"
$displayName = "E_POS Local Print Agent"
$firewallName = "E_POS Local Print Agent $Port"

function Test-IsAdministrator {
    return ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
        ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-IsLoopbackOnly {
    param([string[]]$Ranges)
    if ($Ranges.Count -eq 0) { return $false }
    foreach ($range in $Ranges) {
        if ($range -notin @("127.0.0.1/32", "::1/128", "127.0.0.0/8")) {
            return $false
        }
    }
    return $true
}

function Test-ApiKeyAcceptable {
    param([string]$Key)
    if ([string]::IsNullOrWhiteSpace($Key) -or $Key.Length -lt 24) { return $false }
    $placeholders = @(
        "CHANGE_ME", "CHANGEME", "PASSWORD", "SECRET", "DEFAULT",
        "LOCAL-PRINT-KEY", "YOUR_API_KEY", "REPLACE_ME", "TODO", "TEST", "SAMPLE", "EXAMPLE"
    )
    foreach ($token in $placeholders) {
        if ($Key.ToUpperInvariant().Contains($token)) { return $false }
    }
    $unique = ($Key.ToCharArray() | Select-Object -Unique).Count
    return $unique -ge 8
}

if (-not (Test-IsAdministrator) -and -not $ValidateOnly) {
    throw "Run this script from an elevated Administrator PowerShell."
}

$source = (Resolve-Path -LiteralPath $PublishDirectory).Path
$exe = Join-Path $source "E_POS.LocalPrintAgent.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Published executable not found: $exe. Run publish-print-agent.ps1 first."
}
if (-not (Get-Printer -Name $PrinterName -ErrorAction SilentlyContinue)) {
    throw "Windows printer queue '$PrinterName' was not found. Install the driver/queue first."
}

$cidr = '^(?:\d{1,3}\.){3}\d{1,3}/(?:[0-9]|[12][0-9]|3[0-2])$|^(?:[0-9a-fA-F:]+)/(?:[0-9]|[1-9][0-9]|1[01][0-9]|12[0-8])$'
if ($AllowedNetworkRanges.Count -eq 0 -or
    ($AllowedNetworkRanges | Where-Object { $_ -notmatch $cidr })) {
    throw "AllowedNetworkRanges must contain explicit CIDR values such as 127.0.0.1/32 or 192.168.18.0/24."
}

$loopbackOnly = Test-IsLoopbackOnly -Ranges $AllowedNetworkRanges
if ($UseHttps) {
    if (-not (Test-Path -LiteralPath $CertificatePath)) {
        throw "HTTPS certificate PFX was not found."
    }
    if ([string]::IsNullOrWhiteSpace($AgentHostName)) {
        throw "AgentHostName is required for HTTPS and must match the certificate SAN."
    }
}

if ($ValidateOnly) {
    Write-Host "Validation passed. Administrator at validation time: $(Test-IsAdministrator)."
    Write-Host "Loopback-only binding: $loopbackOnly"
    Write-Host "No service, files, secrets, ACLs, or firewall rules were changed."
    return
}

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing -and -not $Force) {
    throw "Service '$serviceName' already exists. Re-run with -Force to reinstall/update, or use update-print-agent.ps1."
}

$apiKey = Read-Host "Enter Local Print Agent API key (minimum 24 characters, store-specific)" -AsSecureString
$credential = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey)
$certificateCredential = [IntPtr]::Zero
try {
    $plainKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($credential)
    if (-not (Test-ApiKeyAcceptable -Key $plainKey)) {
        throw "API key rejected: must be ≥24 characters, store-specific, and must not use placeholder/default values."
    }

    $scheme = "http"
    $healthHost = "127.0.0.1"
    $listenHost = if ($loopbackOnly) { "127.0.0.1" } else { "0.0.0.0" }
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
        $listenHost = "0.0.0.0"
    }

    New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $DataDirectory -Force | Out-Null
    $operationsDir = Join-Path $DataDirectory "operations"
    $logsDir = Join-Path $DataDirectory "logs"
    New-Item -ItemType Directory -Path $operationsDir -Force | Out-Null
    New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

    if ($UseHttps) {
        $protectedCertificate = Join-Path $DataDirectory "print-agent.pfx"
        Copy-Item -LiteralPath $CertificatePath -Destination $protectedCertificate -Force
        $serviceEnvironment += @(
            "ASPNETCORE_Kestrel__Certificates__Default__Path=$protectedCertificate",
            "ASPNETCORE_Kestrel__Certificates__Default__Password=$plainCertificatePassword"
        )
    }

    if ($existing) {
        Write-Host "Stopping existing service for -Force reinstall..."
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
        & sc.exe delete $serviceName | Out-Null
        Start-Sleep -Seconds 2
    }

    Copy-Item -Path (Join-Path $source "*") -Destination $InstallDirectory -Recurse -Force

    $config = @{
        PrintAgent = @{
            ListenUrl = "${scheme}://${listenHost}:$Port"
            PrinterName = $PrinterName
            PaperWidth = "80mm"
            AutoCut = $true
            FeedLinesBeforeCut = 5
            SpoolerTimeoutSeconds = 5
            LocalApiKey = ""
            DrawerRequestMaxAgeSeconds = 120
            IdempotencyDirectory = $operationsDir
            OperationRetentionDays = 30
            AllowedNetworkRanges = $AllowedNetworkRanges
            RequestBodyLimit = 262144
            LoggingDirectory = $logsDir
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

    # Remove published development example secrets/config if present
    $devSettings = Join-Path $InstallDirectory "appsettings.Development.json"
    if (Test-Path -LiteralPath $devSettings) {
        Remove-Item -LiteralPath $devSettings -Force
    }

    & icacls $DataDirectory /inheritance:r /grant:r `
        "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "LOCAL SERVICE:(OI)(CI)M" | Out-Null
    & icacls $InstallDirectory /grant:r "LOCAL SERVICE:(OI)(CI)RX" | Out-Null

    $installedExe = Join-Path $InstallDirectory "E_POS.LocalPrintAgent.exe"
    & sc.exe create $serviceName binPath= "`"$installedExe`"" `
        start= delayed-auto obj= "NT AUTHORITY\LocalService" DisplayName= $displayName | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sc.exe create failed with exit code $LASTEXITCODE" }

    & sc.exe description $serviceName "Private-LAN RAW receipt print agent for E_POS." | Out-Null
    & sc.exe failure $serviceName reset= 86400 `
        actions= restart/60000/restart/120000/restart/300000 | Out-Null
    & sc.exe failureflag $serviceName 1 | Out-Null

    $serviceKey = "HKLM:\SYSTEM\CurrentControlSet\Services\$serviceName"
    # Ensure service working directory is the install folder (not System32).
    New-ItemProperty -Path $serviceKey -Name AppDirectory -PropertyType String -Force `
        -Value $InstallDirectory | Out-Null
    New-ItemProperty -Path $serviceKey -Name Environment -PropertyType MultiString -Force `
        -Value $serviceEnvironment | Out-Null

    Remove-NetFirewallRule -DisplayName $firewallName -ErrorAction SilentlyContinue
    if (-not $SkipFirewall -and -not $loopbackOnly) {
        New-NetFirewallRule -DisplayName $firewallName -Direction Inbound -Protocol TCP `
            -LocalPort $Port -Action Allow -Profile Private `
            -Program $installedExe -RemoteAddress $AllowedNetworkRanges | Out-Null
        Write-Host "Firewall rule created for Private profile / configured CIDR only."
    }
    else {
        Write-Host "Firewall inbound rule skipped (loopback-only or -SkipFirewall)."
    }

    Start-Service -Name $serviceName
    $deadline = (Get-Date).AddSeconds(60)
    $ready = $null
    do {
        Start-Sleep -Seconds 2
        try {
            $ready = Invoke-RestMethod "${scheme}://${healthHost}:$Port/health/ready" -TimeoutSec 3
        }
        catch { $ready = $null }
    } until (($ready -and $ready.ready -eq $true) -or (Get-Date) -gt $deadline)

    if (-not $ready -or $ready.ready -ne $true) {
        throw "Service installed but readiness did not become healthy. Check Event Log and $logsDir."
    }

    Write-Host "Installed and verified '$serviceName'."
    Write-Host "ListenUrl=${scheme}://${listenHost}:$Port"
    Write-Host "Customer daily operation does NOT require 'dotnet run'."
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
