# E_POS Windows Local Print Agent

This Windows-only LAN service sends 80 mm ESC/POS RAW bytes to a USB printer
installed in the Windows print spooler. It is separate from the public E_POS API
and must be exposed only on a trusted private LAN.

## Configuration and run

Use an API key of at least 24 characters. Do not commit the key.

```powershell
cd "C:\Users\User\Downloads\EPOS\POS Backend\Unified-Commerce"
$env:PrintAgent__LocalApiKey = "<create-a-long-random-local-key>"
dotnet run --project "tools\E_POS.LocalPrintAgent\E_POS.LocalPrintAgent.csproj"
```

The agent listens on `http://0.0.0.0:9101`. Settings use the `PrintAgent`
configuration section:

- `PrinterName` (default `POSPrinter POS80`)
- `PaperWidth` (`80mm` or `58mm`)
- `AutoCut`
- `SpoolerTimeoutSeconds` (1-30; prevents a stalled Windows driver from hanging the API)
- `LocalApiKey`
- `IdempotencyDirectory`
- `AllowedNetworkRanges` (explicit CIDR allow-list)
- `RequestBodyLimit`
- `LoggingDirectory`, `LogRetentionDays`, `MaxLogFileBytes`

Environment variables use double underscores, for example
`PrintAgent__PrinterName` and `PrintAgent__LocalApiKey`.

## Private Windows Firewall rule

Run PowerShell as Administrator. This rule permits only the Private profile and
the local subnet:

```powershell
New-NetFirewallRule -DisplayName "E_POS Local Print Agent 9101" -Direction Inbound -Protocol TCP -LocalPort 9101 -Action Allow -Profile Private -RemoteAddress LocalSubnet
```

Do not create a public-profile or any-source Internet firewall rule.

## Health check

Laptop:

```powershell
Invoke-RestMethod "http://localhost:9101/health/live"
Invoke-RestMethod "http://localhost:9101/health/ready"
```

Physical phone browser (replace with the laptop's current LAN IP):

```text
http://<PC-LAN-IP>:9101/health/live
```

## PowerShell print test

This request contains operator-supplied test values only. Use a new `requestId`
for an intentional new print. Reusing an ID is blocked to prevent duplicates.

```powershell
$headers = @{ "X-Local-Print-Key" = $env:PrintAgent__LocalApiKey }
$body = @{
  requestId = [guid]::NewGuid()
  receiptNumber = "<real-receipt-number>"
  printedAt = [DateTimeOffset]::Now
  merchantName = "<merchant-name>"
  outletName = "<outlet-name>"
  tillName = "<till-name>"
  cashierName = "<cashier-name>"
  currency = "LKR"
  items = @(
    @{
      name = "<real-item-name>"
      quantity = 1
      unitPrice = 0
      lineTotal = 0
    }
  )
  subtotal = 0
  discountTotal = 0
  taxTotal = 0
  total = 0
  paymentMethod = "<payment-method>"
  amountTendered = 0
  change = 0
  footerLines = @()
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Method Post `
  -Uri "http://localhost:9101/api/print/receipt" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $body
```

The request contract does not recalculate financial values. Chunk 2 must supply
authoritative completed receipt values from the POS/backend.

## Production

The detailed printer health, diagnostics, operation status and print endpoints
require `X-Local-Print-Key`. Contract versions are API `1` and receipt `1`.
Production Windows Service deployment, update/rollback, ACL, firewall, HTTPS,
backup and troubleshooting guidance:

- `docs/Local_Print_Agent_Production_Deployment.md`
- `docs/Local_Print_Agent_Operations_Runbook.md`
- `scripts/local-print-agent/`
