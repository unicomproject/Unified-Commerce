# ACS Email — Platform Password Reset (Phase 1)

<!-- status: COMPLETED -->
<!-- last_updated: 2026-07-24 -->
<!-- module: PlatformAdministration / Integrations.Email -->

## Summary

Phase 1 adds Azure Communication Services (ACS) Email infrastructure and connects it to the **existing** Platform Admin password-reset flow (SA-P1-06).

| Item | Status |
|---|---|
| Overall | **COMPLETED** |
| Manual E2E verification | **PASSED** |
| ACS email infrastructure | **COMPLETE** |
| Platform password reset sends email when ACS is configured | **COMPLETE** (`deliveryMode: email`) |
| Tenant self-service password reset | **Not implemented** (out of scope) |
| Tenant-admin tenant-user password reset | **Not implemented** (out of scope) |
| Auth / token architecture redesign | **Not changed** |

## Manual E2E verification (PASSED)

Real Azure ACS delivery was verified for the admin-initiated platform password-reset journey. Evidence below excludes tokens, full reset URLs, connection strings, access keys, and recipient-sensitive screenshots.

| Check | Result |
|---|---|
| ACS email received | **Passed** |
| API `deliveryMode` | `email` |
| API `resetUrl` | `null` (omitted from admin response in email mode) |
| Reset link completed successfully | **Passed** |
| Old password rejected after reset | **Passed** |
| New password accepted (login) | **Passed** |
| Token reuse rejected | **Passed** |
| Previous refresh session revoked | **Passed** |

## Compatibility decision (admin_secure_link)

- **Production** (`AllowAdminSecureLinkFallback: false`): ACS must be configured; API returns `deliveryMode: email` and **does not** return `resetUrl`.
- **Development** (`AllowAdminSecureLinkFallback: true`): if ACS is not configured, delivery falls back to `admin_secure_link` so local work and existing Passthrough-based tests remain usable.
- Existing integration tests continue to inject a passthrough delivery stub that returns `admin_secure_link` for token extraction during complete-flow assertions.

## Packages added

| Package | Project | Version |
|---|---|---|
| `Azure.Communication.Email` | `E_POS.Infrastructure` | 1.1.0 |
| `Azure.Identity` | `E_POS.Infrastructure` | 1.14.2 |

`Azure.Identity` is included for production `Endpoint` + `DefaultAzureCredential`.

## Configuration keys (non-secret)

```json
"AzureCommunicationEmail": {
  "ConnectionString": "",
  "Endpoint": "",
  "SenderAddress": "",
  "SenderDisplayName": "OneVerz",
  "AllowAdminSecureLinkFallback": false
}
```

Development sets `AllowAdminSecureLinkFallback` to `true`.

Existing platform reset URL settings (unchanged):

```json
"PlatformPasswordReset": {
  "PublicAppBaseUrl": "http://localhost:4200",
  "ResetPath": "/reset-password"
}
```

### Local user-secrets (do not commit secrets)

```powershell
dotnet user-secrets set "AzureCommunicationEmail:ConnectionString" "<secret>" --project src/E_POS.Api
dotnet user-secrets set "AzureCommunicationEmail:SenderAddress" "DoNotReply@your-verified-domain.com" --project src/E_POS.Api
```

Production alternative:

```text
AzureCommunicationEmail:Endpoint = https://<resource>.communication.azure.com
AzureCommunicationEmail:SenderAddress = DoNotReply@your-verified-domain.com
# Auth via Managed Identity / DefaultAzureCredential
```

## Azure portal prerequisites

1. Create an Azure Communication Services resource.
2. Create/connect an Email Communication Services resource and verify a domain.
3. Create a MailFrom sender address (e.g. `DoNotReply@...`).
4. For connection-string auth: copy the ACS connection string into user-secrets / Key Vault.
5. For Managed Identity: grant the App Service identity access to the Communication resource and configure `Endpoint` only.

## Files changed / added

### Application

- `Common/Email/IApplicationEmailSender.cs`
- `Common/Email/ApplicationEmailMessage.cs`
- `Common/Email/ApplicationEmailSendResult.cs`
- `Modules/Platform/PlatformAdmin/Email/PlatformPasswordResetEmailComposer.cs`
- `Modules/Platform/PlatformAdmin/Contracts/IPlatformPasswordResetDelivery.cs` (result type + DisplayName)
- `Modules/Platform/PlatformAdmin/Services/PlatformPasswordResetService.cs` (delivery failure handling)

### Domain

- `PlatformPasswordResetConstants.DeliveryModeEmail = "email"`

### Infrastructure

- `Integrations/Email/AzureCommunicationEmailOptions.cs`
- `Integrations/Email/AzureCommunicationEmailOptionsValidator.cs`
- `Integrations/Email/AzureCommunicationEmailSender.cs`
- `Modules/Platform/PlatformAdmin/Services/PlatformPasswordResetDelivery.cs` (`AcsPlatformPasswordResetDeliveryService`)
- `DependencyInjection.cs` (options + email sender + delivery registration)
- `E_POS.Infrastructure.csproj` (NuGet packages)

### API

- `appsettings.json` / `appsettings.Development.json` (empty ACS keys)
- `PlatformAdminUsersController.cs` (map email provider errors to 502)

### Tests

- `AcsPlatformPasswordResetDeliveryTests.cs`
- `PlatformPasswordResetEmailFlowUnitTests.cs`
- `AzureCommunicationEmailOptionsValidationTests.cs`
- Updated integration passthrough delivery signatures
- `PlatformPasswordResetApiSurfaceTests` asserts no tenant reset controllers

## Security decisions

- Provider-neutral `IApplicationEmailSender` — no Azure types in Application.
- Logs: operation id, status, correlation id, provider error code only.
- Never log connection string, access key, raw token, reset URL query token, or password.
- Reset URL built only from `PlatformPasswordReset` trusted settings (existing builder).
- HTML-encode recipient display name in template.
- `WaitUntil.Started` — accept send without claiming inbox delivery.
- Production responses omit `resetUrl` when email delivery succeeds.

## Manual test steps

1. Set user-secrets for ConnectionString + SenderAddress.
2. Set `AllowAdminSecureLinkFallback` false (or use production appsettings).
3. Run API; sign in as platform admin with `platform.users.update`.
4. `POST /api/v1/platform-admin/users/{userId}/password-reset`.
5. Confirm response `deliveryMode` is `email` and `resetUrl` is null.
6. Open the email inbox; open the link; complete reset on `/reset-password`.
7. Confirm old sessions cannot call protected APIs; login with new password works.

## Remaining work (not in Phase 1)

- Platform Admin logged-out self-service Forgot Password
- Tenant self-service password reset
- Tenant-admin initiated tenant-user password reset
- Invite / activation emails
- Outbox / retry queue for durable delivery
- Production custom email domain

## Confirmation

Status: **COMPLETED**. Manual E2E verification: **PASSED**.

Tenant self-service reset, tenant-admin tenant-user reset, and Platform Admin self-service Forgot Password were **not** implemented in this phase.
