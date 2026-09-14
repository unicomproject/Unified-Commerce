namespace E_POS.Domain.Modules.Shared.ReturnExchange.Snapshots;

public sealed record SaleLineReturnPolicySnapshot(
    Guid? PolicyId,
    int? PolicyVersion,
    string PolicyCode,
    string PolicyName,
    int ReturnWindowDays,
    int ExchangeWindowDays,
    bool RequiresReceipt,
    bool RequiresManagerApproval,
    bool AllowDefectiveReturn,
    bool IsReturnable,
    string? RulesJson = null);

