namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record ReturnPolicyTemplateSummaryResponse(
    Guid Id,
    string TemplateCode,
    string Name,
    string? Description,
    int? ReturnWindowDays,
    int? ExchangeWindowDays,
    bool RequiresReceipt,
    bool AllowDefectiveReturn,
    bool RequiresManagerApproval,
    bool IsPlatformDefault,
    int VersionNumber,
    string LifecycleStatus,
    Guid ConcurrencyToken,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
