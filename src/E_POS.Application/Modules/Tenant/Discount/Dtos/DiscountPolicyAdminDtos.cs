namespace E_POS.Application.Modules.Tenant.Discount.Dtos;

public sealed record DiscountPolicyTargetRequestDto(
    string TargetType, string TargetMode, Guid TargetId);
public sealed record DiscountPolicyConditionRequestDto(
    int GroupNo, string GroupOperator, string ConditionType,
    string ConditionOperator, string ValueJson, int SortOrder);
public sealed record DiscountPolicyAdminRequestDto(
    Guid DiscountTypeId, string Code, string Name, string? Description,
    string Scope, decimal Value, string? CurrencyCode, decimal? MaxDiscountAmount,
    decimal? MinOrderAmount, decimal? MinQuantity, bool RequiresManagerApproval,
    bool IsStackable, string? StackingGroupCode, int Priority,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt,
    IReadOnlyList<Guid>? OutletIds, IReadOnlyList<Guid>? ChannelIds,
    IReadOnlyList<DiscountPolicyTargetRequestDto>? Targets,
    IReadOnlyList<DiscountPolicyConditionRequestDto>? Conditions);
public sealed record DiscountPolicyAdminResponseDto(
    Guid Id, string Code, string Name, string? Description, string Scope,
    string CalculationMethod, decimal Value, string? CurrencyCode,
    decimal? MaxDiscountAmount, decimal? MinOrderAmount, decimal? MinQuantity,
    bool RequiresManagerApproval, bool IsStackable, string? StackingGroupCode,
    int Priority, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, string Status,
    IReadOnlyList<Guid> OutletIds, IReadOnlyList<Guid> ChannelIds,
    IReadOnlyList<DiscountPolicyTargetRequestDto> Targets,
    IReadOnlyList<DiscountPolicyConditionRequestDto> Conditions);
