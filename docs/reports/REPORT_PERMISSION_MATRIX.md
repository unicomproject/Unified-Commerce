# Tenant Admin Reports Permission Matrix

Phase 1 verification only. This file documents required access checks and gaps; it does not add permissions.

## Existing Permission Evidence

| Existing permission/constant | Observed use |
|---|---|
| `tenant.reports.products.view` | Product/fast-moving report permission. |
| `tenant.reports.sales.view` | Seeded and used for outlet revenue context. |
| `tenant.outlets.revenue.view` | Outlet revenue summary access. |
| `tenant.stock.view` | Inventory/current stock access. |
| `tenant.stock.value.view` | Sensitive stock valuation access. |
| `tenant.stock.movements.view` | Stock movement access. |
| `tenant.stock.expiry.view` | Expiry access. |

## Required Report Permissions

| Endpoint/section | Required permission | Existing? | Notes |
|---|---|---|---|
| `GET /reports/filter-options` | `tenant.reports.view` or section-specific view | Missing | Should still restrict option rows by outlet scope. |
| `GET /reports/dashboard` | `tenant.reports.dashboard.view` | Missing | Could aggregate from multiple section permissions. |
| Sales summary/transactions/daily | `tenant.reports.sales.view` | Partial | Seed has `reports.sales.view`; constant alignment needed. |
| Product/category sales | `tenant.reports.products.view` | Exists | Already used by product dashboard fast-moving logic. |
| Payment report | `tenant.reports.payments.view` | Missing | Financial and sensitive. |
| Tax report | `tenant.reports.tax.view` | Missing | Financial/statutory. |
| Discount report | `tenant.reports.discounts.view` | Missing | Manager approval count may be sensitive. |
| Returns/refunds report | `tenant.reports.returns.view` | Missing | Customer-sensitive. |
| Cashier performance | `tenant.reports.cashiers.view` | Missing | User performance-sensitive. |
| Stock current/low/out-of-stock | `tenant.stock.view` | Exists | Use existing stock permission. |
| Batch expiry | `tenant.stock.expiry.view` | Exists | Use existing expiry permission. |
| Stock movement | `tenant.stock.movements.view` | Exists | Use existing movement permission. |
| Inventory valuation/cost fields | `tenant.stock.value.view` | Exists | Field-level masking required. |
| Outlet performance | `tenant.reports.outlets.view` or `tenant.outlets.revenue.view` | Partial | Existing outlet revenue permission can be reused with naming decision. |
| Till summary | `tenant.reports.tills.view` | Missing | Can reuse till-management view only if Second Brain allows. |
| Export reports | `tenant.reports.export` | Missing | Must also require underlying section permission. |
| Customer PII fields | `tenant.reports.customer-pii.view` | Missing | Required for email/phone/full customer detail. |

## Field-Level Sensitivity

| Field group | Required handling |
|---|---|
| Customer name/email/phone | Hide or redact unless customer PII permission is present. |
| Stock unit cost, stock value, average cost, inventory valuation | Hide or redact unless `tenant.stock.value.view` is present. |
| Cashier performance | Require cashier report permission; consider manager/admin role constraints. |
| Export | Must apply the same field masking as on-screen reports. |

## Outlet Scope Rules

- Resolve authorized outlet IDs from tenant context/role assignment before any report query.
- If `outletId` is supplied, it must be inside authorized outlet scope.
- If `tillId` is supplied, its outlet must be inside authorized outlet scope.
- If user has tenant-wide report permission but limited outlet assignment, outlet assignment should still restrict data unless Second Brain says otherwise.

## Feature/Entitlement Rules

- Current inspected backend feature codes did not show a reports feature entitlement.
- If Release 1 requires module feature entitlement, reports must check it consistently with existing tenant feature predicates.

## Authorization Gaps

- Missing consolidated report permission constants.
- Missing field-level customer PII permission.
- Missing report export permission and export job authorization model.
- Current tenant context endpoint does not clearly expose user-assigned outlet scoping; repository returns tenant outlets after feature predicate filtering.
