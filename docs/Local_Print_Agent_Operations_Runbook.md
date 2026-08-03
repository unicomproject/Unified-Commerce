# E_POS Local Print Agent Operations Runbook

## Safe status commands

```powershell
Get-Service "E_POS.LocalPrintAgent"
sc.exe qc "E_POS.LocalPrintAgent"
sc.exe qfailure "E_POS.LocalPrintAgent"
Get-NetFirewallRule -DisplayName "E_POS Local Print Agent 9101"
Invoke-RestMethod "http://localhost:9101/health/live"
Invoke-RestMethod "http://localhost:9101/health/ready"
Get-Printer -Name "POSPrinter POS80" | Format-List Name,PrinterStatus,JobCount
Get-Service Spooler
```

Logs: `%ProgramData%\E_POS\LocalPrintAgent\logs`.
Operations: `%ProgramData%\E_POS\LocalPrintAgent\operations`.
Never edit or delete an unresolved operation to force a reprint.

## Service does not start

1. Check Event Viewer and rolling logs.
2. Confirm the key exists in the service-specific environment registry value.
3. Validate JSON, paths, CIDRs and port.
4. Run the executable interactively only in a controlled maintenance window.
5. If recovery repeatedly restarts, stop the service and correct configuration;
   do not shorten the configured recovery delays.

## Port in use

```powershell
Get-NetTCPConnection -LocalPort 9101 -State Listen |
  Select-Object LocalAddress,LocalPort,OwningProcess
Get-Process -Id "<owning-process-id>"
```

Do not terminate an unknown process without owner approval.

## Phone cannot reach agent

1. Confirm phone and laptop are on the same trusted store network.
2. Confirm Windows profile is Private.
3. Check firewall remote CIDR and `AllowedNetworkRanges`.
4. Test laptop liveness/readiness first.
5. Test `https://<certificate-hostname>:9101/health/live` from the phone.
6. Do not create a Public-profile or `Any` remote-address rule.

## Wrong API key / rotation

401 means key mismatch; 429 means repeated failures are temporarily limited.
Update the service key, restart, then save the same key through the permitted
Flutter Hardware Settings screen. Blank UI key updates preserve the existing
device key. Logs never contain the supplied key.

## Printer/spooler failures

- `printer_not_found`: configured queue name mismatch or queue removed.
- `printer_not_ready`: paused/offline/error status reported by Windows.
- `spooler_timeout`: outcome may be uncertain; do not reuse as a new request.
- RAW submission failure: inspect queue permissions and vendor driver.

```powershell
Get-Service Spooler
Restart-Service Spooler   # maintenance approval required
Get-PrintJob -PrinterName "POSPrinter POS80"
```

Windows cannot reliably prove paper-out or final physical delivery for every
USB printer/driver. Confirm the physical device before operator recovery.

## Receipt printed but audit pending

Restore backend connectivity and use audit-only recovery. Never press physical
print retry merely to repair backend audit.

## Unknown print outcome

Query the stable request ID. If agent reports completed, continue audit-only.
If still unknown, inspect paper/queue and use Hardware Settings operator decision.
Use Receipt History for a controlled reprint; never invent a request ID.

## Duplicate request

HTTP 409 `duplicate_request` means the agent intentionally did not print again.
`idempotency_conflict` means the same ID had different content; escalate.

## Early cut

Confirm `FeedLinesBeforeCut=5`, then verify bytes end with:

```text
LF -> ESC d 5 -> GS V 0
```

Do not add printable content after cut.

## Corrupted idempotency record

The agent quarantines corrupted JSON and returns
`idempotency_record_corrupted`; it never treats it as not found. Stop the
service, preserve both the quarantine and backup, and escalate. Do not resend
until physical outcome is resolved.

## Disk full

Stop the service, preserve operations, clear unrelated disk usage, then restart.
Rolling log retention must not delete operation files.

## Upgrade failure

The update script rolls application/configuration back. Confirm old request
status after rollback before accepting prints. Keep operation state across every
upgrade.

## Escalation

Stop accepting new print requests and escalate to the POS/platform owner when
an idempotency record is corrupt, an outcome remains unknown after queue/paper
inspection, the service enters a recovery loop, LocalService lacks queue access,
or rollback readiness fails. Preserve logs, operation files, agent/app versions,
request IDs and timestamps. Do not include API keys, customer contact data,
card data or full receipt payloads in the escalation bundle.

## Physical acceptance checklist

- Service install, running, readiness, reboot auto-start.
- One original 80mm completed-sale receipt; footer/feed/cut and one audit.
- One authorized Receipt History reprint with marker and audit.
- Printer-off sale remains completed and recovery creates no duplicate.
- Flutter kill/reopen and agent stop/restart reconcile without auto-reprint.
- Backend audit outage resumes audit only.
- Wrong key, Public profile and unapproved subnet rejected.
- Logs contain no key/customer/payment payload.
- Upgrade preserves old request status and blocks duplicate ID.

Record date, operator, device ID, agent version, app version, printer queue,
request IDs and pass/fail evidence. Automated tests are not physical sign-off.
