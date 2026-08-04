using System.Text.Json;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record TenantOnboardingAddressDto(string? Line1, string? Line2, string? City, string? StateOrProvince, string? PostalCode, string? CountryCode);
public sealed record TenantOnboardingContactDto(string? Name, string? Email, string? Phone);
public sealed record TenantOnboardingBasicDetailsDto(string? DisplayName, string? LegalName, string? TenantCode, string? TenantSlug, string? RequestedSubdomain, string? RegistrationNumber, string? TaxNumber, string? BusinessTypeCode, string? OperatingMode, string? DefaultCountryCode, string? BaseCurrencyCode, string? Timezone, string? Locale);
public sealed record TenantOnboardingBusinessContactDto(TenantOnboardingAddressDto? RegisteredAddress, TenantOnboardingContactDto? PrimaryContact, string? WebsiteUrl, bool BillingContactSameAsPrimary, TenantOnboardingContactDto? BillingContact, bool BillingAddressSameAsRegistered, TenantOnboardingAddressDto? BillingAddress, TenantOnboardingContactDto? SupportContact);
public sealed record TenantOnboardingAddonDto(Guid AddonId, int Quantity);
public sealed record TenantOnboardingLimitsDto(int? MaxOutlets, int? MaxTills, int? MaxUsers);
public sealed record TenantOnboardingPlanDto(Guid? SubscriptionPlanId, string? SubscriptionType, string? BillingCycle, IReadOnlyList<TenantOnboardingAddonDto>? Addons, TenantOnboardingLimitsDto? RequestedLimits);
public sealed record TenantOnboardingBillingDto(string? InvoiceEmail, string? PaymentMethod, DateTimeOffset? TrialStartAt, DateTimeOffset? TrialEndAt, DateTimeOffset? BillingStartAt, DateTimeOffset? NextBillingAt, bool AutoRenew, string? DiscountType, decimal? DiscountValue, decimal? TaxPercentage, string? Notes, string? WaiverReason);
public sealed record TenantOnboardingEntitlementsDto(IReadOnlyList<Guid>? FeatureIds);
public sealed record TenantOnboardingAdminDto(string? FirstName, string? LastName, string? Email, string? Phone);
public sealed record TenantOnboardingPayloadDto(TenantOnboardingBasicDetailsDto? BasicDetails, TenantOnboardingBusinessContactDto? BusinessContact, TenantOnboardingPlanDto? Plan, TenantOnboardingBillingDto? Billing, TenantOnboardingEntitlementsDto? Entitlements, TenantOnboardingAdminDto? TenantAdmin, bool ReviewConfirmed = false);

public sealed record CreateTenantOnboardingDraftRequest(TenantOnboardingPayloadDto? Payload, short CurrentStep = 1);
public sealed record UpdateTenantOnboardingDraftRequest(TenantOnboardingPayloadDto Payload, short CurrentStep);
public sealed record FinalizeTenantOnboardingRequest(IReadOnlyList<string>? AcknowledgedWarningCodes, bool FinalReviewConfirmed);
public sealed record RetryTenantOnboardingOperationRequest(string? Component);

public sealed record TenantOnboardingDraftResponse(Guid Id, Guid OwnerPlatformUserId, string Status, short CurrentStep,
    IReadOnlyList<int> CompletedSteps, short ProgressPercent, TenantOnboardingPayloadDto Payload, int SchemaVersion,
    long Version, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, DateTimeOffset ExpiresAt, Guid? CreatedTenantId,
    IReadOnlyList<string> Warnings);
public sealed record TenantOnboardingDraftSummaryResponse(Guid Id, string? DisplayName, string? TenantCode, string Status,
    short CurrentStep, short ProgressPercent, Guid OwnerPlatformUserId, DateTimeOffset? UpdatedAt, DateTimeOffset ExpiresAt, long Version);
public sealed record TenantOnboardingDraftListResponse(IReadOnlyList<TenantOnboardingDraftSummaryResponse> Items, int TotalCount);
public sealed record TenantOnboardingValidationResponse(bool IsValid, IReadOnlyList<int> CompletedSteps, short ProgressPercent,
    IReadOnlyList<ApplicationFieldErrorDto> Errors, IReadOnlyList<string> Warnings);
public sealed record ApplicationFieldErrorDto(string Field, string Message);
public sealed record TenantOnboardingReceiptResponse(Guid TenantId, Guid DraftId, Guid OperationId, string TenantStatus,
    string ProvisioningStatus, string PaymentStatus, string InvitationStatus, DateTimeOffset CreatedAt, bool IdempotentReplay);
public sealed record TenantOnboardingOperationResponse(Guid Id, Guid DraftId, Guid TenantId, string Status,
    string ProvisioningStatus, string PaymentStatus, string InvitationStatus, int AttemptCount, string? FailureCode,
    bool Retryable, DateTimeOffset? NextRetryAt, long Version, DateTimeOffset? UpdatedAt);
