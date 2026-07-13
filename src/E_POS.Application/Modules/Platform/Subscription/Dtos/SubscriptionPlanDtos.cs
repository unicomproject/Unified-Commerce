namespace E_POS.Application.Modules.Platform.Subscription.Dtos;

public sealed class SubscriptionPlanListQuery
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public string? Status { get; set; }

    public string? BillingCycle { get; set; }
}

public sealed record SubscriptionPlanListItemDto(
    Guid Id,
    string PlanCode,
    string Name,
    string? Description,
    string Status,
    string BillingCycle,
    string BaseCurrency,
    decimal BasePrice,
    int? MaxOutlets,
    int? MaxUsers,
    int? MaxTills,
    int FeatureCount,
    int ActiveTenantCount,
    bool CanEdit,
    bool CanDuplicate,
    bool CanArchive,
    bool CanDelete,
    DateTimeOffset UpdatedAt);

public sealed record SubscriptionPlanListResponse(
    IReadOnlyList<SubscriptionPlanListItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool CanCreate,
    bool CanEdit,
    bool CanDuplicate,
    bool CanArchive,
    bool CanDelete,
    bool CanReactivate);

public sealed record SubscriptionPlanCatalogFeatureDto(
    Guid Id,
    string FeatureCode,
    string Name,
    string? Description,
    int SortOrder);

public sealed record SubscriptionPlanCatalogModuleDto(
    Guid Id,
    string ModuleCode,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<SubscriptionPlanCatalogFeatureDto> Features);

public sealed record SubscriptionPlanCatalogResponse(
    IReadOnlyList<SubscriptionPlanCatalogModuleDto> Modules);

public sealed record CreateSubscriptionPlanRequest
{
    public string? PlanCode { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? BillingCycle { get; init; }

    public string? BaseCurrency { get; init; }

    public decimal? BasePrice { get; init; }

    public int? MaxOutlets { get; init; }

    public int? MaxUsers { get; init; }

    public int? MaxTills { get; init; }
}

public sealed record UpdateSubscriptionPlanPricingRequest
{
    public decimal BasePrice { get; init; }

    public string? BaseCurrency { get; init; }
}

public sealed record UpdateSubscriptionPlanLimitsRequest
{
    public int? MaxOutlets { get; init; }

    public int? MaxUsers { get; init; }

    public int? MaxTills { get; init; }
}

public sealed record UpdateSubscriptionPlanFeaturesRequest
{
    public IReadOnlyList<Guid>? FeatureIds { get; init; }
}

public sealed record UpdateSubscriptionPlanRequest
{
    public string? PlanCode { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? BillingCycle { get; init; }
}

public sealed record SubscriptionPlanMutationResponse(
    Guid Id,
    string PlanCode,
    string Name,
    string Status,
    string BillingCycle,
    string BaseCurrency,
    decimal BasePrice,
    int? MaxOutlets,
    int? MaxUsers,
    int? MaxTills,
    int FeatureCount,
    DateTimeOffset UpdatedAt);

public sealed record SubscriptionPlanDetailLimitDto(
    Guid Id,
    string Code,
    string Name,
    decimal? Value,
    bool IsUnlimited,
    string? UnitCode);

public sealed record SubscriptionPlanDetailFeatureDto(
    Guid Id,
    string Code,
    string Name,
    string? Description);

public sealed record SubscriptionPlanDetailModuleDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<SubscriptionPlanDetailFeatureDto> Features);

public sealed record SubscriptionPlanDetailResponse(
    Guid Id,
    string PlanCode,
    string Name,
    string? Description,
    string Status,
    string BillingCycle,
    string BaseCurrency,
    decimal BasePrice,
    string PricingModel,
    int TrialDays,
    int? MaxOutlets,
    int? MaxUsers,
    int? MaxTills,
    int FeatureCount,
    int ActiveTenantCount,
    bool CanEdit,
    bool CanDuplicate,
    bool CanArchive,
    bool CanDelete,
    bool CanReactivate,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SubscriptionPlanDetailLimitDto> Limits,
    IReadOnlyList<SubscriptionPlanDetailModuleDto> Modules,
    DateTimeOffset UpdatedAt);

