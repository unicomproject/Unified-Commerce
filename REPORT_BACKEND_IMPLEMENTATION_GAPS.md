# Tenant Admin Reports Backend Implementation Gaps

Phase 1 verification only. This file lists what blocks or constrains a correct backend implementation.

## API Surface Status

- `api/v1/tenant-admin/reports/*` controller is now present.
- Report application contracts/services/repositories are now present.
- Export job metadata endpoints are present, but durable export persistence and file storage are still missing.

## Existing APIs That Are Related but Not Sufficient

| Existing API | Relevance | Gap |
|---|---|---|
| `/api/v1/tenant-admin/products/dashboard` | Product dashboard style metrics | Not the reports API, uses product-specific DTO, default date behavior is UTC-based. |
| `/api/v1/tenant-admin/inventory/current-stock` | Current stock list | Does not cover all report sections/contract fields. |
| `/api/v1/tenant-admin/inventory/current-stock/summary` | Stock counts | Does not cover valuation/movement/export reports. |
| `/api/v1/tenant-admin/outlets/{id}/revenue-summary` | Outlet revenue | Single outlet detail endpoint, not report contract. |
| POS home dashboard endpoints | POS/cashier dashboard | Not Tenant Admin Reports contract. |

## Remaining Data Model Gaps

- `sales_orders.business_date` and reporting outlet snapshot fields were added for new POS sales.
- `TenantAdminContextDto` now exposes tenant timezone, currency, locale, business date, accessible outlets, feature codes, and permission codes.
- Sales order lines now support barcode, brand, department, category, and subcategory snapshots for new POS sales.
- `sales_order_taxes` supports tax amounts but no explicit refunded tax allocation was verified.
- `sales_order_discounts` supports discounts but no explicit manager approval relationship was verified.
- Return approved quantity was not verified as a direct field.
- Stock movement references store reference IDs but not a display reference number.
- Stock movement reason is not normalized for every movement type; movement note exists.
- Durable report export job persistence, blob/file storage, short-lived download tokens, and binary generation are absent.

## Authorization Gaps

- Consolidated Tenant Admin Reports permission constants were added.
- Existing permissions cover only parts of reports: product reports, sales reports, stock view/value/movement/expiry, outlet revenue.
- Missing explicit permissions for report dashboard, payments, tax, discounts, returns, cashiers, outlets, tills, exports, and customer PII.
- User outlet scope enforcement must be verified/implemented; current tenant context outlet query appears tenant-wide.

## Query and Performance Risks

- Reports need cross-module joins across sales orders, lines, payments, refunds, inventory, catalog, outlets, tills, and users.
- Large report tables require pagination, allow-listed sorting, and indexed filters.
- Export endpoints need async/background processing or strict row limits.
- Current catalog joins for historical product/category reporting may produce historically inaccurate labels if catalog changed after sale.

## Date/Timezone Risks

- Existing product dashboard defaults to `DateTime.UtcNow` converted to `DateOnly`.
- Reports must interpret `from`/`to` in tenant timezone, not server UTC.
- Business-day based reports cannot be fully correct until timezone/business-date rules are defined.

## Contract Gaps

- Flutter `/reports` endpoints are now exposed.
- Backend returns generic `ReportResultDto` with summary/records/pagination. Rich Flutter `metrics[]` and array-style `sections[]` remain partial.
- Backend existing DTOs are adapted through report DTOs.
- Field-level sensitivity metadata (`requiredPermission`, `isSensitive`) is expected in metrics and should also guide rows/export masking.

## Remaining Later Implementation Sequence

1. Add durable export job/storage/download-token infrastructure if Release 1 requires real downloads.
2. Add API/integration tests for every stock/outlet/export section and tenant/outlet scope scenario.
3. Backfill business date/reporting outlet/product snapshots for historical sales if historical accuracy is required.
4. Replace current `summary` dictionary with rich `metrics[]` if Flutter dashboard widgets require metric metadata.
5. Add query-plan/performance review against production-sized data.
