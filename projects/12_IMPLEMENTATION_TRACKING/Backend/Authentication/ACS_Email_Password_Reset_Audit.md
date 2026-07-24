# ACS Email and Password Reset Backend Audit

**Audit date:** 2026-07-24  
**Repository audited:** `C:\Users\User\Desktop\Nytroz__POS\Nytroz POS - Backend New\Unified-Commerce`  
**Second Brain source used:** `C:\Users\User\Desktop\Nytroz__POS\second-brain-docs-worktree`  
**Requested Second Brain path not available:** `W:\UNIFIED COMMERCE\2nd Brain commerce\Pos-system-Knowledge` (drive/path missing on this machine)  
**Scope:** AUDIT ONLY — no application source, migrations, packages, or configuration were modified.

---

## 1. Executive Summary

| Question | Finding |
|---|---|
| Does email infrastructure already exist? | **No.** No ACS/SMTP/MailKit/SendGrid packages, no `IEmailSender`, no email options. Platform reset uses `admin_secure_link` only. Notification **tables** exist in EF; no Application email/notification services found. |
| Does password reset already exist? | **Platform admin-initiated: yes** (SA-P1-06, complete with minor email gap). **Tenant self-service: no.** **Tenant-admin initiated: no.** Tenant `password_reset_tokens` is a shell entity/table only. |
| ASP.NET Identity or custom auth? | **Custom auth.** No `UserManager` / `SignInManager` / Identity packages. |
| Can tenant-user reset reuse existing password services? | **Partially.** Reuse `IPasswordHashService` (PBKDF2), `ITokenHashService`, `IRefreshTokenGenerator`, session validator pattern, and platform reset as a **template**. Tenant needs new service/repo, all-session revoke (missing today), and email delivery. |
| Biggest blockers | (1) No email provider. (2) Second Brain has **no approved tenant/self-service reset journey** (explicitly out of R1 for SA-P1-06). (3) Tenant email uniqueness vs login mismatch. (4) No tenant `RevokeAllSessions`. (5) Empty `Email_Service_Integration.md`. |
| Complexity | **High** — new ACS integration + three flows (email, self-service, tenant-admin) across auth, permissions, DB semantics, and product decisions not yet signed off in Second Brain. |

---

## 2. Second Brain Requirements

| Requirement | Source document | Section | Intended behavior | Status in backend |
|---|---|---|---|---|
| Store only token hashes; never log raw tokens | `05_BACKEND_ARCHITECTURE/Authentication.md` | Token Rules | Hash-only storage; no raw tokens in DB/logs/responses | **Implemented** for platform reset & refresh tokens |
| Revoke sessions on logout / lock / suspension | same | Session Rules | Block protected actions when session revoked | **Implemented** via `AuthSessionValidator` + session `RevokedAt` |
| Platform users separate from tenant users | same | Identity Types / Platform User Authentication | Separate auth surfaces & sessions | **Implemented** (`platform_users` vs `tenant_users`) |
| Admin-initiated platform password reset | `03_USER_JOURNEYS/Platform_Admin/17_Platform_User_Password_Reset_Flow.md` | Main Flow | `platform.users.update` initiate; 1h one-time token; public validate/complete; revoke all sessions; R1 delivery = `admin_secure_link` | **Implemented** |
| Automated email for platform reset | same + `SA-P1-06_...Implementation.md` | Out Of Scope / Email Delivery | Deferred; `pending_email` reserved | **Missing** (gap documented) |
| Self-service forgot password (platform) | `17_Platform_User_Password_Reset_Flow.md` | Out Of Scope | Not in R1 | **Missing** (by design for R1) |
| Tenant staff / customer password reset | same | Out Of Scope | Not in R1 SA-P1-06 | **Missing** (table shells only) |
| Tenant password reset table | `06_DATABASE_KNOWLEDGE/Tables/07_Invitations_...UPDATED.md` | `password_reset_tokens` | Tenant-scoped reset tokens; hash-only; expires/used/revoked | **Table/entity exist; no service/API** |
| Tenant auth API group includes `/api/v1/password-reset` | `04_MODULE_KNOWLEDGE/06_Auth_Tokens_Security_Audit/01_Module_Overview.md` + `03_Technical_Contract.md` | API Contract | Staff password-reset API group | **Conflict:** actual routes are `/api/v1/tenant-auth/*`; no `/password-reset` |
| Invite / setup / email verification tokens as hashes | same module | Core Business Rules | Hash-only invite/setup/verify/reset | **Partial:** entities exist; invite create persists invite; no email; no setup/verify/reset services |
| Avoid account enumeration | `05_BACKEND_ARCHITECTURE/Backend_Coding_Principles.md` | (coding principle) | Same failure for missing users | **Partial** on login; platform reset initiate returns 404 for unknown user (admin path — OK); self-service N/A |
| Azure email services as integration folder | `E-POS Macro-Modules Architecture.md` | Integrations/Email | Infra folder intended for Azure email providers | **Missing** — no `Integrations/Email` implementation |
| Email service integration doc | `12_INTEGRATIONS/Email_Service_Integration.md` | (entire file) | Intended ACS/email contract | **Empty draft** (frontmatter only) |
| Notification schema | `06_DATABASE_KNOWLEDGE/Tables/26_Notification_UPDATED.md` | Entity Tables | Channels, templates, messages, delivery attempts | **EF entities/tables present; no Application usage for email** |
| R1 includes admin platform reset; email deferred | `01_RELEASE_SCOPE/Included_Features.md` | Platform And Tenant Setup | Admin-initiated platform reset included; automated email deferred | **Matches backend** |
| `password_reset_tokens.status` CHECK | `06_DATABASE_KNOWLEDGE/Status_And_Type_Check_Rules.md` | Module 07 | `PENDING\|USED\|EXPIRED\|REVOKED` | **Conflict:** tenant entity uses `UsedAt`/`RevokedAt` only — **no Status column/CHECK** in EF config |
| Permission `tenant.users.manage` | `02_ACCESS_CONTROL/Permission_Code_List.md` | codes | Manage tenant users | **Seeded plus granular codes**; no `reset_password` code in catalog |
| Tenant invite email on create | Platform/Tenant journeys (`08_Tenant_User_Management_Flow`, activation flows) | Invite steps | System sends invitation email | **Missing** — invite rows created; no sender |

**Ambiguities / conflicts between Second Brain documents**

1. Module 06 technical contract still lists `/api/v1/auth/login|refresh|logout` and `/api/v1/password-reset`, while `API_ENDPOINTS.md` documents implemented `/api/v1/tenant-auth/*` and platform password-reset under `/api/v1/platform-auth/password-reset`.
2. `Status_And_Type_Check_Rules.md` requires `password_reset_tokens.status`; table detail doc `07_...UPDATED.md` documents `used_at`/`revoked_at` without a status column.
3. Platform reset journey says hash via bcrypt *or* platform hash service; implementation is PBKDF2 (not bcrypt).
4. ACS Email + self-service tenant reset + tenant-admin reset are **requested for implementation** but **not** covered by an approved Second Brain journey (explicitly out of SA-P1-06 R1).

---

## 3. Current Authentication Architecture

### Facts

1. **ASP.NET Core Identity:** Not used (no Identity package / `UserManager` / `SignInManager` in `*.csproj` / `*.cs`).
2. **Custom users:** `PlatformUser` → `platform_users`; `TenantUser` → `tenant_users`; customers separate (`CustomerAuthAccount`).
3. **Password hashing:** `PasswordHashService` — PBKDF2-SHA256, 100 000 iterations, 16-byte salt, 32-byte hash, fixed-time verify.  
   Evidence: `src/E_POS.Infrastructure/Common/Security/PasswordHashService.cs` L6–60.
4. **JWT:** Custom HS256 via `IJwtTokenFactory`; access ~15 min, refresh ~7 days (platform/tenant) from `PlatformJwt` / `TenantJwt` config keys.
5. **Session binding:** `Program.cs` `OnTokenValidated` calls `IAuthSessionValidator.IsCurrentSessionActiveAsync` (L78–93). Revoked sessions fail subsequent API calls even if JWT not expired.
6. **Tenant login:** email + password only (`TenantLoginRequest`). Repository comment claims global email uniqueness; EF unique index is `(TenantId, Email)`.  
   Evidence: `TenantAuthRepository.cs` L18–35; `TenantUserConfiguration.cs` L154–156.
7. **Tenant context after login:** JWT claims `tenant_id`, `sub`, `session_id`, `identity_type`; APIs use `TenantOnly` + `ITenantRequestContextFactory`.
8. **Statuses:** Platform `ACTIVE|INACTIVE|LOCKED|DELETED`; Tenant users `ACTIVE|INACTIVE|INVITED` (+ login treats locked); invite sentinel password `PENDING_INVITE:UNSET`.
9. **Email verification:** Shell tables/entities; `EmailVerifiedAt` on platform user unused in Application.
10. **Platform password reset:** Full stack (controllers, service, repository, token entity, policy validator, delivery stub, tests).

### Platform reset flow (evidence)

- Initiate: `POST /api/v1/platform-admin/users/{userId}/password-reset` — `PlatformAdminUsersController`  
- Validate/Complete: `POST /api/v1/platform-auth/password-reset/{validate|complete}` — `PlatformPasswordResetController.cs` L12–60  
- Service: `PlatformPasswordResetService.cs` (token issue L87–113, initiate L180–258, complete L300–407)  
- Delivery: `AdminSecureLinkPasswordResetDeliveryService` — returns URL to admin; **no email** (`PlatformPasswordResetDelivery.cs` L30–57)

---

## 4. Existing Email Infrastructure

| Component | Exists? | File/evidence | Reusable? | Notes |
|---|---|---|---|---|
| Generic email abstraction (`IEmailSender`) | No | Grep across Application — no matches | N/A | Must create |
| ACS / `Azure.Communication.Email` package | No | All `*.csproj` PackageReferences inspected | N/A | Must add |
| `Azure.Identity` | No | same | N/A | Optional depending on ACS auth mode |
| SMTP / MailKit / SendGrid | No | same | N/A | — |
| EmailClient DI registration | No | `DependencyInjection.cs` — only admin secure link delivery | N/A | — |
| Email options / ACS endpoint / connection string keys | No | `appsettings.json` — no Email/ACS section | N/A | Keys must be designed |
| Sender address / display name config | No | — | N/A | — |
| Frontend / reset URL config | Partial | `PlatformPasswordReset:PublicAppBaseUrl`, `ResetPath` | Yes for platform | Tenant frontend URL keys missing |
| Email templates / rendering | No | — | N/A | Notification template tables exist unused |
| Email queue / outbox / background worker | No | Sync outbox is POS offline, not email | N/A | — |
| Retry / delivery status tracking | No | Notification delivery attempt **tables** in snapshot | Schema maybe later | No services |
| Invitation / activation email flow | No | `TenantAdminUserService` creates `UserInvite` when `SendInviteEmail` (L175–196) but does not send | Invite persistence reusable | Email missing |
| Platform reset delivery seam | Yes | `IPlatformPasswordResetDeliveryService` + `AdminSecureLink...` | **Yes** — swap/implement email mode | `DeliveryModePendingEmail` constant reserved |
| Second Brain email integration doc | Draft empty | `12_INTEGRATIONS/Email_Service_Integration.md` | No contract yet | Blocker for product decisions |

**Config key names present today (no secret values):**  
`ConnectionStrings:DefaultConnection`, `PlatformJwt:*`, `TenantJwt:*`, `CustomerJwt:*`, `PlatformPasswordReset:PublicAppBaseUrl`, `PlatformPasswordReset:ResetPath`.

---

## 5. Existing Password and Token Infrastructure

| Component | Exists? | File/evidence | Reusable? | Gap |
|---|---|---|---|---|
| Platform password reset service | Yes | `PlatformPasswordResetService.cs` | Pattern for tenant | Platform-only |
| Platform reset token table | Yes | `platform_password_reset_tokens` / `PlatformPasswordResetToken.cs` | Platform only | Separate from tenant |
| Tenant `password_reset_tokens` | Shell | `PasswordResetToken.cs` L5–15; config L8–46 | Schema base | No factories/status helpers; no Status column; no repo/service |
| Customer password reset tokens | Shell | entity + config in snapshot | No for this feature | Separate product |
| Token hash service | Yes | `ITokenHashService` (HMAC with JWT signing key) | Yes | Same pattern as platform |
| Refresh token generator (CSPRNG) | Yes | used by platform reset `CreateRefreshToken` | Yes | — |
| Password policy (platform) | Yes | `PlatformPasswordPolicyValidator.cs` | Copy/adapt | No shared tenant policy yet |
| Password hash update | Yes | `IPasswordHashService` + entity setters | Yes | Tenant uses `EncryptedPassword` column name |
| Revoke all platform sessions | Yes | `PlatformAuthRepository.RevokeAllSessionsForUserAsync` L132–169 | Pattern | Tenant equivalent **missing** |
| Revoke current tenant session | Yes | `TenantAuthRepository.RevokeCurrentSessionAsync` L235–268 | Partial | All-session revoke needed for reset |
| TokenVersion / SecurityStamp | No | Grep — zero matches | N/A | Not required if session revoke works |
| Access token blacklist | No | Invalidation via session check | Session model is the reuse target | — |
| Rate limiting on public auth | Yes | `RateLimitingExtensions.cs` — 10/min/IP `AuthLogin` | Yes | Apply to new public request endpoints |
| Outbox for email | No | — | N/A | Decide sync vs outbox |

**Session/token Q&A**

| Question | Answer | Evidence |
|---|---|---|
| Can all refresh tokens for one **platform** user be revoked? | **Yes** | `RevokeAllSessionsForUserAsync` |
| Can all refresh tokens for one **tenant** user be revoked? | **No** (current session only) | `RevokeCurrentSessionAsync` only |
| Does password change invalidate sessions (platform)? | **Yes** on reset complete | `PlatformPasswordResetService` L380–385 |
| Does password change invalidate sessions (tenant)? | N/A — no change/reset API | — |
| Can already-issued access tokens be invalidated? | **Effectively yes** if session revoked | `Program.cs` L78–93 + `AuthSessionValidator` |
| What should password reset reuse? | Hash services, token hash, session revoke pattern, rate limit policy, ApplicationResult errors, platform reset as blueprint | — |
| What is missing? | Email; tenant all-session revoke; tenant reset service/API; self-service product rules; tenant frontend URL config; permission for admin reset (optional) | — |

---

## 6. Tenant User Management and Permissions

### Endpoints (`TenantAdminUsersController`, route `api/v1/tenant-admin/users`)

| Method | Route | Purpose |
|---|---|---|
| GET | `/` | List users |
| GET | `/create-options` | Create form options |
| POST | `/` | Create (optional invite flag) |
| GET | `/{id}` | Detail |
| PUT | `/{id}` | Update |
| DELETE | `/{id}` | Soft-disable/delete path |
| — | **No** `/{id}/password-reset` | Missing |

### Permissions (`TenantAdminUserPermissions.cs`)

- `tenant.users.view`, `.create`, `.invite`, `.update`, `.delete`, `.disable`, `.details.view`, `.permission_override`, `.manage`
- Seeded in migration `20260708151915_SeedTenantAdminUserManagementPermissions.cs`
- **No** `tenant.users.reset_password` (or similar) in constants, Permission_Code_List, or seeds

**Closest existing permission for admin-initiated reset:** `tenant.users.update` (mirrors platform `platform.users.update`) or umbrella `tenant.users.manage`.  
**Recommendation (not implemented):** Prefer a new granular `tenant.users.reset_password` if product wants least privilege; otherwise reuse `.update` like platform.

### Tenant isolation

- Lookups scoped by `context.TenantId` from JWT claims.
- Cross-tenant user id → `user.not_found` → **HTTP 404** (not 403). Evidence: controller `ToErrorResult` L159–170.
- Permission failures → **403** `user.permission_denied`.

### Password operations today

- Create with invite: pending invite user + `UserInvite` row; **no email send**.
- Create without invite: random PBKDF2 placeholder hash, status `INACTIVE`.
- No change-password / set-password / force-change / admin reset APIs.

---

## 7. Database Findings

| Table / entity | App usage | Notes |
|---|---|---|
| `platform_users` | Full | Password hash, status |
| `platform_password_reset_tokens` | Full | Status lifecycle PENDING/USED/EXPIRED/REVOKED; unique token_hash |
| `platform_auth_sessions` / `platform_refresh_tokens` / `platform_login_audits` | Full | Reset revokes sessions + audits |
| `tenant_users` | Full | Unique `(tenant_id, email)` |
| `password_reset_tokens` | **Shell** | Hash, tenant_id, user_id, expires/used/revoked; **no status**; AuditableEntity UpdatedAt |
| `user_invites` / `user_setup_tokens` / `email_verification_tokens` | Invite create only / shell | No acceptance/email/verify services |
| `tenant_auth_sessions` / `tenant_refresh_tokens` / `tenant_login_audits` | Login/refresh/logout | No all-session revoke helper |
| `customer_password_reset_tokens` | Shell | Out of scope for this audit feature set |
| Notification tables (`notification_*`) | Mapped in snapshot | No Application email pipeline |

**Is a new password-reset-token table needed?**  
**No for tenant** — reuse `password_reset_tokens`, but likely need a migration to align with Second Brain CHECK/`status` (or explicitly decide timestamp-only lifecycle).  
**No for platform** — already complete.  
**Not a single reusable generic token table** — three parallel designs (platform / tenant / customer).

**Token storage facts**

- Raw tokens: never persisted (platform reset hashes via `ITokenHashService`).
- Tenant shell expects `token_hash` unique.
- Platform stores `status` + timestamps; tenant shell stores timestamps only.

**Tenant email uniqueness (blocker for self-service)**

- DB allows same email in different tenants.
- Login resolves by email alone (`FirstOrDefaultAsync`) — **ambiguous if duplicates exist**.
- Self-service reset **cannot** safely use email-only without resolving this conflict.

---

## 8. Existing API Conventions

### Observed conventions

- **Controllers** (`[ApiController]`), not minimal APIs for auth.
- **No MediatR**; services injected into controllers.
- **Versioned routes:** `api/v1/...`
- **Errors:** custom `{ code, message, details, traceId, timestamp }` — not ASP.NET ProblemDetails middleware.
- **Success:** often raw DTO or `{ data: ... }` (tenant admin); platform admin sometimes `LegacyApiResponse`.
- **Validation:** hand-rolled validators; **no FluentValidation** package.
- **Result type:** `ApplicationResult` / `ApplicationError`.
- **Auth policies:** `PlatformOnly`, `TenantOnly`, `CustomerOnly`.
- **Rate limit:** `[EnableRateLimiting(RateLimitingPolicies.AuthLogin)]` on anonymous auth endpoints.

### Candidate routes vs recommendation

| Candidate | Fit | Recommendation |
|---|---|---|
| `POST /api/v1/auth/password-reset/request` | Conflicts with current `tenant-auth` / `platform-auth` prefixes | Prefer identity-scoped prefixes |
| `POST /api/v1/auth/password-reset/confirm` | same | Prefer `complete` naming to match platform |
| `POST /api/v1/tenant-admin/users/{userId}/password-reset` | **Matches** platform admin pattern | **Recommended** for tenant-admin initiated |

**Recommended routes (based on repository conventions)**

| Flow | Route | Auth |
|---|---|---|
| Tenant self-service request | `POST /api/v1/tenant-auth/password-reset/request` | Anonymous + rate limit |
| Tenant self-service validate | `POST /api/v1/tenant-auth/password-reset/validate` | Anonymous + rate limit |
| Tenant self-service complete | `POST /api/v1/tenant-auth/password-reset/complete` | Anonymous + rate limit |
| Tenant-admin initiate | `POST /api/v1/tenant-admin/users/{userId}/password-reset` | Tenant JWT + permission |
| Platform email upgrade | Keep existing platform routes; change delivery implementation | Existing |

**Request/response shapes (mirror platform)**

- Initiate (admin): empty body → `{ userId, email, expiresAt, deliveryMode, resetUrl?, message }` (or suppress `resetUrl` when email-only).
- Self-service request: `{ email }` (+ **tenant discriminator if product allows duplicate emails**) → **generic success** always.
- Validate: `{ token }` → `{ isValid, status, expiresAt }`
- Complete: `{ token, newPassword, confirmPassword }` → `{ success, message }`

**Tenant context for self-service (fact-based)**

- Current login does **not** accept `tenantCode`.
- Login comment assumes globally unique email, but schema is per-tenant unique.
- **Cannot recommend email-only self-service without a product decision.** Options that fit code today:
  1. Enforce **global unique email** (restore global unique index) and resolve tenant from user row (matches login comment).
  2. Keep per-tenant emails and require `tenantCode` / host/domain on login **and** reset request (requires login change too).

---

## 9. Security Gap Analysis

| Security requirement | Current status | Evidence | Required action |
|---|---|---|---|
| Generic response for unknown emails (self-service) | **Missing** | No self-service endpoint | Implement always-same response |
| No account enumeration | **Partial** | Login / coding principles; admin initiate 404 OK | Design self-service carefully |
| Cryptographically secure reset token | **Implemented** (platform) | Uses refresh token generator | Reuse for tenant |
| Only token hash stored | **Implemented** (platform); **schema ready** (tenant) | Platform service L100; tenant entity `TokenHash` | Implement tenant hashing |
| Single-use token | **Implemented** (platform) | Mark used + revoke pending | Port to tenant |
| Token expiry | **Implemented** (platform, 1h) | `DefaultLifetimeHours = 1` | Define tenant TTL |
| Revoke previous reset tokens | **Implemented** (platform) | `RevokeActivePendingTokensAsync` | Port to tenant |
| Password policy validation | **Implemented** (platform only) | `PlatformPasswordPolicyValidator` | Shared or tenant validator |
| Refresh-token/session revocation on reset | **Platform yes / Tenant missing** | Platform L380–385; tenant no all-revoke | Add `RevokeAllSessionsForUser` for tenant |
| Cross-tenant protection | **Implemented** for admin APIs | TenantId scoping → 404 | Keep for admin reset |
| Rate limiting | **Reusable** | AuthLogin 10/min/IP | Apply to new public endpoints |
| Audit logging | **Platform yes / Tenant pattern exists** | `platform_login_audits` methods; `tenant_login_audits` table | Define tenant audit method codes |
| No password/token in logs | **Platform delivery OK** | Delivery logs userId/expiry only | Enforce for ACS adapter |
| No arbitrary callback URL | **Platform OK** | Trusted `PublicAppBaseUrl` + `ResetPath` | Same for tenant |
| Reset link from trusted config | **Platform yes / Tenant missing** | `PlatformPasswordReset` section | Add tenant frontend URL settings |
| Concurrency on token consume | **Partial** | Mark used then revoke pending; no explicit row version | Consider transactional conditional update |
| Email provider failure handling | **Missing** | No provider | Decide fail-open (admin link) vs fail-closed |
| Secrets not committed | **Risk** | `appsettings.json` contains placeholder JWT signing keys (not ACS) | Keep ACS secrets in Key Vault / env; do not commit |

---

## 10. Test Coverage Findings

### Reusable existing tests

| Area | Files |
|---|---|
| Platform password reset E2E | `PlatformPasswordResetFlowTests.cs` |
| Platform reset service edges | `PlatformPasswordResetServiceTests.cs` |
| Platform reset API | `PlatformPasswordResetControllerTests.cs`, `PlatformPasswordResetApiSurfaceTests.cs` |
| Password policy | `PlatformPasswordResetUnitTests.cs` / policy tests |
| Platform auth refresh / login | `PlatformAuthServiceTests`, `PlatformAuthRefreshServiceTests`, integration auth suites |
| Tenant auth | `TenantAuthServiceTests`, `TenantAuthControllerTests` |
| Tenant user management | `TenantAdminUserServiceTests`, `TenantAdminUsersControllerTests` |
| Password hashing | `PlatformSecurityServiceTests` (hash) |

### Fixtures / patterns

- Integration tests substitute delivery with passthrough (`DeliveryModeAdminSecureLink`).
- API tests mock `IPlatformPasswordResetService`.
- Auth rate limiting policy exists for anonymous endpoints.

### Missing tests required for requested features

1. ACS / `IEmailSender` unit tests (no secrets; mock EmailClient).
2. Platform reset email delivery mode (`pending_email`) success/failure.
3. Tenant self-service request: unknown email, known email, rate limit, enumeration safety.
4. Tenant self-service complete: policy, reuse, expiry, revoke, session invalidation.
5. Tenant-admin initiate: permission grant/deny, cross-tenant 404, eligibility.
6. Tenant email uniqueness / tenantCode resolution tests (whichever decision is chosen).
7. Concurrent double-consume of same token.
8. Audit row assertions for tenant reset events.
9. No raw token / password in log assertions.

---

## 11. Second Brain vs Backend Conflicts

1. **ACS Email:** Architecture folder intends Azure email; integration doc empty; backend has zero email provider — R1 explicitly deferred email for platform reset.
2. **Self-service / tenant reset:** Requested for implementation, but Second Brain marks them **out of scope** for SA-P1-06 / R1 platform journey — **no approved tenant journey**.
3. **API prefixes:** Module 06 still documents `/api/v1/auth/*` and `/api/v1/password-reset`; backend uses `/api/v1/tenant-auth/*` and `/api/v1/platform-auth/password-reset`.
4. **`password_reset_tokens.status`:** Required in Status_And_Type_Check_Rules; absent in EF tenant entity/config and in table detail attribute list (uses used_at/revoked_at).
5. **Bcrypt vs PBKDF2:** Journey text mentions bcrypt or platform hash service; backend is PBKDF2-SHA256 only.
6. **Tenant email uniqueness:** Login code assumes global uniqueness; EF unique index is per-tenant.
7. **Invite emails:** Journeys describe sending invite/activation email; backend creates invite rows only.
8. **Notification module:** Full ERD in Second Brain + EF mapping; no Application notification/email pipeline — not usable as drop-in for ACS without new work.
9. **Permission catalog:** Second Brain `Permission_Code_List` only lists `tenant.users.manage`; backend seeds finer-grained codes — catalog doc lag.

---

## 12. Files That Would Need Changes

### Domain

| Path | Why |
|---|---|
| `src/E_POS.Domain/Modules/Tenant/TenantAuth/Entities/PasswordResetToken.cs` | Add factories/status helpers (or timestamp lifecycle methods) |
| `src/E_POS.Domain/Modules/Tenant/TenantAuth/Constants/*` (existing or new) | Reset TTL, audit method codes, revoke reasons |
| `src/E_POS.Domain/Modules/Tenant/AccessControl/Constants/TenantAdminUserPermissions.cs` | Optional new `reset_password` permission |
| `src/E_POS.Domain/Modules/Platform/PlatformAdmin/Constants/PlatformPasswordResetConstants.cs` | Possibly activate `pending_email` semantics |
| Proposed: `.../Common/Email/*` or Integrations contracts | Email message model if Domain-owned |

### Application

| Path | Why |
|---|---|
| Proposed: `IEmailSender` / email DTOs | Abstraction for ACS |
| `IPlatformPasswordResetDeliveryService` impl consumers | Wire email delivery mode |
| Proposed: `ITenantPasswordResetService` + DTOs + validators | Self-service + complete |
| Proposed: extend `ITenantAuthRepository` or new reset repo | Token CRUD + all-session revoke |
| `TenantAdminUserService` / contracts | Admin-initiated reset orchestration |
| `PlatformPasswordPolicyValidator` or shared policy | Tenant password policy |
| `Application/DependencyInjection.cs` | Register new services |

### Infrastructure

| Path | Why |
|---|---|
| Proposed: `Integrations/Email/AzureCommunicationEmailSender.cs` | ACS EmailClient adapter |
| `PlatformPasswordResetDelivery.cs` | Email-capable delivery (keep admin link fallback) |
| Proposed: `TenantPasswordResetRepository.cs` | Persist/consume `password_reset_tokens` |
| `TenantAuthRepository.cs` | Add `RevokeAllSessionsForUserAsync` |
| `PasswordResetTokenConfiguration.cs` | Status/CHECK/indexes if aligned to Second Brain |
| `DependencyInjection.cs` | Register EmailClient/options |
| `E_POS.Infrastructure.csproj` | Add Azure.Communication.Email (+ Identity if needed) |

### API

| Path | Why |
|---|---|
| Proposed: `TenantPasswordResetController` under `tenant-auth/password-reset` | Self-service endpoints |
| `TenantAdminUsersController.cs` | `POST {userId}/password-reset` |
| Existing platform reset controllers | Unchanged routes; behavior via delivery |
| `RateLimitingExtensions.cs` | Possibly dedicated reset policy |
| `appsettings*.json` | Email/ACS + tenant public URL keys (**values via env/Key Vault**) |

### Migrations

| Path | Why |
|---|---|
| New migration (not created in this audit) | Optional `status` CHECK on `password_reset_tokens`; permission seed; indexes |
| Permission seed migration | If new `tenant.users.reset_password` |
| **Do not** invent migration until schema decision settled | Status vs timestamp-only conflict |

### Tests

| Path | Why |
|---|---|
| New unit/integration/API tests listed in §10 | Coverage for email + both reset flows |
| Extend platform reset tests | Email delivery mode |

### Configuration / Second Brain (docs only for product sign-off)

| Path | Why |
|---|---|
| `12_INTEGRATIONS/Email_Service_Integration.md` | Currently empty — must define ACS contract before coding |
| New tenant password-reset journey | Currently missing |
| Permission_Code_List.md | Add reset permission if chosen |

---

## 13. Recommended Implementation Sequence

Adjusted for this repository (email first unlocks admin platform gap and both tenant flows):

1. **Product decisions** (email uniqueness, tenantCode, permission granularity, ACS auth mode) — unblock architecture.
2. **Azure configuration** — options keys, Key Vault/env wiring, sender address (no secrets in git).
3. **Email abstraction + ACS provider** — `IEmailSender`, DI, unit tests with mocks; keep `admin_secure_link` fallback.
4. **Platform delivery upgrade** — implement `pending_email` via existing `IPlatformPasswordResetDeliveryService` seam (lowest-risk first consumer).
5. **Tenant session revocation** — add `RevokeAllSessionsForUserAsync` mirroring platform.
6. **Token persistence for tenant** — repository + domain helpers on existing `password_reset_tokens` (schema migration only if status CHECK required).
7. **Self-service request + confirm** — `tenant-auth/password-reset/*` with enumeration-safe responses + rate limits.
8. **Session revocation on complete** — wire all-session revoke + audits.
9. **Tenant-admin initiated reset** — `tenant-admin/users/{id}/password-reset` + permission.
10. **Permission/audit/catalog docs** — seeds + Second Brain updates.
11. **Rate limiting review** — dedicated policy if AuthLogin window too coarse.
12. **Tests** — unit/integration/API as in §10.

---

## 14. Final Decision Questions

Unresolved from repository + Second Brain (cannot be answered from evidence alone):

1. Is **ACS Email** approved as the sole transactional provider for Release next, or is SMTP fallback required?
2. ACS auth: **connection string** vs **Managed Identity** + endpoint?
3. Should platform reset switch from `admin_secure_link` to email-only, or dual-mode?
4. Is **tenant self-service forgot-password** in scope now despite R1 Second Brain exclusion?
5. Is **tenant-admin initiated reset** in scope, and which permission: `tenant.users.update` vs new `tenant.users.reset_password`?
6. Are tenant user emails **globally unique** or **per-tenant**? (Login vs unique index conflict.)
7. If per-tenant emails: what tenant discriminator for login/reset — `tenantCode`, subdomain, or domain mapping?
8. Should tenant reset reuse platform-style **status** column or keep **used_at/revoked_at** only?
9. Token TTL for tenant reset — also 1 hour?
10. Should failed ACS sends fall back to admin secure link, fail the API, or enqueue retry/outbox?
11. Which frontend base URL hosts tenant reset pages (Angular tenant admin vs Flutter vs separate)?
12. Must invite/activation emails ship in the same ACS effort, or password-reset only?

---

## Audit Evidence Index (primary)

- Second Brain: `second-brain-docs-worktree` (Authentication, SA-P1-06, journey 17, module 06, table 07, notification 26, Email_Service_Integration empty, Included_Features, Macro architecture Email folder).
- Backend: `PlatformPasswordReset*`, `PasswordHashService`, `TenantAuth*`, `TenantAdminUsers*`, `AuthSessionValidator`, `Program.cs` JWT events, `RateLimitingExtensions`, EF `PasswordResetTokenConfiguration`, `EPosDbContextModelSnapshot`, `appsettings.json` keys, all `*.csproj` package refs.

**End of audit.**
