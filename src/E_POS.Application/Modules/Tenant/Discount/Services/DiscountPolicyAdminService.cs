using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Application.Modules.Tenant.Discount.Dtos;
using E_POS.Domain.Modules.Tenant.Orders.Constants;

namespace E_POS.Application.Modules.Tenant.Discount.Services;

public sealed class DiscountPolicyAdminService : IDiscountPolicyAdminService
{
    private readonly IDiscountPolicyAdminRepository _repository;
    private readonly IDateTimeProvider _clock;
    public DiscountPolicyAdminService(IDiscountPolicyAdminRepository repository, IDateTimeProvider clock)
    { _repository = repository; _clock = clock; }

    public async Task<ApplicationResult<IReadOnlyList<DiscountPolicyAdminResponseDto>>> ListAsync(TenantRequestContext context, CancellationToken ct)
    {
        if (!context.HasPermission(SalesPermissions.DiscountPolicy.View)) return Denied<IReadOnlyList<DiscountPolicyAdminResponseDto>>();
        var result = await _repository.ListAsync(context.TenantId, ct);
        return result.Error is null ? ApplicationResult<IReadOnlyList<DiscountPolicyAdminResponseDto>>.Success(result.Items) : Fail<IReadOnlyList<DiscountPolicyAdminResponseDto>>(result.Error);
    }
    public async Task<ApplicationResult<DiscountPolicyAdminResponseDto>> GetAsync(TenantRequestContext context, Guid id, CancellationToken ct)
    {
        if (!context.HasPermission(SalesPermissions.DiscountPolicy.View)) return Denied<DiscountPolicyAdminResponseDto>();
        var result = await _repository.GetAsync(context.TenantId, id, ct);
        return result.Error is null && result.Item is not null ? ApplicationResult<DiscountPolicyAdminResponseDto>.Success(result.Item) : Fail<DiscountPolicyAdminResponseDto>(result.Error ?? "discount_policy.not_found");
    }
    public Task<ApplicationResult<DiscountPolicyAdminResponseDto>> CreateAsync(TenantRequestContext context, DiscountPolicyAdminRequestDto request, CancellationToken ct) =>
        Save(context, null, request, SalesPermissions.DiscountPolicy.Create, ct);
    public Task<ApplicationResult<DiscountPolicyAdminResponseDto>> UpdateAsync(TenantRequestContext context, Guid id, DiscountPolicyAdminRequestDto request, CancellationToken ct) =>
        Save(context, id, request, SalesPermissions.DiscountPolicy.Update, ct);
    private async Task<ApplicationResult<DiscountPolicyAdminResponseDto>> Save(TenantRequestContext context, Guid? id, DiscountPolicyAdminRequestDto request, string permission, CancellationToken ct)
    {
        if (!context.HasPermission(permission)) return Denied<DiscountPolicyAdminResponseDto>();
        var validation = Validate(request); if (validation is not null) return Fail<DiscountPolicyAdminResponseDto>("discount_policy.invalid_request", validation);
        var result = await _repository.SaveAsync(context.TenantId, context.UserId, id, request, _clock.UtcNow, ct);
        return result.Error is null && result.Item is not null ? ApplicationResult<DiscountPolicyAdminResponseDto>.Success(result.Item) : Fail<DiscountPolicyAdminResponseDto>(result.Error ?? "discount_policy.save_failed");
    }
    public async Task<ApplicationResult<DiscountPolicyAdminResponseDto>> SetActiveAsync(TenantRequestContext context, Guid id, bool active, CancellationToken ct)
    {
        if (!context.HasPermission(SalesPermissions.DiscountPolicy.Activate)) return Denied<DiscountPolicyAdminResponseDto>();
        var result = await _repository.SetActiveAsync(context.TenantId, context.UserId, id, active, _clock.UtcNow, ct);
        return result.Error is null && result.Item is not null ? ApplicationResult<DiscountPolicyAdminResponseDto>.Success(result.Item) : Fail<DiscountPolicyAdminResponseDto>(result.Error ?? "discount_policy.not_found");
    }
    public async Task<ApplicationResult<bool>> DeleteAsync(TenantRequestContext context, Guid id, CancellationToken ct)
    {
        if (!context.HasPermission(SalesPermissions.DiscountPolicy.Delete)) return Denied<bool>();
        var error = await _repository.DeleteAsync(context.TenantId, context.UserId, id, _clock.UtcNow, ct);
        return error is null ? ApplicationResult<bool>.Success(true) : Fail<bool>(error);
    }
    private static string? Validate(DiscountPolicyAdminRequestDto x)
    {
        if (x.DiscountTypeId == Guid.Empty || string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Name)) return "Type, code and name are required.";
        if (x.Scope?.Trim().ToUpperInvariant() is not ("ORDER" or "LINE" or "PRODUCT" or "CATEGORY" or "BRAND" or "COLLECTION" or "BATCH")) return "Unsupported scope.";
        if (x.Value < 0 || x.Priority < 0 || x.EndsAt < x.StartsAt) return "Values, priority or validity dates are invalid.";
        foreach (var target in x.Targets ?? [])
        {
            if (target.TargetType?.Trim().ToUpperInvariant() is not ("PRODUCT" or "PRODUCT_VARIANT" or "CATEGORY" or "BRAND" or "COLLECTION") ||
                target.TargetMode?.Trim().ToUpperInvariant() is not ("INCLUDE" or "EXCLUDE") ||
                target.TargetId == Guid.Empty)
                return "Targets must use valid type, mode, and id.";
        }
        foreach (var condition in x.Conditions ?? [])
        {
            if (condition.GroupNo <= 0 || condition.SortOrder < 0 ||
                condition.GroupOperator?.Trim().ToUpperInvariant() is not ("AND" or "OR") ||
                string.IsNullOrWhiteSpace(condition.ConditionType) ||
                string.IsNullOrWhiteSpace(condition.ConditionOperator) ||
                string.IsNullOrWhiteSpace(condition.ValueJson))
                return "Conditions must use valid grouping, operator, type, and value.";
        }
        return null;
    }
    private static ApplicationResult<T> Denied<T>() => Fail<T>("discount_policy.permission_denied", "You do not have permission to manage discount policies.");
    private static ApplicationResult<T> Fail<T>(string code, string? message = null) => ApplicationResult<T>.Failure(new ApplicationError(code, message ?? "Discount policy operation failed."));
}
