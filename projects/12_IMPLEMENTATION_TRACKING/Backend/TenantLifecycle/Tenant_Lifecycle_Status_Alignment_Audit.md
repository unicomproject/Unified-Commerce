# Tenant Lifecycle Status Alignment Audit

**Date:** 2026-07-27
**Worktrees audited:**

- Backend: `C:\Users\User\Desktop\Nytroz__POS\Unified-Commerce-tenant-lifecycle` (`22235ed`)
- Frontend: `C:\Users\User\Desktop\Nytroz__POS\Nytroz-POS-Platform-Admin-tenant-lifecycle` (`6274607`)
- Second Brain: `C:\Users\User\Desktop\Nytroz__POS\Pos-system-Knowledge-tenant-lifecycle` (`302b87b`)

---

## 1. Executive Summary

**Root cause:** Tenant creation (wizard and legacy paths) writes the **billing status** value (`pending`, `paid`, `overdue`, `failed`, `waived`) into the **`tenants.status`** column instead of a lifecycle status (`draft`, `pending_payment`, `pending_activation`, `active`, `suspended`, `cancelled`). This is documented as an approved-defect in the Second Brain canonical docs.

**Backend impact:** High — create path, activation eligibility, list/detail API, summary counts, and login gates are all affected. Activation has no payment verification gate.

**Frontend impact:** Low-Medium — the FE correctly separates `status`, `billingStatus`, and `billingCycle` into distinct fields and relies on backend `canActivate`/`canSuspend` flags. Issues are limited to unstyled status badges for non-standard lifecycle states, a hidden `pendingActivationTenants` count, and a hardcoded plan billing-cycle filter mismatch.

**Database impact:** Medium — `tenants.status` is `varchar(40)` with no CHECK constraint. Runtime-created tenants may contain billing vocabulary. A data cleanup migration and a lifecycle CHECK constraint are required.

**Migration required:** Yes — code correction + data cleanup migration + CHECK constraint migration.

**Implementation complexity:** Medium

---

## 2. Approved Second Brain Contract

### `tenants.status` — lifecycle only

| Approved value | Meaning |
|---|---|
| `DRAFT` | Created, not yet configured |
| `PENDING_PAYMENT` | Paid tenant awaiting payment |
| `PENDING_ACTIVATION` | Payment verified, awaiting activation |
| `ACTIVE` | Operational |
| `SUSPENDED` | Temporarily disabled |
| `CANCELLED` | Terminated |

Evidence: `Email_Architecture_And_Provider_Decisions.md` L74–83; `18_Tenant_Onboarding_Email_Flows.md` L23–25; `03_Technical_Contract.md` L13–18; `API_ENDPOINTS.md` L288.

### Must NOT be in `tenants.status`

`MONTHLY`, `ANNUAL`, `PAID`, `UNPAID`, `TRIAL`, `DEMO`, billing-cycle values, subscription-type values, payment-state values.

Evidence: `Email_Architecture_And_Provider_Decisions.md` L85; `18_Tenant_Onboarding_Email_Flows.md` L27–28; `04_Create_Tenant_Wizard_Flow.md` L44; `16_Platform_Tenant_Create_Wizard_Alignment.md` L14.

### Correct homes for non-lifecycle concerns

| Concern | Correct home |
|---|---|
| Subscription type (PAID/TRIAL/DEMO) | Create mode / plan model; no `tenants.status` |
| Billing cycle (monthly/yearly) | `subscription_plans.billing_cycle`, `tenant_subscriptions.billing_cycle` |
| Subscription status | `tenant_subscriptions.subscription_status` (CHECK: `TRIAL\|ACTIVE\|PAST_DUE\|CANCELLED\|EXPIRED`) |
| Billing/payment status | `billingStatus` request field; `subscription_invoices.invoice_status`; payment link/txn status columns |

### Approved flows

| Scenario | Status after create | Activation |
|---|---|---|
| **Paid** | `PENDING_PAYMENT` | Manual verify payment → manual activate → `ACTIVE` → set-password email |
| **Trial** | Auto-activate after provisioning | `ACTIVE` → created email + separate activated/set-password email |
| **Demo** | Auto-activate after provisioning | Same as Trial |

### Implementation defect (approved canonical finding)

> Current code may write billing values into `tenants.status` and block `CanActivate`. That is a **defect** requiring an approved fix.

— `18_Tenant_Onboarding_Email_Flows.md` L29–31; `Email_Architecture_And_Provider_Decisions.md` L96–100; `API_ENDPOINTS.md` L288–290.

---

## 3. Current Backend Behaviour

### Tenant entity

- `Tenant.Status` is `string` — not an enum or value object.
- File: `src/E_POS.Domain/Modules/Tenant/TenantFoundation/Entities/Tenant.cs:11`

### Status constants (code)

| Constant | Value |
|---|---|
| `Draft` | `draft` |
| `SetupPending` | `setup_pending` |
| `PendingActivation` | `pending_activation` |
| `PendingPayment` | `pending_payment` |
| `Inactive` | `inactive` |
| `Active` | `active` |
| `Suspended` | `suspended` |

File: `src/E_POS.Domain/Modules/Tenant/TenantFoundation/Constants/TenantStatusConstants.cs:3–12`

**Note:** `CANCELLED` is approved but missing from `TenantStatusConstants`. `setup_pending` and `inactive` are code-only values not in the approved lifecycle set.

### Billing status constants (separate, but misused)

| Constant | Value |
|---|---|
| `Pending` | `pending` |
| `Paid` | `paid` |
| `Overdue` | `overdue` |
| `Failed` | `failed` |
| `Waived` | `waived` |

File: `src/E_POS.Domain/.../Constants/TenantBillingStatusConstants.cs:5–9`

### Create defect

**Wizard create** (`PlatformTenantService.Wizard.cs:205`):

```
Tenant.Create(..., billingStatus, ...)
```

Billing status is normalized from `request.BillingStatus` (default `"pending"` when omitted) and passed directly as the tenant lifecycle status.

**Legacy create** (`PlatformTenantService.Wizard.cs:520`): same pattern.

**Consequence:** Wizard-created tenants get `tenants.status = 'pending'`, `'paid'`, etc. — none of which are valid lifecycle values. `CanActivate` rejects these, making paid tenants un-activatable through the approved path.

### Activation eligibility

`TenantLifecycleRules.cs:4–12`:

Activatable set: `{setup_pending, pending_payment, pending_activation, inactive, draft}`

**`ActivateTenantAsync`** (`PlatformTenantService.cs:225–267`):

1. Checks `CanActivate(tenant.Status)` — correct lifecycle gate
2. Checks subscription exists — correct
3. **Does NOT verify payment** — missing for paid tenants
4. Does NOT check if already `ACTIVE` — `CanActivate(active)` returns false (correct)
5. Does NOT clear `SuspendedAt` when re-activating from suspended (but `CanActivate(suspended)` is false anyway)

### List/detail API exposure

- `PlatformTenantRepository.cs:230`: `BillingStatus = currentSubscription?.SubscriptionStatus ?? "UNKNOWN"` — subscription status masquerading as billing status.
- `PlatformTenantRepository.cs:583`: same for list items.
- `PlatformTenantRepository.cs:72–77`: filter "billingStatuses" = distinct subscription statuses.

### Summary counts

`PlatformTenantRepository.cs:55–56`: `PendingActivationTenants` counts `setup_pending || pending_payment`. Misses `pending_activation`, `draft`, and the actual billing values from create.

### Login gate

`TenantAuthConstants.cs:16–19`: Tenant login allowed only when status is `active` or `setup_pending`. Any billing value in status blocks tenant user login.

### Trial/Demo

- Trial: `TenantSubscriptionStatusConstants.Trial` exists; subscription created with status `TRIAL` and `billing_cycle = monthly` by default.
- Demo: **No** demo lifecycle concept in code.
- Auto-activation on Trial/Demo create: **Not implemented**.
- Trial detail: `TrialEndsAt` hard-coded to `null` in detail mapper (`PlatformTenantRepository.cs:221`).

### Cancel tenant

**Not implemented.** No `CancelTenantAsync`, no `CANCELLED` status constant, no cancel API endpoint.

---

## 4. Current Frontend Behaviour

### Field separation — correct

The Angular create wizard sends three independent fields:

| Field | Form control | Sent as |
|---|---|---|
| `billingStatus` | `billingSubscriptionForm.controls.billingStatus` | `request.billingStatus` |
| `billingCycle` | `billingSubscriptionForm.controls.billingCycle` | `request.subscription.billingCycle` |
| `subscriptionStatus` | `billingSubscriptionForm.controls.subscriptionStatus` | `request.subscription.subscriptionStatus` |

Files: `platform-tenant-create.mapper.ts:164,184,185`; `platform-create-tenant-page.ts:258–284`

The FE does **not** send billing-cycle or subscription-type values through the `status` field.

### Activation/suspension

Governed entirely by backend `canActivate`/`canSuspend` booleans plus client-side `tenantsActivate`/`tenantsSuspend` permissions. Correct.

Files: `platform-tenant-detail-page.ts:913–927,1082–1086`

### Status badges

Status badge renders raw `status` string with a CSS class from `status.toLowerCase()`. Only `active`, `suspended`, and `trial` have styling. All other statuses (`draft`, `pending_payment`, `pending_activation`, `pending`, `paid`, etc.) render **unstyled**.

Files: `platform-tenant-list-page.ts:179,814–816,552–553`; `platform-tenant-detail-page.ts:502–503`

### Summary KPIs

`pendingActivationTenants` is folded into a derived `inactiveTenants` count and hidden from the admin. No dedicated pending-activation KPI card.

File: `platform-tenant.mapper.ts:133–144`

### Plan billing-cycle filter

Hardcoded filter includes `monthly`, `annual`, `both`. Backend uses `yearly` not `annual`; `both` has no backend equivalent.

File: `platform-subscription-plans-page.ts:93–98`

### `trial`/`demo` as billing-cycle

`SubscriptionDbBillingCycle` type includes `trial` and `demo` as plan-level billing cycle values (not tenant status). Handled by normalizer.

File: `platform-subscription-plan.model.ts:3`; `platform-create-subscription-plan-page.ts:640–646`

---

## 5. Database and Migration Findings

### `tenants.status` column

| Attribute | Value |
|---|---|
| Type | `varchar(40)` NOT NULL |
| CHECK constraint | **None** |
| Default | None (entity sets empty string) |

File: `TenantConfiguration.cs:49–53`; snapshot ~L22378–22421

### Historical context

- `tenants.billing_status` column existed in `InitialCreate`.
- Dropped in migration `20260707185919_UpdateTenantAuthAndFoundationEntities.cs:242–244`.
- After that, create code collapsed billing status into `tenants.status`.

### Subscription status constraint

`tenant_subscriptions.subscription_status` has CHECK: `TRIAL|ACTIVE|PAST_DUE|CANCELLED|EXPIRED`
File: `TenantSubscriptionConfiguration.cs:82–84`

### Seeds

Only `'active'` written to `tenants.status` in seeds. No billing vocabulary in seed data.

### Fix requirements

| Fix type | Required? |
|---|---|
| Code-only correction | Yes — create flow, activation gate, API exposure |
| Enum/value-object correction | Recommended — replace `string` with enum or value object |
| CHECK constraint migration | Yes — add `ck_tenants_status` |
| Data cleanup migration | Yes — fix any runtime-created rows with billing values |
| Seed correction | No — seeds are clean |
| API DTO correction | Yes — rename/separate `BillingStatus` in list/detail responses |

### Field-to-concern matrix

| Business concern | Correct table | Correct field | Current implementation |
|---|---|---|---|
| Tenant lifecycle | `tenants` | `status` | **Polluted with billing values by create** |
| Subscription type | Plan/create-mode | N/A | No dedicated column (correct — journey vocabulary) |
| Billing cycle | `tenant_subscriptions` | `billing_cycle` | Correct (not in `tenants.status`) |
| Subscription status | `tenant_subscriptions` | `subscription_status` | Correct in DB; **mislabelled as `BillingStatus` in API** |
| Payment status | `subscription_invoices` | `invoice_status` | Correct in DB |
| Trial start/expiry | `tenant_subscriptions` | `trial_started_at`/`trial_ends_at` | Duplicated columns; detail hard-codes `null` |
| Activation date | `tenants` | `activated_at` | Exists; no dedicated `activated_by` |
| Suspension date | `tenants` | `suspended_at` | Exists; not cleared on re-activation |
| Cancellation date | `tenant_subscriptions` | `cancelled_at`/`ended_at` | Subscription-level only; no tenant-level cancel |

---

## 6. API Contract Findings

### Create tenant

| Attribute | Current | Approved | Change |
|---|---|---|---|
| Route | `POST /api/v1/platform-admin/tenants` | Same | None |
| `billingStatus` field | Mapped into `tenants.status` | Must NOT map into lifecycle status | **Breaking internally** — lifecycle must derive from create mode |
| Response `status` | Returns billing value | Must return lifecycle value | Fix create logic |
| Compatibility | Frontend sends `billingStatus` correctly as separate field | Backend misuses it | Backend-only fix |

### Get/List tenants

| Attribute | Current | Approved | Change |
|---|---|---|---|
| `status` | Correct lifecycle string (from DB) | Same | No change |
| `billingStatus` | Returns `subscriptionStatus` or `"UNKNOWN"` | Should return actual billing/invoice status | Rename or re-source |
| Filter `billingStatuses` | Distinct subscription statuses | Actual billing statuses | Re-source from invoices |
| Compatibility | Frontend reads `billingStatus` for display | Will need UI label/logic update | Frontend-breaking |

### Activate tenant

| Attribute | Current | Approved | Change |
|---|---|---|---|
| Route | `POST /{tenantId}/activate` | Same | None |
| Payment gate | **Missing** | Required for paid tenants | Add payment verification |
| Auto-activate for Trial/Demo | **Missing** | Required | Add post-create auto-activate |
| Compatibility | N/A (new behaviour) | N/A | Additive |

### Suspend tenant

| Attribute | Current | Approved | Change |
|---|---|---|---|
| Route | `POST /{tenantId}/suspend` | Same | None |
| `CanSuspend` allows `TRIAL` + non-active | Overgenerous | Should require `ACTIVE` | Tighten rule |

### Cancel tenant

Not implemented. Approved as a lifecycle state but no API endpoint exists.

---

## 7. Paid Tenant Lifecycle Findings

| Step | Approved | Current | Gap |
|---|---|---|---|
| Create paid tenant | `tenants.status = PENDING_PAYMENT` | `tenants.status = 'pending'` (billing) | **Defect** |
| Send `tenant.paid_created` email | Required | Not implemented | Expected (known) |
| Manual payment verification | Required before activation | Not checked | **Defect** |
| Manual activation | Sets `ACTIVE` | Sets `active` but unreachable from billing `pending` | **Defect chain** |
| Send activated/set-password email | Required | Not implemented | Expected (known) |

---

## 8. Trial/Demo Lifecycle Findings

| Step | Approved | Current | Gap |
|---|---|---|---|
| Create Trial tenant | Skip payment; status → auto-activate to `ACTIVE` | Subscription `TRIAL`; tenant status = billing; no auto-activate | **Defect** |
| Send `tenant.trial_created` email | Required | Not implemented | Expected (known) |
| Auto-activate after provisioning | Required | Not implemented | **Defect** |
| Send `tenant.trial_activated` email | Required | Not implemented | Expected (known) |
| Demo lifecycle | Same as Trial | Demo not modeled at all | **Gap** |

---

## 9. Activation Eligibility Findings

| Prerequisite | Approved | Implemented | Gap |
|---|---|---|---|
| Valid tenant profile | Yes | Yes (DisplayName + Code) | None |
| Subscription assigned | Yes | Yes | None |
| Payment verified (paid only) | Yes | **No** | **Defect** |
| Payment skipped (trial/demo) | Yes | N/A (no auto-activate) | **Defect** |
| Set-password only after ACTIVE | Yes | No email system | Expected (known) |

---

## 10. Status Mapping Defects

| # | Defect | Layer | File / Evidence | Approved behaviour | Required correction |
|---|---|---|---|---|---|
| 1 | Billing status written as lifecycle status | Application | `PlatformTenantService.Wizard.cs:205,520` | Create must set lifecycle status (`PENDING_PAYMENT` or auto-activate) | Map create-mode to lifecycle, not billing |
| 2 | `NormalizeBillingStatus` default `pending` goes into lifecycle | Application | `PlatformTenantService.cs:500–507` → `Wizard.cs:91` | N/A for lifecycle | Remove billing→lifecycle path |
| 3 | Subscription status labelled `BillingStatus` in API | Infrastructure | `PlatformTenantRepository.cs:230,583` | Separate billing from subscription status | Rename field or re-source from invoices |
| 4 | Filter `billingStatuses` returns subscription statuses | Infrastructure | `PlatformTenantRepository.cs:72–77` | Billing filter should source from invoice/payment | Re-source |
| 5 | `PendingActivationTenants` count misses lifecycle states | Infrastructure | `PlatformTenantRepository.cs:55–56` | Count all non-active, non-suspended, non-cancelled | Use lifecycle constant set |
| 6 | `CanActivate` allows `pending_payment` without payment gate | Domain | `TenantLifecycleRules.cs:8` + `PlatformTenantService.cs:241` | Must verify payment for paid path | Add payment check before activate |
| 7 | `CanSuspend` allows TRIAL + non-active states | Domain | `TenantLifecycleRules.cs:24–27` | Suspend only from `ACTIVE` | Tighten eligibility |
| 8 | `CANCELLED` missing from constants | Domain | `TenantStatusConstants.cs` | Required lifecycle value | Add constant |
| 9 | No CHECK on `tenants.status` | Database | `TenantConfiguration.cs:49–53` | Must constrain to lifecycle set | Add migration with CHECK |
| 10 | `TrialEndsAt` hard-coded `null` in detail | Infrastructure | `PlatformTenantRepository.cs:221` | Return actual value | Map from subscription |
| 11 | FE status badges unstyled for most lifecycle states | Frontend | `platform-tenant-list-page.ts:552–553` | All lifecycle states need badge styles | Add CSS classes |
| 12 | FE `pendingActivationTenants` hidden in "Inactive" | Frontend | `platform-tenant.mapper.ts:133–144` | Separate KPI or label | Add dedicated display |
| 13 | FE plan filter sends `annual` / `both` | Frontend | `platform-subscription-plans-page.ts:93–98` | Backend uses `yearly`; `both` invalid | Use backend-driven options |

---

## 11. Data Compatibility Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Existing tenants with billing values in `status` | High | Data migration: map `pending` → `pending_payment`, `paid` → `active` (or `pending_activation`), etc. |
| Adding CHECK constraint rejects existing rows | High | Data cleanup BEFORE adding constraint |
| API `billingStatus` field rename/re-source | Medium | Frontend update required simultaneously |
| Changing create response `status` from billing to lifecycle | Low | Frontend already sends them separately |
| Login gate blocking tenants with invalid status | High | Already happening — fix restores login for affected tenants |

---

## 12. Existing Test Coverage

### Backend — reusable

| Test / fixture | File | Covers |
|---|---|---|
| `FakeLifecycleTenantRepository` | `PlatformTenantLifecycleServiceTests.cs` | Activate/suspend from valid lifecycle states |
| `FakeWizardTenantRepository` | `PlatformTenantWizardServiceTests.cs` | Wizard persistence shape |
| `PlatformTenantDetailMapperTests` | Same folder | `canActivate` / `canSuspend` flags |
| `PlatformTenantCreateRequestValidatorTests` | Same folder | Rejects `billingStatus: "trial"` |
| `PlatformTenantLifecycleRepositoryTests` | Integration tests | `draft` → `active` → `suspended` persistence |
| `SubscriptionBillingAlignmentTests` | Integration tests | Payment link / invoice / subscription columns |

### Backend — misleading

- `CreateTenantAsync_WithValidRequest_ReturnsCreatedTenant` asserts `Draft` from **fake detail response**, not from the entity actually written with billing status. Gives false confidence.
- Wizard tests never assert `LastWriteModel.Tenant.Status`.

### Frontend — covered

| Test | File | Covers |
|---|---|---|
| Wizard payload shape | `platform-create-tenant-page.spec.ts:382–418` | Separate fields sent correctly |
| Invalid billing status error | `platform-create-tenant-page.spec.ts:433–454` | Server rejects `trial` as billing status |
| Billing mapper | `platform-tenant-create.mapper.spec.ts:99–149` | Field separation |
| Activate button visibility | `platform-tenant-detail-page.spec.ts:86–98` | Backend flag honoured |
| Activate action | `platform-tenant-detail-page.spec.ts:100–121` | Click → API → success |
| KPIs | `platform-tenant-list-page.spec.ts:53–69` | Trial/Active/Total display |
| Filter reload | `platform-tenant-list-page.spec.ts:114–131` | Status change triggers reload |

### Frontend — missing

- Suspend action flow
- Status badge rendering for non-standard lifecycle states
- Plan billing-cycle filter value mapping
- Permission-denied activation
- `pendingActivationTenants` → `inactiveTenants` derivation

---

## 13. Missing Tests

| Scenario | Layer | Priority |
|---|---|---|
| Paid create → `pending_payment` lifecycle status on entity | Backend | **P0** |
| Paid cannot activate before payment verification | Backend | **P0** |
| Paid can activate after verified payment | Backend | **P0** |
| Trial create → auto-activation to `active` | Backend | **P0** |
| Demo create → auto-activation to `active` | Backend | **P0** |
| Billing cycle never writes into `tenants.status` | Backend | **P1** |
| Subscription type never writes into `tenants.status` | Backend | **P1** |
| Invalid lifecycle value rejected (API validation + DB CHECK) | Backend | **P1** |
| `ACTIVE` tenant cannot be reactivated | Backend | Present ✓ |
| `SUSPENDED` → re-activation rules | Backend | **P1** |
| `CANCELLED` lifecycle rules | Backend | **P1** |
| Frontend request DTO sends separate fields | Frontend | Present ✓ |
| Frontend status badge uses lifecycle status only | Frontend | **P2** |
| Status badge styling for all lifecycle states | Frontend | **P2** |
| Suspend action component test | Frontend | **P2** |

---

## 14. Files Requiring Future Changes

### Domain

| File | Change |
|---|---|
| `TenantStatusConstants.cs` | Add `CANCELLED`; consider removing `setup_pending`/`inactive` or mapping to approved set |
| `TenantLifecycleRules.cs` | Add payment-verification gate to `CanActivate` for paid; tighten `CanSuspend` to `ACTIVE` only |
| `Tenant.cs` | Consider replacing `string Status` with an enum or value object |
| `TenantBillingStatusConstants.cs` | Keep, but ensure it is never used for `Tenant.Status` |

### Application

| File | Change |
|---|---|
| `PlatformTenantService.Wizard.cs` | Map create-mode to lifecycle status (not billing); add auto-activate for Trial/Demo |
| `PlatformTenantService.cs` | Add payment-verification check in `ActivateTenantAsync` |

### Infrastructure

| File | Change |
|---|---|
| `PlatformTenantRepository.cs` | Fix `BillingStatus` sourcing; fix `PendingActivationTenants` count; map `TrialEndsAt` |
| `TenantConfiguration.cs` | Add `HasCheckConstraint` for lifecycle values |

### API

| File | Change |
|---|---|
| `PlatformTenantDetailResponse.cs` | Rename/clarify `BillingStatus` |
| `PlatformTenantListItemDto.cs` | Same |
| `PlatformAdminTenantsController.cs` | Add cancel endpoint (future) |

### Database / Migrations

| File | Change |
|---|---|
| New migration | Data cleanup: map billing values → lifecycle values |
| New migration | Add `ck_tenants_status` CHECK constraint |

### Frontend

| File | Change |
|---|---|
| `platform-tenant-list-page.ts` | Add CSS classes for all lifecycle status badges |
| `platform-tenant.mapper.ts` | Surface `pendingActivationTenants` as dedicated KPI |
| `platform-subscription-plans-page.ts` | Make billing-cycle filter backend-driven; fix `annual`→`yearly` |
| `platform-tenant-detail-page.ts` | Style lifecycle status badge |

### Tests

| File | Change |
|---|---|
| `PlatformTenantWizardServiceTests.cs` | Assert `Tenant.Status` is lifecycle value |
| `PlatformTenantLifecycleServiceTests.cs` | Add payment-verification tests; add Trial/Demo auto-activate |
| New test file | Invalid lifecycle value rejection (API + DB) |
| `platform-tenant-list-page.spec.ts` | Badge rendering, suspend action |
| `platform-tenant-detail-page.spec.ts` | Suspend action, permission denial |

### Second Brain Status Updates

| Document | Update needed |
|---|---|
| `04_Create_Tenant_Wizard_Flow.md` | Implementation status after fix |
| `11_Tenant_Activation_Flow.md` | Implementation status after fix |
| `18_Tenant_Onboarding_Email_Flows.md` | Implementation status after fix |
| `16_Platform_Tenant_Create_Wizard_Alignment.md` | Implementation status after fix |
| `Included_Features.md` | Mark defect as fixed |
| `API_ENDPOINTS.md` | Update defect note |
| New tracking file: `Tenant_Lifecycle_Status_Alignment_Implementation.md` | Record migration, files changed, test evidence |

---

## 15. Recommended Implementation Sequence

1. **Domain:** Add `CANCELLED` to `TenantStatusConstants`; add lifecycle enum or sealed value object (optional but recommended).
2. **Application — Create:** Replace `billingStatus` → `tenants.status` with lifecycle mapping: Paid → `PENDING_PAYMENT`; Trial/Demo → auto-activate to `ACTIVE`.
3. **Application — Activate:** Add payment-verification gate for paid tenants (check invoice `PAID` or manual verify flag).
4. **Domain — Rules:** Tighten `CanSuspend` to `ACTIVE` only. Ensure `CANCELLED` is terminal.
5. **Infrastructure — Repository:** Fix `BillingStatus` sourcing in list/detail. Fix `PendingActivationTenants` count. Map `TrialEndsAt`.
6. **Infrastructure — EF Config:** Add `HasCheckConstraint` for `tenants.status`.
7. **Migration — Data cleanup:** Map existing `pending`→`pending_payment`, `paid`→`active` (or `pending_activation`), `overdue`/`failed`/`waived`→`suspended` or as business decides.
8. **Migration — CHECK constraint:** Add `ck_tenants_status` AFTER data cleanup.
9. **Tests:** Add all P0 missing tests; update misleading wizard test.
10. **Frontend:** Badge styles, `pendingActivationTenants` KPI, plan filter fix.
11. **Second Brain:** Update implementation status in affected docs.

---

## 16. Decisions Already Approved

All of the following are canonical in Second Brain and must not be re-debated:

- `tenants.status` is lifecycle-only (6 values)
- Billing cycle, subscription type, payment status are separate concerns
- Paid: `PENDING_PAYMENT` → manual verify → manual activate → `ACTIVE`
- Trial/Demo: auto-activate after provisioning → `ACTIVE`
- Two emails for Trial/Demo (created + activated); set-password only on activation
- Paid: payment-required email with payment link; no set-password until activated
- Payment Received email deferred for R1
- Current billing-in-status write is an implementation defect
- `Tenant_Wizard_State.md` is historical/superseded

---

## 17. Remaining Questions

1. **Data migration mapping for `overdue`/`failed`/`waived`:** Should these map to `SUSPENDED` or `PENDING_PAYMENT`? Business decision needed for each.
2. **`setup_pending` and `inactive` constants:** Are these still needed or should they be removed/mapped to approved values? Not mentioned in approved lifecycle set.
3. **Cancel tenant API:** When should this be implemented? Approved as lifecycle state but no API exists.
4. **Demo subscription type:** Should `DEMO` be added as a subscription constant alongside `TRIAL`, or is it a plan-level `billing_cycle` value only?
5. **`BillingStatus` API field:** Rename to `subscriptionStatus`, or re-source from invoice status? Frontend impact either way.
6. **Re-activation from `SUSPENDED`:** Should this be allowed and what are the prerequisites? `CanActivate` currently returns false for `suspended`.

---

## 18. Implementation Results (2026-07-27)

Branch: `feat/tenant-lifecycle-status-alignment`
Worktree: `Unified-Commerce-tenant-lifecycle` only (original dirty `Unified-Commerce` untouched).

### Root cause fixed

Wizard and legacy create no longer pass `NormalizeBillingStatus(...)` into `Tenant.Create` as lifecycle status.

| Mode | Detection | Create lifecycle outcome |
|---|---|---|
| Paid | subscription status not trial/demo | `pending_payment` |
| Trial | subscription status trial / blank / legacy minimal | create as `draft` then `Activate()` → `active` |
| Demo | billing cycle `demo` | create as `draft` then `Activate()` → `active` |

### Domain changes

- `TenantStatusConstants`: six approved lowercase values only (`draft` … `cancelled`)
- `TenantLifecycleRules`: activate = `pending_activation`/`draft`; suspend = `active` only; login = `active` only
- `TenantLifecycleLegacyMapper`: DATA MIGRATION RULES only
- `TenantCreateMode` / `TenantCreateModeResolver`
- `Tenant.Create` rejects non-lifecycle values; `MarkPendingActivation`, `Cancel` added

### Paid activation gate

- Authoritative evidence: `subscription_invoices.invoice_status = PAID` and `paid_at IS NOT NULL`
- `MarkPaid` promotes tenant `pending_payment` → `pending_activation`
- Activate blocked for `pending_payment`, `active`, `cancelled`
- Activate on `pending_activation` requires `HasVerifiedPaidInvoiceAsync`
- No waiver flags accepted

### API compatibility

- Authoritative: `lifecycleStatus` (+ existing `status`) from `tenants.status`
- Temporary: `billingStatus` remains subscription-status compatibility field (deprecated in XML docs); not removed
- Summary `PendingActivationTenants` / dashboard attention count = `pending_activation` only
- Login: `ACTIVE` only (`setup_pending` removed)

### Migrations

1. `20260727150000_RepairTenantLifecycleStatusData` — approved legacy mapping; unknown values abort
2. `20260727151000_AddTenantLifecycleStatusCheckConstraint` — `ck_tenants_status`

Casing convention used by application: **lowercase**.

### Tests run

| Command | Result |
|---|---|
| `dotnet restore` / `dotnet build` | Success |
| Unit: PlatformTenant / TenantLifecycle / TenantAuth | **112 passed** |
| Unit: AuthSecurity / PlatformBilling | **20 passed** |
| Integration: PlatformTenant / PlatformBilling / PlatformDashboard / SubscriptionBilling | **75 passed** |
| ApiTests: PlatformAdminTenants / TenantAuth | **28 passed** |

### Remaining frontend work

- Consume `lifecycleStatus`
- Badge styles for all six lifecycle values
- KPI for `pendingActivationTenants`
- Stop treating billing vocabulary as tenant status

### Deferred scope (unchanged)

- Onboarding emails / payment links / email outbox
- Payment-waiver persistence/API/UI
- Tenant cancellation endpoint
- Full `billingStatus` semantic rename away from subscription status

### Files changed (primary)

Domain, Application (create/activate/DTOs/validator), Infrastructure (repositories, auth session, EF config, 2 migrations + snapshot), unit/integration tests, this audit report.
