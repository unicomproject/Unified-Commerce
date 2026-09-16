# Media Assets Phased Migration Plan

Status: Phase 3 backend stabilization implemented; Phase 4 not started  
System: TM-EPOS MVP  
Storage target: Azure Blob Storage only  
Last updated: 2026-07-23

## Purpose

Create one common media metadata model for uploaded images while keeping the existing API and frontend image fields stable during migration.

The goal is to support product, variant, category, brand, storefront, tenant logo, profile image, and return inspection media with consistent metadata:

- Storage key
- Public/display URL
- MIME type
- File size
- Width and height
- Checksum
- Tenant ownership
- Audit fields

## Decision: New Table Requirement

If the requirement is only to connect Azure Blob Storage for a small first release, a new table is not strictly required because existing image fields can store URLs and storage keys.

If the requirement is that all uploaded images across product, variant, category, brand, storefront, tenant logo, profile image, and return inspection flows must have consistent metadata, then a new `media_assets` table is required.

For TM-EPOS long-term maintainability, the recommended direction is:

- Add `media_assets`.
- Keep existing image URL/storage fields during migration.
- Write both the new media metadata and old compatibility fields in Phase 1.
- Keep API response fields such as `imageUrl`, `logoUrl`, `profileImageUrl`, and `imageStorageKey` stable so Flutter/Angular display screens do not break.

## Current Image And Media Locations

These existing tables/fields currently carry image, logo, or file storage data.

| Area | Table | Current attributes |
|---|---|---|
| Product and variant images | `product_images` | `image_storage_key`, `image_url`, `mime_type`, `file_size_bytes`, `width_px`, `height_px`, `checksum_hash` |
| Category image | `categories` | `image_url` |
| Brand logo | `brands` | `logo_url` |
| Option value image | `product_option_values` | `image_url` |
| Option template value image | `product_option_template_values` | `image_url` |
| Storefront banner image | `storefront_banners` | `image_url` |
| Tenant/store logo | `tenant_profiles` | `logo_url` |
| Tenant user profile image | `tenant_users` | `profile_image_url` |
| Return inspection staged media | `return_inspection_media_staging` | `storage_key`, `file_name`, `content_type`, `size_bytes` |
| Return inspection final media | `return_inspection_media` | `storage_key`, `file_name`, `content_type`, `size_bytes` |

## New Table

Add `media_assets`.

| Attribute | Suggested type | Null | Notes |
|---|---:|---|---|
| `id` | `uuid` | NOT NULL | Primary key |
| `tenant_id` | `uuid` | NOT NULL | Tenant owner |
| `container_name` | `varchar(100)` | NOT NULL | Azure container name |
| `storage_key` | `varchar(500)` | NOT NULL | Azure blob path/name |
| `public_url` | `varchar(500)` | NULL | Display URL only; do not store SAS tokens |
| `original_file_name` | `varchar(255)` | NULL | Sanitized original name |
| `mime_type` | `varchar(100)` | NOT NULL | Example: `image/jpeg`, `image/png`, `image/webp` |
| `file_extension` | `varchar(20)` | NOT NULL | Example: `.jpg`, `.png`, `.webp` |
| `file_size_bytes` | `bigint` | NOT NULL | Must be greater than zero |
| `width_px` | `int` | NULL | Image width when available |
| `height_px` | `int` | NULL | Image height when available |
| `checksum_hash` | `varchar(128)` | NULL | SHA-256 or approved checksum |
| `asset_type` | `varchar(40)` | NOT NULL | For now: `IMAGE` |
| `asset_purpose` | `varchar(80)` | NOT NULL | Example: `PRODUCT_IMAGE`, `CATEGORY_IMAGE`, `BRAND_LOGO` |
| `status` | `varchar(40)` | NOT NULL | `ACTIVE`, `INACTIVE`, `DELETED` |
| `created_at` | `timestamp with time zone` | NOT NULL | Audit |
| `created_by_tenant_user_id` | `uuid` | NULL | Tenant user audit |
| `updated_at` | `timestamp with time zone` | NOT NULL | Audit |
| `updated_by_tenant_user_id` | `uuid` | NULL | Tenant user audit |

Suggested constraints:

- `PK(id)`
- `UNIQUE(tenant_id, id)`
- `UNIQUE(tenant_id, storage_key)`
- `CHECK(file_size_bytes > 0)`
- `CHECK(width_px IS NULL OR width_px > 0)`
- `CHECK(height_px IS NULL OR height_px > 0)`
- `CHECK(asset_type IN ('IMAGE'))`
- `CHECK(status IN ('ACTIVE', 'INACTIVE', 'DELETED'))`

## New Reference Columns

Add nullable media reference columns first. Do not remove old attributes in the first migration.

| Table | New attribute | Keep existing attributes |
|---|---|---|
| `product_images` | `media_asset_id` | Yes |
| `categories` | `image_media_asset_id` | Yes |
| `brands` | `logo_media_asset_id` | Yes |
| `product_option_values` | `image_media_asset_id` | Yes |
| `product_option_template_values` | Deferred | Platform-level table has no `tenant_id`; needs separate platform-media decision before adding a safe FK. |
| `storefront_banners` | `image_media_asset_id` | Yes |
| `tenant_profiles` | `logo_media_asset_id` | Yes |
| `tenant_users` | Needs separate review | Keep `profile_image_url` until the current UUID/FK intent is confirmed |
| `return_inspection_media_staging` | Optional `media_asset_id` | Yes |
| `return_inspection_media` | Optional `media_asset_id` | Yes |

Tenant-aware FK rule:

```text
(tenant_id, media_asset_id) -> media_assets(tenant_id, id)
```

## Azure Storage Key Standard

Use backend-resolved `tenant_id`; never trust frontend-provided tenant identifiers.

```text
tenants/{tenantId}/products/{productId}/images/{mediaAssetId}.{ext}
tenants/{tenantId}/products/{productId}/variants/{variantId}/images/{mediaAssetId}.{ext}
tenants/{tenantId}/categories/{categoryId}/image/{mediaAssetId}.{ext}
tenants/{tenantId}/brands/{brandId}/logo/{mediaAssetId}.{ext}
tenants/{tenantId}/product-option-values/{optionValueId}/image/{mediaAssetId}.{ext}
tenants/{tenantId}/storefront/banners/{bannerId}/{mediaAssetId}.{ext}
tenants/{tenantId}/tenant-profile/logo/{mediaAssetId}.{ext}
tenants/{tenantId}/return-inspections/{saleId}/{saleLineId}/{mediaAssetId}.{ext}
```

## Phase 1: Additive Foundation

Objective: introduce the common media metadata foundation and Azure Blob upload path without breaking current backend APIs or Flutter/Angular display fields.

Phase 1 is additive only:

- Add new structures.
- Keep existing attributes.
- Keep existing API response field names.
- Do not remove or rename old columns.
- Do not change frontend display contracts.

### Phase 1 Scope

In scope:

- Create `media_assets`.
- Add nullable media reference columns to selected tenant-owned image-bearing tables.
- Add Azure Blob Storage configuration and infrastructure service.
- Add common image validation for backend uploads.
- Save new uploads into Azure Blob Storage.
- Save metadata into `media_assets`.
- Continue writing existing compatibility fields such as `image_url`, `logo_url`, and `image_storage_key`.
- Implement the first upload APIs for product, category, and brand images.

Out of scope:

- Removing old image/logo/storage attributes.
- Changing existing API response field names.
- Reworking Flutter or Angular display screens.
- Migrating old image rows into `media_assets`.
- Updating return inspection media to `media_assets` unless explicitly approved for Phase 1.
- Adding `product_option_template_values.image_media_asset_id`; that table is platform-level and has no `tenant_id`, so it needs a separate platform-media decision.
- Adding AWS, S3, MinIO, or other storage providers.

### Phase 1 Database Work

Create `media_assets` with tenant ownership and metadata fields.

Add nullable FK columns:

| Table | New nullable attribute |
|---|---|
| `product_images` | `media_asset_id` |
| `categories` | `image_media_asset_id` |
| `brands` | `logo_media_asset_id` |
| `product_option_values` | `image_media_asset_id` |
| `storefront_banners` | `image_media_asset_id` |
| `tenant_profiles` | `logo_media_asset_id` |

Keep existing attributes unchanged.

Phase 1 deliberately skips `product_option_template_values` because it is platform-level reference data and does not carry `tenant_id`.

Use tenant-aware FK rules wherever possible:

```text
(tenant_id, media_asset_id) -> media_assets(tenant_id, id)
```

### Phase 1 Backend Work

Add Azure-only storage infrastructure:

- Azure storage options.
- Azure Blob upload service.
- Blob key builder.
- Image validation service.
- Checksum calculation.
- Image dimension reader.
- Media asset repository/service.

Do not expose Azure SDK types outside Infrastructure.

Application services should receive clean upload inputs and return safe DTOs only.

### Phase 1 API Work

First API batch:

```text
POST /api/v1/products/{productId}/images
POST /api/v1/categories/{categoryId}/image
POST /api/v1/brands/{brandId}/logo
```

Product image upload must support variant and channel scope through optional request fields:

```text
product_variant_id optional
sales_channel_id optional
alt_text optional
image_purpose optional
sort_order optional
is_primary_image optional
```

Compatibility rule:

- Product upload writes `media_assets` and `product_images`.
- Category upload writes `media_assets` and `categories.image_url`.
- Brand upload writes `media_assets` and `brands.logo_url`.

### Phase 1 Access Checks

Each upload API must enforce:

- Authenticated tenant user.
- Server-side tenant context.
- Active tenant status where the existing module requires it.
- Required feature entitlement where plan-controlled.
- Required permission code.
- Target record belongs to the same tenant.
- Product variant belongs to the same product and tenant when supplied.
- No cross-tenant media asset attachment.

Do not hardcode role names.

### Phase 1 Validation Rules

Backend image validation must check:

- File is present and non-empty.
- File size is within configured limit.
- MIME type is allowed.
- File extension matches allowed image types.
- File signature matches the claimed type.
- Width/height can be read when applicable.
- Checksum is calculated before metadata save.

Initial allowed MIME types:

```text
image/jpeg
image/png
image/webp
```

### Phase 1 Frontend Impact

No required frontend display change if backend responses continue to return existing fields:

```text
imageUrl
logoUrl
profileImageUrl
imageStorageKey
```

Flutter/Angular upload UI changes are separate work and should be planned only after backend upload APIs are stable.

### Phase 1 Test Plan

Minimum backend tests:

- Upload success creates Azure blob and `media_assets` row.
- Upload success updates old compatibility field.
- Invalid MIME type is rejected.
- Invalid file signature is rejected.
- File too large is rejected.
- Permission denied returns 403.
- Missing/invalid tenant context returns 401/403 as per existing API standard.
- Target record from another tenant is not accessible.
- Product variant from another product or tenant is rejected.
- Failed DB save cleans up uploaded blob when possible.

### Phase 1 Exit Criteria

Phase 1 is complete only when:

- Build passes.
- Relevant automated tests pass.
- New upload APIs store images in Azure Blob Storage.
- New upload APIs create `media_assets` records.
- Existing compatibility fields are still written.
- Existing image display APIs still return the same response fields.
- No old attributes are removed.
## Phase 2: Backfill And Read Preference

Objective: Move existing data into `media_assets` and make reads prefer the new model.

Database:

- Backfill existing rows from old URL/storage fields into `media_assets`.
- Populate nullable media FK columns where reliable mapping is possible.

Backend:

- Read image URL from `media_assets.public_url` when FK exists.
- Fall back to old fields when FK is null.
- Keep API response fields stable, for example still return `image_url` to frontend.

Testing:

- Backfilled image displays correctly.
- New uploaded image displays correctly.
- Old row without `media_asset_id` still displays through fallback.
- Cross-tenant media FK is rejected.

Exit criteria:

- Existing records have media rows where possible.
- Read APIs work with both new and old records.

## Phase 3: Deprecation And Stabilization

Objective: Treat `media_assets` as the source of truth while keeping old columns as compatibility fallback.

Implementation note for 2026-07-23:

- Backend upload APIs and legacy URL compatibility writes create or link `media_assets` records.
- Existing response fields stay stable for Flutter/Admin compatibility.
- Legacy URL/storage columns are deprecated and must be used only as fallback compatibility fields.
- Replacing category images or brand logos marks the previous media asset `INACTIVE`.
- Product legacy image replacement marks previously linked product image media assets `INACTIVE`.
- Azure blobs are not physically deleted during profile/update replacement in MVP; cleanup remains explicit/best-effort only after DB success.
- Phase 4 legacy column removal is still not approved.
Backend:

- New upload/update flows must create or update `media_assets`.
- Avoid using old fields as the primary source in new code.
- Continue returning existing response fields so frontend remains stable.

Operations:

- Monitor missing media FK cases.
- Monitor broken image URLs.
- Confirm delete/replace cleanup rules for Azure blobs.

Documentation:

- Mark old image URL/storage columns as deprecated in project docs.
- Document that `media_assets` is the source of truth.

Exit criteria:

- New records consistently use `media_assets`.
- All major read APIs prefer `media_assets`.
- Old fields are only fallback/compatibility.

## Phase 4: Optional Legacy Column Removal

Objective: Remove duplicate legacy columns only after the system is stable.

Do not start this phase until:

- Existing image data is fully backfilled.
- Frontend no longer depends on old database fields.
- API responses are stable.
- Seed data uses `media_assets`.
- Automated tests cover reads, uploads, tenant isolation, and fallback removal.
- A rollback plan exists.

Columns to review for removal:

| Table | Legacy attributes to review |
|---|---|
| `product_images` | `image_storage_key`, `image_url`, `mime_type`, `file_size_bytes`, `width_px`, `height_px`, `checksum_hash` |
| `categories` | `image_url` |
| `brands` | `logo_url` |
| `product_option_values` | `image_url` |
| `product_option_template_values` | `image_url` |
| `storefront_banners` | `image_url` |
| `tenant_profiles` | `logo_url` |
| `return_inspection_media_staging` | `storage_key`, `file_name`, `content_type`, `size_bytes` |
| `return_inspection_media` | `storage_key`, `file_name`, `content_type`, `size_bytes` |

Special case:

- `tenant_users.profile_image_url` must be reviewed separately because current documentation/code indicates UUID-style behavior despite the name ending in `_url`.

Recommended decision:

- Do not remove legacy columns during MVP unless there is a strong reason.
- Remove only after one or more stable releases using `media_assets` as the source of truth.

## Safety Rules

- Do not trust frontend-provided `tenant_id`.
- Do not store Azure secrets in database rows.
- Do not store SAS tokens in `public_url`.
- Do not expose raw storage credentials.
- Do not allow cross-tenant media attachment.
- Do not delete Azure blobs before DB transaction success is confirmed.
- Use soft-delete/status first where recovery matters.

## Summary Recommendation

Use `media_assets` as the long-term image metadata source of truth.

For MVP safety:

1. Add the table and nullable FK columns.
2. Keep existing image fields.
3. Write both new media metadata and old display fields.
4. Switch reads gradually.
5. Remove old columns only after the system is proven stable.
