# Tenant Admin Reports API Contracts

Phase 1 verification only. Contracts are specification targets for later backend implementation.

## Common Rules

- Base route: `/api/v1/tenant-admin/reports`
- Auth: `[Authorize(Policy = "TenantOnly")]`
- Tenant context: derive tenant and user from JWT claims through `ITenantRequestContextFactory`.
- Do not accept `tenantId` in query/body.
- Response success envelope follows current backend style: `{ "data": ... }`.
- Error envelope follows current middleware/controller style: `code`, `message`, `details`, `traceId`, `timestamp`.

## Query Parameters

All list/report endpoints accept the relevant subset of:

| Parameter | Type | Notes |
|---|---|---|
| `from` | date `yyyy-MM-dd` | Tenant-local report start date. |
| `to` | date `yyyy-MM-dd` | Tenant-local report end date. |
| `outletId` | GUID | Must be inside authorized outlet scope. |
| `tillId` | GUID | Must belong to an authorized outlet. |
| `cashierId` | GUID | Tenant user filter. |
| `customerId` | GUID | Sensitive/customer reporting. |
| `departmentId` | GUID | Product hierarchy filter. |
| `categoryId` | GUID | Product category filter. |
| `subcategoryId` | GUID | Category hierarchy filter. |
| `brandId` | GUID | Product brand filter. |
| `productId` | GUID | Product filter. |
| `productVariantId` | GUID | Variant filter. |
| `salesChannelId` | GUID | Sales channel filter. |
| `paymentMethodId` | GUID | Payment filter. |
| `orderStatus` | string | Sales order status. |
| `paymentStatus` | string | Payment status. |
| `stockStatus` | string | Calculated stock status. |
| `expiryStatus` | string | Calculated expiry status. |
| `movementType` | string | Stock movement type. |
| `batchNumber` | string | Batch search/filter. |
| `search` | string | Section-specific search. |
| `section` | string | Report section/tab key. |
| `page` | int | 1-based. |
| `pageSize` | int | Enforce backend maximum. |
| `sortBy` | string | Allow-list per section. |
| `sortDirection` | `asc`/`desc` | Default per section. |

## `GET /filter-options`

Returns report filter option groups.

```json
{
  "data": {
    "groups": {
      "outlets": [{ "id": "...", "code": "OUT-001", "name": "Main Outlet", "status": "ACTIVE", "parentId": null, "secondaryLabel": null, "isActive": true }],
      "tills": [],
      "cashiers": [],
      "customers": [],
      "departments": [],
      "categories": [],
      "subcategories": [],
      "brands": [],
      "products": [],
      "productVariants": [],
      "salesChannels": [],
      "paymentMethods": [],
      "orderStatuses": [],
      "paymentStatuses": [],
      "stockStatuses": [],
      "expiryStatuses": [],
      "movementTypes": []
    }
  }
}
```

## `GET /dashboard`

Returns cross-report dashboard metrics and sections.

Required response shape:

```json
{
  "data": {
    "section": "dashboard",
    "metrics": [],
    "sections": [],
    "currencyCode": "LKR",
    "generatedAt": "2026-07-15T00:00:00Z"
  }
}
```

## `GET /sales`

Sections:

- `summary`
- `transactions`
- `products`
- `categories`
- `payments`
- `tax`
- `discounts`
- `returns`
- `cashiers`
- `daily`

Response shape:

```json
{
  "data": {
    "section": "transactions",
    "metrics": [],
    "sections": [],
    "records": [],
    "pagination": {
      "page": 1,
      "pageSize": 10,
      "totalCount": 0,
      "totalPages": 0
    },
    "currencyCode": "LKR",
    "generatedAt": "2026-07-15T00:00:00Z"
  }
}
```

## `GET /sales/{orderId}`

Returns sales transaction detail.

Required fields:

- `orderId`, `orderNumber`, `invoiceInformation`, `financialSummary`, `currencyCode`, `customerEmail`, `customerPhone`
- detail sections: `items`, `payments`, `discounts`, `taxes`, `returnsAndRefunds`, `notes`

Sensitive fields:

- `customerEmail`
- `customerPhone`
- customer-identifying notes if exposed

## `GET /stock`

Sections:

- `current`
- `low-stock`
- `out-of-stock`
- `batch-expiry`
- `movements`
- `valuation`

Response shape is the same as `/sales`, with `records` matching the requested stock section.

Status: implemented for all listed sections. Cost/value fields are returned as `null` unless the user has `tenant.stock.value.view`.

## `GET /outlets`

Sections:

- `performance`
- `tills`
- `cashiers`

Response shape is the same as `/sales`, with `records` matching the requested outlet section.

Status: implemented for `performance`, `tills`, and `cashiers`. `cashiers` reuses the sales cashier aggregation shape.

## `POST /exports`

Starts report export job.

Request target:

```json
{
  "reportType": "sales",
  "section": "transactions",
  "format": "csv",
  "filters": {}
}
```

Response target:

```json
{
  "data": {
    "jobId": "...",
    "reportType": "sales",
    "format": "csv",
    "status": "pending",
    "requestedAt": "2026-07-15T00:00:00Z",
    "completedAt": null,
    "fileName": null,
    "downloadUrl": null,
    "expiresAt": null,
    "errorMessage": null
  }
}
```

Status: partially implemented. The backend validates permissions and returns completed export metadata, but it does not yet persist jobs, generate binary CSV/XLSX/PDF files, expose a download URL, or audit/expire jobs durably.

## `GET /exports/{jobId}`

Returns report export status using the same `ReportExportDto` response target.

Status: partially implemented using in-memory metadata for the current process only.

## DTO Targets

### ReportResultDto

| Property | Required |
|---|---|
| `section` | Yes |
| `metrics` | Yes |
| `sections` | Yes |
| `records` / `items` / `rows` | Yes for row reports |
| `pagination` | Yes for row reports |
| `currencyCode` | Yes for financial reports |
| `generatedAt` | Yes |

### ReportMetricDto

| Property | Notes |
|---|---|
| `key` | Stable frontend key. |
| `label` | Display label. |
| `rawValue` | Numeric/string raw value. |
| `formattedValue` | Optional server formatting; frontend can also format. |
| `currencyCode` | For financial metrics. |
| `comparisonValue` | Previous period value. |
| `percentageChange` | Calculated comparison percent. |
| `comparisonLabel` | Example: `vs previous period`. |
| `trendDirection` | `positive`, `negative`, `neutral`. |
| `requiredPermission` | Field-level permission metadata. |
| `isSensitive` | True for cost/customer-sensitive values. |

### ReportFilterOptionDto

| Property | Notes |
|---|---|
| `id` | GUID/string ID. |
| `code` | Business code. |
| `name` | Display name. |
| `status` | Source status. |
| `parentId` | For hierarchy. |
| `secondaryLabel` | Optional helper label. |
| `isActive` | Boolean active state. |

## Backend Files Expected in Later Phase

No files are implemented in Phase 1. Later implementation likely affects:

- `src/E_POS.Api/Controllers/V1/Tenant/Reports/TenantAdminReportsController.cs`
- `src/E_POS.Application/Modules/Tenant/Reports/...`
- `src/E_POS.Infrastructure/Modules/Tenant/Reports/...`
- `tests/E_POS.ApiTests/...`
- `tests/E_POS.UnitTests/...`
- `tests/E_POS.IntegrationTests/...`
