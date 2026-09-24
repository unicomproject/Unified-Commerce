# Till remote diagnostics — 2026-09-18

Status: implemented development slice; **not production or physical acceptance**.

## Local Print Agent recovery — 2026-09-20

On user request, started the existing development agent on localhost:9101.
The first launch failed because the built configuration had no API key. Relaunched
with the existing source development key supplied through the child process
environment, without displaying or changing the key. PID 4428 returned live
status and authenticated printer health (agent 1.1.0, receipt contract 3).
This is a development background process, not an installed auto-start service.

The agent reports printerExists=true and ready=true from spooler fault flags,
but a separate Windows check still reports OneVerz POS-80 on USB001 with
WorkOffline=true and no present matching printer USB/USBPRINT entry. Thus agent
availability is restored; physical printer connectivity is not. No test receipt
or drawer pulse was sent. POS-side Test connection and saved-key agreement remain
to be observed after the user reconnects the printer USB cable.

Earlier September 20 screenshots show a completed remote scan with the scanner
DETECTED, then HTTP 409 on another request. Readback found a later completed scan
at 15:09:17Z; the exact cause of the rejected request was not captured. The user
reports Notepad barcode input, and the Windows sale screenshot shows Team Jersey
quantity 10. These observations do not establish receipt output or payment.

## Latest live acceptance attempt — 2026-09-19

Recurring scan-request failure: the user supplied another ONLINE screenshot with
the generic request failure at 19:45 local. Earlier COMPLETED scans do not resolve
this recurring HTTP/UI failure. TillScanController now distinguishes HTTP 400,
401, 403, 404, 409, 429, server errors, no HTTP response and unreadable successful
responses, without displaying response bodies or credentials. Retry preserves
the same request ID. Eight focused diagnostics tests passed, including injected
HTTP failures, redaction and successful recovery with the original request ID;
targeted analyzer reports no issues. Live HTTP status/root cause remain pending
reproduction in the updated Admin app. No permissions, binding or scans were
changed to conceal the error.

Windows detection acceptance: fresh DB read confirms Windows reactivation at
2026-09-19T14:09:59.962278Z. Persisted scan
35ffbe4a-dda8-4e0b-bfda-7f64961f7930, requested 14:13:32.404747Z (19:43:32 local),
is COMPLETED. The configured TB-8200D is DETECTED with the exact registered PnP
path and USB serial; the configured XP-80T is DETECTED with printerQueue
OneVerz POS-80. Both health values remain UNKNOWN. A15 and the other unconfigured
printer row remain UNKNOWN. This later success follows the screenshot's generic
request error; that earlier request's HTTP failure reason was not captured and
is not established by this result. Connected-device matching and persisted results
are now verified for these two registrations. Controlled remote unplug/reconnect,
receipt output, compatible drawer operation and reassignment acceptance remain
pending. No further DB mutations or physical output commands were made here.

Offline follow-up: current DB verification still shows the Android activation at
10:58:38.972287Z. Windows Debug PID 24092 is running and its kernel timestamp is
19:35:29 local, newer than the native header-transport change. Rebuilding alone
therefore does not restore the Windows installation's binding. Supported Windows
activation remains necessary; no platform/proof value was overwritten directly.
Corrected the Admin dialog so terminal TILL_OFFLINE device rows say "Not scanned"
instead of indefinitely awaiting evidence, with guidance to reconnect/activate
the intended native POS. Live recovery remains pending Windows activation.

Identity-scan follow-up: the user's completed scan reports scanner NOT_DETECTED.
Readback confirms the registered scanner devicePath and XP-80T printerQueue are
present in the scan snapshot; both return NOT_DETECTED. The other printer row
also named OneVerz POS-80 has no identifier and remains UNKNOWN (it is not the
configured XP-80T row). Current Windows enumeration independently finds the exact
registered scanner path and the XP-80T USB/USBPRINT entries OK, with queue
OneVerz POS-80 WorkOffline=false. Crucially, DB binding now records POS-01 as
Android, paired_at 2026-09-19T10:58:38.972287Z, superseding the previous Windows
binding. Windows identity detection requires restoring the intended Windows
binding via supported activation; ONLINE alone does not establish the platform.
No identity values were changed to mask the mismatch and no output commands ran.

User-authorized identity registration: a focused development DB transaction set
config_json.devicePath on assigned TURBOGEAR TB-8200D device
9da32353-278d-43fe-bdaa-be3b75f47a2a to
`USB\VID_26F1&PID_8801\1234567890ABCD`, and config_json.printerQueue on assigned
Xprinter XP-80T d6b5a93a-7aca-4ee2-8046-27a5cea15d6b to `OneVerz POS-80`.
These use prior controlled Windows observations. Other JSON fields and display
names were preserved; versions advanced to 4 and 3 respectively. Tenant/till,
active assignment and model were checked under row locks and the diagnostics
tenant advisory lock. Dry-run rollback, commit, repeat no-op and separate readback
passed. This was a direct DB repair, not an authenticated UI/API operation;
updated_by_tenant_user_id was left null for the maintenance update. Other device
rows, physical confirmation and scan history were unchanged. A new authenticated
scan is required to verify detection using these identities.

Prior to registration, the latest persisted scan (4432af35-b9bb-44e8-9e92-22f7fbcbdffa,
2026-09-19T13:35:05.39691Z) was COMPLETED with empty registered identifiers and
UNKNOWN results for all four devices. User screenshots already confirmed POS
ONLINE and heartbeat after the updated Windows Release launch (PID 40120,
explicit localhost:5150 API). Receipt and compatible drawer output remain untested.

Live Admin transport now confirmed by user screenshot: "Live updates connected",
POS OFFLINE and a terminal TILL_OFFLINE scan with four registered devices. This
establishes the Admin connection and an offline scan response, not device results.
Read-only DB verification retains the Windows trusted ACTIVE assignment. Windows
PID 25148 is still the Debug process started at 10:30:38 local time; its on-disk
kernel predates the native header-transport change. Preparing a separate Release
build while keeping its current cart/process intact. Updated Windows runtime
registration and heartbeat remain to be observed after switching builds.

Transport follow-up: Admin now displays the connection-stage error, before the
SignalR handshake/WatchTill completes. A synthetic 10,000-character non-credential
probe against the local API returns HTTP 414 when placed in access_token query,
but 401 (expected authentication rejection) in Authorization. The native runtime
client had put full JWTs in the URL. Android/Windows now send Bearer credentials in
Authorization using the IO WebSocket connector, with no credential in the URL.
Browser behavior remains query-based and is still subject to URL-size limits;
no server limits or auth checks were relaxed. Thirteen focused tests passed,
including a 12,000-character synthetic credential completing a local WebSocket
handshake/registration. This reproduces a transport limitation, not the actual
signed-in user's HTTP status. Updated Android Admin and Windows POS processes
must reconnect to establish whether this resolves their live failure.

Disposed-widget crash follow-up: reproduced the user's exact "Cannot use ref
after the widget was disposed" error in a widget regression by keeping Scan Till
open, removing its originating overview, then rebuilding the overlay during
reassembly. The dialog builder read outletId through the disposed overview's
WidgetRef. Scan Till and Test All now capture the selected outlet ID while the
overview is alive. The regression failed before this change and passes afterward.
Hardware overview, diagnostics and Test All tests: 12 passed. Targeted analyzer:
no issues. Running Admin UI recovery and authenticated live connection remain
pending; this fixes the reproduced lifecycle crash, not evidence of ONLINE status.

Admin live-connection follow-up: the latest screenshot remains at "Connecting to
live updates" with scanning disabled. The client previously discarded transport,
handshake and WatchTill errors. Added bounded, stage-specific connection messages,
an allowlist for known hub rejection text (never arbitrary exceptions or token-bearing
URLs), and Retry live connection. Successful registration clears the error;
authorization and scan gating remain enforced. Six focused Flutter diagnostics
tests passed, including rejected WatchTill, unknown-error redaction and retry
recovery through a local test WebSocket server. Local API health returns 200 and
unauthenticated hub negotiation returns 401. The actual authenticated live failure
is still unconfirmed; the running Admin app needs to load the new code before its
connection error can be observed. No hardware scan, receipt or drawer output was
claimed from these tests.

Following Retry guidance, the user supplied a Windows-title-bar sale screenshot
with Team Jersey quantity 4 and "Team Jersey quantity increased" feedback.
This supports Windows sale-screen access and barcode/cart behavior; it does not
prove runtime heartbeat or a restart recovery test. A fresh DB read retains the
Windows binding at 08:36:24.711699Z. Printer USB and USBPRINT entries have returned
with status OK; OneVerz POS-80 on USB001 reports WorkOffline=false. This supersedes
the earlier disconnected observation. No receipt or drawer output has been tested.
The IDE console visible behind the screenshot also contains a Duplicate GlobalKey
exception; its stack trace and trigger are not established by this screenshot.

Latest Windows-home screenshot shows "POS context is not ready". A fresh database
read now confirms POS-01 reactivated as Windows at `2026-09-19T08:36:24.711699Z`,
trusted and ACTIVE with the same Front Till 01 assignment and five recent consumed
codes. This supersedes the Android binding below. The screenshot still does not
confirm successful client context recovery or runtime heartbeat; Retry and the
resulting authenticated current-device response remain to be observed.

Follow-up at 13:46 Asia/Colombo: the user's Windows screenshot now shows the
cart fields, Team Jersey quantity 7, subtotal LKR 31,500.00, discount LKR 1,575.00
and total LKR 29,925.00. This confirms visible cart rendering; it does not establish
a completed payment or fresh device-proof acceptance. A separate read still finds
POS-01 bound to Android at 08:07:04.834195Z. Bootstrap can reuse its ready state,
so a displayed sale screen alone cannot verify current server binding.

Current OS enumeration finds the identified scanner USB/HID entries present and
OK. The previously identified printer USB/USBPRINT entries are absent and
OneVerz POS-80 on USB001 now reports WorkOffline=true, superseding the earlier
online observation. Registry reads still show null serials for Barcode Scanner 1
and Cashier Printer; their TURBOGEAR/TB-8200D and Xprinter/XP-80T model fields
match the supplied packaging. No registry changes or physical output commands
were issued. Windows reactivation and printer reconnection are pending; live
heartbeat, remote scan and drawer compatibility remain unverified.

Subsequent POS-context failure: read-only DB inspection now shows POS-01 reactivated
as Android at `2026-09-19T08:07:04.834195Z` (four recent consumed codes), superseding
the preceding Windows activation. Front Till 01 still has one OPEN session. The
displayed "POS context is not ready" originates when the home provider has no
device context after bootstrap. If the screenshot is Windows, reactivation on
Android explains invalidation of its previous installation proof; screenshot
platform and the exact current-device response still need confirmation. Do not
change DB platform text or bypass proof checks to restore access.

Scanner input follow-up: user reports barcode 2000000000114 arrives in Notepad
with an automatic newline. After the requested POS scan, the screenshot shows
"Team Jersey — Team Jersey quantity increased", supporting application barcode
resolution and a cart quantity change, but the cart fields were hidden. Audited
the development role and found no granular cart display grants or canonical
pos.sales.cart.manage parent. Applied the focused
scripts/repair-development-cart-display-permissions.sql: 13 cart display grants
plus the canonical parent, conditional on an existing cart-manage grant. The
actual user's role candidates passed production parent resolution; repeat and
revocation-preservation checks passed before and after commit. Refreshed cart UI
remains unverified; no payment or physical output command was issued.

Scanner reconnect observation: after reconnecting to the same port, the same
`USB\VID_26F1&PID_8801\1234567890ABCD` and HID keyboard child returned with status
OK. The controlled unplug/reconnect identifies this as the user's scanner; barcode
input into the app remains untested. The previously observed descriptor-failed
USB entry was absent in this enumeration. A printer is now present as
`USB\VID_1FC9&PID_2016\BC78106A3732`, with child
`USBPRINT\PRINTERPOS-80\7&36079EBE&0&USB001`, both status OK. OneVerz POS-80 on
USB001 now reports WorkOffline=false. These observations establish OS detection,
not receipt output, application readiness or cash drawer compatibility.

Scanner unplug observation: after the user reported unplugging only the scanner,
both `USB\VID_26F1&PID_8801\1234567890ABCD` and its HID keyboard child disappeared
from present-device enumeration. This supports attribution to the connected
TB-8200D; reconnect confirmation remains pending. The descriptor-failed USB device
remained present, so it is a separate unresolved connection. The drawer underside
photo confirms DBL POS 405A Black 5B8C and identifier 202604020038 but supplies no
voltage or pinout. Drawer pulse testing remains pending compatibility evidence.

Peripheral connection follow-up: supplied packaging identifies Xprinter XP-80T,
80 mm, USB/network, input 24 V 1.25 A and drawer output 24 V 1 A; TURBOGEAR
TB-8200D USB scanner with printed serial 82111001003; DBL POS 405A Black 5B8C
drawer with packaging identifier 202604020038. The drawer box prints a 12 V option
with an unmarked checkbox; actual drawer voltage and pinout remain unconfirmed.
Do not send a drawer pulse until compatibility with the printer output is established.

Current Windows enumeration finds a new USB HID keyboard/input device
`USB\VID_26F1&PID_8801\1234567890ABCD` (OK), not yet positively attributed to the
scanner; its USB identifier differs from the packaging serial. A separate present
USB device reports Device Descriptor Request Failed, problem code 43. The installed
OneVerz POS-80 queue has WorkOffline=true. No physical printer readiness is proven.
Database comparison: Barcode Scanner 1 already has TURBOGEAR/TB-8200D and Cashier
Printer has Xprinter/XP-80T, but both serial_number fields are null. Other development
hardware rows contain placeholder serial SN123456789; these are not physical evidence.
No registry edits, print commands or drawer pulses were performed in this check.

Follow-up: PostgreSQL now confirms Windows reactivation at
`2026-09-19T04:55:25.116223Z`, trusted ACTIVE POS-01 with the same Front Till 01
assignment; three recent activation codes consumed. The user's Windows-title-bar
screenshot shows the sale screen and populated product cards. Subsequently closed
Windows PID 34776 gracefully and relaunched the same Debug executable as PID 25148
for recovery acceptance. Persisted DB binding remains Windows; post-restart UI
recovery and live runtime heartbeat still require observation. The Android state
in the earlier attempt below is superseded by this activation.

User authorized restart, online/heartbeat, physical peripheral and reassignment
acceptance. Current PostgreSQL binding has since changed to platform `android`,
paired_at `2026-09-19T00:41:51.138726Z`, with two recent consumed codes. Earlier
Windows activation evidence is historical, not evidence of a current Windows binding.
The stale Windows process had no main window and did not close gracefully; it was
stopped and the existing Debug executable relaunched (PID 34776, visible main window).
API `/api/v1/health` on localhost:5150 returned 200. Authenticated restart recovery
and runtime ONLINE/heartbeat remain unverified pending interactive Windows login.

Windows lists `OneVerz POS-80`, driver `Generic / Text Only`, port `USB001`.
This is an installed queue only. Present USB enumeration showed root hubs, webcam
and Bluetooth; no scanner or USB receipt printer was identified. No invented
peripheral identifiers were registered and no print/drawer commands were sent.
Physical connections, model identification and operator observations are required
before unplug/reconnect, barcode, receipt and compatible drawer tests. No release
or reassignment was performed in this attempt.

## A. Audit and reuse

Existing Till/PosDevice identities, trusted native device proof, TillDeviceAssignments,
HardwareDeviceAssignments, hardware registry configuration versions, permissions,
entitlements and hardware_test_logs are reused. Existing remote Test All remains an
operator-assisted physical test flow. Previously there was no SignalR hardware command
transport; the notification WebSocket is a separate protocol and is unchanged.

## B. Architecture

```mermaid
sequenceDiagram
  participant A as Tenant Admin
  participant API as Backend
  participant POS as Bound native POS
  participant OS as Local OS
  A->>API: WatchTill (JWT + authorized till)
  POS->>API: RegisterPos (JWT + native proof)
  API->>API: Resolve current trusted POS and assignment
  A->>API: POST hardware-scans (requestId)
  API->>API: Permission, entitlement, scope, binding; persist snapshot
  API->>POS: RunDeviceScan (one current connection)
  POS->>API: ScanStarted
  POS->>OS: Enumerate USB/PnP/local print queues
  OS-->>POS: Identifiers and available observations
  POS->>API: DeviceScanResult
  API->>API: Revalidate session, assignment, version, identities; persist
  API-->>A: TillHardwareScanUpdated
```

Till codes remain display identifiers. GUIDs and the existing active assignment ID
are authoritative. Client-supplied tenant/till IDs do not establish POS ownership.
Targeting uses server-authorized individual connection IDs rather than allowing
client-supplied group joins. One latest authenticated connection per POS wins.

## C. Database

No schema migration. Existing hardware_test_logs stores REMOTE_SCAN sessions with
registered device/version/identity snapshot and results in result_payload_json.
Request IDs use the existing tenant/request unique constraint. Per-tenant advisory
transaction lock serializes creation/completion. Terminal records are immutable.
Generic physical-test create/complete routes cannot write REMOTE_SCAN records.
These records do not overwrite physical confirmation or make dashboard devices ready.
Existing unrelated payment seed/migration work is untouched.

## D. API and transport

- POST `/api/v1/tenant-admin/tills/{tillId}/hardware-scans`: `{requestId: UUID}`.
- GET same path `/{scanId}`: authorized persisted scan recovery.
- GET same path `/runtime`: ONLINE/OFFLINE and last heartbeat.
- Hub `/hubs/till-runtime`, JWT authentication, closes on authentication expiry.
- Methods: RegisterPos(proof), WatchTill(tillId), Heartbeat(), ScanStarted(scanId),
  DeviceScanResult(scanId, results).
- Events: RunDeviceScan, TillHardwareScanUpdated, TillRuntimeUpdated.
- States: REQUESTED → DISPATCHED → RUNNING → COMPLETED; FAILED, TIMED_OUT,
  TILL_OFFLINE are terminal alternatives. COMPLETED is not physical-test PASSED.
- Default timeout 60 seconds, configurable `HardwareDiagnostics:TimeoutSeconds`
  (10–300); worker sweeps every 5 seconds. Runtime lease expires after 60 seconds.
- At most 50 registered devices per scan, one active scan per till, five-second
  request cooldown, 64 KiB hub inbound limit, bounded result fields.
- Reconnection uses 2/5/10/30 second backoff, fresh auth, and new binding registration.
  Admin re-subscribes and recovers its known scan over HTTP. Interrupted POS scans
  expire; they are not automatically redelivered or physically executed.

## E. Tenant Admin

Hardware Overview has a separate Scan Till action. Only authorized outlet tills are
selectable. UI shows live connection, POS heartbeat, session progress, per-device
detection, health and explicit physical-test-not-performed text. Existing Test All
retains its physical/operator meaning. Configuration accepts optional stable device
path / Windows queue name and manufacturer serial. Registered IDs must come from
the assigned POS, not the Admin browser machine.

## F. Cashier adapters

- Windows: present USB/HID PnP instance paths, available USB serial, VID/PID, COM
  port identity; local spooler queue identities and driver-reported fault flags.
  Zero spooler flags remain UNKNOWN, never assumed READY.
- Android: USB Host device enumeration in background; serial only with existing
  OS permission. Enumeration does not silently request permissions or operate devices.
- Browser and other platforms: UNSUPPORTED. No browser/cloud USB enumeration.
- Exact registered strong identity is required. VID/PID or a generic keyboard alone
  cannot establish scanner identity. Multiple matching observations yield UNKNOWN.
- Drawer: UNSUPPORTED by passive scan; use existing operator-confirmed drawer test.
- Provider terminal/network/Bluetooth readiness, scale/display and vendor-specific
  protocols are not implemented by this slice. No payment, drawer pulse, receipt or
  automatic scanner-to-cart action is invoked by remote diagnostics.

## G. Security

Every request checks tenant, hardware permission/entitlement, active user/outlet/till,
outlet/till membership and configured account restriction. POS requires trusted
native fingerprint proof and a single current assignment. Hub invocations revalidate
auth sessions. Dispatch and results revalidate active binding and assignment identity;
results also check hardware assignment and configuration version. Monitor events
recheck session and authorization. Logs contain IDs/status, not JWTs or device proofs.

## H. Verification

Completed checks as of this implementation:
- Debug API build: 0 errors / 0 warnings.
- Hardware unit regression: 97 passed (includes identity, scope and reassignment).
- Hardware API regression: 57 passed, including three dispatch tests with five mocked tills and cross-tenant watchers.
- Flutter diagnostics/setup/Test All: 11 passed; expanded diagnostics suite subsequently 5 passed (12 distinct tests across these runs).
- Targeted Flutter analyzer: no issues.
- Android debug APK built successfully; existing mobile_scanner KGP warning.
- Release API: 0 errors / 0 warnings. Web build passed, existing CupertinoIcons font warning.
- Windows build blocked: suitable Visual Studio C++ toolchain absent.

These tests do not establish live PostgreSQL scan persistence or five real authenticated
WebSocket clients. The dispatcher isolation test uses fake hub clients. Full persisted
five-client acceptance, signed-in browser visual inspection and physical tests remain
unverified. Later test/build results should be appended rather than inferred.

## I. Physical validation plan

1. Install Windows C++ desktop toolchain; compile and run native POS.
2. Activate trusted POS and assign Front Till 01; log in with authorized cashier.
3. Register actual device serial/path/queue from that machine. Do not invent IDs.
4. Open Admin Scan Till; confirm ONLINE and updated heartbeat.
5. Scan with scanner/printer connected, then unplug each and scan again; compare
   OS evidence and UI, retaining UNKNOWN where drivers cannot report status.
6. Separately run operator tests: scan real barcode into cart; print receipt; open
   voltage/pin-compatible printer-linked drawer. Record observed physical output.
7. Revoke user access, release/reassign POS, disconnect/reconnect and restart backend;
   verify no old connection can submit and pending scans terminate.
8. Connect five authenticated test POS clients; scan only Till-03; persist its three
   controlled adapter results; verify the other four receive zero commands and Admin
   updates without refresh. Test tenant/outlet negatives and duplicate result replay.

## J. Deployment limitations

Registry/monitor state is process-local: use a single API process for this implementation.
Multiple API replicas need a distributed registry and SignalR backplane with routing
tests. No production deployment was performed. Windows native build is unverified;
enumeration currently uses a synchronous native method callback and needs measured
latency acceptance on target hardware. End-to-end DB/session/concurrency acceptance
and complete device adapter coverage remain before a production-ready declaration.

## K. Git

Both code repositories use `feature/till-realtime-hardware-diagnostics`.
Changes remain uncommitted. No main merge, force push or discard of existing work. No push performed for this slice.

Protocol reference: [ASP.NET Core SignalR Hub Protocol](https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/docs/specs/HubProtocol.md).

## L. Stage 1 verified; Stage 2 progress — 2026-09-19

This appended evidence supersedes the earlier Windows build blocker above.
Visual Studio Professional 2022 17.14.27 now has the C++ workload, MSVC
14.44.35207, Windows SDK 10.0.26100.0 and CMake 3.31.6. Flutter 3.44.0 /
Dart 3.12.0 doctor passed. Windows Debug and Release builds both succeeded in
Stage 1. Stage 2 Windows Debug build also succeeded after the changes below.

Stage 2 target confirmed by the user: Development Main Store / Front Till 01.
Real device activation, native proof validation, restart persistence and live
reassignment are still unverified; a native Windows Tenant Admin login is pending.
Do not interpret passing automated tests as real POS binding evidence.

Existing pos_devices, tills, outlets, till_device_assignments and
till_activation_codes are reused. Active assignment indexes enforce one device
per till and one till per device. Device activation stores a hash of the native
installation credential; Flutter retains the credential in secure storage.
This is installation credential possession, not hardware attestation.

Stage 2 changes:
- DeviceContextRepository validates active outlet and matching device/outlet
  before activation mutates a device or consumes its code, and during recovery.
- TillDiagnosticService.BindAsync checks the resolved till outlet and unique
  current till assignment.
- Native current-device requests send their proof in the header only, removing
  it from the URL query.
- The previously placeholder Generate activation code action now uses POST
  /api/v1/tenant-admin/tills/{tillId}/activation-codes. The new service checks
  till.activation_code.generate, till_management entitlement, active user and
  tenant/outlet/till scope, and an active assigned device. Codes are random,
  expire after ten minutes, are stored hashed and are returned to the UI once.
  Existing device activation consumes the code. Concurrent issuance/consumption
  against real PostgreSQL remains unverified.
- Database migration for Stage 2: NONE. Existing unrelated payment migrations
  are not part of this stage.

Executed verification:
- API Debug build: 0 errors / 0 warnings.
- Targeted unit tests: 15 passed (including issuance scope and runtime reassignment).
- DeviceContextRepository tests: 20 passed, using EF InMemory, not PostgreSQL.
- Targeted API tests: 16 passed before the new issuance endpoint was added;
  these do not validate that endpoint over HTTP.
- Flutter binding tests: 26 passed.
- Full Flutter suite before adding the code-generation UI: 2,121 passed, 1 skipped.
- After adding that UI: activation dialog and till list tests, 12 passed.
- Final full Flutter analyze: no issues. Final Windows Debug build: passed.
- Local API health: HTTP 200. Native app opened for interactive login.

Existing work remains uncommitted and unpushed on
feature/till-realtime-hardware-diagnostics. No scanner/printer registration,
remote scan execution or physical tests were performed during Stage 2.

## M. Activation permission repair — 2026-09-19

The live configured PostgreSQL database had no permission_definitions entry for
till.activation_code.generate. This explained the missing permission-gated menu
action; the earlier issuance implementation omitted its catalog setup.

Added TillActivationPermissionSeedData and the data-only migration
20260919010000_AddTillActivationCodePermission. The permission belongs to the
existing outlet_till_core / till_management catalog. Default grants are limited
to active TENANT_ADMIN roles; other roles and explicitly revoked grants remain
unchanged. Backend issuance uses the shared GenerateActivationCode constant.
No schema/model snapshot changes are required for this data-only migration.

The focused seed SQL was applied directly to the configured local-development
database, without applying unrelated pending migrations or modifying EF migration
history. The idempotent migration is still available for normal deployment.
Post-commit verification found one permission entry and eight active Tenant Admin
role grants. Transactional PostgreSQL checks passed for repeat application,
unchanged non-admin grants and preservation of explicit revocation; temporary
test mutations were rolled back. Activation service tests: 9 passed.

This supersedes the earlier Stage 2 statement that no migration was added.
The signed-in UI must reload its permissions through sign-out/sign-in. Menu
visibility after re-login and real Windows POS binding remain unverified.
No commit or push was performed.

## N. Windows activation and opening cash permissions — 2026-09-19

Read-only PostgreSQL verification now finds POS-01 assigned to Front Till 01 at
Development Main Store with platform `windows`, trusted=true, status ACTIVE and
paired_at `2026-09-18T19:53:48.538321Z`. One recent activation code was consumed.
The user's screenshots also show the generation action and issued-code dialog.
This supersedes the earlier unverified menu visibility and activation state.
Restart recovery, live proof-negative checks and reassignment remain unverified.

The Open Till screenshot showed the summary but no amount field and a disabled
button. The development TENANT_ADMIN role had the legacy `pos.till.open` grant
but lacked the granular starting-cash and numpad grants. Applied
`scripts/repair-development-till-opening-permissions.sql` to add 18 cash-entry,
view, validation and keypad permissions to that development role only. The script
requires an existing active open-till grant, preserves revoked grants and does
not change other roles or tenants. No schema migration or application bypass.

Transactional PostgreSQL repeat-application and revocation-preservation checks
passed; test mutations were rolled back before committing the repair. A separate
read confirmed the starting-cash and numpad grants after commit. Focused Flutter
form and numpad permission tests: 19 passed. Sign-out/sign-in is required to load
the new grants; the resulting UI remains to be checked. No cash session was opened,
no physical tests were run, and no commit or push was performed.

### Opening-permission follow-up

The subsequent screenshot still lacked cash entry. The first repair omitted
the canonical parent `pos.till.session.open`: the production effective-permission
resolver rejects opening children without that exact parent, even when legacy
`pos.till.open` permits navigation. Updated the focused development SQL to include
the parent (19 grants in total) and applied the one missing grant.

Verified the actual development user's active role grants through the production
CashierPosEffectivePermissionResolver: starting-cash entry, numpad and keys survive;
removing the canonical parent rejects starting-cash entry. PostgreSQL idempotency
and revocation-preservation checks passed, including a separate post-commit run.
This verifies role candidates and parent resolution, not an authenticated login
response or the refreshed UI. Sign-out/sign-in and UI verification remain pending.

### Sale screen observed

The next user screenshot shows the sale screen with Popular Products (0).
Read-only PostgreSQL checks confirm one OPEN session for Front Till 01, 16 active
tenant products and no active POS_POPULAR collection. The catalog defaults to the
popular segment; its repository returns an empty list when that collection is
absent. This explains the displayed empty popular list without establishing that
all 16 products are eligible for sale at this outlet. The user has progressed past
Open Till; the agent did not create the session or change product curation.
Restart persistence and live negative/reassignment acceptance remain pending.

### Popular products populated on user request

Created the development tenant's active POS_POPULAR / POS_QUICK_LIST collection
and linked its 16 existing ACTIVE, sellable products in product-name order. This
was a focused transactional database operation, not a UI interaction. Existing
links are preserved; repeated application adds no duplicates. Rollback verification
and a separate post-commit check both found 16 eligible linked products. Stock,
prices, product status and other tenants were unchanged. This tenant-wide curation
does not establish outlet stock availability or successful sales. Reopening the
sale screen is needed to reload its cached popular catalog; UI rendering remains
unverified.

### Product card visibility follow-up

The next screenshot explicitly shows Android Studio's Android emulator and
Popular Products (16), confirming collection loading there, not Windows UI
acceptance. Cards lacked their names/images/prices. The development admin role
was missing all five visual card grants and their canonical parent
pos.sales.catalog.view. Applied scripts/repair-development-product-card-permissions.sql
to add those six grants, conditional on an existing catalog-view grant and scoped
to the development admin role. Production parent resolution over the user's active
role grants confirms all five visual fields survive. Repeat application and
revocation preservation passed before and after commit. Re-login and visual
confirmation are pending. No cart, payment, stock or device assignment changes.
