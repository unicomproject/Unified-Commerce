using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Application.Modules.Tenant.Discount.Dtos;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Repositories;

public sealed class DiscountPolicyAdminRepository : IDiscountPolicyAdminRepository
{
    private readonly EPosDbContext _db;
    public DiscountPolicyAdminRepository(EPosDbContext db) => _db = db;

    public async Task<(string? Error, IReadOnlyList<DiscountPolicyAdminResponseDto> Items)> ListAsync(Guid tenantId, CancellationToken ct)
    {
        var ids = await _db.DiscountPolicies.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != "DELETED")
            .OrderByDescending(x => x.Priority).ThenBy(x => x.DiscountPolicyName)
            .Select(x => x.Id).ToListAsync(ct);
        var items = new List<DiscountPolicyAdminResponseDto>(ids.Count);
        foreach (var id in ids) items.Add((await GetAsync(tenantId, id, ct)).Item!);
        return (null, items);
    }

    public async Task<(string? Error, DiscountPolicyAdminResponseDto? Item)> GetAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var policy = await _db.DiscountPolicies.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.Status != "DELETED", ct);
        return policy is null ? ("discount_policy.not_found", null) : (null, await MapAsync(policy, ct));
    }

    public async Task<(string? Error, DiscountPolicyAdminResponseDto? Item)> SaveAsync(
        Guid tenantId, Guid userId, Guid? id, DiscountPolicyAdminRequestDto request,
        DateTimeOffset now, CancellationToken ct)
    {
        var type = await _db.DiscountTypes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.DiscountTypeId && x.Status == "ACTIVE", ct);
        if (type is null) return ("discount_policy.invalid_type", null);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.DiscountPolicies.AnyAsync(x => x.TenantId == tenantId && x.DiscountPolicyCode == code &&
                x.Status != "DELETED" && (!id.HasValue || x.Id != id), ct))
            return ("discount_policy.duplicate_code", null);

        if (!await ReferencesBelongToTenantAsync(tenantId, request, ct))
            return ("discount_policy.cross_tenant_reference", null);

        DiscountPolicy? policy;
        if (id.HasValue)
        {
            policy = await _db.DiscountPolicies.SingleOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == id && x.Status != "DELETED", ct);
            if (policy is null) return ("discount_policy.not_found", null);
            policy.UpdateProfile(request.DiscountTypeId, code, request.Name, request.Description,
                request.Scope, request.Value, request.CurrencyCode, request.MaxDiscountAmount,
                request.MinOrderAmount, request.MinQuantity, request.RequiresManagerApproval,
                request.IsStackable, request.StackingGroupCode, request.Priority, request.StartsAt,
                request.EndsAt, policy.Status, userId, now);
            await SoftDeleteChildren(policy.Id, userId, now, ct);
        }
        else
        {
            policy = DiscountPolicy.Create(Guid.NewGuid(), tenantId, request.DiscountTypeId,
                code, request.Name, request.Description, request.Scope, request.Value,
                request.CurrencyCode, request.MaxDiscountAmount, request.MinOrderAmount,
                request.MinQuantity, request.RequiresManagerApproval, request.IsStackable,
                request.StackingGroupCode, request.Priority, request.StartsAt, request.EndsAt,
                "INACTIVE", userId, now);
            _db.DiscountPolicies.Add(policy);
        }

        AddChildren(policy!.Id, tenantId, userId, request, now);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return ("discount_policy.concurrency_conflict", null); }
        return (null, await MapAsync(policy!, ct));
    }

    public async Task<(string? Error, DiscountPolicyAdminResponseDto? Item)> SetActiveAsync(
        Guid tenantId, Guid userId, Guid id, bool active, DateTimeOffset now, CancellationToken ct)
    {
        var policy = await _db.DiscountPolicies.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == id && x.Status != "DELETED", ct);
        if (policy is null) return ("discount_policy.not_found", null);
        policy.UpdateProfile(policy.DiscountTypeId, policy.DiscountPolicyCode, policy.DiscountPolicyName,
            policy.Description, policy.DiscountScope, policy.DiscountValue, policy.CurrencyCode,
            policy.MaxDiscountAmount, policy.MinOrderAmount, policy.MinQuantity,
            policy.RequiresManagerApproval, policy.IsStackable, policy.StackingGroupCode,
            policy.Priority, policy.StartsAt, policy.EndsAt, active ? "ACTIVE" : "INACTIVE", userId, now);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return ("discount_policy.concurrency_conflict", null); }
        return (null, await MapAsync(policy, ct));
    }

    public async Task<string?> DeleteAsync(Guid tenantId, Guid userId, Guid id, DateTimeOffset now, CancellationToken ct)
    {
        var policy = await _db.DiscountPolicies.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == id && x.Status != "DELETED", ct);
        if (policy is null) return "discount_policy.not_found";
        policy.SoftDelete(userId, now);
        await SoftDeleteChildren(id, userId, now, ct);
        try { await _db.SaveChangesAsync(ct); return null; }
        catch (DbUpdateConcurrencyException) { return "discount_policy.concurrency_conflict"; }
    }

    private async Task SoftDeleteChildren(Guid policyId, Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        foreach (var x in await _db.DiscountPolicyOutlets.Where(x => x.DiscountPolicyId == policyId && x.Status != "DELETED").ToListAsync(ct)) x.SoftDelete(userId, now);
        foreach (var x in await _db.DiscountPolicyChannels.Where(x => x.DiscountPolicyId == policyId && x.Status != "DELETED").ToListAsync(ct)) x.SoftDelete(userId, now);
        foreach (var x in await _db.DiscountPolicyTargets.Where(x => x.DiscountPolicyId == policyId && x.Status != "DELETED").ToListAsync(ct)) x.SoftDelete(userId, now);
        foreach (var x in await _db.DiscountPolicyConditions.Where(x => x.DiscountPolicyId == policyId && x.Status != "DELETED").ToListAsync(ct)) x.SoftDelete(userId, now);
    }

    private void AddChildren(Guid policyId, Guid tenantId, Guid userId, DiscountPolicyAdminRequestDto request, DateTimeOffset now)
    {
        foreach (var outletId in request.OutletIds?.Distinct() ?? [])
            _db.DiscountPolicyOutlets.Add(DiscountPolicyOutlet.Create(Guid.NewGuid(), tenantId, policyId, outletId, "ACTIVE", userId, now));
        foreach (var channelId in request.ChannelIds?.Distinct() ?? [])
            _db.DiscountPolicyChannels.Add(DiscountPolicyChannel.Create(Guid.NewGuid(), tenantId, policyId, channelId, "ACTIVE", userId, now));
        foreach (var x in request.Targets ?? [])
        {
            Guid? product = null, variant = null, category = null, brand = null, collection = null;
            switch (x.TargetType.Trim().ToUpperInvariant())
            { case "PRODUCT": product = x.TargetId; break; case "PRODUCT_VARIANT": variant = x.TargetId; break; case "CATEGORY": category = x.TargetId; break; case "BRAND": brand = x.TargetId; break; case "COLLECTION": collection = x.TargetId; break; }
            _db.DiscountPolicyTargets.Add(DiscountPolicyTarget.Create(Guid.NewGuid(), tenantId, policyId,
                x.TargetType, x.TargetMode, product, variant, category, brand, collection, "ACTIVE", userId, now));
        }
        foreach (var x in request.Conditions ?? [])
            _db.DiscountPolicyConditions.Add(DiscountPolicyCondition.Create(Guid.NewGuid(), tenantId, policyId,
                x.GroupNo, x.GroupOperator, x.ConditionType, x.ConditionOperator, x.ValueJson,
                x.SortOrder, "ACTIVE", userId, now));
    }

    private async Task<DiscountPolicyAdminResponseDto> MapAsync(DiscountPolicy p, CancellationToken ct)
    {
        var method = await _db.DiscountTypes.AsNoTracking().Where(x => x.Id == p.DiscountTypeId)
            .Select(x => x.CalculationMethod).SingleAsync(ct);
        var outlets = await _db.DiscountPolicyOutlets.AsNoTracking().Where(x => x.DiscountPolicyId == p.Id && x.Status == "ACTIVE").Select(x => x.OutletId).ToListAsync(ct);
        var channels = await _db.DiscountPolicyChannels.AsNoTracking().Where(x => x.DiscountPolicyId == p.Id && x.Status == "ACTIVE").Select(x => x.SalesChannelId).ToListAsync(ct);
        var targets = await _db.DiscountPolicyTargets.AsNoTracking().Where(x => x.DiscountPolicyId == p.Id && x.Status == "ACTIVE")
            .Select(x => new DiscountPolicyTargetRequestDto(x.TargetType, x.TargetMode,
                x.ProductId ?? x.ProductVariantId ?? x.CategoryId ?? x.BrandId ?? x.CollectionId ?? Guid.Empty)).ToListAsync(ct);
        var conditions = await _db.DiscountPolicyConditions.AsNoTracking().Where(x => x.DiscountPolicyId == p.Id && x.Status == "ACTIVE")
            .OrderBy(x => x.ConditionGroupNo).ThenBy(x => x.SortOrder)
            .Select(x => new DiscountPolicyConditionRequestDto(x.ConditionGroupNo, x.GroupOperator,
                x.ConditionType, x.ConditionOperator, x.ConditionValueJson, x.SortOrder)).ToListAsync(ct);
        return new(p.Id, p.DiscountPolicyCode, p.DiscountPolicyName, p.Description, p.DiscountScope,
            method, p.DiscountValue, p.CurrencyCode, p.MaxDiscountAmount, p.MinOrderAmount,
            p.MinQuantity, p.RequiresManagerApproval, p.IsStackable, p.StackingGroupCode,
            p.Priority, p.StartsAt, p.EndsAt, p.Status, outlets, channels, targets, conditions);
    }

    private async Task<bool> ReferencesBelongToTenantAsync(
        Guid tenantId, DiscountPolicyAdminRequestDto request, CancellationToken ct)
    {
        var outletIds = request.OutletIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (outletIds.Count > 0 &&
            await _db.Outlets.CountAsync(x => x.TenantId == tenantId && outletIds.Contains(x.Id), ct) != outletIds.Count)
            return false;

        var channelIds = request.ChannelIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (channelIds.Count > 0 &&
            await _db.SalesChannels.CountAsync(x => x.TenantId == tenantId && channelIds.Contains(x.Id), ct) != channelIds.Count)
            return false;

        foreach (var target in request.Targets ?? [])
        {
            if (target.TargetId == Guid.Empty) return false;
            var type = target.TargetType.Trim().ToUpperInvariant();
            var exists = type switch
            {
                "PRODUCT" => await _db.Products.AnyAsync(x => x.TenantId == tenantId && x.Id == target.TargetId, ct),
                "PRODUCT_VARIANT" => await _db.ProductVariants.AnyAsync(x => x.TenantId == tenantId && x.Id == target.TargetId, ct),
                "CATEGORY" => await _db.Categories.AnyAsync(x => x.TenantId == tenantId && x.Id == target.TargetId, ct),
                "BRAND" => await _db.Brands.AnyAsync(x => x.TenantId == tenantId && x.Id == target.TargetId, ct),
                "COLLECTION" => await _db.Collections.AnyAsync(x => x.TenantId == tenantId && x.Id == target.TargetId, ct),
                _ => false
            };
            if (!exists) return false;
        }

        return true;
    }
}
