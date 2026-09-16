# Product Option Seed Migration FK Blocker Closure

Date: 2026-08-26
Scope: Product-option seed migration blocker only.

## Root Cause

- Exact failing test: `E_POS.IntegrationTests.PlatformAdministration.ManualPaymentPostgreSqlConcurrencyTests.Migrations_ApplyToCleanPostgreSqlDatabase`.
- Test project: `tests/E_POS.IntegrationTests/E_POS.IntegrationTests.csproj`.
- PostgreSQL error: `23503` foreign-key violation.
- Constraint: `fk_product_options_source_option_template_id_product_option_tem`.
- Child: `product_options.source_option_template_id`.
- Missing parent: `product_option_templates.id`.
- Failing key: `d0000000-0000-4000-8000-000000000101` (`SIZE`).
- Invalid child insertion: `20260710143000_SeedDevelopmentVariableProductCatalog` through `DevelopmentVariableProductCatalogSeedData.UpSql`.
- Required parent insertion previously occurred only in `20260813063333_SeedReferenceProductOptionTemplates` through `ReferenceProductOptionTemplateSeedData.UpSql`.
- Classification: **E. Historical migration inconsistency**. The development variable catalog migration was ordered before the reference option-template and template-value master seed it referenced.

## Fix

- Added `20260710142500_SeedReferenceProductOptionsBeforeVariableCatalog`.
- The corrective migration runs immediately before `20260710143000_SeedDevelopmentVariableProductCatalog` and reuses the existing idempotent reference seed SQL.
- The historical failing migration and its seed helper were not rewritten.
- Existing databases that already contain the master rows receive safe `ON CONFLICT (id) DO UPDATE` behavior.
- The correction does not weaken, remove, defer, or disable any FK.
- `Down` is non-destructive because the fixed master IDs may pre-exist or be referenced by later product data.

## Seed Integrity

Dedicated PostgreSQL validation checks zero orphans for:

- `product_options.product_id -> products.id`;
- `product_options.source_option_template_id -> product_option_templates.id`;
- `product_option_values.product_option_id -> product_options.id`;
- `product_option_values.source_option_template_value_id -> product_option_template_values.id`;
- `product_variant_option_values.product_variant_id -> product_variants.id`;
- `product_variant_option_values.product_option_id -> product_options.id`;
- `product_variant_option_values.product_option_value_id -> product_option_values.id`.

## Verification

### Original Failing Test

Before correction: failed at `20260710143000_SeedDevelopmentVariableProductCatalog` with PostgreSQL `23503`.

After correction: the migration chain passes the original product-option migration and proceeds to `20260812105504_SeedStorefrontAzureMediaAssets`, where a different historical duplicate-key problem is exposed. The original product-option FK failure no longer occurs.

### Product-Option Focused Tests

- `ReferenceProductOptionTemplateSeedTests`: **4 passed, 0 failed**.
- `ProductOptionSeedMigrationPostgreSqlTests`: **1 passed, 0 failed**.
- Clean migration to `20260710143000_SeedDevelopmentVariableProductCatalog`: **PASS**.
- Simulated existing-database reapplication with the corrective history row absent: **PASS**.
- Seven product option/variant orphan checks: **PASS, all zero**.

### User Creation Protection

- User Creation Unit: **70 passed, 0 failed**.
- User Creation API: **8 passed, 0 failed**.
- User Creation PostgreSQL: **14 passed, 0 failed**.
- User Creation production logic changed by this blocker fix: **NO**.
- User Creation migration `20260826120000_AddTenantUserExplicitOutletTillAccess` changed: **NO**.
- Permission/delegation behavior changed: **NO**.

### Build and Model

- Solution build: **PASS**, 0 errors; 5 unrelated existing warnings.
- EF pending model changes: **NONE**.
- `git diff --check`: **PASS**; line-ending notices only.

### Full Backend Suite

Latest full execution:

- Unit: 1163 passed, 0 failed.
- API: 481 passed, 0 failed.
- Integration: 572 passed, 5 failed.
- Flow4 fixture CLI: 17 passed, 0 failed.
- Local print agent: 50 passed, 0 failed.
- Total: **2283 passed, 5 failed**.

Four integration failures were PostgreSQL `53200 out of shared memory` transients during parallel database fixture creation/cleanup. All four passed when rerun together in isolation: **4 passed, 0 failed**.

The remaining deterministic failure is the clean migration test at `20260812105504_SeedStorefrontAzureMediaAssets`, PostgreSQL `23505`, constraint `pk_media_assets`, duplicate ID `eeee0001-0001-4000-8000-000000000001`. That ID was already inserted by `20260812103455_AddPromoBannersSeed` through `DevelopmentStorefrontSeedData.UpSql`. This separate media seed blocker is outside the product-option-only correction scope.

## Scope Audit

- Product-option historical migration rewritten: **NO**.
- Forward corrective migration added: **YES**.
- Product-option regression tests added/updated: **YES**.
- User Creation production logic changed: **NO**.
- User Creation migration changed: **NO**.
- Permission/delegation behavior changed: **NO**.
- Flutter or Angular changed: **NO**.
- Unrelated schema changes: **NO**.

## Remaining Blocker

The repository-wide clean PostgreSQL chain still cannot reach latest because `20260812105504_SeedStorefrontAzureMediaAssets` inserts a `media_assets.id` already inserted by `20260812103455_AddPromoBannersSeed`. This must be corrected in a separate media/storefront seed migration task before the full backend suite can be fully green.

## Final Verdict

`CORRECTED 5-STEP USER CREATION BACKEND STILL HAS BLOCKERS`
