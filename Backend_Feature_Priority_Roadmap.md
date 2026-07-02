<!-- title: Backend Feature Priority Roadmap -->
<!-- status: Active -->
<!-- system: TM-EPOS MVP -->
<!-- last_updated: 2026-07-01 -->

# Backend Feature Priority Roadmap

## Purpose

This note records the recommended backend feature order for TM-EPOS MVP after reading the current Second Brain source-of-truth files.

This is a delivery sequence guide only. Before implementing any feature, read the related module overview, functional rules, technical contract, database table file, backend architecture files, and testing template.

## Source Files Read

| Area | Files |
|---|---|
| Source of truth | `00_START_HERE/Current_Source_Of_Truth.md` |
| Release scope | `01_RELEASE_SCOPE/Release_1_Scope.md`, `Included_Features.md`, `Excluded_Features.md`, `EPOS_Technology_Stack.md` |
| Backend architecture | `Authentication.md`, `Authorization_And_Permissions.md`, `Multi_Tenant_Handling.md` |
| Module knowledge | Platform Administration, Tenant Foundation, Subscription Catalog, Tenant User Permission Access, Outlet/Till/POS Device Foundation, Catalog Master Data, Product Core, Pricing Tax, Discount, Inventory, Unified Order Sales, POS Operations, Online Store Cart Checkout, Fulfilment Pickup ClickCollect, Offline Operation Sync |
| Implementation tracking | `Platform_Admin_Login_Implementation_Status.md`, `Tenant_Login_Implementation_Status.md`, Backend tracking files for Subscription, Tenant Outlet/Till, Sales |

## Current Confirmed Backend Status

| Feature | Status | Notes |
|---|---|---|
| Platform Admin Login | Completed | Current TM-EPOS tracking exists and tests are marked passed. |
| Tenant Login | Completed | Current TM-EPOS tracking exists, migration/database update applied, 18 tests passed. |
| Subscription backend tracking | Revalidate | Existing note is old `SCS-TIX` wording and must be checked against current `E_POS` code before treating as complete. |
| Outlet Create tracking | Revalidate | Existing note is old `SCS-TIX` wording and old paths. |
| Till Create tracking | Revalidate | Existing note is old `SCS-TIX` wording and old paths. |
| Create Sale tracking | Revalidate | Existing note says in progress and uses old `SCS` paths. Do not treat as current TM-EPOS completed work. |

## Priority Principles

- Follow the MVP flow: tenant/plan setup, product and inventory setup, users/outlets/tills/devices, POS selling, online store, checkout, fulfilment/pickup, reporting.
- Backend must remain final authority for tenant isolation, permissions, entitlements, final sale totals, payments, refunds, inventory, offline sync, and audit.
- Do not implement excluded scope: delivery management, franchise/chain management, supplier purchasing, stock transfer, full accounting, AI features, kiosk, Redis dependency, CQRS/MediatR.
- Treat old `SCS-TIX` implementation notes as migration clues only until verified against current `E_POS` code and TM-EPOS scope.

## Recommended Backend Priority Order

| Priority | Feature Area | Why This Comes Next | Main Modules |
|---:|---|---|---|
| P0 | Auth login foundation | Already completed for platform and tenant login. Keep as baseline. | `01_Platform_Administration`, `06_Auth_Tokens_Security_Audit` |
| P1 | Auth session completion | Add refresh token, logout, session revoke/reuse detection, and current user/context endpoints before many protected APIs depend on auth state. | `06_Auth_Tokens_Security_Audit` |
| P2 | Authorization foundation | Add reusable permission, entitlement, tenant status, outlet access, and operational-context checks so every later feature uses one security pattern. | `05_Tenant_User_Permission_Access`, backend architecture access rules |
| P3 | Tenant foundation | Platform admin must be able to create/activate tenants, tenant profile/address/domain/settings, and tenant admin setup before business operations. | `02_Tenant_Foundation`, `01_Platform_Administration` |
| P4 | Subscription and entitlements | Tenant access to POS, online store, inventory, reports, and offline features depends on plan/features/limits/entitlements. | `03_Subscription_Catalog_Entitlements`, `04_Subscription_Billing_Usage` |
| P5 | Tenant users, roles, permissions, outlet access | Business admin needs staff creation, role assignment, permission catalog, direct permissions, and outlet-scoped access before POS operations. | `05_Tenant_User_Permission_Access` |
| P6 | Outlet, till, POS device foundation | POS cannot safely start selling until tenant outlet, till, trusted device, and assignment rules exist. | `07_Outlet_Till_POS_Device_Foundation` |
| P7 | Catalog master data and product core | Product/category/brand/UOM/product/variant/barcode/channel visibility are required before price, stock, POS search, and online store. | `09_Catalog_Master_Data`, `10_Product_Core`, `11_Product_Media_Attributes_Channel_Visibility`, `12_Product_Option_Variant_Configuration` |
| P8 | Pricing, tax, and discount setup | POS and checkout totals require backend-controlled price lists, tax classes/rates, product tax assignment, and discount policies. | `14_Pricing_Tax_Management`, `15_Discount_Expiry_Discount_Management` |
| P9 | Inventory foundation and stock availability | Stock availability, batches, low-stock alerts, and channel allocation are needed before reliable POS/online selling. | `16_Inventory_Foundation_Stock_Availability`, `17_Reservations_Stock_Movements_Serial_Cost` |
| P10 | POS sale path and unified order | Build the core revenue path: POS product lookup, basket validation, sale/order creation, order line snapshots, receipt/till links. | `20_Unified_Order_Sales`, `21_POS_Operations` |
| P11 | Payment then refund | Cash payment can come first; card/QR uses provider handoff. Refund must be backend validated and should follow stable payment/order records. | `24_Payment_Refund`, backend split modules `Payment` and `Refund` |
| P12 | Online store cart and checkout | Customer website needs product visibility, pricing/tax, stock, cart, checkout session, and backend total recalculation. | `22_Online_Store_Cart_Checkout` |
| P13 | Fulfilment and click & collect | Click and collect depends on confirmed orders, pickup outlets, pickup slots, fulfilment orders, and pickup status history. | `23_Fulfilment_Pickup_ClickCollect` |
| P14 | Offline operation and sync | Offline client registration, number blocks, sync batches/items, idempotency, mapping, and conflict handling should wrap stable POS/order/payment rules. | `28_Offline_Operation_Sync` |
| P15 | Reporting, notification, integration hardening | Reports, notifications, provider events, hardware logs, and integration support become stronger after transaction flows exist. | `26_Notification`, `27_Reporting_Analytics`, `Platform-Level Integration Core` |

## Best Next Feature To Start

Start with **Auth Session Completion + Current User/Tenant Context**.

Reason: platform and tenant login already exist, but future protected APIs need consistent refresh/logout/session validation, tenant context resolution, permission claims, and current user context before modules like tenant setup, roles, outlet, product, and POS are built.

Suggested feature name:

```text
Module: Auth
Feature: Auth Refresh Logout And Current Context
```

## Files To Read For The Next Feature

```text
04_MODULE_KNOWLEDGE/06_Auth_Tokens_Security_Audit/01_Module_Overview.md
04_MODULE_KNOWLEDGE/06_Auth_Tokens_Security_Audit/02_Functional_Rules.md
04_MODULE_KNOWLEDGE/06_Auth_Tokens_Security_Audit/03_Technical_Contract.md
05_BACKEND_ARCHITECTURE/Authentication.md
05_BACKEND_ARCHITECTURE/Authorization_And_Permissions.md
05_BACKEND_ARCHITECTURE/Multi_Tenant_Handling.md
05_BACKEND_ARCHITECTURE/API_Standards.md
05_BACKEND_ARCHITECTURE/Error_Response_Standards.md
06_DATABASE_KNOWLEDGE/Tables/01_Platform_Administration.md
06_DATABASE_KNOWLEDGE/Tables/[tenant user/auth related table file]
10_TESTING_QA/Test_Case/_Feature_Test_Case_Template.md
10_TESTING_QA/Permission_Test_Cases.md
10_TESTING_QA/Tenant_Isolation_Test_Cases.md
10_TESTING_QA/Idempotency_Test_Cases.md
```

## Review Reminder

For each feature, update:

```text
10_TESTING_QA/Test_Case/[MODULE_FOLDER_NAME]/[FEATURE_NAME]_Test_Cases.md
15_IMPLEMENTATION_TRACKING/Backend/[MODULE_FOLDER_NAME]/[FEATURE_NAME]_Implementation_Status.md
```

Do not mark a feature completed unless implementation is done, tests are written, relevant tests are run, failures are fixed or documented, and Second Brain tracking is updated.
