# E_POS Windows Local Print Agent

Windows-only local service that sends ESC/POS RAW bytes to a USB receipt
printer (and cash-drawer pulse) via the Windows spooler.

Component: `E_POS.LocalPrintAgent`

## Production path (required for stores)

Customers must **not** use `dotnet run` for daily operation.

```powershell
cd "<repo>\POS Backend\Unified-Commerce"
.\scripts\local-print-agent\publish-print-agent.ps1
.\scripts\local-print-agent\install-print-agent.ps1 `
  -PublishDirectory ".\artifacts\local-print-agent\publish" `
  -PrinterName "POSPrinter POS80" `
  -AllowedNetworkRanges "127.0.0.1/32","::1/128"
```

For LAN tablet → PC agent, pass the store private CIDR (example
`192.168.18.0/24`) and prefer HTTPS (`-UseHttps` …) for Android release builds.

After install:

- Service name: `E_POS.LocalPrintAgent`
- Startup: Automatic (Delayed)
- Failure recovery: restart with backoff
- Config/logs/ops: `%ProgramData%\E_POS\LocalPrintAgent\`
- Binaries: `%ProgramFiles%\E_POS\LocalPrintAgent\`

Upgrade without re-entering secrets: `update-print-agent.ps1` (preserves
`appsettings.json` and service environment key).

## Development path

```powershell
$env:PrintAgent__LocalApiKey = "<create-a-long-random-local-key-24plus>"
dotnet run --project "tools\E_POS.LocalPrintAgent\E_POS.LocalPrintAgent.csproj"
```

Empty / placeholder keys fail closed at startup.

## Security

- Header: `X-Local-Print-Key` (store-specific; never commit)
- CIDR allow-list required
- Loopback-only installs bind `127.0.0.1` and skip inbound firewall
- LAN installs bind `0.0.0.0` with Private-profile firewall scoped to CIDR
- Logs never include the API key

## Health

```powershell
Invoke-RestMethod "http://127.0.0.1:9101/health/live"
Invoke-RestMethod "http://127.0.0.1:9101/health/ready"
```

Authenticated detail: `GET /api/print/health` with `X-Local-Print-Key`.

## Docs

- `docs/Local_Print_Agent_Production_Deployment.md`
- `docs/Local_Print_Agent_Operations_Runbook.md`
- Second Brain: `12_INTEGRATIONS/Local_Print_Agent.md`
