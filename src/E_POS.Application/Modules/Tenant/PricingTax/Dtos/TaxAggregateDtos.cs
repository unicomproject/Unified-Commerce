using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.PricingTax.Dtos;

public sealed class TaxAggregateCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TaxTreatment { get; set; } = string.Empty;
    public decimal? InitialRate { get; set; }
    public DateOnly EffectiveFrom { get; set; }

    // Legacy Flutter compatibility aliases
    public string? TaxName { get => Name; set { if (!string.IsNullOrWhiteSpace(value)) Name = value; } }
    public string? TaxCode { get => Code; set { if (!string.IsNullOrWhiteSpace(value)) Code = value; } }
    public string? TaxType { get => TaxTreatment; set { if (!string.IsNullOrWhiteSpace(value)) TaxTreatment = value; } }
    public decimal? TaxPercentage { get => InitialRate; set { if (value.HasValue) InitialRate = value; } }
}

public sealed class TaxAggregateUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TaxTreatment { get; set; }
    public string? Code { get; set; }

    public string? TaxName { get => Name; set { if (!string.IsNullOrWhiteSpace(value)) Name = value; } }
    public string? TaxType { get => TaxTreatment; set { if (!string.IsNullOrWhiteSpace(value)) TaxTreatment = value; } }
}

public sealed class TaxScheduleRateRequest
{
    public decimal NewRate { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public string? Notes { get; set; }
}

public sealed class TaxFutureRateUpdateRequest
{
    public decimal NewRate { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public string? Notes { get; set; }
}

public sealed class TaxStatusChangeRequest
{
    public string? Reason { get; set; }
}

public sealed class TaxRateHistoryItemResponse
{
    public Guid Id { get; set; }
    public decimal Rate { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string State { get; set; } = string.Empty; // HISTORICAL | CURRENT | SCHEDULED
    public string? Notes { get; set; }
}

public sealed class TaxAggregateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TaxTreatment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? CurrentRate { get; set; }
    public DateOnly? CurrentRateEffectiveFrom { get; set; }
    public decimal? NextRate { get; set; }
    public DateOnly? NextRateEffectiveFrom { get; set; }
    public int ProductCount { get; set; }
    public bool IsSeeded { get; set; }
    public IReadOnlyList<TaxRateHistoryItemResponse>? RateHistory { get; set; }

    // Legacy Flutter compatibility
    public string TaxName => Name;
    public string TaxCode => Code;
    public string TaxType => TaxTreatment;
    public decimal TaxPercentage => CurrentRate ?? 0m;
}

public sealed record TaxAggregateListResponse(
    IReadOnlyCollection<TaxAggregateResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed class TaxProductUsingResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TaxPriceMode { get; set; } = string.Empty; // INCLUSIVE | EXCLUSIVE
}

public sealed record TaxProductsUsingListResponse(
    IReadOnlyCollection<TaxProductUsingResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed class TaxStatusChangeResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}
