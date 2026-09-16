# Stripe Checkout (Hosted) Payment Gateway — Implementation Notes (2026-09-14)

Written for: engineers working on the Unified-Commerce backend and the E-commerce storefront app.

## 1. Summary

Added Stripe Checkout (Hosted) as an online payment option for the storefront's Click & Collect checkout, alongside the existing "Pay at pickup" flow. Customers choose a payment method on the review step; Stripe orders redirect to Stripe's hosted page and are confirmed asynchronously via a webhook. No existing order-placement behavior changed for "Pay at pickup" — that path is byte-for-byte what it was before.

While wiring this up against a real database and a real Stripe account, several **pre-existing, unrelated bugs and data gaps** were also found and fixed (see §7) — none of them were introduced by this change, but all of them were blocking any checkout at all, Stripe or not.

## 2. Flow

```text
Customer reviews order, picks "Pay at pickup" or "Pay online with card"
        |
        v
POST /api/v1/ecommerce/storefront/checkout/{id}/confirm  { paymentMethodCode }
        |
        v
StorefrontCheckoutConfirmationRepository.ConfirmAsync (existing transaction, unchanged for Pay-at-pickup)
        |
        +-- Pay at pickup: SalesOrder created CONFIRMED/UNPAID, exactly as before
        |
        +-- Stripe: SalesOrder created CONFIRMED/UNPAID (unchanged) +
        |           SalesPayment created PENDING +
        |           SalesPaymentTransaction created PENDING (CAPTURE)
        v
StorefrontCheckoutService.ConfirmAsync calls IOnlineCheckoutPaymentGateway.CreateCheckoutSessionAsync
        |
        v
StripeCheckoutGateway creates a real Stripe Checkout Session (card only), returns session.url
        |
        v
Angular redirects the browser to session.url (full page navigation)
        |
        +----------------------------+
        |                            |
        v                            v
Browser returns to               Stripe sends
/checkout/success                checkout.session.completed
or /checkout/cancelled           to StripeWebhookController
                                       |
                                       v
                              Verify Stripe-Signature (StripeOptions.WebhookSecret)
                                       |
                                       v
                              Idempotency: no-op if SalesPayment is no longer PENDING
                                       |
                                       v
                     SalesOrder.PaymentStatus -> PAID, SalesPayment -> PAID,
                     SalesPaymentTransaction -> SUCCEEDED
                                       |
                                       v
                  OrderPaymentSucceeded notification to customer + all active staff
```

If the session expires or the gateway call itself fails, the order is cancelled (`SalesOrder.CancelForFailedOnlinePayment`) and the customer gets the existing `OrderStatusChanged("CANCELLED")` notification.

## 3. Backend changes

### Domain (`E_POS.Domain`) — new behavior methods on existing entities, no schema changes

| File | What was added |
| --- | --- |
| `Modules/Tenant/Orders/Entities/SalesOrder.cs` | `MarkOnlinePaymentSucceeded`, `CancelForFailedOnlinePayment` |
| `Modules/Tenant/Payment/Entities/SalesPayment.cs` | `CreatePendingOnlinePayment` factory, `MarkPaid`, `MarkFailedOrCancelled` |
| `Modules/Tenant/Payment/Entities/SalesPaymentTransaction.cs` | `CreatePendingProviderCharge` factory, `MarkSucceeded`, `MarkFailed` |
| `Modules/Tenant/Payment/Entities/PaymentMethod.cs` | `MarkActiveForOnline` (the existing `Create` factory always set `IsActiveForOnline = false`) |

**Important constraint discovered late:** `sales_orders.payment_status`, `sales_payment_transactions.transaction_type` etc. have fixed Postgres `CHECK` constraints. The order-level status intentionally **stays `"UNPAID"`** for the whole time a Stripe payment is in flight — there is no `AWAITING_PAYMENT` value in the schema. The real "payment in flight" signal is `SalesPayment.PaymentStatus = "PENDING"` (that table's constraint already allows `PENDING`). `SalesPaymentTransaction.TransactionType` uses the existing `"CAPTURE"` value, not a new `"CHARGE"` value. Do not reintroduce new status string literals here without checking the constraint first (`SELECT pg_get_constraintdef(oid) FROM pg_constraint WHERE conrelid = '<table>'::regclass AND contype = 'c'`).

### Application (`E_POS.Application`)

New: `Modules/ECommerce/CartCheckout/Payment/`
- `Contracts/IOnlineCheckoutPaymentGateway.cs` — the gateway abstraction (`CreateCheckoutSessionAsync`). No such abstraction existed before; `ICardPaymentGateway` is shaped for in-person POS terminal card payments and isn't reusable for a redirect flow.
- `Contracts/IOnlineCheckoutPaymentConfirmationRepository.cs` / `IOnlineCheckoutPaymentConfirmationService.cs` — used by the webhook path to apply the paid/expired transitions and fire notifications.
- `Services/OnlineCheckoutPaymentConfirmationService.cs` — orchestrates repository call + notifications.
- `Services/UnavailableOnlineCheckoutPaymentGateway.cs` — no-op default, mirrors `UnavailableCardPaymentGateway`, used by the service's test-friendly constructor.

Changed:
- `CartCheckout/Contracts/IStorefrontCheckoutRepository.cs` / `IStorefrontCheckoutService.cs` — `ConfirmAsync` now takes a `paymentMethodCode`; added `CancelAwaitingOnlinePaymentAsync`.
- `CartCheckout/Dtos/StorefrontCheckoutModels.cs` — added `StorefrontPaymentMethodCodes` (`STRIPE` / `PAY_AT_PICKUP`), `ConfirmStorefrontCheckoutRequest`, `PaymentRedirectUrl` on `StorefrontCheckoutReadModel`, `PaymentId` on `StorefrontCheckoutOrderReadModel`.
- `CartCheckout/Services/StorefrontCheckoutService.cs` — `ConfirmAsync` branches on payment method; for Stripe it calls the gateway after the repository commits and rolls the order back to cancelled if the gateway call fails. The existing `OrderPlaced` notifications are **not** fired for the Stripe path at confirm time (the order isn't actually paid yet) — `OrderPaymentSucceeded` fires later from the webhook instead.
- `CustomerOrders/Notifications/ECommerceOrderNotificationFactory.cs` — added `OrderPaymentSucceeded` / `OrderPaymentSucceededForStaff`, same pattern as the existing `OrderPlaced` / `OrderPlacedForStaff`.

### Infrastructure (`E_POS.Infrastructure`)

New: `Modules/ECommerce/CartCheckout/Payment/`
- `StripeCheckoutGateway.cs` — `Stripe.net` `SessionService.CreateAsync`. `PaymentMethodTypes` is explicitly set to `["card"]` (see §7 for why). Success/cancel URLs are built from `StripeOptions.SuccessUrl`/`CancelUrl` with `?checkoutId={id}` appended, since the checkout UI is otherwise modal-only with no route to land back on after Stripe's full-page redirect.
- `StripeMoney.cs` — minor-unit conversion (handles Stripe's zero-decimal currency list; not relevant for LKR but kept generic).
- `OnlineCheckoutPaymentConfirmationRepository.cs` — implements the webhook-side repository; idempotency check is `SalesPayment.PaymentStatus == "PENDING"` (not the order, since the order stays `UNPAID` for both payment methods — see the constraint note above).

New: `Modules/Shared/Payment/Options/StripeOptions.cs` — `PublishableKey`, `SecretKey`, `WebhookSecret`, `SuccessUrl`, `CancelUrl`, bound from the `Stripe` config section.

Changed:
- `Modules/ECommerce/CartCheckout/Repositories/StorefrontCheckoutRepositoryBase.cs` — added `ResolveOrEnsureOnlinePaymentMethodAsync` (lazily creates a `PaymentMethod` row for `STRIPE` since no tenant-admin UI manages that table yet — see §7) and `GeneratePaymentNumberAsync`.
- `Modules/ECommerce/CartCheckout/Repositories/StorefrontCheckoutConfirmationRepository.cs` — Stripe branch inside the existing `ConfirmAsync` transaction; `CancelAwaitingOnlinePaymentAsync` for the gateway-call-failed rollback path.
- `Modules/ECommerce/CartCheckout/Repositories/StorefrontCheckoutRepository.cs` — forwards the new interface members to the confirmation repository.
- `DependencyInjection.cs` — registers `StripeOptions`, `IOnlineCheckoutPaymentGateway → StripeCheckoutGateway`, `IOnlineCheckoutPaymentConfirmationRepository`, `IOnlineCheckoutPaymentConfirmationService`.
- `E_POS.Infrastructure.csproj` — added `Stripe.net` package reference (this project already holds the other 3rd-party integrations — Azure Blob, Azure Communication Email — so it's the consistent place for it).
- `Modules/Shared/Media/Options/AzureBlobStorageOptionsValidator.cs` — unrelated pre-existing bug fix, see §7.1.

### API (`E_POS.Api`)

New:
- `Controllers/V1/ECommerce/Payments/StripeWebhookController.cs` — `POST api/v1/ecommerce/payments/stripe/webhook`, unauthenticated (Stripe can't send our JWTs; `Stripe-Signature` verification via `EventUtility.ConstructEvent` is the auth boundary). Reads the raw body directly (no `[FromBody]` model binding, required for signature verification). Handles `checkout.session.completed` and `checkout.session.expired`.

Changed:
- `Controllers/V1/ECommerce/CartCheckout/StorefrontCheckoutController.cs` — `Confirm` now accepts an optional JSON body `{ "paymentMethodCode": "STRIPE" | "PAY_AT_PICKUP" }`; `storefront_checkout.online_payment_unavailable` added to the 409 error mapping.
- `appsettings.json` / `appsettings.Development.json` — new `Stripe` section (placeholders in `appsettings.json`; real dev keys live only in `appsettings.Development.json`, which is git-ignored the same as before).

### Data

New EF migration: `Persistence/Migrations/20260914070000_SeedOneVerzeProductVariantBarcodes.cs` (+ `Persistence/Seed/OneVerze/OneVerzeProductVariantBarcodeSeedData.cs`) — see §7.3.

## 4. Frontend changes (`E-commerce`)

- `features/checkout/models/checkout.model.ts` — `StorefrontPaymentMethodCode` type, `paymentRedirectUrl` on `StorefrontCheckoutReadModel`.
- `features/checkout/services/checkout.service.ts` — `selectedPaymentMethod` signal (default `PAY_AT_PICKUP`); `confirmOrder` sends the chosen method and, if the response carries `paymentRedirectUrl`, does `window.location.href = ...` instead of advancing to the in-modal success step (the cart is only cleared once payment is actually confirmed, i.e. on the new success route).
- `features/checkout/components/checkout-review/checkout-review.component.ts` — added the "Pay at pickup" / "Pay online with card" choice above the Place Order button.
- `features/checkout/pages/payment-success/payment-success-page.component.ts` and `payment-cancelled/payment-cancelled-page.component.ts` — new standalone page components.
- `app.routes.ts` — new top-level `checkout/success` and `checkout/cancelled` routes. These had to be real routes (not modal state) because Stripe does a full-page browser navigation away and back, which a modal-only checkout can't survive.

No Stripe.js/Elements and no publishable key are used client-side — the hosted-redirect flow only needs the backend to hand back `session.url`.

## 5. Configuration

```json
"Stripe": {
  "PublishableKey": "pk_test_...",
  "SecretKey": "sk_test_...",
  "WebhookSecret": "whsec_...",
  "SuccessUrl": "http://localhost:4200/checkout/success",
  "CancelUrl": "http://localhost:4200/checkout/cancelled"
}
```

`WebhookSecret` must match whatever is currently forwarding events to `api/v1/ecommerce/payments/stripe/webhook` — get it from `stripe listen --print-secret` (stable per Stripe CLI login) or from the Dashboard if a permanent webhook endpoint is registered there instead.

**Use test-mode keys (`pk_test_`/`sk_test_`) for local development.** Stripe will hard-reject Stripe's own published test card numbers (e.g. `4242 4242 4242 4242`) when live keys are configured, and completing a checkout against live keys with a real card is a real charge.

## 6. Out of scope

- **Stripe refunds.** The existing refund pipeline (`SalesRefund`, `PosReturnService`) is synchronous/staff-initiated only. A Stripe refund is asynchronous and would need its own pending-refund state and webhook handling. `IOnlineCheckoutPaymentGateway` is the natural place to add a `RefundAsync` method later.
- **Per-tenant payment method administration.** `ResolveOrEnsureOnlinePaymentMethodAsync` auto-provisions the `STRIPE` `PaymentMethod` row per tenant on first use — there's still no admin UI to manage/disable it.
- **Per-tenant Stripe accounts.** Configuration is a single platform-wide Stripe account (`StripeOptions` bound once), not Stripe Connect.

## 7. Bugs and gaps found and fixed while integration-testing (none introduced by this feature)

These were all pre-existing and blocked checkout entirely — including "Pay at pickup" — regardless of Stripe.

### 7.1 `AzureBlobStorageOptionsValidator` crashed every local startup

A second, separately-registered `IValidateOptions<AzureBlobStorageOptions>` didn't honor `AllowLocalFallback: true` the way the inline validator next to it does, so `.ValidateOnStart()` always failed with `AzureBlobStorage:ConnectionString is required.` and the host never came up. Fixed to check `AllowLocalFallback` first, matching the inline validator. File: `Modules/Shared/Media/Options/AzureBlobStorageOptionsValidator.cs`.

### 7.2 Local dev database was several migrations behind

`dotnet ef database update` had 4+ pending migrations, including `BackfillSalesOrdersEntitlementForClickCollectTenants` and `RepairDevelopmentClickCollectBarcodeSnapshots` — both directly relevant to storefront checkout. Applying them fixed "Collection is not available for this outlet" for the OneVerze tenant. No code change; this is an environment-sync issue, but worth remembering: **run `dotnet ef database update` after any pull**, this project has no auto-migrate-on-startup.

### 7.3 OneVerze catalog had zero primary barcodes

`StorefrontCheckoutConfirmationRepository.ConfirmAsync` requires exactly one active primary barcode per order line (`TryResolvePrimaryBarcodeSnapshot`) — a pre-existing requirement, presumably for pickup scanning. All 33 OneVerze product variants had none, so **every** checkout confirmation failed with `storefront_checkout.barcode_unavailable`, regardless of payment method. Added migration `20260914070000_SeedOneVerzeProductVariantBarcodes` to backfill one `EAN13` primary barcode per variant.

### 7.4 CHECK constraint violations from the new status values (real bug in this feature, now fixed)

Initial versions of the Stripe domain methods used `"AWAITING_PAYMENT"` and `"CANCELLED"` for `SalesOrder.PaymentStatus`, and `"CHARGE"` for `SalesPaymentTransaction.TransactionType` — none of which exist in the corresponding Postgres `CHECK` constraints, so the very first real Stripe confirm attempt threw and returned a bare `500 internal_server_error`. Fixed by reusing the schema's existing allowed vocabulary (§3, Domain table above). **Lesson for future status/enum-like additions in this codebase: check `pg_constraint` before inventing a new string value**, since there's no compile-time enforcement tying C# string literals to the DB's `CHECK` constraints.

## 8. Current status / open item

As of this writing, checkout → Stripe redirect → payment all work end-to-end and have been verified against a real Stripe test-mode payment (`4242 4242 4242 4242`, session `status: complete`, `payment_status: paid` confirmed directly via the Stripe API). The **webhook delivery leg is still being diagnosed** in this environment — `stripe listen` reports `Ready!` with the correct signing secret but events aren't consistently showing up as forwarded, so affected orders stay at `PaymentStatus = UNPAID` / `SalesPayment = PENDING` instead of flipping to `PAID`. This looks like a local Stripe CLI / networking issue rather than an application bug (the webhook signature verification, event handling, and idempotency logic in `StripeWebhookController` / `OnlineCheckoutPaymentConfirmationService` have not shown any errors when exercised) — a `stripe trigger checkout.session.completed` synthetic-event test was the next diagnostic step. Anyone picking this up should confirm the webhook leg to close this out.

## 9. Verification

1. `dotnet build` (Unified-Commerce) and `ng build` (E-commerce) — both clean.
2. `dotnet test tests/E_POS.UnitTests` — 1658 tests passing, including the updated `StorefrontCheckoutServiceTests`.
3. Manual: place a Click & Collect order choosing "Pay at pickup" — confirm identical behavior to before this change.
4. Manual: place an order choosing "Pay online with card" with test keys — confirm redirect to a real `checkout.stripe.com` URL, and (once §8 is resolved) confirm the order lands as `PAID` with both customer and staff notifications created.
