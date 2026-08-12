using System.Data;
using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Services;

public sealed class TenantResourceLimitGuard : ITenantResourceLimitGuard
{
    private readonly EPosDbContext _dbContext;
    private readonly ITenantSubscriptionLimitResolver _limitResolver;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<TenantResourceLimitGuard> _logger;

    public TenantResourceLimitGuard(
        EPosDbContext dbContext,
        ITenantSubscriptionLimitResolver limitResolver,
        IDateTimeProvider dateTimeProvider,
        ILogger<TenantResourceLimitGuard> logger)
    {
        _dbContext = dbContext;
        _limitResolver = limitResolver;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<TenantResourceLimitEvaluation> EvaluateAsync(
        Guid tenantId,
        string limitKey,
        int requestedIncrease,
        CancellationToken cancellationToken)
    {
        if (requestedIncrease < 0)
        {
            return Deny(
                limitKey,
                "unknown",
                0,
                requestedIncrease,
                null,
                false,
                false,
                SubscriptionLimitErrorCodes.Invalid,
                "Requested capacity increase cannot be negative.");
        }

        if (requestedIncrease == 0)
        {
            var zeroResolution = await _limitResolver.ResolveAsync(tenantId, limitKey, cancellationToken);
            var zeroUsage = await CountUsageAsync(tenantId, limitKey, cancellationToken);
            return Allow(
                zeroResolution,
                zeroUsage,
                0);
        }

        var resolution = await _limitResolver.ResolveAsync(tenantId, limitKey, cancellationToken);
        if (!resolution.IsConfigurationValid)
        {
            return Deny(
                resolution.LimitKey,
                resolution.Resource,
                0,
                requestedIncrease,
                resolution.EffectiveLimit,
                resolution.IsUnlimited,
                resolution.OverrideApplied,
                resolution.FailureCode ?? SubscriptionLimitErrorCodes.EvaluationFailed,
                resolution.FailureMessage ?? "Subscription limit configuration is invalid.");
        }

        var currentUsage = await CountUsageAsync(tenantId, limitKey, cancellationToken);
        return EvaluateAgainstLimit(resolution, currentUsage, requestedIncrease);
    }

    public async Task<TenantResourceCapacitySnapshot> GetCapacitySnapshotAsync(
        Guid tenantId,
        string limitKey,
        CancellationToken cancellationToken)
    {
        var evaluation = await EvaluateAsync(tenantId, limitKey, requestedIncrease: 1, cancellationToken);
        return new TenantResourceCapacitySnapshot(
            evaluation.LimitKey,
            evaluation.Resource,
            evaluation.CurrentUsage,
            evaluation.EffectiveLimit,
            evaluation.RemainingCapacity,
            evaluation.IsUnlimited,
            evaluation.Allowed || evaluation.IsUnlimited,
            evaluation.OverrideApplied);
    }

    public async Task<TenantResourceLimitGuardResult<T>> ExecuteWithinCapacityAsync<T>(
        Guid tenantId,
        string limitKey,
        int requestedIncrease,
        Func<CancellationToken, Task<TenantResourceCapacityOperationResult<T>>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var useRelationalLock = _dbContext.Database.IsRelational();
        var ownsTransaction = useRelationalLock && _dbContext.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;

        try
        {
            if (useRelationalLock)
            {
                await AcquireTenantLimitLockAsync(tenantId, limitKey, cancellationToken);
                _logger.LogDebug(
                    "Subscription limit advisory lock acquired. TenantId={TenantId} LimitKey={LimitKey} Provider={Provider}",
                    tenantId,
                    limitKey,
                    _dbContext.Database.ProviderName);
            }

            var evaluation = await EvaluateAsync(tenantId, limitKey, requestedIncrease, cancellationToken);
            if (!evaluation.Allowed)
            {
                _logger.LogInformation(
                    "Subscription limit reached or denied. TenantId={TenantId} LimitKey={LimitKey} Resource={Resource} CurrentUsage={CurrentUsage} RequestedIncrease={RequestedIncrease} EffectiveLimit={EffectiveLimit} OverrideApplied={OverrideApplied} Code={Code}",
                    tenantId,
                    evaluation.LimitKey,
                    evaluation.Resource,
                    evaluation.CurrentUsage,
                    evaluation.RequestedIncrease,
                    evaluation.EffectiveLimit,
                    evaluation.OverrideApplied,
                    evaluation.FailureCode);

                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return TenantResourceLimitGuardResult<T>.Denied(evaluation);
            }

            var outcome = await operation(cancellationToken);
            if (!outcome.Commit)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return TenantResourceLimitGuardResult<T>.Succeeded(outcome.Value, evaluation);
            }

            var refreshedUsage = await CountUsageAsync(tenantId, limitKey, cancellationToken);
            await SyncUsageCounterAsync(tenantId, limitKey, refreshedUsage, evaluation.EffectiveLimit, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            var postEvaluation = evaluation with
            {
                CurrentUsage = refreshedUsage,
                RemainingCapacity = evaluation.IsUnlimited || !evaluation.EffectiveLimit.HasValue
                    ? null
                    : Math.Max(0, evaluation.EffectiveLimit.Value - refreshedUsage)
            };

            return TenantResourceLimitGuardResult<T>.Succeeded(outcome.Value, postEvaluation);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    private TenantResourceLimitEvaluation EvaluateAgainstLimit(
        TenantSubscriptionLimitResolution resolution,
        int currentUsage,
        int requestedIncrease)
    {
        if (resolution.IsUnlimited)
        {
            return Allow(resolution, currentUsage, requestedIncrease);
        }

        var effective = resolution.EffectiveLimit!.Value;
        var projected = currentUsage + requestedIncrease;
        if (projected <= effective)
        {
            return Allow(resolution, currentUsage, requestedIncrease);
        }

        return Deny(
            resolution.LimitKey,
            resolution.Resource,
            currentUsage,
            requestedIncrease,
            effective,
            false,
            resolution.OverrideApplied,
            SubscriptionLimitErrorCodes.LimitReached,
            $"{Capitalize(resolution.Resource)} subscription limit reached. Current usage {currentUsage} of {effective}.");
    }

    private async Task<int> CountUsageAsync(Guid tenantId, string limitKey, CancellationToken cancellationToken)
    {
        if (!TenantSubscriptionLimitKeys.TryGet(limitKey, out var definition))
        {
            return 0;
        }

        return definition.LimitKey switch
        {
            TenantSubscriptionLimitKeys.MaxOutlets => await _dbContext.Outlets
                .AsNoTracking()
                .CountAsync(
                    outlet => outlet.TenantId == tenantId &&
                              outlet.Status != OutletConstants.DeletedStatus,
                    cancellationToken),
            TenantSubscriptionLimitKeys.MaxTills => await _dbContext.Tills
                .AsNoTracking()
                .CountAsync(
                    till => till.TenantId == tenantId &&
                            till.Status != TillConstants.DeletedStatus,
                    cancellationToken),
            TenantSubscriptionLimitKeys.MaxUsers => await _dbContext.TenantUsers
                .AsNoTracking()
                .CountAsync(
                    user => user.TenantId == tenantId &&
                            (user.AccountStatus == TenantUserConstants.StatusActive ||
                             user.AccountStatus == TenantUserConstants.StatusInvited),
                    cancellationToken),
            _ => 0
        };
    }

    private async Task SyncUsageCounterAsync(
        Guid tenantId,
        string limitKey,
        int currentUsage,
        int? effectiveLimit,
        CancellationToken cancellationToken)
    {
        if (!TenantSubscriptionLimitKeys.TryGet(limitKey, out var definition) ||
            !definition.FeatureLimitDefinitionId.HasValue)
        {
            return;
        }

        var counter = await _dbContext.TenantUsageCounters
            .Where(item =>
                item.TenantId == tenantId &&
                item.FeatureLimitDefinitionId == definition.FeatureLimitDefinitionId.Value &&
                item.UsageScope == TenantUsageCounterAlignmentConstants.UsageScope.Tenant)
            .OrderByDescending(item => item.PeriodStart)
            .FirstOrDefaultAsync(cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        if (counter is null)
        {
            var limitDefinition = await _dbContext.FeatureLimitDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == definition.FeatureLimitDefinitionId.Value, cancellationToken);
            if (limitDefinition is null)
            {
                return;
            }

            counter = TenantUsageCounter.Create(
                Guid.NewGuid(),
                tenantId,
                limitDefinition.Id,
                limitDefinition.PlatformFeatureId,
                TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
                null,
                currentUsage,
                effectiveLimit,
                now,
                null,
                now,
                lastCalculatedAt: now);
            await _dbContext.TenantUsageCounters.AddAsync(counter, cancellationToken);
            return;
        }

        counter.SetCurrentValue(currentUsage, now);
        counter.UpdateFromUpsert(
            counter.PlatformFeatureId,
            counter.PeriodStart,
            counter.PeriodEnd,
            effectiveLimit,
            now);
    }

    private async Task AcquireTenantLimitLockAsync(
        Guid tenantId,
        string limitKey,
        CancellationToken cancellationToken)
    {
        // Transaction-scoped advisory lock: serializes capacity checks per tenant+limit without global locks.
        var lockKey = HashLockKey(tenantId, limitKey);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            [lockKey],
            cancellationToken);
    }

    private static long HashLockKey(Guid tenantId, string limitKey)
    {
        var payload = Encoding.UTF8.GetBytes($"{tenantId:N}:{limitKey.Trim().ToLowerInvariant()}");
        var hash = SHA256.HashData(payload);
        return BitConverter.ToInt64(hash, 0);
    }

    private static TenantResourceLimitEvaluation Allow(
        TenantSubscriptionLimitResolution resolution,
        int currentUsage,
        int requestedIncrease)
    {
        int? remaining = resolution.IsUnlimited || !resolution.EffectiveLimit.HasValue
            ? null
            : Math.Max(0, resolution.EffectiveLimit.Value - currentUsage);

        return new TenantResourceLimitEvaluation(
            resolution.LimitKey,
            resolution.Resource,
            currentUsage,
            requestedIncrease,
            resolution.EffectiveLimit,
            remaining,
            resolution.IsUnlimited,
            true,
            resolution.OverrideApplied,
            null,
            null);
    }

    private static TenantResourceLimitEvaluation Deny(
        string limitKey,
        string resource,
        int currentUsage,
        int requestedIncrease,
        int? effectiveLimit,
        bool isUnlimited,
        bool overrideApplied,
        string failureCode,
        string failureMessage)
    {
        int? remaining = isUnlimited || !effectiveLimit.HasValue
            ? null
            : Math.Max(0, effectiveLimit.Value - currentUsage);

        return new TenantResourceLimitEvaluation(
            limitKey,
            resource,
            currentUsage,
            requestedIncrease,
            effectiveLimit,
            remaining,
            isUnlimited,
            false,
            overrideApplied,
            failureCode,
            failureMessage);
    }

    private static string Capitalize(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];
}
