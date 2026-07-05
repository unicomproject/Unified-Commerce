namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record ReturnPolicyResponse(
    Guid Id,
    string ReturnPolicyCode,
    string ReturnPolicyName,
    int ReturnWindowDays,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);