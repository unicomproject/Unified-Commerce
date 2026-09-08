# Tenant Admin User Creation Corrected 5-Step Backend Closure

Date: 2026-08-26
Git base: `1ba541817580b28fe874a350e3ac59b00e75a9a8`
Branch: `userper`
Scope: `Unified-Commerce` backend/database only; Flutter and Angular were not modified.

## 1. Executive Summary

The corrected Tenant Admin Add New User backend contract is implemented for the canonical five-step flow: identity, base role, additive user permission grants, outlet/till scope, and review/save. The final `POST /api/v1/tenant-admin/users` command remains the single atomic creation boundary and preserves tenant isolation, delegation limits, idempotency, audit history, invitation safety, and soft-revocable assignments.

All feature-focused unit, API, and PostgreSQL tests pass. The solution build and EF pending-model check pass. The full backend suite has one unrelated pre-existing clean-database migration failure in `20260710143000_SeedDevelopmentVariableProductCatalog`; therefore the repository-level release gate remains blocked even though no corrected-user-flow regression remains.

## 2. Existing Backend Audit

- The existing user aggregate, invitation workflow, staff-code generator, MediaAsset lifecycle, idempotency service, audit infrastructure, entitlement evaluator, and effective-permission resolver were reusable.
- The previous create contract represented empty outlet IDs as implicit tenant-wide access and had no explicit no-access state, till assignment, default till, or catalog fingerprint.
- Role options previously projected active tenant roles without excluding privileged platform/super-admin codes.
- User permission overrides were already additive direct grants; they did not need a second catalog or role mutation path.
- Direct ACTIVE creation was already intentionally prohibited; safe creation states were INVITED and INACTIVE.

## 3. Existing Components Reused

- `TenantUser`, `TenantUserRole`, `OutletUserRole`, and `TenantUserPermission` authorization entities.
- Existing permission catalog and effective-permission resolution queries.
- Shared `IIdempotencyService`, `idempotency_requests`, audit logs, invitation/outbox, tenant context, and MediaAsset validation.
- Existing tenant role/outlet lookup APIs and repository transaction boundary.

## 4. Schema Gap Analysis

The existing schema could not distinguish ALL_OUTLETS from NO_OUTLET_ACCESS when no outlet rows existed and could not persist an explicit selected-till set or default till. An additive migration was therefore required. No duplicate RBAC tables or role records were introduced.

## 5. Step 1 Backend Contract

Step 1 owns basic user identity only: full name, normalized email, phone, employee metadata, backend-generated staff code, and optional validated profile `MediaAsset`. Role selection is not owned by Step 1, and client-provided effective permissions or staff codes are not trusted.

## 6. Step 2 Role Contract

`GET /api/v1/tenant-admin/users/create-options` returns tenant-owned, active, assignable base roles with role code, description, module count, permission count, and previews. `SUPER_ADMIN`, `PLATFORM_ADMIN`, and `PLATFORM_*` role codes are excluded and server-side role validation blocks attempts to submit them directly.

## 7. Step 3 User Permission Override Contract

User-specific permission overrides are additive direct grants stored through the existing tenant-user permission relation. They never replace or mutate the selected base role's global permission set. Explicit permission denies are unsupported and return `user.permission_denies_unsupported` instead of being silently accepted.

## 8. Effective Permission Resolution

The authoritative response derives inherited role permissions plus active direct user grants through the canonical backend resolver. Revoked assignments, inactive roles/permissions, tenant boundaries, and entitlements remain enforced. Counts and effective permission codes are backend-derived.

## 9. Step 4 Outlet Scope Contract

Supported explicit scopes are:

- `ALL_OUTLETS`: no selected outlet IDs are accepted.
- `SELECTED_OUTLETS`: at least one tenant-owned active outlet is required.
- `NO_OUTLET_ACCESS`: outlet IDs, default outlet, till IDs, and default till are prohibited.

Legacy requests without an explicit scope remain backward compatible: no outlet IDs map to ALL_OUTLETS and a non-empty list maps to SELECTED_OUTLETS.

## 10. Till Access Contract

Supported explicit till scopes are `ALL_ACCESSIBLE_TILLS`, `SELECTED_TILLS`, and `NO_TILL_ACCESS`. Selected tills must be active, belong to the authenticated tenant, and fall within the user's allowed outlet scope. Historical till rows use soft revoke/reactivation and tenant-scoped uniqueness.

## 11. Default Outlet/Till Rules

- A default outlet must be tenant-owned and, for SELECTED_OUTLETS, part of the selected outlet set.
- A default till must be tenant-owned, inside the allowed outlet scope, and, for SELECTED_TILLS, part of the selected till set.
- NO_OUTLET_ACCESS and NO_TILL_ACCESS cannot carry contradictory defaults.

## 12. Step 5 Security Contract

Final review data is informational only. `POST /api/v1/tenant-admin/users` revalidates identity, role, permission catalog version, permission delegation, profile media, tenant ownership, outlet/till scope, defaults, subscription limits, invitation state, and idempotency before persistence.

## 13. Active vs Invited Resolution

Capabilities explicitly advertise `supportsDirectActiveCreation = false`. Creation accepts INVITED or INACTIVE only. INVITED creation uses the existing invitation/outbox lifecycle; insecure temporary passwords, force-password flags, create-time 2FA, access start dates, and save-draft behavior are not advertised or synthesized.

## 14. Create Options Contract

The create-options response now includes:

- assignable role metadata and backend counts;
- entitled/delegable module-permission groups;
- tenant outlet and till options;
- supported outlet/till scopes;
- truthful capability flags;
- SHA-256 `permissionCatalogVersion` fingerprint.

## 15. Final Create Contract

The existing `POST /api/v1/tenant-admin/users` endpoint was extended compatibly with `outletAccessScope`, `defaultOutletId`, `tillAccessScope`, `tillIds`, `defaultTillId`, `permissionCatalogVersion`, and `deniedPermissionIds`. Existing fields and routes remain compatible.

## 16. Atomicity

User, base role assignment, direct permission grants, outlet assignments, till assignments, invitation/outbox records, MediaAsset link, and audit records are persisted in the existing repository `SaveChanges` transaction. Validation happens before mutation, and persistence failures do not return a created response. Mixed valid/invalid permission tests prove no partial logical creation.

## 17. Idempotency

The existing `Idempotency-Key` contract is retained. The canonical request fingerprint now includes explicit outlet/till scopes, defaults, selected till IDs, catalog version, and denied permission IDs. Same key/same payload replays; same key/different payload conflicts; tenant and actor isolation remain part of the key boundary.

## 18. Tenant Isolation

Tenant and actor IDs come only from authenticated server context. Role, permission, outlet, till, MediaAsset, assignment, and detail queries are tenant-scoped. Cross-tenant role, outlet, till, permission, and media references return controlled failures without exposing foreign data.

## 19. Delegation Ceiling

Assignable permissions are intersected with the actor's effective permissions, active catalog entries, module/feature status, and tenant entitlements. Unknown, inactive, unentitled, platform, or actor-unowned permissions are rejected; they are never silently removed. Privileged role assignment is separately blocked.

## 20. Permission Catalog Safety

The existing backend-driven catalog remains the only source of truth. The new fingerprint detects stale Step 3 selections. An enabled override with selected permission IDs and an empty/incomplete authoritative catalog fails with `user.permission_catalog_mismatch`, preventing silent permission loss.

## 21. API Changes

- Extended `GET /api/v1/tenant-admin/users/create-options` response.
- Extended `POST /api/v1/tenant-admin/users` request/response.
- Extended `PUT /api/v1/tenant-admin/users/{id}` for the same scope/default semantics.
- Added safe mappings for catalog conflict (`409`), non-delegable role/permission (`403`), and invalid/cross-tenant till (`404` or controlled validation response).

## 22. DTO Changes

Added role previews/counts, module metadata, assignability flags, till options, capabilities, catalog version, explicit scopes/defaults, denied-permission input, role code, invitation status, effective permission codes, and inherited/direct/till counts. Existing positional fields were preserved and new fields were appended with compatible defaults.

## 23. Domain Changes

- Added `TenantUserAccessScopes` constants and normalization helpers.
- Added explicit outlet/till scope and default-till state to `TenantUser`.
- Added `TenantUserTillAccess` with assign, soft revoke, and reactivate behavior.

## 24. DB/Migration Changes

Migration: `20260826120000_AddTenantUserExplicitOutletTillAccess`

- Adds `outlet_access_scope`, `till_access_scope`, and `default_till_id` to `tenant_users`.
- Creates `tenant_user_till_access` with tenant/user/till FKs and a unique tenant-user-till key.
- Adds scope/default constraints and indexes.
- Backfills legacy selected-outlet users where active outlet role/direct assignments exist.
- Updates `EPosDbContextModelSnapshot`.
- PostgreSQL migration/backfill test passes, and the current model has no operations beyond the migration snapshot.

## 25. Unit Test Results

Focused command: `TenantAdminUserServiceTests`
Result: **PASS — 70 passed, 0 failed, 0 skipped**.

Coverage includes options/capabilities, role and tenant validation, additive grants, catalog mismatch, all/selected/no outlet scope, till scope/defaults, invitation state, MediaAsset validation, atomic failure, idempotency, and cross-tenant isolation.

## 26. API Test Results

Focused command: `TenantAdminUsersControllerTests`
Result: **PASS — 8 passed, 0 failed, 0 skipped**.

Coverage includes authenticated context mapping, idempotency header, catalog conflict, and non-delegable role response mapping.

## 27. PostgreSQL Integration Results

Focused user/invite/idempotency/profile/scope/migration groups: **PASS — 14 passed, 0 failed, 0 skipped**.

Additional concurrency regressions that initially exposed shared database ordering were rerun after migration/build synchronization: **PASS — 3 passed, 0 failed**. Explicit outlet/till creation, transition to no access, soft revoke/reactivation, no duplicate rows, tenant role filtering, staff-code uniqueness, invitation lifecycle, idempotency, MediaAsset lifecycle, and migration backfill are verified on PostgreSQL.

## 28. Full Backend Test Results

Command: `dotnet test E_POS.sln --configuration Release --no-build --no-restore -m:1`

- Unit: 1163 passed, 0 failed.
- API: 481 passed, 0 failed.
- Integration: 572 passed, 5 failed.
- Flow4 fixture CLI: 17 passed, 0 failed.
- Local print agent: 50 passed, 0 failed.
- Total: **2283 passed, 5 failed**.

The original product-option FK blocker is fixed by `20260710142500_SeedReferenceProductOptionsBeforeVariableCatalog`. Four PostgreSQL `53200 out of shared memory` failures from the full parallel integration run passed when rerun in isolation (`4/4`). The remaining deterministic failure is `ManualPaymentPostgreSqlConcurrencyTests.Migrations_ApplyToCleanPostgreSqlDatabase`, now stopping later at `20260812105504_SeedStorefrontAzureMediaAssets` with PostgreSQL `23505`: `media_assets.id = eeee0001-0001-4000-8000-000000000001` was already inserted by `20260812103455_AddPromoBannersSeed`.

## 29. EF Pending Model Changes

Command: `dotnet ef migrations has-pending-model-changes --project .\src\E_POS.Infrastructure\E_POS.Infrastructure.csproj --startup-project .\src\E_POS.Api\E_POS.Api.csproj --configuration Release --no-build`
Result: **PASS — No changes have been made to the model since the last migration.**

Solution build: `dotnet build E_POS.sln --configuration Release --no-restore -m:1`
Result: **PASS — 5 unrelated existing warnings, 0 errors**.

`git diff --check`: **PASS**; only line-ending conversion notices were emitted.

## 30. Unsupported UI Features

The backend truthfully reports these as unsupported: direct ACTIVE creation, explicit permission denies, access-start scheduling, temporary passwords, force-password-change at creation, create-time 2FA setup, and save draft. Clients must hide or disable these controls rather than inventing behavior.

## 31. Remaining Flutter Work

No Flutter files were changed. Flutter must consume the extended create-options capabilities/catalog version, keep Role in Step 2, submit explicit outlet/till scopes and defaults, never send effective counts as authority, and display controlled API errors. UI implementation remains outside this backend task.

## 32. Remaining Blockers

1. Repository-wide clean PostgreSQL migration execution is blocked by the unrelated historical migration `20260812105504_SeedStorefrontAzureMediaAssets` inserting a media ID already created by `20260812103455_AddPromoBannersSeed`.
2. Until that storefront/media seed collision is corrected in its owning scope, a clean database cannot migrate through the complete chain, so the repository-level production release gate cannot be marked fully green.

There are no known corrected-user-flow code, API, model-alignment, focused-test, or existing-database upgrade blockers.

## 33. Final Verdict

`CORRECTED 5-STEP USER CREATION BACKEND STILL HAS BLOCKERS`
