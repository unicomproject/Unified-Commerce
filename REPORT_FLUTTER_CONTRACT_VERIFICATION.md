# Tenant Admin Reports Flutter Contract Verification

Verified against `Tenantadmin/Nytroz-POS-App/lib/features/tenant_admin/reports` after Phase 4 backend work.

## Matching Contracts

| Item | Flutter | Backend | Status |
|---|---|---|---|
| Base path | `/api/v1/tenant-admin/reports` | Same | Implemented |
| Filter options | `GET /filter-options` | Same | Implemented |
| Dashboard | `GET /dashboard` | Same | Implemented |
| Sales | `GET /sales` | Same | Implemented |
| Sales detail | `GET /sales/{orderId}` | Same | Implemented |
| Stock | `GET /stock` | Same | Implemented |
| Outlets | `GET /outlets` | Same | Implemented |
| Export create | `POST /exports` | Same | Partially implemented |
| Export status | `GET /exports/{jobId}` | Same | Partially implemented |
| Query names | `from`, `to`, `outletId`, `tillId`, `cashierId`, `departmentId`, `categoryId`, `subcategoryId`, `brandId`, `productId`, `productVariantId`, `salesChannelId`, `paymentMethodId`, `orderStatus`, `paymentStatus`, `stockStatus`, `expiryStatus`, `movementType`, `batchNumber`, `search`, `section`, `page`, `pageSize`, `sortBy`, `sortDirection` | Same supported subset | Implemented |
| Section names | `summary`, `transactions`, `products`, `categories`, `payments`, `tax`, `discounts`, `returns`, `cashiers`, `daily`, `current`, `low-stock`, `out-of-stock`, `batch-expiry`, `movements`, `valuation`, `performance`, `tills` | Same plus outlet `cashiers` | Implemented |
| Success envelope | `{ data: ... }` | Same | Implemented |
| Pagination | `page`, `pageSize`, `totalCount`, `totalPages` | Same | Implemented |
| Date-only query | `yyyy-MM-dd` | ASP.NET `DateOnly` binding | Implemented |
| Currency | `currencyCode` | Returned on report result | Implemented |
| Export flat body | Flutter sends `reportType`, `section`, `format` plus flat filters | Backend accepts flat body and nested `filters` | Implemented |

## Backend-Compatible Corrections Made

- Export request parsing accepts Flutter's flat request body.
- Stock/outlet endpoints now return the same generic `ReportResultDto` shape used by Flutter.
- Export status strings use uppercase lifecycle values such as `COMPLETED`.

## Mismatches / Remaining Flutter Work

| Area | Status |
|---|---|
| `metrics` array | Backend currently returns summary dictionaries, not Flutter `ReportMetricDto[]`; Flutter tolerates empty metrics but rich metric cards may need mapper adaptation. |
| `sections` array | Backend returns dictionary-style sections for dashboard/sales detail; Flutter generic `ReportSectionDto[]` expects arrays. Current report list screens rely mainly on `records`. |
| Export binary download | Backend returns metadata only with `downloadUrl = null`; Flutter download/open flow must handle this pending state. |
| `inventoryLocationId` filter | Backend supports it; Flutter `ReportQuery` currently does not expose it. |
| XLSX/PDF binary | Backend validates formats but does not generate XLSX/PDF files yet. |

## Endpoint Availability

- Available: dashboard, filter options, sales, sales detail, stock, outlets.
- Partial: exports metadata/status.
- Not available: physical export file download endpoint/token.
