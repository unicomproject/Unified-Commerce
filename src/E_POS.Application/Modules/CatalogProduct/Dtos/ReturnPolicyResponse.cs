namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record ReturnPolicyResponse(
    Guid Id,
    string PolicyCode,
    string Name,
    int? ReturnWindowDays,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);