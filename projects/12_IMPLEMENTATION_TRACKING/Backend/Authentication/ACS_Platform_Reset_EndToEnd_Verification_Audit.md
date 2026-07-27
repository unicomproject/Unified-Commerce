# ACS Platform Reset End-to-End Verification Audit

**Audit date:** 2026-07-24  
**Scope:** Read-only verification after ACS Email Phase 1  
**Backend:** `Nytroz POS - Backend New/Unified-Commerce`  
**Frontend:** `nytroz-pos-platform-admin`  
**Reports read first:**  
- `projects/12_IMPLEMENTATION_TRACKING/Backend/Authentication/ACS_Email_Password_Reset_Audit.md`  
- `projects/12_IMPLEMENTATION_TRACKING/Backend/Authentication/ACS_Email_Platform_Reset_Phase1_Implementation.md`  

No application code, packages, migrations, or configuration were modified by this audit.

---

## 1. Verdict

The login-page text **"Ask a Platform Admin to send a password reset."** is **correct for the currently approved admin-initiated reset scope**.

It is **not** a UI bug and **not** a missing forgot-password button for Release 1 / SA-P1-06.

| Question | Answer |
|---|---|
| Is the message correct for admin-initiated reset? | **Yes** — intentional, non-clickable guidance |
| Is it a UI bug? | **No** |
| Is self-service missing as a product gap? | **Yes as deferred scope**, not as a regression of the approved journey |
| Documentation mismatch? | **Yes (partial)** — Second Brain / Included Features still say R1 email is deferred / `admin_secure_link` only; Phase 1 wired ACS email for the **same admin-initiated** journey |

**Classification of the observed UI:** (1) Correct for admin-initiated reset, with (4) Second Brain lag after ACS Phase 1.

---

## 2. Approved User Journey

### Sources

| Document | Section | Intended behaviour |
|---|---|---|
| `second-brain-docs-worktree/03_USER_JOURNEYS/Platform_Admin/17_Platform_User_Password_Reset_Flow.md` | Purpose / Out Of Scope | **Admin-initiated only**; explicitly **not** self-service forgot-password |
| same | Trigger / Main Flow | Initiator opens Platform Users → Send Password Reset → token → public `/reset-password?token=` → complete → login |
| same | Permissions | Initiate: `platform.users.update`; validate/complete: anonymous |
| `15_IMPLEMENTATION_TRACKING/Backend/Auth/SA-P1-06_...Implementation.md` | Feature Summary / Out of scope | Self-service forgot password **not in scope**; R1 delivery was `admin_secure_link` |
| `01_RELEASE_SCOPE/Included_Features.md` | Platform And Tenant Setup | Admin-initiated platform reset included; **automated email deferred** (doc lag vs Phase 1) |
| `ACS_Email_Platform_Reset_Phase1_Implementation.md` | Summary | ACS email connected to **existing** admin-initiated flow only; tenant/self-service still out of scope |

### Direct answers

| Question | Answer | Evidence |
|---|---|---|
| Can a logged-out Platform Admin request their own reset? | **No** (approved journey) | Journey Out Of Scope: self-service forgot password |
| Must another Platform Admin initiate the reset? | **Yes** (or any admin with `platform.users.update`, including potentially self via user management — see §5) | Journey Actor / Trigger |
| Is the login-page message intentional? | **Yes** | `login-page.ts` + unit test asserting admin-assisted guidance |
| Was self-service explicitly deferred? | **Yes** | Journey §Out Of Scope; SA-P1-06 |
| Is an email-first reset journey now approved, or only delivery changed? | **Delivery changed** for the same admin-initiated journey; no new self-service journey approved | Phase 1 implementation doc |
| Does any document require a "Forgot Password?" link? | **No** — documents explicitly exclude it | Journey Out Of Scope |

---

## 3. Login Page Findings

| Item | Finding |
|---|---|
| Component | `nytroz-pos-platform-admin/src/app/features/auth/pages/login-page/login-page.ts` |
| Exact message lines | **L158–161** — `<span class="unavailable-link" aria-disabled="true">Ask a Platform Admin to send a password reset.</span>` |
| Hardcoded? | **Yes** (inline template string) |
| i18n/config? | **No** |
| Clickable? | **No** (`aria-disabled="true"`, not an `<a>` / routerLink) |
| Forgot-password route? | **No** |
| Reset-password route? | **Yes** — `app.routes.ts` L18–22 `path: 'reset-password'`, `guestGuard` |
| Intentional test | `login-page.spec.ts` L94–98: *“shows admin-assisted password reset guidance instead of self-service forgot password”* |
| Layouts | Single standalone `LoginPage` component; responsive CSS in same file (one composition for desktop/mobile) |

---

## 4. Platform User Management Reset Action

**The initiating UI exists** on the Platform Users detail slide-over.

| Item | Finding |
|---|---|
| Screen | `platform-users-page.ts` — user edit aside |
| Button | **Send Password Reset** L232–239 |
| Visibility | `canUpdate() && !user.invitePending` L217 |
| Permission | `platformPermissions.usersUpdate` → `canUpdate()` L497–498 |
| Confirm modal | L247–266 |
| API | `PlatformUserApiService.initiatePasswordReset` → `POST {apiBaseUrl}/platform-admin/users/{id}/password-reset` (`platform-user-api.service.ts` L68–71) |
| Route correctness | Matches backend `POST /api/v1/platform-admin/users/{userId}/password-reset` |
| `deliveryMode=email` | Partially handled: toast uses null-`resetUrl` branch L677–680; **does not** use `result.message` (“email has been sent”) |
| Expects `resetUrl`? | Shows copy box **only if** `resetUrl` present L221–230 — correct for email mode (no box) |
| Useful message when `resetUrl` null? | Weak: `"Password reset initiated for {email}."` — does not say email was sent |
| Modal copy | Still says “A secure password reset link will be created” L251–253 — outdated for email mode |
| 502 / errors | Uses `ApiErrorService.toSafeMessage` — works for legacy errors with `success: false` (backend 502 uses that shape) |
| Hidden for current user? | **No** special self-hide; any editable eligible user including self can be targeted if UI opened |
| Tests | Spec still mocks `admin_secure_link` + `resetUrl` (`platform-users-page.spec.ts` L267–306); **no** email-mode UI test |

**Main frontend gap is not a missing button** — it is incomplete email-mode UX/copy/tests after ACS Phase 1.

---

## 5. Backend Endpoint Findings

| Check | Result | Evidence |
|---|---|---|
| Route | `POST /api/v1/platform-admin/users/{userId}/password-reset` | `PlatformAdminUsersController.cs` L130–162 |
| Permission | `platform.users.update` | `PlatformPasswordResetService.cs` L180–183 |
| User lookup | By `userId` | L195–200 |
| Active / eligible | `ACTIVE` or `LOCKED`; not inactive/deleted/invite-pending | L422–437 |
| Email required | User entity always has email; no separate verified-email gate | PlatformUser + eligibility |
| Self-reset blocked? | **No** — no `actor == target` check | L174–193 |
| One-time token | Hash stored; mark used; reuse fails | Service complete + constants |
| TTL | 1 hour | `PlatformPasswordResetConstants.DefaultLifetimeHours` |
| Hash only | Yes via `ITokenHashService` | Service create pending |
| Prior tokens revoked | Yes on initiate | `RevokeActivePendingTokensAsync` before create |
| Sessions revoked | On **complete**, all sessions | L380–385 area (complete path) |
| Email timing | On **initiation** (delivery after token create) | Initiate → `DeliverAsync` |
| `resetUrl` null when email | Yes | `AcsPlatformPasswordResetDeliveryService` L119–123 |
| Fallback when not configured | Development `AllowAdminSecureLinkFallback: true` → `admin_secure_link` | appsettings.Development.json L41; delivery L67–80 |
| ACS failure → 502 | Yes | Controller L198–199 |
| Token/URL in logs | Delivery logs userId/expiry/operationId; “Raw token not logged” | Delivery L113–117; ACS sender L96–100 |

Public validate/complete:

| Endpoint | Auth |
|---|---|
| `POST /api/v1/platform-auth/password-reset/validate\|complete` | Anonymous + rate limit |
| Legacy aliases used by Angular: `POST /api/v1/auth/platform-password-reset/validate\|complete` | Anonymous + rate limit + **LegacyApiResponse** |

---

## 6. Configuration Findings

**Do not print secret values.** A terminal attempt to set ACS connection string was observed; the command **failed** (see below). This audit does not repeat any access key.

| Key | Present in committed appsettings? | Expected source |
|---|---|---|
| `AzureCommunicationEmail:ConnectionString` | Empty placeholder | User-secrets / Key Vault / env |
| `AzureCommunicationEmail:Endpoint` | Empty placeholder | Env / Key Vault (prod MI) |
| `AzureCommunicationEmail:SenderAddress` | Empty placeholder | User-secrets / env |
| `AzureCommunicationEmail:SenderDisplayName` | `"OneVerz"` | appsettings |
| `AzureCommunicationEmail:AllowAdminSecureLinkFallback` | `false` (base), `true` (Development) | appsettings |
| `PlatformPasswordReset:PublicAppBaseUrl` | `http://localhost:4200` | appsettings |
| `PlatformPasswordReset:ResetPath` | `/reset-password` | appsettings |

| Check | Result |
|---|---|
| `UserSecretsId` | `epos-api-development-secrets` on `src/E_POS.Api/E_POS.Api.csproj` |
| User secrets loaded? | `WebApplication.CreateBuilder` loads user-secrets for Development by default **when secrets exist** |
| Observed secrets command | Ran from `...\src\E_POS.Api` with `--project src/E_POS.Api` → path resolved to non-existent `...\E_POS.Api\src\E_POS.Api` → **exit code 1** (`terminals/4.txt`) |
| Implication | ACS ConnectionString may **not** be stored yet; Development fallback likely still active |
| Correct command (from repo root `Unified-Commerce`) | `dotnet user-secrets set "AzureCommunicationEmail:ConnectionString" "<secret>" --project src/E_POS.Api` **or** from Api folder: `--project .` / omit wrong relative path |
| Also set | `AzureCommunicationEmail:SenderAddress` (required when ConnectionString/Endpoint set) |
| Options validation | `ValidateOnStart` + `AzureCommunicationEmailOptionsValidator` in `DependencyInjection.cs` L104–107 |
| Empty connection string | ACS disabled; with Dev fallback → `admin_secure_link` |
| SenderAddress from user-secrets overrides empty appsettings | Yes (config hierarchy) **once secrets succeed** |

**Security note (ops):** An ACS access key appeared in local terminal history during the failed secrets command. Rotate that key in Azure if it was a live credential; do not commit it.

---

## 7. Email URL and Reset Route Compatibility

| Check | Result |
|---|---|
| Builder | `PlatformPasswordResetLinkBuilder` L22–31 |
| Shape | `{PublicAppBaseUrl}{ResetPath}?token={Uri.EscapeDataString(rawToken)}` |
| Dev expected | `http://localhost:4200/reset-password?token=...` |
| Angular route | `/reset-password` with query `token` (`reset-password-page.ts` L595–605) |
| Param name match | **Yes** (`token`) |
| Arbitrary callback | **Not accepted** — trusted config only |
| Frontend proxy | `proxy.conf.json` proxies `/api` → `http://localhost:5150`; SPA itself on 4200 |
| Production risk | Committed `PublicAppBaseUrl` is localhost — must be overridden in production config |

**No path mismatch** between backend URL builder and Angular route/query param.

---

## 8. Reset Password Page Findings

| Check | Result | Evidence |
|---|---|---|
| Route exists | Yes | `app.routes.ts` L18–22 |
| Accessible logged out | Yes (`guestGuard`) | If already authenticated, guard redirects to `/admin/dashboard` — **blocks reset while logged in** |
| Token from query | `params.get('token')` | L595–605 |
| New/confirm password | Yes | form L110–190 |
| Policy guidance | Yes | `passwordPolicyGuidance` |
| Visibility toggles | Yes | |
| Submit loading | Yes | `isSubmitting` |
| Invalid/expired/used/revoked states | Yes | view switch L65–97 |
| Success → login | Link to `/login` | L104 |
| Token in storage | Not found | No localStorage/sessionStorage in page |
| Token logging | Not in page code | |
| Validate API | Legacy `/api/v1/auth/platform-password-reset/validate` | `api-endpoints.ts` L6–7 |
| Complete API | Legacy `/api/v1/auth/platform-password-reset/complete` | same |
| DTO validate | Matches `{ isValid, status, expiresAt }` in legacy envelope | Compatible |
| DTO complete | Frontend types `ApiResponse<boolean>` and tests flush `data: true`; backend returns `LegacyApiResponse<CompletePlatformPasswordResetResponse>` with `data: { success, message }` | **Contract mismatch**; page still shows success because `next` ignores boolean |

---

## 9. ACS Delivery Findings

| Check | Result |
|---|---|
| Package | `Azure.Communication.Email` 1.1.0 on Infrastructure |
| DI | `IApplicationEmailSender` → `AzureCommunicationEmailSender`; delivery → `AcsPlatformPasswordResetDeliveryService` |
| ConnectionString client | `new EmailClient(connectionString)` when set |
| Endpoint + DefaultAzureCredential | Implemented when Endpoint set and ConnectionString empty |
| SenderAddress | Required when ACS configured (validator) |
| `WaitUntil.Started` | Yes — accept send, not inbox proof |
| Logging | OperationId, Status, CorrelationId; failures log ErrorCode/Status |
| Inbox delivery claimed? | Message: “email has been sent” / “accepted” — soft claim; Phase 1 doc notes Started ≠ delivered |
| HTML encode display name | Composer uses `WebUtility.HtmlEncode` |
| Token/URL in logs | Asserted avoided in delivery/ACS sender |

---

## 10. API Contract Compatibility

| Surface | Compatible? | Notes |
|---|---|---|
| Initiate → Angular | **Yes** | Legacy envelope + `PlatformPasswordResetInitiation` (`resetUrl` nullable) |
| Initiate email mode UX | **Partial** | Frontend ignores `message`; toast weak |
| Validate (legacy) | **Yes** | |
| Complete (legacy) | **Partial / latent bug** | Frontend expects `data: boolean`; backend returns object; UI still advances to success |
| Error shape for initiate | **Yes** for `success: false` legacy errors including 502 |
| Second Brain initiate deliveryMode | **Stale** | Still documents `admin_secure_link` as R1 only |

---

## 11. Security Findings

| Item | Status |
|---|---|
| Hash-only tokens | Working (backend) |
| No token in FE storage | Working |
| Trusted reset base URL | Working (config) |
| ACS secrets in git | Not in appsettings placeholders |
| Failed user-secrets + key in terminal history | **Security risk / ops** — rotate if live; re-set secrets correctly |
| Dev fallback exposes `resetUrl` to admin when ACS unset | By design for Development |
| Production fallback disabled in base appsettings | Correct default |
| Guest guard + authenticated session | Authenticated users cannot open reset page without logging out first |
| Self-reset allowed | Product decision — not blocked |

---

## 12. Automated Test Coverage

| Layer | Coverage |
|---|---|
| Backend unit/integration | Platform reset + ACS delivery + options validation (Phase 1) |
| Angular login | Asserts admin-assisted message (no forgot link) |
| Angular users page | Initiate + `admin_secure_link` display; **missing email-mode assertions** |
| Angular auth-api | Validate/complete; complete test uses `data: true` (not matching live backend payload) |
| Real ACS smoke | Not in automated suite (by design) |

---

## 13. Manual End-to-End Test Steps

Prerequisites: fix user-secrets against **`Unified-Commerce/src/E_POS.Api`** (correct project path), set ConnectionString **and** SenderAddress, restart API.

1. Start backend Development (`E_POS.Api`).
2. Start Angular Platform Admin (`ng serve` → port 4200; proxy `/api` → 5150).
3. Login as Platform Admin with `platform.users.update`.
4. Open **Platform Users** (`/admin/platform-users`).
5. Open an eligible user (ACTIVE/LOCKED, not invite-pending).
6. Click **Send Password Reset** → confirm.
7. Expect API **200** with legacy envelope:
   - `data.deliveryMode === "email"`
   - `data.resetUrl === null`
8. Backend logs: ACS OperationId; **no** raw token/URL query.
9. Check inbox/spam for OneVerz reset mail.
10. Open link → `/reset-password?token=...` (logout first if already signed in).
11. Page validates token; form appears.
12. Submit new password meeting policy.
13. Expect complete **200**; success view → login.
14. Old password login → **401**.
15. New password login → **200**.
16. Prior sessions fail API calls (session revoked).
17. Reuse same token complete → **400** (`platform_password_reset.token_used`).

If secrets missing: expect `deliveryMode: admin_secure_link` and non-null `resetUrl` (Dev fallback) — copy link instead of email.

---

## 14. Missing Items

| Priority | Gap | Layer | Evidence | Required action |
|---:|---|---|---|---|
| P0 | User-secrets command failed; ACS may not be active | Config / Ops | Terminal exit 1 wrong `--project` path | Re-run secrets on `src/E_POS.Api`; set SenderAddress; restart API; rotate exposed key if live |
| P1 | Email-mode toast/modal copy still link-centric | Frontend | `platform-users-page.ts` L251–253, L677–680 | Use `result.message` / email-specific copy when `deliveryMode=email` |
| P1 | No Angular test for `deliveryMode=email` / null `resetUrl` | Frontend tests | `platform-users-page.spec.ts` still admin_secure_link | Add email-mode test |
| P2 | Complete DTO expects `boolean` vs `{success,message}` | Frontend contract | `auth-api.service.ts` L73–79 vs legacy controller | Align DTO mapping |
| P2 | Second Brain / Included Features still say email deferred | Docs | Journey Out Of Scope; Included_Features L42 | Update to ACS email on admin-initiated flow |
| P2 | Authenticated guestGuard blocks reset page | Frontend | `guest.guard.ts` L10 | Product: allow reset while logged in or document logout requirement |
| P3 | Self-reset not blocked | Backend/product | No actor≠target check | Decide and enforce if required |
| P3 | Production PublicAppBaseUrl still localhost in repo defaults | Config | appsettings | Ensure prod override |
| — | Self-service Forgot Password link on login | Product | Explicitly out of scope | Do not implement in this phase |
| — | Tenant password reset | Product | Phase 1 out of scope | Deferred |

---

## 15. Final Classification

| Area | Status |
|---|---|
| ACS infrastructure | **COMPLETE** |
| Platform admin-initiated reset backend | **COMPLETE** |
| Platform admin-initiated reset frontend | **Implemented** button/flow; email-mode copy polish optional (null `resetUrl` already hides Copy Link) |
| Reset-password frontend page | **Working** for logged-out users; guestGuard caveat when logged in |
| Real Azure email delivery | **PASSED** (manual E2E after ACS BadRequest sender fix) |
| Session revocation | **Working** (backend complete path + integration tests + manual E2E) |
| Self-service forgot password | **Out of current scope** (login message correct) |
| Production readiness | Follow-ups remain: Key Vault / prod PublicAppBaseUrl, outbox/retry, custom domain, optional FE email-mode copy polish |

---

## 16. Manual E2E verification addendum (PASSED)

**Status:** COMPLETED  
**Manual E2E verification:** PASSED  

Verified (no sensitive evidence recorded):

- ACS email received
- `deliveryMode=email`
- `resetUrl=null`
- reset link completed successfully
- old password rejected
- new password accepted
- token reuse rejected
- previous refresh session revoked

---

### Bottom line for the screenshot

Seeing **"Ask a Platform Admin to send a password reset."** on login means the product expects another admin to use **Platform Users → Send Password Reset**, then the user completes via email (or Dev secure link). It is **not** evidence that forgot-password is broken; forgot-password was never approved for this surface.
