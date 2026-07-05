namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record ReturnPolicyUpdateRequest(
    string PolicyCode, 
    string Name, 
    string? Description,
    int ReturnWindowDays, 
    int ExchangeWindowDays,
    bool? RequiresReceipt,
    bool? AllowDefectiveReturn,
    bool? RequiresManagerApproval,
    bool? IsDefaultPolicy,
    string Status);
