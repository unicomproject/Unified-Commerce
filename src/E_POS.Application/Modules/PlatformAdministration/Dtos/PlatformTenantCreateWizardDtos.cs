namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record CreatePlatformTenantAddressRequest
{
    public string? Line1 { get; init; }

    public string? Line2 { get; init; }

    public string? City { get; init; }

    public string? State { get; init; }

    public string? PostalCode { get; init; }

    public string? CountryCode { get; init; }
}

public sealed record CreatePlatformTenantContactRequest
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }
}

public sealed record CreatePlatformTenantLimitsRequest
{
    public int? MaxOutlets { get; init; }

    public int? MaxTills { get; init; }

    public int? MaxUsers { get; init; }
}

public sealed record CreatePlatformTenantAddonSelectionRequest
{
    public Guid AddonId { get; init; }

    public int Quantity { get; init; }
}

public sealed record CreatePlatformTenantAdminRequest
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public bool SendInvite { get; init; } = true;

    public string? TemporaryPassword { get; init; }
}

public sealed record CreatePlatformTenantSubscriptionDetailsRequest
{
    public string? BillingCycle { get; init; }

    public string? SubscriptionStatus { get; init; }

    public DateTimeOffset? TrialStartAt { get; init; }

    public DateTimeOffset? TrialEndAt { get; init; }

    public DateTimeOffset? BillingStartAt { get; init; }

    public DateTimeOffset? NextBillingAt { get; init; }

    public bool AutoRenew { get; init; } = true;

    public string? DiscountType { get; init; }

    public decimal? DiscountValue { get; init; }

    public decimal? TaxPercentage { get; init; }

    public string? InvoiceEmail { get; init; }

    public string? PaymentMethod { get; init; }

    public string? Notes { get; init; }

    public bool CreateDraftInvoice { get; init; }
}
