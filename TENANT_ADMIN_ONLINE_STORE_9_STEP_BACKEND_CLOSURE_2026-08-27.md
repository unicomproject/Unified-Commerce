# Tenant Admin Online Store 9-Step Backend Final Closure - 2026-08-27

## 1. Blocker Closure Matrix

| Blocker | Before | Fix | Verification | Final Status |
| --- | --- | --- | --- | --- |
| Policy contract | UI required four policies while readiness required an additional `RETURN_REFUND` policy | One canonical `RequiredPolicyTypes` set now drives counts and readiness | Unit contract tests plus PostgreSQL tenant/channel/version projection test | PASS |
| Support readiness | Readiness checked only email and phone | Shared evaluator now requires valid email, normalized phone, business address, and valid support hours | Unit validation/readiness tests and shared overview/readiness/publish call path | PASS |
| DNS provider | Verification state could not be established by a production provider | Added provider-neutral contract and a configurable DNS-over-HTTPS TXT verifier using the stored token hash | Found, missing/mismatch, timeout, retry, rotated-token, and disabled-provider tests | PASS |
| SSL provider | Provisioning had no authoritative external provider lifecycle | Added provider-neutral asynchronous HTTP provisioning/status adapter and persisted provider-derived state transitions | Active, unavailable, and unexpected-provider-failure tests; service precondition/API tests | PASS |
| Media seed failure | Clean migration chain collided on fixed `media_assets` IDs | Moved development storefront seed IDs out of the later historical migration's reserved ID range | Clean PostgreSQL migration chain and full integration suite | PASS |

Flutter was not modified by this closure.

## 2. Policy Contract

The final Release-1 required policy set is defined once in `OnlineStoreContractRules.RequiredPolicyTypes`:

1. `TERMS`
2. `PRIVACY`
3. `CANCELLATION`
4. `COLLECTION`

`RETURN_REFUND` remains a valid optional policy type but is not a Release-1 publish requirement. Readiness counts a required policy only when the current tenant and current Online Store sales channel have a current `PUBLISHED` version. Draft, archived, previous, duplicate, optional, and cross-tenant rows do not satisfy the requirement.

The canonical set drives overview totals, readiness, final validation, and publish blocking; no independent numeric policy count is used.

## 3. Support Readiness

Publish requires all of the following:

- A required, normalized, syntactically valid support email.
- A required, normalized, valid support phone.
- A required, trimmed business address within the existing length limit.
- Required valid support hours containing valid day/time intervals where opening precedes closing.

WhatsApp contact and FAQ/help-center URL remain optional and do not block publication. `OnlineStoreContractRules.IsSupportReady` is reused by overview, readiness, and publish.

## 4. DNS Architecture

```text
TenantAdminOnlineStoreController
  -> TenantAdminOnlineStoreService
  -> IDomainVerificationProvider
  -> DnsOverHttpsDomainVerificationProvider
  -> configured HTTPS DNS resolver
```

The service verifies tenant ownership, uses the separately generated and persisted token hash, and never trusts a client-supplied verification result. The provider queries `_oneverz-verification.<domain>` TXT records, hashes returned tokens, compares them in fixed time, and maps missing, timeout, unavailable, invalid, and verified outcomes without fail-open behavior.

Verification is retry-safe and does not rotate the token. Rotation remains an explicit operation. Provider results update the domain verification status and audit trail.

## 5. SSL Architecture

```text
TenantAdminOnlineStoreController
  -> TenantAdminOnlineStoreService
  -> ICertificateProvisioningProvider
  -> HttpCertificateProvisioningProvider
  -> configured production certificate service
```

Provisioning requires an active current-tenant domain with DNS status `VERIFIED`. Requests carry a stable domain-scoped idempotency key. `ACTIVE` is persisted only when returned by the provider; `PROVISIONING`, `FAILED`, `TIMEOUT`, and `UNAVAILABLE` remain non-ready.

Domain status refresh reconciles asynchronous provider state. Setting a primary domain requires tenant ownership, DNS `VERIFIED`, and SSL `ACTIVE`; existing primary selection is replaced transactionally under the existing uniqueness invariant.

## 6. Readiness Contract

The single `BuildReadinessAsync` engine used by `GET /overview`, `GET /readiness`, and `POST /publish` blocks publication unless all applicable checks pass:

1. Current tenant is active.
2. Online Store entitlement and setup activation are effective.
3. Store identity fields are complete.
4. Hosted slug is valid and unique.
5. A configured primary custom domain, when used, is DNS verified and has active SSL.
6. Required branding and active banner state are complete.
7. Support email, phone, business address, and support hours are complete and valid.
8. At least one eligible Click & Collect outlet is configured with usable hours.
9. At least one online-visible product is available.
10. All four canonical required policy types have a current published version.
11. Required email/notification infrastructure is configured.

Publish additionally preserves authentication, tenant isolation, permission checks, entitlement checks, idempotency, transactionality, and audit logging.

## 7. Test Results

| Gate | Result |
| --- | --- |
| Build | PASS - 0 errors, 4 pre-existing warnings |
| Focused Online Store unit tests | PASS - 47/47 |
| Focused Online Store API tests | PASS - 52/52 |
| Focused PostgreSQL tests | PASS - 3/3 |
| Full unit suite | PASS - 1205/1205 |
| Full API suite | PASS - 481/481 |
| Full PostgreSQL/integration suite | PASS - 579/579 |
| Other solution test projects | PASS - 67/67 |
| Full backend solution suite | PASS - 2332/2332 |
| `git diff --check` | PASS - no whitespace errors; line-ending notices only |

Focused PostgreSQL coverage includes case-insensitive slug uniqueness, required-policy tenant/channel/version semantics, and the complete clean migration chain including the media seed regression.

## 8. Migration State

- New migration for this final blocker closure: none required.
- Existing Online Store migration retained: `20260827120000_HardenTenantAdminOnlineStoreSlugUniqueness`.
- Historical applied migrations were not edited.
- Production uniqueness constraints were not weakened.
- Media collision was corrected in development seed source by assigning non-overlapping deterministic IDs.
- EF pending model changes: `NONE` (`No changes have been made to the model since the last migration.`).

## 9. External Configuration

Deployment must provide values for these configuration keys; secret values are not stored in source control:

```text
OnlineStoreDomainVerification__Enabled
OnlineStoreDomainVerification__QueryEndpoint
OnlineStoreDomainVerification__RecordNamePrefix
OnlineStoreDomainVerification__TimeoutSeconds
OnlineStoreCertificateProvisioning__Enabled
OnlineStoreCertificateProvisioning__ProvisionEndpoint
OnlineStoreCertificateProvisioning__StatusEndpoint
OnlineStoreCertificateProvisioning__BearerToken
OnlineStoreCertificateProvisioning__TimeoutSeconds
```

Both provider integrations fail closed when disabled or unavailable. Production deployment readiness therefore requires enabling them and supplying the approved infrastructure endpoints and secret through environment configuration or a secret store.

## Remaining Blockers

None in the backend source, database contract, or automated verification scope. Provider account credentials and endpoints are deployment configuration, not committed application secrets.

## Final Verdict

TENANT ADMIN ONLINE STORE 9-STEP BACKEND — COMPLETE
