# E_POS Backend

Clean Architecture backend scaffold for E_POS MVP.

## Architecture

```text
API -> Application -> Domain
Infrastructure -> Application + Domain
```

The backend uses service/use-case orchestration with repository contracts. It does not use CQRS, MediatR, Redis, or event sourcing.

## Projects

```text
src/
  E_POS.Api/
  E_POS.Application/
  E_POS.Domain/
  E_POS.Infrastructure/
tests/
  E_POS.UnitTests/
  E_POS.IntegrationTests/
  E_POS.ApiTests/
```

## Rules

- Keep controllers thin.
- Put workflow logic in Application services/use cases.
- Keep Domain free of EF Core, HTTP, and infrastructure dependencies.
- Implement repository contracts in Infrastructure.
- Use DTOs for API input/output.
- Enforce tenant isolation, feature entitlements, permissions, audit, and idempotency in protected workflows.

## Development platform admin login

After applying EF migrations to a local PostgreSQL database, sign in to the Angular Platform Admin with:

| Field | Value |
| --- | --- |
| Email | `posunique001@gmail.com` |
| Password | `Admin@12345` |

Copy `src/E_POS.Api/appsettings.Development.example.json` to `appsettings.Development.json` and set your local connection string. Do not commit real database passwords or production JWT signing keys.

### Development Platform Admin billing test accounts (optional)

In Development only, the API can seed two login-capable Platform Admin accounts for billing permission browser testing. Credentials must come from user-secrets or environment variables — never from committed appsettings.

Required roles must already exist in the database: `billing_viewer_dev` and `platform_ops_no_billing_dev`.

From `src/E_POS.Api`:

```powershell
cd src/E_POS.Api

dotnet user-secrets set "DevelopmentSeed:PlatformAdmin:BillingViewer:Email" "billing.viewer.dev@local.test"
dotnet user-secrets set "DevelopmentSeed:PlatformAdmin:BillingViewer:Password" "<local-secret>"
dotnet user-secrets set "DevelopmentSeed:PlatformAdmin:NoBilling:Email" "billing.none.dev@local.test"
dotnet user-secrets set "DevelopmentSeed:PlatformAdmin:NoBilling:Password" "<local-secret>"
```

Optional display names:

```powershell
dotnet user-secrets set "DevelopmentSeed:PlatformAdmin:BillingViewer:DisplayName" "Billing Viewer Development"
dotnet user-secrets set "DevelopmentSeed:PlatformAdmin:NoBilling:DisplayName" "No Billing Development"
```

Missing secrets skip that profile with a warning and do not fail startup. The seeder never runs outside Development and never modifies `posunique001@gmail.com`.

### Platform auth routes

| Route | Purpose |
| --- | --- |
| `POST /api/v1/platform-auth/login` | Primary login (flat response) |
| `POST /api/v1/platform-auth/refresh` | Primary refresh |
| `POST /api/v1/platform-auth/logout` | Primary logout (requires Bearer JWT) |
| `POST /api/v1/auth/platform-login` | Legacy Angular-compatible login |
| `POST /api/v1/auth/platform-refresh` | Legacy Angular-compatible refresh |
| `POST /api/v1/auth/platform-logout` | Legacy Angular-compatible logout |
| `GET /api/v1/platform-admin/dashboard` | Platform dashboard summary (requires `platform.dashboard.view`) |
| `GET /api/v1/platform-admin/tenants` | Platform tenant list (requires `platform.tenants.view`) |
| `GET /api/v1/platform-admin/tenants/summary` | Platform tenant summary counts |
| `GET /api/v1/platform-admin/tenants/filter-options` | Distinct tenant/plan filter values |
| `GET /api/v1/platform-admin/tenants/{tenantId}` | Platform tenant detail |
| `POST /api/v1/platform-admin/tenants` | Create draft tenant (requires `platform.tenants.create`) |
| `PUT /api/v1/platform-admin/tenants/{tenantId}` | Update tenant (requires `platform.tenants.update`) |
| `POST /api/v1/platform-admin/tenants/{tenantId}/activate` | Activate tenant (requires `platform.tenants.activate`) |
| `POST /api/v1/platform-admin/tenants/{tenantId}/suspend` | Suspend tenant (requires `platform.tenants.suspend`) |
| `PUT /api/v1/platform-admin/tenants/{tenantId}/entitlements` | Replace tenant entitlements (requires `platform.tenants.entitlements.update`) |
| `GET /api/v1/platform-admin/permission-catalog` | Platform permission catalog tree (requires `platform.permissions.view`) |
| `GET /api/v1/platform-admin/permission-catalog/flat` | Flat platform permission catalog |
| `GET /api/v1/platform-admin/roles` | Platform role list (requires `platform.roles.view`) |
| `GET /api/v1/platform-admin/roles/{roleId}` | Platform role detail |
| `POST /api/v1/platform-admin/roles` | Create platform role (requires `platform.roles.create`) |
| `PUT /api/v1/platform-admin/roles/{roleId}` | Update platform role (requires `platform.roles.update`) |
| `GET /api/v1/platform-admin/roles/{roleId}/permissions` | Role permissions (requires `platform.roles.permissions.view`) |
| `PUT /api/v1/platform-admin/roles/{roleId}/permissions` | Replace role permissions (requires `platform.roles.permissions.update`) |
| `GET /api/v1/platform-admin/users` | Platform user list (requires `platform.users.view`) |
| `GET /api/v1/platform-admin/users/{userId}` | Platform user detail |
| `POST /api/v1/platform-admin/users` | Create platform user invite (requires `platform.users.create`) |
| `PUT /api/v1/platform-admin/users/{userId}` | Update platform user status (requires `platform.users.update`) |
| `PUT /api/v1/platform-admin/users/{userId}/roles` | Replace platform user roles (requires `platform.users.roles.assign`) |
| `GET /api/v1/platform/subscription-plans` | Subscription plan list (requires `platform.subscription_plans.view`) |
| `GET /api/v1/platform/subscription-plans/catalog` | Commercial modules/features catalog |
| `POST /api/v1/platform/subscription-plans` | Create draft subscription plan |
| `PATCH /api/v1/platform/subscription-plans/{planId}/pricing` | Update draft plan pricing |
| `PATCH /api/v1/platform/subscription-plans/{planId}/limits` | Update draft plan limits |
| `PATCH /api/v1/platform/subscription-plans/{planId}/features` | Update draft plan features |
| `POST /api/v1/platform/subscription-plans/{planId}/publish` | Publish draft plan to active |

### Refresh token cookie paths

| Flow | Cookie name | Path |
| --- | --- | --- |
| Primary platform auth | `platform_refresh_token` | `/api/v1/platform-auth` |
| Legacy Angular auth | `platform_refresh_token` | `/api/v1/auth` |

Use the legacy routes end-to-end from the Platform Admin SPA so login sets a cookie the refresh endpoint can read.