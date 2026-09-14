namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record ReturnPolicyTemplateCreateRequest(
    string TemplateCode,
    string Name,
    string? Description,
    int? ReturnWindowDays,
    int? ExchangeWindowDays,
    bool? RequiresReceipt,
    bool? AllowDefectiveReturn,
    bool? RequiresManagerApproval,
    bool? IsPlatformDefault,
    string Status);

