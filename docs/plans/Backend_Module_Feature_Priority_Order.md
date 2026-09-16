<!-- title: Backend Module Feature Priority Order -->
<!-- status: Active -->
<!-- system: TM-EPOS MVP -->
<!-- last_updated: 2026-07-01 -->

# Backend Module Feature Priority Order

## Purpose

This note breaks the TM-EPOS backend into module-wise feature priorities.

Use this before starting a new backend feature so the next task follows the MVP dependency order instead of randomly selecting tables or APIs.

## Source Basis

This priority order is based on:

- `00_START_HERE/Current_Source_Of_Truth.md`
- `01_RELEASE_SCOPE/Release_1_Scope.md`
- `01_RELEASE_SCOPE/Included_Features.md`
- `01_RELEASE_SCOPE/Excluded_Features.md`
- `01_RELEASE_SCOPE/EPOS_Technology_Stack.md`
- `05_BACKEND_ARCHITECTURE/Authentication.md`
- `05_BACKEND_ARCHITECTURE/Authorization_And_Permissions.md`
- `05_BACKEND_ARCHITECTURE/Multi_Tenant_Handling.md`
- `04_MODULE_KNOWLEDGE/*/01_Module_Overview.md`
- `15_IMPLEMENTATION_TRACKING/Backend/*`

## Delivery Rule

Do not implement a whole module at once.

Implement one feature at a time:

```text
Read related Second Brain files
Plan feature
Implement API/Application/Domain/Infrastructure
Add automated tests
Update feature test case document
Update implementation tracking
Run relevant dotnet test command
```

## Global Priority Phases

| Phase | Focus | Reason |
|---:|---|---|
| P0 | Completed auth login foundation | Platform Admin Login and Tenant Login are already completed. |
| P1 | Auth session + current context | Future protected APIs need refresh/logout/session/current-user context. |
| P2 | Authorization foundation | Permissions, entitlements, tenant status, outlet access, and policies must be reusable. |
| P3 | Platform, tenant, subscription setup | A business cannot operate before tenant, plan, and entitlement setup. |
| P4 | Users, outlets, tills, devices | POS cannot safely operate without staff, outlet, till, and trusted device context. |
| P5 | Catalog, product, pricing, inventory | Selling requires sellable products, prices, tax, discount, and stock availability. |
| P6 | POS sale, payment, receipt | Core revenue flow comes after setup data is stable. |
| P7 | Online store, checkout, click & collect | Customer ordering depends on product, price, inventory, order, payment, and fulfilment foundations. |
| P8 | Offline sync, reports, notifications | These depend on stable operational events and transaction records. |

## Module Wise Feature Priorities

### 01 Platform Administration

| Order | Feature | Notes |
|---:|---|---|
| 1 | Platform current user/context | Protected platform APIs need identity, session, and permissions. |
| 2 | Platform user list/detail/create/update/status | Platform admin user management after login. |
| 3 | Platform role and permission management | Role-permission grouping for platform admin actions. |
| 4 | Platform settings | SaaS-level settings after users/permissions are stable. |
| 5 | Platform audit log read APIs | Needed for support and security review. |
| 6 | Platform tenant support actions | Only after tenant foundation exists and audit is enforced. |

### 02 Tenant Foundation

| Order | Feature | Notes |
|---:|---|---|
| 1 | Tenant create | Platform admin creates tenant root record. |
| 2 | Tenant profile/address/business type/currency | Required before outlet, product, and billing setup. |
| 3 | Tenant status activation/suspension | Tenant status controls admin, POS, online store, checkout, and offline sync. |
| 4 | Tenant domain/storefront context | Needed before public online store APIs. |
| 5 | Tenant settings | Use typed setting definitions, not random key-value behavior. |
| 6 | Tenant admin bootstrap/invite handoff | Connects tenant foundation to staff auth/user access. |

### 03 Subscription Catalog, Plans, Add-ons And Entitlements

| Order | Feature | Notes |
|---:|---|---|
| 1 | Platform modules/features catalog | Required source for entitlements and feature checks. |
| 2 | Subscription plan create/update/list/publish | Platform admin plan builder. |
| 3 | Plan feature and limit assignment | Controls what a tenant can use. |
| 4 | Add-on catalog and add-on feature assignment | Only after base plan flow is stable. |
| 5 | Tenant entitlement assignment | Connects tenant to enabled features. |
| 6 | Entitlement check service/API | Must be reused by protected feature APIs. |
| 7 | Feature flags | Add after entitlement model is working. |

### 04 Subscription Billing, Payments And Usage

| Order | Feature | Notes |
|---:|---|---|
| 1 | Tenant subscription create/activate | Tenant needs active subscription state. |
| 2 | Subscription status/history | Trial, active, past due, cancelled, expired behavior. |
| 3 | Invoice and invoice line generation | Billing summary foundation. |
| 4 | Payment link generation | Store only token hashes. |
| 5 | Subscription payment transaction record | Separate from POS/customer payments. |
| 6 | Usage counters | Required for plan limits and billing periods. |
| 7 | Credit notes | Later billing correction flow, not first path. |

### 05 Tenant Users, Roles, Permissions And Outlet Access

| Order | Feature | Notes |
|---:|---|---|
| 1 | Permission catalog read API | UI and backend need stable permission definitions. |
| 2 | Tenant role CRUD | Roles are permission groups, not hardcoded behavior. |
| 3 | Tenant role permission assignment | Enables role-based access. |
| 4 | Tenant user CRUD/status | Admin creates and controls staff users. |
| 5 | User role assignment | Staff access starts here. |
| 6 | Direct user permissions | For overrides after role flow is stable. |
| 7 | Outlet-scoped roles/permissions | Required before multi-outlet POS restrictions. |
| 8 | Tenant admin context endpoint | Return current user permissions/features/outlet access safely. |

### 06 Auth, Tokens And Security Audit

| Order | Feature | Notes |
|---:|---|---|
| 1 | Tenant login | Completed. |
| 2 | Refresh token | Add expiry/revoke/reuse protection before long sessions. |
| 3 | Logout and session revoke | Required for security and support. |
| 4 | Current user/session context | Needed by mobile POS and admin clients after login. |
| 5 | Staff invitation and setup token | Needed for tenant user onboarding. |
| 6 | Password reset and email verification | Add after invite/setup flow. |
| 7 | Auth/session audit read APIs | Security review and troubleshooting. |

### 07 Outlet, Till And POS Device Foundation

| Order | Feature | Notes |
|---:|---|---|
| 1 | Outlet CRUD | Tenant business location foundation. |
| 2 | Outlet address and business hours | Used by POS, online store, and pickup. |
| 3 | Till CRUD | POS needs till assignment. |
| 4 | POS device registration/pairing | Device trust boundary. |
| 5 | Till-device assignment | Prevents random device/till usage. |
| 6 | Hardware profile link | Connects device/till setup to hardware operations. |
| 7 | POS context selection API | Mobile app chooses active outlet/till/device context safely. |

### 08 Hardware Operations, Till Session And Cash Control

| Order | Feature | Notes |
|---:|---|---|
| 1 | Till open/current session | POS sale and cash movement require open till session. |
| 2 | Cash movement type seed/list | Required before cash in/out. |
| 3 | Cash in/out movement | Permission, till context, and audit required. |
| 4 | Hardware device assignment/test log | Backend records config and results; app/local service talks to devices. |
| 5 | Till close/reconciliation | Backend validates final close. |
| 6 | Cash count denominations | Add after close/reconciliation base flow. |

### 09 Catalog Master Data

| Order | Feature | Notes |
|---:|---|---|
| 1 | Business type and UOM seed/list | Speeds tenant setup and product setup. |
| 2 | Department/category CRUD | Required for product organization. |
| 3 | Brand CRUD | Product filtering and admin setup. |
| 4 | Collection CRUD | Online/POS grouping after product basics. |
| 5 | Return policy setup | Needed before return/exchange behavior. |
| 6 | Catalog filter/reference APIs | Used by product forms and storefront filters. |

### 10 Product Core

| Order | Feature | Notes |
|---:|---|---|
| 1 | Product create/list/detail/update/status | Core sellable item foundation. |
| 2 | Product variant create/update | Variant is sellable identity. |
| 3 | SKU and identifier validation | Must be tenant-safe. |
| 4 | POS product search/read API | POS product grid and barcode workflows depend on this. |
| 5 | Storefront product read API | Online store depends on product visibility, price, and stock checks. |
| 6 | Product activation/deactivation rules | Inactive products cannot be sold. |

### 11 Product Media, Attributes And Channel Visibility

| Order | Feature | Notes |
|---:|---|---|
| 1 | Product category/collection links | Product organization after product core exists. |
| 2 | Product image metadata | Binary files go to object storage; DB stores metadata. |
| 3 | Product barcode management | Barcode lookup for POS. |
| 4 | Attribute definitions/options | Storefront filters and variant display support. |
| 5 | Product attribute values | Link product data to attributes. |
| 6 | Channel visibility | Explicit POS/online/click-and-collect publishing. |
| 7 | Product publish validation | Online-visible product still needs price, stock policy, and fulfilment support. |

### 12 Product Option Templates And Variant Configuration

| Order | Feature | Notes |
|---:|---|---|
| 1 | Option template/value CRUD | Size, color, portion, type setup. |
| 2 | Business type default options | Faster onboarding for food, beverage, merchandise. |
| 3 | Product option assignment | Product-level option configuration. |
| 4 | Product option values | Allowed values per product. |
| 5 | Variant option mapping | Connects variant identity to selected option values. |
| 6 | POS/storefront option read APIs | Used by product selection screens. |

### 13 Product Combo, Choice Options And Inventory Impact

| Order | Feature | Notes |
|---:|---|---|
| 1 | Combo definition/component setup | Base bundle model. |
| 2 | Combo groups/items | Required for grouped combo choices. |
| 3 | Choice group/option setup | Cashier/customer selection options. |
| 4 | Product choice group assignment | Attach choice rules to products. |
| 5 | Choice inventory impact | Reduce correct ingredient/product stock. |
| 6 | Combo selection validation/snapshot | Needed before POS/order checkout uses combos. |

### 14 Pricing And Tax Management

| Order | Feature | Notes |
|---:|---|---|
| 1 | Price list CRUD | Base pricing foundation. |
| 2 | Price list item assignment | Variant/product pricing. |
| 3 | Outlet/channel price assignment | Price can vary by outlet/channel. |
| 4 | Tax jurisdiction/class/rate setup | Required before tax calculation. |
| 5 | Product tax assignment | Link product/variant to tax rules. |
| 6 | Price/tax calculation API | POS and checkout need backend validated totals. |
| 7 | Snapshot support for orders | Order lines snapshot price and tax facts. |

### 15 Discount And Expiry Discount Management

| Order | Feature | Notes |
|---:|---|---|
| 1 | Discount type seed/list | Discount policy foundation. |
| 2 | Discount policy CRUD | Scope, limits, status, approval requirement. |
| 3 | Outlet/channel targeting | Control where discounts apply. |
| 4 | Policy target/condition rules | Product/category/order-level rules. |
| 5 | POS discount validation/apply | Must snapshot policy and approval result. |
| 6 | Manager approval rules | Required when cashier exceeds allowed limit. |
| 7 | Expiry discount rules/tiers/applications | Add after batch/expiry inventory foundation exists. |

### 16 Inventory Foundation, Product Tracking And Stock Availability

| Order | Feature | Notes |
|---:|---|---|
| 1 | Inventory location setup | Stock belongs to tenant/outlet/location context. |
| 2 | Product inventory settings | Tracking, reorder, batch/serial rules. |
| 3 | Product batches | Required for expiry tracking. |
| 4 | Inventory balances | Availability read foundation. |
| 5 | Channel allocation | Reserve stock for POS, online store, click and collect. |
| 6 | Reorder rules and alerts | Low stock MVP reporting/notification input. |
| 7 | Serial number and cost layer setup | Add when product tracking needs it. |

### 17 Reservations, Stock Movements, Serial And Cost Allocation

| Order | Feature | Notes |
|---:|---|---|
| 1 | Append-only stock movement service | All stock changes should go through a controlled ledger. |
| 2 | Movement references | Link movement to sale, return, adjustment, sync, etc. |
| 3 | Inventory reservations | Protect stock during cart, checkout, click collect, or held POS flows. |
| 4 | Reservation allocations | Allocate reserved quantity to batches/locations. |
| 5 | Serial movement records | Required for serial-tracked products. |
| 6 | Cost allocation records | Separate from sale price and accounting ledger. |

### 18 Stock Adjustment, Transfer And Stocktake

| Order | Feature | Notes |
|---:|---|---|
| 1 | Stock adjustment reason setup | Adjustment requires reason and audit. |
| 2 | Stock adjustment create/approve/apply | Included inventory operation. |
| 3 | Adjustment stock movement integration | Must write append-only movement records. |
| 4 | Stocktake session and count lines | Inventory count/variance workflow. |
| 5 | Serial stocktake lines | Only for serial-tracked products. |
| 6 | Stock transfer | Scope conflict exists: excluded file says stock transfer is excluded unless approved; confirm before implementing. |

### 19 Customer Basic, Authentication And Consent

| Order | Feature | Notes |
|---:|---|---|
| 1 | Customer basic record create/search | POS customer attach and online order identity. |
| 2 | Guest/customer checkout support | Customer account should not be mandatory unless business rule requires it. |
| 3 | Customer auth register/login | Separate from tenant/platform auth. |
| 4 | OTP/email verification/password reset | Add after customer auth exists. |
| 5 | Customer sessions and refresh tokens | Token hashes only. |
| 6 | Customer consent capture | Required for marketing/privacy-sensitive communication. |

### 20 Unified Order And Sales

| Order | Feature | Notes |
|---:|---|---|
| 1 | Document number sequence service | Controlled order/sale numbering. |
| 2 | POS sales order create | First revenue/order backend path. |
| 3 | Sales order lines/options/components | Snapshot product and selected options. |
| 4 | Discounts/taxes/charges snapshot | Preserve checkout facts. |
| 5 | Sales order status history | Append-only audit of state. |
| 6 | Order list/detail/tracking | Used by POS, admin, fulfilment, customer surfaces. |
| 7 | Online checkout to order conversion | After storefront checkout is stable. |

### 21 POS Operations

| Order | Feature | Notes |
|---:|---|---|
| 1 | POS home/bootstrap context | Return outlet, till, device, permissions, cached reference hints. |
| 2 | Held sale/park and recall | Included offline/POS continuity feature. |
| 3 | Receipt template/read API | Required before receipt creation/print logs. |
| 4 | Receipt create and print log | Print failure must not cancel completed sale. |
| 5 | Till session events/summaries | Sales/payment/cash summary support. |
| 6 | POS operational event log | Useful for troubleshooting and audit. |

### 22 Online Storefront, Cart And Checkout

| Order | Feature | Notes |
|---:|---|---|
| 1 | Storefront tenant/channel resolver | Public reads must resolve tenant safely. |
| 2 | Storefront catalog/product search | Only active channel-visible products. |
| 3 | Shopping cart create/update/read | Browser customer cart. |
| 4 | Checkout session create/update | Temporary until converted to backend order. |
| 5 | Server-side checkout total recalculation | Backend validates price, tax, stock, fulfilment, payment readiness. |
| 6 | Checkout event tracking | Audit and troubleshooting. |
| 7 | Order conversion/payment handoff | After cart/checkout totals are stable. |

### 23 Fulfilment, Pickup And Click & Collect

| Order | Feature | Notes |
|---:|---|---|
| 1 | Fulfilment method setup | Pickup/click-and-collect foundation. |
| 2 | Fulfilment method outlet assignment | Outlet must support the method. |
| 3 | Pickup slot setup | Customer collection time selection. |
| 4 | Pickup slot reservation | Protect capacity until order confirmation. |
| 5 | Fulfilment order create/status/events | Staff preparation workflow. |
| 6 | Pickup order ready/collected/events | Backend-confirmed pickup state. |
| 7 | Notification hooks | Pickup ready/customer notification after notification module basics. |

### 24 Payment And Refund

The Second Brain module is currently documented as `Payment_Refund`, while backend folders were split into `Payment` and `Refund`. Use the split boundaries in backend code.

| Order | Feature | Backend Boundary | Notes |
|---:|---|---|---|
| 1 | Payment method setup | Payment | Cash/card/QR/split method configuration. |
| 2 | Cash sales payment | Payment | First MVP payment path. |
| 3 | Payment transaction/event records | Payment | Provider/device outcome history. |
| 4 | Card/QR payment handoff references | Payment | Never store sensitive card data. |
| 5 | Split payment allocation support | Payment | After single payment flow is stable. |
| 6 | Refund validation | Refund | Refund amount cannot exceed refundable amount. |
| 7 | Sales refund create/lines | Refund | Backend-validated refund record. |
| 8 | Refund payment allocations | Refund | Link refund to original payment allocation. |
| 9 | Refund idempotency/audit | Refund | Required before exposed refund APIs are widely used. |

### 25 Return, Inspection And Exchange

| Order | Feature | Notes |
|---:|---|---|
| 1 | Return reason setup | Needed before return create. |
| 2 | Sales return create | Must reference original sale/order. |
| 3 | Return line quantity validation | Cannot exceed sold and not-yet-returned quantity. |
| 4 | Return inspection capture | Condition and disposition before restock/scrap/reject. |
| 5 | Return events | Append-only return history. |
| 6 | Exchange create/lines/events | After return/payment/order foundations. |
| 7 | Refund and inventory movement integration | Connects to refund and stock modules. |

### 26 Notification

| Order | Feature | Notes |
|---:|---|---|
| 1 | Notification channel/event type seed/list | Foundation for all notifications. |
| 2 | Notification template/version management | Preserve sent-message history. |
| 3 | Notification preferences | Control channel eligibility. |
| 4 | Notification event record | Business event driven, not random UI trigger. |
| 5 | Message create and delivery attempt tracking | Append-only and retry-safe. |
| 6 | Inbox item/read receipt | In-app notifications. |
| 7 | Business event hooks | Pickup ready, low stock, expiry alert, auth/security events. |

### 27 Reporting And Analytics

| Order | Feature | Notes |
|---:|---|---|
| 1 | Dashboard summary API | Backend projection, no hardcoded UI cards. |
| 2 | Sales report | Tenant/outlet/channel/permission scoped. |
| 3 | Payment report | Cash/card/QR/split summaries. |
| 4 | Inventory report | Stock balances, low stock, movement summary. |
| 5 | Order and click-collect report | Online/POS channel filters. |
| 6 | Report export jobs | For large reports. |
| 7 | Daily summary projections | Add after enough transaction data exists. |

### 28 Offline Operation And Sync

| Order | Feature | Notes |
|---:|---|---|
| 1 | Offline client registration/approval | Device/client trust boundary. |
| 2 | Device sync state | Track last sync and client status. |
| 3 | Offline number blocks | Safe local numbering for offline records. |
| 4 | Sync batch/item intake | Validate tenant, outlet, device, payload, and idempotency. |
| 5 | Offline ID mapping | Map client records to server IDs safely. |
| 6 | Conflict creation/resolution | Backend resolves or rejects conflicts. |
| 7 | Sync status API | Pending, accepted, rejected, conflict counts for client. |
| 8 | Offline sale sync | Only after POS sale/order/payment/cash rules are stable. |

## Best Immediate Next Backend Features

| Order | Feature | Module | Reason |
|---:|---|---|---|
| 1 | Auth Refresh Logout And Current Context | Auth | Login exists; sessions must be completed before many protected APIs. |
| 2 | Permission Entitlement Tenant Context Guard | Access/Auth/Tenant | Shared authorization foundation for all future APIs. |
| 3 | Tenant Create And Activate | Tenant Foundation | Required before real tenant/business setup. |
| 4 | Tenant Role Permission User Management | Tenant User Permission Access | Required before staff can safely use admin/POS. |
| 5 | Outlet Till Device Foundation | Outlet/Till/POS Device | Required before POS operations. |
| 6 | Product Core Create/List/Variant | Product Core | Required before pricing, inventory, POS, online store. |
| 7 | Price Tax Basic Setup | PricingTax | Required before sale/checkout totals. |
| 8 | Inventory Location Balance Basic Setup | Inventory | Required before stock-aware selling. |
| 9 | POS Sale With Cash Payment And Receipt | Order/POS/Payment | First end-to-end MVP selling path. |
| 10 | Online Store Cart Checkout Click Collect | Storefront/Checkout/Fulfilment | Customer-facing path after product/order/payment basics. |

## Scope Caution

Confirm before implementing these areas:

| Area | Reason |
|---|---|
| Stock transfer | `Excluded_Features.md` says stock transfer is excluded unless approved, while module overview mentions transfer. Ask before coding. |
| Delivery management | Deferred/excluded. Do not implement driver assignment or delivery tracking. |
| Advanced promotions/coupons | Excluded. Keep discount module to discount policies and expiry discounts. |
| Full accounting/general ledger | Excluded. Payment/refund records are operational, not accounting ledger. |
| Offline final card/QR payment/refund/exchange | Excluded. Backend validation is required. |
| CQRS/MediatR/Redis | Excluded unless later approved. |

## Per Feature Documentation Rule

For every feature implemented from this roadmap, create or update:

```text
10_TESTING_QA/Test_Case/[MODULE_FOLDER_NAME]/[FEATURE_NAME]_Test_Cases.md
15_IMPLEMENTATION_TRACKING/Backend/[MODULE_FOLDER_NAME]/[FEATURE_NAME]_Implementation_Status.md
```

Use this only after tests pass:

```md
## Implementation Status

| Item | Value |
|---|---|
| Feature | [FEATURE_NAME] |
| Module | [MODULE_FOLDER_NAME] |
| Platform | Backend |
| Status | Completed |
| Completed Date | [YYYY-MM-DD] |
| Tests | Passed |
| PR / Commit | - |
```
