# OneVerz Email Scenario and Tenant Onboarding Audit

<!-- status: Audit Complete -->
<!-- audit_date: 2026-07-27 -->
<!-- scope: Email notifications + tenant onboarding (paid / trial / demo) -->
<!-- mode: AUDIT-ONLY — no code, migrations, packages, or Second Brain updates -->

## 1. Executive Summary

OneVerz today has **one production email path**: Platform Admin–initiated **password reset** via Azure Communication Services (ACS), with `deliveryMode=email` and `resetUrl=null`. That path is implemented, tested, and documented (SA-P1-06).

**Tenant onboarding emails are not implemented.** Second Brain contains **aspirational** journeys that require payment-link and setup/invite emails, while the **implemented** create-wizard contract and API docs explicitly say **email is not sent** until notification infrastructure exists. Payment Links are **Release 1 mandatory but PLANNED_NOT_STARTED** (domain/DB only; no API/UI/email).

| Area | Verdict |
|---|---|
| ACS infrastructure | **COMPLETE** for platform password reset only |
| Tenant subscription confirmation email | **Missing** (aspirational / decision gap) |
| Payment-link email | **Documented target; not implemented** |
| Tenant activation / invite / set-password email | **Documented in journeys; deferred in implemented wizard** |
| Trial/Demo welcome vs Created+Activated split | **Product decision required** — evidence conflicts |
| Email outbox / retry / resend | **Not implemented** (explicitly out of ACS Phase 1 scope) |
| Complete email scenario catalog in Second Brain | **Does not exist** |

**Critical implementation mismatch (non-email but blocks Flow A/B):** wizard create writes **billing status** values (`pending`/`paid`/…) into `tenants.status`, while activation only allows lifecycle statuses (`setup_pending`, `pending_payment`, `pending_activation`, `inactive`, `draft`). See §7.

**Audit trees:** Backend primary = `Nytroz POS - Backend New/Unified-Commerce`. Frontend = `nytroz-pos-platform-admin`. Second Brain = `second-brain-docs-worktree`.

---

## 2. Existing Second Brain Documents

### 2.1 Inventory (email / onboarding / billing related)

| Exact path | Section(s) | Stated behaviour | Approved/current? | Conflicts? | Matches implementation? |
|---|---|---|---|---|---|
| `01_RELEASE_SCOPE/Included_Features.md` | Platform And Tenant Setup | Includes plan assignment, Issue Invoice / Mark Paid, **Payment Links (PayHere) R1 mandatory not implemented**, ACS **platform** password reset; tenant resets out of scope | Active | vs older R2 deferral (superseded) | Partial — billing Issue/Mark Paid yes; payment links no; ACS reset yes |
| `03_USER_JOURNEYS/Platform_Admin/04_Create_Tenant_Wizard_Flow.md` | Rules L42–45 | **No email until notification infrastructure**; payment links **not** invoked; invite row persisted; activate separately | **Implemented SOT** | vs Activation / Pre-login email claims | **Matches** backend/FE “email not wired” |
| `03_USER_JOURNEYS/Platform_Admin/16_Platform_Tenant_Create_Wizard_Alignment.md` | Billing / create-options | `subscriptionStatus: trial` ≠ billing status; `sendInvite` default true | Active | sendInvite field vs no email send | Matches persistence; not send |
| `03_USER_JOURNEYS/Platform_Admin/11_Tenant_Activation_Flow.md` | Main Flow L42 | On activate: **send tenant admin invite/password setup email** and mark active | Active (deck-derived) | **Conflicts** with wizard “no email” | **Does not match** — activate has no email |
| `03_USER_JOURNEYS/Tenant_Admin/01_Pre_Login_Payment_Trial_Demo_Flow.md` | Main Flow L33–41 | Payment Now → **payment link email**; success → activate + **setup email**; Trial/Demo → **setup email only** | Active (deck-derived) | Conflicts with implemented deferred email; auto-activate after pay vs manual activate | **Does not match** |
| `03_USER_JOURNEYS/Tenant_Admin/02_First_Login_Flow.md` | L10–39 | First login after **setup email**; set password via setup token | Active | Assumes email exists | No setup-email delivery; setup tokens unused in wizard |
| `03_USER_JOURNEYS/Platform_Admin/08_Tenant_User_Management_Flow.md` | Invite steps | Invitation email when activation allows | Active / aspirational | Deferred email | Not implemented |
| `03_USER_JOURNEYS/Platform_Admin/10_Billing_Flow.md` | L12–14, L218–235 | Issue/Mark Paid release-ready; generate/resend payment link unsupported; full invite/set-password product flow still future | Active | vs Pre-login payment-link email | Matches “no payment link UI/API” |
| `03_USER_JOURNEYS/Platform_Admin/12_Subscription_Billing_Management_Flow.md` | Future work | Separates completed billing from trial/demo/payment-link work | Active | — | Accurate |
| `03_USER_JOURNEYS/Platform_Admin/13_Platform_User_Management_Flow.md` / `17_Platform_User_Password_Reset_Flow.md` | ACS email | Admin-initiated platform reset via ACS; `deliveryMode=email`, `resetUrl=null` | Active / COMPLETE | — | **Matches** |
| `15_IMPLEMENTATION_TRACKING/Backend/Auth/SA-P1-06_...Implementation.md` | Email / Delivery | ACS COMPLETE; outbox/retry out of scope; tenant reset pending | Completed | — | Matches |
| `05_BACKEND_ARCHITECTURE/API_ENDPOINTS.md` | Wizard create ~L271–280; password reset ~L603+ | Invite persisted; **email is not sent in this slice** | Active | vs Activation L42 | Matches create; matches ACS reset |
| `04_MODULE_KNOWLEDGE/06_Auth_Tokens_Security_Audit/01_Module_Overview.md` | Surfaces | Target invitations / password setup APIs | Active (architecture target) | Not fully wired | Schema exists; product APIs incomplete |
| `04_MODULE_KNOWLEDGE/26_Notification/*` | Channels | Generic EMAIL/SMS for ops alerts | Active | Not SaaS onboarding catalog | N/A for onboarding emails |
| `06_DATABASE_KNOWLEDGE/Tables/05_...Billing...UPDATED.md` | `subscription_payment_links` L214–236 | Link URL, `sent_to_email`, `sent_at`, reminders | Active | — | Entity exists; no service send |
| `06_DATABASE_KNOWLEDGE/Tables/07_Invitations...UPDATED.md` | `user_invites`, `user_setup_tokens` | Invite + setup tokens; `sent_at` / `resend_count` | Active | — | Tables/entities exist; email not sent; setup tokens unused by wizard |
| `12_INTEGRATIONS/Email_Service_Integration.md` | — | **Empty Draft** (SCS-TIX, 2026-06-08) | Draft / unusable | — | ACS lives in code/docs under Auth tracking instead |
| `12_INTEGRATIONS/Payment_Gateway_Integration.md` | — | Empty Draft | Draft | — | — |
| `15_IMPLEMENTATION_TRACKING/.../SA-P1_Payment_Links_Release_1_Scope_And_Sequencing.md` | R1 decision | Payment links **R1 mandatory**, not started | Active (supersedes R2 defer) | vs wizard “no payment links in slice” | Correct status |
| `09_ANGULAR_ADMIN_KNOWLEDGE/Tenant_Wizard_State.md` | Payment link state | Older SCS-TIX wizard model | **Stale vs** `04_Create_Tenant_Wizard_Flow` | Conflicts | Do not use as SOT |
| Cross-role `03_Tenant_Suspended_Flow.md` | Suspension | Access block; **no suspension email** | Active (older branding) | — | No suspension email |

### 2.2 Email scenario catalog in Second Brain?

**No.** There is no complete catalog covering the checklist below. Closest fragments: SA-P1-06 + Notification module (unrelated ops alerts) + empty `Email_Service_Integration.md`.

| Scenario | In Second Brain? |
|---|---|
| Platform password reset | Yes (complete) |
| Tenant subscription confirmation | No dedicated email scenario |
| Payment instructions / payment link | Partial (Pre-login + payment-links schema/sequencing) |
| Payment received acknowledgement | No email scenario (Mark Paid / future webhook) |
| Payment verification | No email (operator/webhook) |
| Tenant activation | Yes in Activation journey (conflicts with deferred) |
| Trial/demo welcome | Partial (Pre-login “setup email only”) |
| Tenant-admin set password | Yes (First Login / Activation) |
| Tenant-user invitation | Yes (aspirational) |
| Tenant-user password reset | Explicitly out of current phase |
| Password changed alert | No |
| Subscription renewal/expiry | Future / unsupported |
| Tenant suspension/deactivation | No email |

---

## 3. Paid Tenant Journey

### 3.1 User-described Flow A vs evidence

| Step | Desired | Second Brain | Implementation |
|---:|---|---|---|
| 1 | Super Admin creates tenant | Wizard create | **Yes** — `POST /api/v1/platform-admin/tenants` |
| 2 | Subscription/payment confirmation email | Pre-login implies **payment link email**, not a separate “confirmation” mail | **No email** |
| 3 | Email contains amount + payment details | Payment link / billing summary in Pre-login L35–36 | Amount available on plan/invoice in DB; **no email** |
| 4 | Tenant pays | PayHere / payment link (R1 planned) | **Not implemented** — manual Mark Paid only |
| 5 | Super Admin verifies payment | Journeys vary: Pre-login says provider validates; Billing is Mark Paid | **Manual** `MarkPaid` by Platform Admin |
| 6 | Super Admin activates tenant | Activation flow + wizard “activate separately” | **Yes** API/UI — subject to status rules (§7) |
| 7 | Tenant-activated email | Activation L42 | **No** |
| 8 | Username + set-password link | First Login / invite | Invite **row** only; **no** email; setup token **not** created by wizard |
| 9 | Set password and login | First Login | Set-password product APIs / UI for tenant admin **not** wired as described |

### 3.2 What “paid” means today

- Wizard **Billing Status** (`pending`/`paid`/`overdue`/`failed`/`waived`) and optional **Create draft invoice**.
- Settlement: Platform Admin Billing → Issue invoice → **Mark as paid** (confirm modal).
- **No** payment-link generate/send UI or API.
- Activation is a **separate** Platform Admin action when `canActivate` is true.

---

## 4. Trial/Demo Tenant Journey

### 4.1 User-described Flow B vs evidence

| Step | Desired | Second Brain | Implementation |
|---:|---|---|---|
| 1 | Create as Trial or Demo | Pre-login branches Trial/Demo; plans may use `billingCycle` trial/demo | Wizard: `subscriptionStatus` default/normalize to **TRIAL**; **no `IsDemo` tenant flag** |
| 2 | Create + activate without payment | Pre-login L40: setup email only (implies skip payment) | Create without invoice optional; activate still separate; payment not required by activate code (subscription required) |
| 3 | Created/activated notification | Pre-login: **one** setup email; Activation: invite on activate | **None** |
| 4–5 | Username + set-password + login | First Login | Same gap as paid |

### 4.2 Created + Activated vs combined Trial/Demo Ready email

| Evidence | Implication |
|---|---|
| Pre-login L40: Trial/Demo → **“setup email only”** | Suggests **one** email, not separate Created + Activated |
| Activation L42: always **send invite on activate** | Suggests activation-time email for all modes |
| Wizard L44: **no email** until infra exists | Current product slice: **zero** emails |
| No document titled “Trial/Demo Ready” combined template | **Unresolved product decision** |

**Audit conclusion:** Do **not** silently pick two emails vs one. Require explicit approval (§15 Q12).

---

## 5. Existing ACS Email Architecture

| Component | Path | Notes |
|---|---|---|
| Abstraction | `src/E_POS.Application/Common/Email/IApplicationEmailSender.cs` | Provider-neutral |
| Message/result | `ApplicationEmailMessage.cs`, `ApplicationEmailSendResult.cs` | |
| Composer | `.../PlatformAdmin/Email/PlatformPasswordResetEmailComposer.cs` | **Only** composer |
| Options + validator | `src/E_POS.Infrastructure/Integrations/Email/AzureCommunicationEmailOptions*.cs` | |
| Sender | `AzureCommunicationEmailSender.cs` | Bare MailFrom; `WaitUntil.Started`; no display-name in `senderAddress` |
| Delivery | `PlatformPasswordResetDelivery.cs` (`AcsPlatformPasswordResetDeliveryService`) | `deliveryMode=email` / Dev `admin_secure_link` |
| DI | `Infrastructure/DependencyInjection.cs` (~L104–109 / ACS registration) | |
| Config placeholders | `appsettings.json` / `appsettings.Development.json` `AzureCommunicationEmail:*` | Empty secrets |
| Package | `E_POS.Infrastructure.csproj` — `Azure.Communication.Email` | |
| Tests | Unit ACS sender/options/delivery/email-flow; Integration/API PlatformPasswordReset* | |
| Tracking | `projects/12_IMPLEMENTATION_TRACKING/Backend/Authentication/ACS_*` | COMPLETE + E2E PASSED |

**Not present:** tenant composers, email outbox table, background worker, retry, resend endpoints for onboarding, idempotent email event store.

---

## 6. Email Scenario Matrix

| Event code | Business trigger | Recipient | Required data | Template | Current status | Source |
|---|---|---|---|---|---|---|
| `PLATFORM_PASSWORD_RESET` | Platform Admin Send Password Reset | Platform user email | Reset URL (email body only), display name | ACS HTML/text composer | **Implemented** | SA-P1-06; `17_...Reset_Flow`; code |
| `TENANT_SUBSCRIPTION_CONFIRMATION` | Tenant created (paid path) | Tenant Admin / invoice email | Plan name, amount, currency, period, due date | TBD | **Product decision required** / Documented but missing | User Flow A; not in SB as named email |
| `TENANT_PAYMENT_LINK` | Paid create or Issue Invoice | Tenant Admin | Payment URL, amount, invoice #, expiry | TBD | **Documented but missing** | Pre-login L35; `subscription_payment_links` |
| `TENANT_PAYMENT_RECEIVED` | Mark Paid / webhook success | Tenant Admin (optional) | Amount, receipt refs | TBD | **Future** / decision required | Not specified as email |
| `TENANT_PAYMENT_VERIFICATION` | Operator/provider verify | Internal / admin | Payment refs | N/A or internal | **Not applicable** as customer email | Manual Mark Paid today |
| `TENANT_ACTIVATED` | Platform Admin Activate | Tenant Admin | Tenant name, login URL, optional setup link | TBD | **Documented but missing** | Activation L42 |
| `TENANT_TRIAL_DEMO_READY` | Trial/Demo create and/or activate | Tenant Admin | Username/email, setup link | TBD | **Product decision required** | Pre-login L40 vs Activation L42 |
| `TENANT_ADMIN_SET_PASSWORD` | Invite/setup after create or activate | Tenant Admin | Email as username?, setup token URL | TBD | **Partially implemented** (invite row only) | Wizard L44; First Login |
| `TENANT_USER_INVITATION` | Add tenant user | Tenant user | Invite URL | TBD | **Documented but missing** | Platform `08_`, Tenant Admin `07_` |
| `TENANT_USER_PASSWORD_RESET` | Tenant user reset | Tenant user | Reset URL | TBD | **Future** / out of current phase | SA-P1-06 scope; Included_Features |
| `PASSWORD_CHANGED_ALERT` | Password changed | Account holder | Timestamp, channel | TBD | **Future** | Not documented |
| `SUBSCRIPTION_RENEWAL_REMINDER` | Upcoming renewal | Tenant Admin | Plan, amount, date | TBD | **Future** | Billing unsupported list |
| `SUBSCRIPTION_EXPIRY_WARNING` | Near expiry / past due | Tenant Admin | Status, grace | TBD | **Future** | — |
| `TENANT_SUSPENDED` | Suspend | Tenant Admin | Reason, contact | TBD | **Not applicable** today | Suspend flow has no email |
| `TENANT_DEACTIVATED` | Deactivate | Tenant Admin | — | TBD | **Future** | — |
| `PAYMENT_LINK_REMINDER` | Reminder job | Tenant Admin | Link, count | TBD | **Future** | Schema `reminder_count` / `last_reminder_at` |

---

## 7. Backend Findings

### 7.1 Tenant create

- Service: `PlatformTenantService.Wizard.cs` — wizard path creates tenant, subscription, entitlements, pending admin, `UserInvite`, optional draft invoice.
- Status on create: **`billingStatus` passed into `Tenant.Create`** (e.g. L91–96, L200–205) — values from `TenantBillingStatusConstants` (`pending`/`paid`/…).
- Subscription status: `NormalizeSubscriptionStatus` defaults to **`TRIAL`** (L719–735).
- Invite: `UserInvite.CreatePending` (L335–344) with **mock token hash** `Guid.NewGuid().ToString("N")` (not HMAC).
- `SendInvite=false` rejected (L308–314); temporary password deferred.
- **No** `IApplicationEmailSender` call on create.

### 7.2 Activation

- `PlatformTenantService.ActivateTenantAsync` L226–268: permission `TenantsActivate`; `TenantLifecycleRules.CanActivate`; requires display name/code + subscription; `tenant.Activate` + `subscription.Activate`.
- Activatable statuses (`TenantLifecycleRules.cs` L4–11): `setup_pending`, `pending_payment`, `pending_activation`, `inactive`, `draft`.
- **Gap:** create stores billing values in `status` → typically **not** activatable without a status fix path.
- **No** email on activate; **no** dedicated platform business audit event (entity `ActivatedAt` / `UpdatedByPlatformUserId` only).

### 7.3 Trial / demo

- Trial = subscription status `TRIAL` + optional trial date fields on `TenantSubscription`.
- Plan `TrialDays` exists; not auto-applied as a full product policy in create.
- **No demo flag** on tenant; “demo” appears as plan `billingCycle` option in FE plan UI / older docs.

### 7.4 Payment amount / link / verification

- Amount/currency/period: from plan onto subscription; draft invoice lines when created (`Wizard.cs` ~L346–367).
- Payment link entity: `SubscriptionPaymentLink.cs` (URL, hash, `SentToEmail`, `SentAt`, reminders) — **no Application service/API** to create or email.
- Verification: **manual** `PlatformBillingService.MarkPaidAsync` / `POST .../mark-paid`. No PayHere webhook verifier in Application for links.

### 7.5 Tenant-admin credentials / setup token

- Pending `TenantUser` + `user_invites` row.
- `UserSetupToken` entity exists (`UserSetupToken.cs`) — **unused by wizard onboarding**.
- Username for login: email-based tenant user identity (admin email); no separate username field in wizard.

### 7.6 Email emission / failure / duplicates

- Only platform password reset emits email.
- Sync send; failure on reset → delivery failure / HTTP 502 mapping; **does not** roll back token creation semantics beyond delivery service design (existing reset flow).
- Tenant create/activate **unaffected** by email (none sent).
- Duplicate prevention: none for onboarding emails; invite has `resend_count` columns but no resend API/email.

### 7.7 Outbox / workers

- No email outbox migration/table; no `IHostedService` email worker under `src`.
- ACS Phase 1 docs list outbox/retry as remaining work.

---

## 8. Frontend Findings

**App:** `nytroz-pos-platform-admin`

| Area | Finding | Path / lines (approx.) |
|---|---|---|
| Wizard | 7 steps: business → plan → limits → features → tenant admin → billing → review | `platform-create-tenant-page.ts` |
| Trial/Demo | No dedicated branch; billing/subscription dropdowns + plan billing cycles | Billing step ~253–300; hint L247–249 |
| Invite email | Explicit: *“Email delivery is not wired in this release.”* | L247–249 |
| `sendInvite` | Always `true` in mapper | `platform-tenant-create.mapper.ts` |
| Payment link UI | **Absent** | repo search empty |
| Payment verify | **Mark as paid** only on Billing invoice detail | `platform-billing-invoice-detail.ts` |
| Activation | Detail **Activate Tenant** when `canActivate` | `platform-tenant-detail-page.ts` ~53–56, 913–923 |
| Create success | Navigate to detail; **no** success toast | create ~582–584 |
| Activate success | `"Tenant activated successfully."` | detail ~1100 |
| Platform reset email UX | Secure-link UI still present; email mode hides copy when `resetUrl` null | `platform-users-page.ts` |

---

## 9. Database Findings

| Need | Exists? | Where |
|---|---|---|
| Plan name / amount / currency | Yes | `subscription_plans`, copied to `tenant_subscriptions` / invoices |
| Billing period / cycle | Yes | subscription + invoice period fields |
| Due date | Yes on invoices (schema) | `subscription_invoices` |
| Payment link | Yes table/entity | `subscription_payment_links` |
| Payment status | Yes | invoice / transaction / link status fields |
| Payment verification actor/date | Partial | Mark Paid updates invoice; link `UsedAt`; platform user on Mark Paid path |
| Activation actor/date | Partial | `ActivatedAt`, `UpdatedByPlatformUserId` on tenant |
| Trial/demo flag | Trial dates/status yes; **demo flag no** | `tenant_subscriptions` |
| Trial start/end | Yes (fields) | Domain + DB docs |
| Tenant-admin email | Yes | `tenant_users` / `user_invites.invited_email` |
| Separate username | No dedicated field | Email identity |
| Password setup token | Table/entity **yes**; wizard **does not write** | `user_setup_tokens` |
| Invite send/resend metadata | Yes | `sent_at`, `last_sent_at`, `resend_count` |
| Email delivery status / outbox | **No** | — |
| Business email audit events | **No** dedicated email event table | Platform login audits for password reset only |

**No migration generated by this audit.**

---

## 10. Second Brain vs Implementation Conflicts

1. **Email on create/activate:** Activation L42 + Pre-login L35–40 vs Wizard L44 + API_ENDPOINTS L280 + FE hint L247–249.
2. **Payment Now auto-activate after pay:** Pre-login L38 vs implemented **manual** Activate + Mark Paid.
3. **Payment link R1:** Sequencing (mandatory) vs wizard “not invoked in this slice” (current code true).
4. **Tenant status model:** Lifecycle constants vs billing values written into `tenants.status` on create.
5. **Invite/setup token security:** DB requires hashed tokens; wizard uses mock Guid string as hash (L342).
6. **Empty** `Email_Service_Integration.md` vs real ACS docs under Auth tracking.
7. **Stale** `Tenant_Wizard_State.md` vs current 7-step wizard.

---

## 11. Missing Product Decisions

1. Canonical paid email sequence (confirmation vs payment-link vs activation vs setup).
2. Trial/Demo: **one Ready email** vs **Created + Activated**.
3. Whether activation email **includes** set-password link or setup is a **second** email.
4. Username = email only, or separate username?
5. Auto-activate after successful payment vs always manual Super Admin activate.
6. Who is payment verifier (Platform Admin Mark Paid vs PayHere webhook as source of truth).
7. Failure policy: block create/activate on email failure vs async outbox.
8. Resend permissions and rate limits per event.
9. Idempotency key strategy per (tenant, event, recipient).
10. Which events are hard R1 vs deferred (payment link R1 already decided; onboarding emails not).
11. Demo representation (flag vs plan cycle vs subscription status).
12. Trial duration source of truth (`TrialDays` vs request dates vs default).

---

## 12. Recommended Canonical Documents

Create/approve (future work — **not done in this audit**):

1. `03_USER_JOURNEYS/.../Email_Notification_Scenario_Catalog.md` — matrix from §6 as SOT.
2. Update `04_Create_Tenant_Wizard_Flow` + `11_Tenant_Activation_Flow` + `01_Pre_Login_...` to one consistent journey.
3. Replace empty `12_INTEGRATIONS/Email_Service_Integration.md` with ACS config + event list (no secrets).
4. Payment Links journey binding Issue Invoice → link email → webhook/Mark Paid → activate.
5. Tenant Admin Invite / Set-Password technical contract (token store, URL builder, resend).

---

## 13. Files Requiring Future Updates

**Second Brain:** journeys in §2.1; `API_ENDPOINTS.md`; `Included_Features.md`; Auth module overview; billing functional specs; Integrations Email draft.

**Backend:** tenant wizard status mapping; activate prerequisites; invite token hashing; optional `UserSetupToken` usage; new composers + delivery services; optional outbox; payment-link Application/API; audit events.

**Frontend:** wizard branching/copy for paid vs trial/demo; payment-link UI; email-sent toasts; invite/setup status; align password-reset email-mode messaging.

**Tracking:** this file under `projects/12_IMPLEMENTATION_TRACKING/Backend/Email/`.

---

## 14. Recommended Implementation Sequence

1. **Approve** email scenario catalog + trial/demo combined-vs-split decision (§15).
2. Fix **tenant status / canActivate** mismatch (blocks Flow A/B regardless of email).
3. Harden invite token hashing; decide setup-token vs invite-token for set-password URL.
4. Extend ACS with **composers** for approved R1 onboarding events (start with tenant-admin set-password / trial-ready).
5. Implement **Payment Links** (SA-P1 sequencing) including optional payment-link email.
6. Wire activate → email only after product decides timing.
7. Add **outbox/retry/resend** if delivery must not fail create/activate.
8. Align Second Brain journeys to implemented behaviour.
9. Defer tenant-user reset, renewal, suspension emails unless R1 scope expands.

---

## 15. Questions Requiring Approval

Answers from **evidence**; open items marked **UNRESOLVED**.

1. **When is a paid tenant considered created?**  
   After successful wizard `POST .../tenants` transaction (tenant + subscription + invite row). **Not** after email.

2. **When is it considered active?**  
   After Platform Admin `POST .../activate` when `CanActivate` and subscription exists. Pre-login’s auto-activate-after-pay is **not** implemented.

3. **Who verifies payment?**  
   **Today:** Platform Admin via **Mark Paid**. **Target:** PayHere webhook (planned). **UNRESOLVED** which is authoritative for R1 go-live.

4. **Manual or automated verification?**  
   **Manual** today. Automated planned, not built.

5. **Which email is sent at creation?**  
   **None** implemented. Target docs suggest payment-link email (paid) or setup email (trial/demo). **UNRESOLVED** whether a separate subscription-confirmation email exists.

6. **Which email after payment?**  
   **None** implemented. Pre-login suggests setup email after success (and activate). **UNRESOLVED**.

7. **Which email after activation?**  
   **None** implemented. Activation journey requires invite/setup email. **UNRESOLVED** relative to trial combined email.

8. **Does activation email include set-password link?**  
   Activation L42 implies yes (invite/password setup). First Login treats setup email as the vehicle. **UNRESOLVED** if link is only on activation or also at create.

9. **Is username the recipient email or separate?**  
   Wizard uses **email** as tenant admin identity. No separate username field. Treat as **email = username** unless product adds a field.

10. **Does trial/demo skip payment records?**  
    **Typically yes** in UI (draft invoice optional/unchecked). Backend auto-creates draft when billing status is `pending`. Demo not first-class. **UNRESOLVED** demo rules.

11. **What is the trial duration?**  
    Plan `TrialDays` + subscription trial date fields exist; wizard normalize defaults status to TRIAL. **Exact default duration policy UNRESOLVED**.

12. **Are created and activated emails combined for trial/demo?**  
    **PRODUCT DECISION REQUIRED.** Pre-login suggests **one** setup email; Activation suggests activate-time invite; neither implemented.

13. **What happens when email delivery fails?**  
    Platform reset: delivery failure surfaced (e.g. 502). Tenant create/activate: N/A (no send). Future: **UNRESOLVED** (sync fail vs outbox).

14. **Can an admin resend each email?**  
    Platform reset: re-initiate creates new token. Onboarding: invite has `resend_count` columns but **no** resend UI/API. **UNRESOLVED**.

15. **How are duplicate sends prevented?**  
    Platform reset: prior pending tokens revoked. Onboarding emails: **none**. Payment link entity supports statuses but unused.

16. **Which audit events are required?**  
    Platform reset: login-audit methods exist. Tenant create/activate/payment emails: **not defined**. Business audit still limited in R1 (`14_Audit_Logs_Flow`). **UNRESOLVED** for onboarding email events.

17. **R1 vs later?**  
    | Likely R1 (if approved) | Later |
    |---|---|
    | Platform password reset (**done**) | Tenant-user password reset |
    | Payment links + pay flow (sequencing: R1 mandatory) | Renewal/expiry emails |
    | Tenant-admin setup/invite email (journeys) | Suspension emails |
    | Trial/demo access without payment | Password-changed alerts |
    | Outbox/retry | Production custom domain |

---

## Appendix A — Decision: Flow A / Flow B vs code

| Flow | Email steps claimed | Code today |
|---|---|---|
| A Paid | Confirmation → (pay) → verify → activate → activated+setup | Create + optional invoice + Mark Paid + Activate; **0 emails** |
| B Trial/Demo | Created/activated notification + setup link | Create (often TRIAL) + Activate; **0 emails**; demo not modeled |

## Appendix B — Key line references (quick)

| Claim | Reference |
|---|---|
| No wizard email | `04_Create_Tenant_Wizard_Flow.md` L44; `API_ENDPOINTS.md` L280; FE create page L247–249 |
| Activation sends invite email | `11_Tenant_Activation_Flow.md` L42 |
| Trial/Demo setup email only | `01_Pre_Login_Payment_Trial_Demo_Flow.md` L40 |
| Payment link email | Same, L35 |
| Billing status into tenant status | `PlatformTenantService.Wizard.cs` L200–205 |
| CanActivate set | `TenantLifecycleRules.cs` L4–22 |
| Invite mock hash | `PlatformTenantService.Wizard.cs` L335–344 |
| ACS only password reset composer | `PlatformPasswordResetEmailComposer.cs` |
| Payment link schema | `05_...Billing...UPDATED.md` L214–236; `SubscriptionPaymentLink.cs` |

---

*End of audit. No application code, migrations, packages, configuration, or Second Brain documents were modified except creation of this tracking report.*
