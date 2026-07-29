# E_POS Local Print Agent Production Deployment

## Scope and architecture

```text
Physical Android POS
  -> trusted store LAN
  -> E_POS backend
  -> E_POS Local Print Agent Windows Service
  -> Windows RAW spooler
  -> USB POSPrinter POS80
```

The public backend never opens the laptop USB printer. The agent has no CORS,
Swagger, directory browsing, or development exception page.

## Supported baseline

- Windows 10/11 or supported Windows Server x64.
- `POSPrinter POS80` driver and queue installed and Windows test page verified.
- Private store network with a known IPv4 CIDR.
- Administrator access for install/update/uninstall only.
- Normal runtime account: `NT AUTHORITY\LocalService`.

Production packaging is self-contained `win-x64`; installing a separate .NET
runtime is not required. Development console execution remains supported.

## Publish and package

```powershell
cd "<repo>\POS Backend\Unified-Commerce"
.\scripts\local-print-agent\publish-print-agent.ps1
.\scripts\local-print-agent\package-print-agent.ps1 -Version "<version>"
```

Output:

```text
artifacts\local-print-agent\publish
artifacts\local-print-agent\E_POS.LocalPrintAgent-<version>-win-x64.zip
```

No API key is included. Publish removes development configuration, runtime data,
debug symbols, test data, and source-control metadata.

## HTTP/HTTPS decision

Android release configuration rejects clear-text HTTP. Production release
therefore requires HTTPS with a certificate trusted by the POS device. Configure
Kestrel certificate settings through protected service configuration and use an
agent URL whose hostname is present in the certificate SAN.

Restricted HTTP may be used only as an explicitly accepted private-LAN
transitional deployment boundary. It is not equivalent to HTTPS. It requires
all of:

- Windows network profile `Private`.
- Firewall remote addresses restricted to the store CIDR.
- matching `AllowedNetworkRanges` inside the agent.
- no router port-forward, public profile rule, VPN-wide rule, or Internet source.
- Flutter debug build; release clear-text remains disabled.

Never bypass certificate validation in Flutter.

### Certificate lifecycle

Use a private-CA or centrally managed certificate whose SAN matches the stable
agent hostname. Install the issuing CA in the Android enterprise trust policy
and Windows trust store before deployment. Keep the PFX in an administrator-only
staging location and pass it to the installer; the installer copies it under the
protected ProgramData ACL. Track expiry centrally. Renew before expiry by
installing the replacement PFX/password during a maintenance window, restarting
the service, verifying `/health/ready`, and then removing the expired
certificate. Never add a Flutter certificate callback or trust bypass.

## Install

From elevated PowerShell:

```powershell
.\scripts\local-print-agent\install-print-agent.ps1 `
  -PublishDirectory "<absolute-publish-directory>" `
  -PrinterName "POSPrinter POS80" `
  -AllowedNetworkRanges "<store-subnet-cidr>" `
  -UseHttps `
  -CertificatePath "<protected-pfx-path>" `
  -AgentHostName "<certificate-san-hostname>"
```

The script securely prompts for the key. It does not accept a key parameter,
write it into JSON, or print it. The key is stored as a service-specific
environment value below the protected SCM registry key. Only administrators and
SYSTEM should read that key. Rotate it by updating that registry environment,
restarting the service, then updating the device-bound Flutter printer setting
in secure storage. Keep the previous value only for the controlled cut-over;
the agent intentionally supports one active key.

Generate a key without printing it or placing it on a command line:

```powershell
$newPrintAgentKey = ConvertTo-SecureString `
  ([Convert]::ToBase64String(
    [Security.Cryptography.RandomNumberGenerator]::GetBytes(32))) `
  -AsPlainText -Force
```

Use a protected administrative procedure to install the value in the
service-specific environment and immediately clear the temporary variable.
Restart the service, verify authenticated health, then update each authorized
Flutter device through Hardware Settings; Flutter stores the key in
device-bound secure storage. Rotation is a coordinated one-key cut-over.

Installation:

- validates admin rights, published executable, printer queue, CIDR and key;
- installs service name `E_POS.LocalPrintAgent` with display name
  `E_POS Local Print Agent`;
- uses `LocalService`;
- sets Automatic Delayed Start;
- configures restart delays 60s, 120s and 300s;
- resets failure count after 86400 seconds;
- grants `LocalService` modify access only to the ProgramData agent folder;
- creates a Private-profile inbound TCP rule restricted to approved CIDRs;
- starts the service and waits for readiness.

If `LocalService` cannot submit to a vendor printer queue, grant that account
Print permission on the specific queue. Do not switch to LocalSystem without a
documented technical reason.

## Configuration

Non-secret production configuration is:

```text
<ProgramFiles>\E_POS\LocalPrintAgent\appsettings.json
```

Operational state is:

```text
<ProgramData>\E_POS\LocalPrintAgent\operations
<ProgramData>\E_POS\LocalPrintAgent\logs
<ProgramData>\E_POS\LocalPrintAgent\backups
```

Supported `PrintAgent` keys:

- `ListenUrl`
- `PrinterName`
- `PaperWidth`
- `AutoCut`
- `FeedLinesBeforeCut`
- `SpoolerTimeoutSeconds`
- `LocalApiKey` (environment/protected service configuration only)
- `IdempotencyDirectory`
- `OperationRetentionDays`
- `AllowedNetworkRanges`
- `RequestBodyLimit`
- `LoggingDirectory`
- `LogRetentionDays`
- `MaxLogFileBytes`
- `MinimumFreeDiskBytes`
- `FailedAuthenticationLimit`
- `FailedAuthenticationWindowMinutes`

Invalid configuration fails startup. Never commit or package a production key.

## Health and diagnostics

```powershell
Invoke-RestMethod "http://localhost:9101/health/live"
Invoke-RestMethod "http://localhost:9101/health/ready"
.\scripts\local-print-agent\test-print-agent.ps1
```

Authenticated endpoints:

```text
GET /api/print/health
GET /api/print/diagnostics
GET /api/print/operations/{requestId}
POST /api/print/receipt
```

`/health/live` confirms process life only. `/health/ready` probes idempotency
storage and Windows printer readiness but never prints. Windows status cannot
prove every physical condition; a ready spooler status is not proof of paper,
cable, cutter, or physical output.

## Update and rollback

```powershell
.\scripts\local-print-agent\update-print-agent.ps1 `
  -PublishDirectory "<absolute-new-publish-directory>" `
  -HealthBaseUrl "https://<certificate-san-hostname>:9101"
```

Update stops the service, backs up application/configuration, preserves
ProgramData operations/logs and the service key, deploys, starts and checks
readiness. Failure restores the previous executable/configuration. Old
idempotency records remain in ProgramData, so rollback cannot make an accepted
request ID new again.

## Uninstall

```powershell
.\scripts\local-print-agent\uninstall-print-agent.ps1
```

Operational data is preserved by default. Delete it only with explicit approval:

```powershell
.\scripts\local-print-agent\uninstall-print-agent.ps1 -DeleteOperationalData
```

The printer driver and Windows queue are never removed.

## Backup

Before upgrade, back up configuration and the complete `operations` directory.
Copy only while the service is stopped. Logs are optional operational evidence.
Protect backups using the same ACLs. Key rollback requires restoring the
service-specific environment value and restarting the service; never put keys
in backup scripts.

Restore by stopping the service, restoring binaries/config and the complete
operations directory together, restoring the protected service environment
only through the approved administrator process, reapplying the documented
ACLs, and starting the service. Confirm old request IDs through the operation
status endpoint before allowing sales. If migration rollback is required,
generate and review the EF rollback SQL according to the repository migration
rules; never drop print-audit history merely to recover deployment.
