namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record ReturnPolicySummaryResponse(
    Guid Id,
    string PolicyCode,
    string Name,
    int? ReturnWindowDays,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);