using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Application.Modules.Tenant.Discount.Dtos;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Repositories;

public sealed class PosDiscountRepository : IPosDiscountRepository
{
    private const string Active = "ACTIVE";
    private const string Open = "OPEN";
    private readonly EPosDbContext _dbContext;
    private readonly ILogger<PosDiscountRepository> _logger;

    public PosDiscountRepository(
        EPosDbContext dbContext,
        ILogger<PosDiscountRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PosDiscountCatalogRepositoryResult> ListAvailableAsync(
        Guid tenantId, Guid tenantUserId, PosDiscountCatalogQueryDto query, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var context = await ResolveDeviceContextAsync(tenantId, tenantUserId, query.DeviceId, cancellationToken);
        if (context.ErrorCode is not null)
        {
            return new(context.ErrorCode, null);
        }

        var candidates = await QueryAvailablePolicies(tenantId, context.OutletId, now)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var policies = new List<PosDiscountPolicySnapshot>();
        foreach (var policy in candidates)
        {
            if (IsManualPolicyCode(policy.Code)) continue;
            var variantIds = query.VariantIds?.Where(x => x != Guid.Empty).Distinct().ToList()
                ?? (query.VariantId.HasValue && query.VariantId.Value != Guid.Empty
                    ? [query.VariantId.Value]
                    : []);
            var applicability = new PosDiscountApplicabilityContext(
                NormalizeScope(query.Scope) ?? policy.Scope, query.VariantId, variantIds, query.CustomerId,
                query.Quantity ?? 0m, query.CartSubtotal ?? 0m, query.CurrencyCode);
            if (await IsApplicableAsync(tenantId, policy, applicability, cancellationToken))
                policies.Add(policy);
        }

        var discounts = policies.Select(policy => new PosDiscountResponseDto(
            policy.Id, policy.Code, policy.Name, policy.Description, policy.Scope,
            policy.CalculationMethod, policy.PredefinedValue, policy.AbsoluteValueLimit,
            ResolveCashierLimit(context.Authority!, policy.CalculationMethod), policy.CurrencyCode,
            policy.MaxDiscountAmount, policy.MinOrderAmount, policy.MinQuantity,
            policy.RequiresManagerApproval, policy.IsStackable, policy.StackingGroupCode, policy.Priority,
            policy.StartsAt, policy.EndsAt)).ToList();

        return new(null, new PosDiscountCatalogResponseDto(context.Authority!, discounts));
    }

    public async Task<PosDiscountContextRepositoryResult> ResolveContextAsync(
        Guid tenantId, Guid tenantUserId, Guid deviceId, Guid discountPolicyId,
        PosDiscountApplicabilityContext applicability, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var context = await ResolveDeviceContextAsync(tenantId, tenantUserId, deviceId, cancellationToken);
        if (context.ErrorCode is not null)
        {
            return new(context.ErrorCode, Guid.Empty, Guid.Empty, Guid.Empty, null, null);
        }

        var policy = (await QueryAvailablePolicies(tenantId, context.OutletId, now)
                .ToListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == discountPolicyId);
        if (policy is null)
        {
            return new("pos_discounts.discount_not_found", context.OutletId, context.TillId,
                context.TillSessionId, context.Authority, null);
        }

        if (!await IsApplicableAsync(tenantId, policy, applicability, cancellationToken))
            return new("pos_discounts.policy_not_applicable", context.OutletId, context.TillId,
                context.TillSessionId, context.Authority, null);

        return new(null, context.OutletId, context.TillId, context.TillSessionId,
            context.Authority, policy);
    }

    public async Task<PosDiscountContextRepositoryResult> ResolveManualContextAsync(
        Guid tenantId, Guid tenantUserId, Guid deviceId, string calculationMethod,
        string requestedScope, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var context = await ResolveDeviceContextAsync(tenantId, tenantUserId, deviceId, cancellationToken);
        if (context.ErrorCode is not null)
            return new(context.ErrorCode, Guid.Empty, Guid.Empty, Guid.Empty, null, null);
        var scope = NormalizeScope(requestedScope);
        if (scope is null)
            return new("pos_discounts.scope_mismatch", context.OutletId, context.TillId,
                context.TillSessionId, context.Authority, null);
        var code = ManualPolicyCode(calculationMethod, scope);
        if (code is null)
            return new("pos_discounts.manual_configuration_not_found", context.OutletId, context.TillId,
                context.TillSessionId, context.Authority, null);
        var policy = (await QueryAvailablePolicies(tenantId, context.OutletId, now)
                .ToListAsync(cancellationToken))
            .FirstOrDefault(x => x.Code == code &&
                x.CalculationMethod == calculationMethod &&
                x.Scope == scope);
        return policy is null
            ? new("pos_discounts.manual_configuration_not_found", context.OutletId, context.TillId,
                context.TillSessionId, context.Authority, null)
            : new(null, context.OutletId, context.TillId, context.TillSessionId,
                context.Authority, policy);
    }

    public async Task<PosDiscountApplicationRepositoryResult> CreateApplicationAsync(
        PosDiscountApplicationCommand command, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PosDiscountApplications.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == command.TenantId &&
                x.RequestedByTenantUserId == command.RequestedByTenantUserId &&
                x.IdempotencyKey == command.IdempotencyKey,
                cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.CartHash, command.CartHash, StringComparison.Ordinal) ||
                existing.DiscountPolicyId != command.DiscountPolicyId ||
                existing.RequestedValue != command.RequestedValue)
            {
                return new("pos_discounts.idempotency_conflict", existing.Id, existing.ApplicationStatus,
                    existing.ExpiresAt, true);
            }

            return new(null, existing.Id, existing.ApplicationStatus, existing.ExpiresAt, true);
        }

        var activeApplications = await (from activeApplication in _dbContext.PosDiscountApplications.AsNoTracking()
                                        join policy in _dbContext.DiscountPolicies.AsNoTracking()
                                            on new { activeApplication.TenantId, Id = activeApplication.DiscountPolicyId }
                                            equals new { policy.TenantId, policy.Id }
                                        where activeApplication.TenantId == command.TenantId &&
                                              activeApplication.RequestedByTenantUserId == command.RequestedByTenantUserId &&
                                              activeApplication.PosDeviceId == command.DeviceId &&
                                              activeApplication.TillSessionId == command.TillSessionId &&
                                              activeApplication.CartHash == command.CartHash &&
                                              (activeApplication.ApplicationStatus == "APPROVED" ||
                                               activeApplication.ApplicationStatus == "PENDING_APPROVAL")
                                        select new { activeApplication.DiscountPolicyId, policy.IsStackable, policy.StackingGroupCode })
            .ToListAsync(cancellationToken);
        if (activeApplications.Any(x => x.DiscountPolicyId == command.DiscountPolicyId) ||
            activeApplications.Any(x => !x.IsStackable || !command.IsStackable ||
                (!string.IsNullOrWhiteSpace(command.StackingGroupCode) &&
                 x.StackingGroupCode == command.StackingGroupCode)))
            return new("pos_discounts.stacking_not_allowed", Guid.Empty, "REJECTED", command.ExpiresAt, false);

        var commandValidationError = await ValidateApplicationCommandAsync(command, cancellationToken);
        if (commandValidationError is not null)
        {
            return new(commandValidationError, Guid.Empty, "REJECTED", command.ExpiresAt, false);
        }

        var application = PosDiscountApplication.Create(
            command.ApplicationId, command.TenantId, command.DiscountPolicyId, command.DiscountTypeId,
            command.OutletId, command.TillId, command.TillSessionId, command.DeviceId,
            command.RequestedByTenantUserId, command.CustomerId, command.TargetVariantId,
            command.IdempotencyKey, command.DiscountSource, command.DiscountScope,
            command.PolicyCode, command.PolicyName, command.CalculationMethod,
            command.RequestedValue, command.CashierLimit, command.AbsoluteLimit,
            command.CartSubtotal, command.EligibleSubtotal, command.DiscountAmount,
            command.TotalAfterDiscount, command.CurrencyCode, command.CartSnapshotJson,
            command.CartHash, command.Reason, command.RequiresManagerApproval,
            command.ExpiresAt, command.Now);

        _dbContext.PosDiscountApplications.Add(application);
        _dbContext.PosDiscountApplicationEvents.Add(PosDiscountApplicationEvent.Record(
            Guid.NewGuid(), command.TenantId, application.Id, "REQUESTED", "NONE",
            application.ApplicationStatus, command.RequestedByTenantUserId, command.Reason, command.Now));
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _dbContext.ChangeTracker.Clear();
            var concurrent = await _dbContext.PosDiscountApplications.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TenantId == command.TenantId &&
                    x.RequestedByTenantUserId == command.RequestedByTenantUserId &&
                    x.IdempotencyKey == command.IdempotencyKey,
                    cancellationToken);
            if (concurrent is null)
            {
                string? errorCode = ResolvePersistenceErrorCode(exception);
                if (errorCode is not null)
                {
                    _logger.LogWarning(
                        exception,
                        "POS discount application persistence rejected for tenant {TenantId}, device {DeviceId}, till session {TillSessionId}.",
                        command.TenantId,
                        command.DeviceId,
                        command.TillSessionId);
                    return new(errorCode, Guid.Empty, "REJECTED", command.ExpiresAt, false);
                }

                throw;
            }

            if (concurrent.CartHash != command.CartHash ||
                concurrent.DiscountPolicyId != command.DiscountPolicyId ||
                concurrent.RequestedValue != command.RequestedValue)
                return new("pos_discounts.idempotency_conflict", concurrent.Id,
                    concurrent.ApplicationStatus, concurrent.ExpiresAt, true);
            return new(null, concurrent.Id, concurrent.ApplicationStatus,
                concurrent.ExpiresAt, true);
        }
        return new(null, application.Id, application.ApplicationStatus, application.ExpiresAt, false);
    }

    private async Task<string?> ValidateApplicationCommandAsync(
        PosDiscountApplicationCommand command, CancellationToken cancellationToken)
    {
        var userExists = await _dbContext.TenantUsers.AsNoTracking().AnyAsync(
            x => x.TenantId == command.TenantId &&
                 x.Id == command.RequestedByTenantUserId &&
                 x.AccountStatus == Active,
            cancellationToken);
        if (!userExists) return "pos_discounts.cashier_not_found";

        var currencyExists = await _dbContext.Currencies.AsNoTracking().AnyAsync(
            x => x.CurrencyCode == command.CurrencyCode,
            cancellationToken);
        if (!currencyExists) return "pos_discounts.currency_not_configured";

        var sessionMatchesContext = await _dbContext.TillSessions.AsNoTracking().AnyAsync(
            x => x.TenantId == command.TenantId &&
                 x.Id == command.TillSessionId &&
                 x.OutletId == command.OutletId &&
                 x.TillId == command.TillId &&
                 x.OpenedFromPosDeviceId == command.DeviceId &&
                 x.ClosedAt == null &&
                 x.Status == Open,
            cancellationToken);
        if (!sessionMatchesContext) return "pos_discounts.till_session_context_mismatch";

        var referencesExist = await (
            from policy in _dbContext.DiscountPolicies.AsNoTracking()
            join type in _dbContext.DiscountTypes.AsNoTracking()
                on policy.DiscountTypeId equals type.Id
            join outlet in _dbContext.Outlets.AsNoTracking()
                on new { policy.TenantId, Id = command.OutletId }
                equals new { outlet.TenantId, outlet.Id }
            join till in _dbContext.Tills.AsNoTracking()
                on new { policy.TenantId, Id = command.TillId }
                equals new { till.TenantId, till.Id }
            join device in _dbContext.PosDevices.AsNoTracking()
                on new { policy.TenantId, Id = command.DeviceId }
                equals new { device.TenantId, device.Id }
            where policy.TenantId == command.TenantId &&
                  policy.Id == command.DiscountPolicyId &&
                  policy.DiscountTypeId == command.DiscountTypeId &&
                  policy.Status == Active &&
                  type.Status == Active &&
                  outlet.Status == Active &&
                  till.Status == Active &&
                  device.Status == PosDeviceConstants.ActiveStatus &&
                  device.IsTrusted
            select policy.Id)
            .AnyAsync(cancellationToken);
        if (!referencesExist) return "pos_discounts.discount_reference_invalid";

        if (command.CustomerId.HasValue)
        {
            var customerExists = await _dbContext.Customers.AsNoTracking().AnyAsync(
                x => x.TenantId == command.TenantId && x.Id == command.CustomerId.Value,
                cancellationToken);
            if (!customerExists) return "pos_discounts.customer_not_found";
        }

        if (command.TargetVariantId.HasValue)
        {
            var variantExists = await _dbContext.ProductVariants.AsNoTracking().AnyAsync(
                x => x.TenantId == command.TenantId && x.Id == command.TargetVariantId.Value,
                cancellationToken);
            if (!variantExists) return "pos_discounts.target_not_found";
        }

        return null;
    }

    private static string? ResolvePersistenceErrorCode(DbUpdateException exception)
    {
        if (exception.GetBaseException() is not PostgresException postgresException)
        {
            return null;
        }

        return postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation
                when postgresException.ConstraintName == "uq_pos_discount_applications_idempotency"
                    => "pos_discounts.idempotency_conflict",
            PostgresErrorCodes.ForeignKeyViolation => "pos_discounts.context_invalid",
            PostgresErrorCodes.CheckViolation => "pos_discounts.invalid_request",
            PostgresErrorCodes.NotNullViolation => "pos_discounts.invalid_request",
            _ => null
        };
    }

    public async Task<PosDiscountDecisionRepositoryResult> DecideAsync(
        Guid tenantId, Guid managerUserId, Guid applicationId, string decision,
        string? note, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var application = await _dbContext.PosDiscountApplications
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == applicationId, cancellationToken);
        if (application is null)
        {
            return new("pos_discounts.application_not_found", null);
        }
        if (application.ApplicationStatus != "PENDING_APPROVAL")
        {
            return new("pos_discounts.application_not_pending", null);
        }
        if (application.RequestedByTenantUserId == managerUserId)
        {
            return new("pos_discounts.self_approval_forbidden", null);
        }
        if (application.ExpiresAt <= now)
        {
            return new("pos_discounts.application_expired", null);
        }

        var fromStatus = application.ApplicationStatus;
        var normalizedDecision = decision.Trim().ToUpperInvariant();
        if (normalizedDecision == "APPROVE")
        {
            application.Approve(managerUserId, note, now);
        }
        else if (normalizedDecision == "REJECT")
        {
            application.Reject(managerUserId, note, now);
        }
        else
        {
            return new("pos_discounts.invalid_decision", null);
        }

        _dbContext.PosDiscountApplicationEvents.Add(PosDiscountApplicationEvent.Record(
            Guid.NewGuid(), tenantId, application.Id, normalizedDecision == "APPROVE" ? "APPROVED" : "REJECTED",
            fromStatus, application.ApplicationStatus, managerUserId, note, now));
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new("pos_discounts.application_not_pending", null);
        }

        return new(null, new PosDiscountDecisionResponseDto(
            application.Id, application.DiscountPolicyId, application.ApplicationStatus,
            managerUserId, now, application.DecisionNote,
            ToMoney(application.DiscountAmountSnapshot), ToMoney(application.TotalAfterDiscountSnapshot),
            application.CartHash));
    }

    public async Task<PosDiscountCancelRepositoryResult> CancelAsync(
        Guid tenantId, Guid tenantUserId, Guid applicationId, Guid deviceId,
        string? reason, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var context = await ResolveDeviceContextAsync(tenantId, tenantUserId, deviceId, cancellationToken);
        if (context.ErrorCode is not null)
            return new(context.ErrorCode, null);
        var application = await _dbContext.PosDiscountApplications.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == applicationId, cancellationToken);
        if (application is null) return new("pos_discounts.application_not_found", null);
        if (application.RequestedByTenantUserId != tenantUserId ||
            application.PosDeviceId != deviceId || application.OutletId != context.OutletId ||
            application.TillId != context.TillId || application.TillSessionId != context.TillSessionId)
            return new("pos_discounts.application_context_mismatch", null);
        if (application.ApplicationStatus == "CANCELLED")
            return new(null, new(application.Id, "cancelled", application.UpdatedAt ?? now));
        if (application.SalesOrderId.HasValue || application.ApplicationStatus is "APPLIED" or "REJECTED" or "EXPIRED")
            return new("pos_discounts.application_not_cancellable", null);
        var previous = application.ApplicationStatus;
        application.Cancel(reason, now);
        _dbContext.PosDiscountApplicationEvents.Add(PosDiscountApplicationEvent.Record(
            Guid.NewGuid(), tenantId, application.Id, "CANCELLED", previous, "CANCELLED",
            tenantUserId, reason, now));
        try { await _dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return new("pos_discounts.application_not_cancellable", null); }
        return new(null, new(application.Id, "cancelled", now));
    }

    private async Task<DeviceDiscountContext> ResolveDeviceContextAsync(
        Guid tenantId, Guid tenantUserId, Guid deviceId, CancellationToken cancellationToken)
    {
        var device = await _dbContext.PosDevices.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == deviceId)
            .Select(x => new { x.Status, x.IsTrusted })
            .FirstOrDefaultAsync(cancellationToken);
        if (device is null) return new("pos_discounts.device_not_found", Guid.Empty, Guid.Empty, Guid.Empty, null);
        if (!device.IsTrusted || device.Status != PosDeviceConstants.ActiveStatus)
            return new("pos_discounts.device_not_trusted", Guid.Empty, Guid.Empty, Guid.Empty, null);

        var assignment = await _dbContext.TillDeviceAssignments.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PosDeviceId == deviceId && x.ReleasedAt == null)
            .OrderByDescending(x => x.AssignedAt)
            .Select(x => new { x.OutletId, x.TillId })
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null) return new("pos_discounts.till_not_assigned", Guid.Empty, Guid.Empty, Guid.Empty, null);

        var sessionId = await _dbContext.TillSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TillId == assignment.TillId && x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!sessionId.HasValue) return new("pos_discounts.till_session_not_open", assignment.OutletId, assignment.TillId, Guid.Empty, null);

        var authority = await _dbContext.PosDiscountAuthorityLimits.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.Status == Active)
            .Select(x => new PosDiscountAuthorityDto(x.MaxPercentage, x.MaxFixedAmount, x.CurrencyCode))
            .FirstOrDefaultAsync(cancellationToken)
            ?? new PosDiscountAuthorityDto(0m, 0m, "LKR");

        return new(null, assignment.OutletId, assignment.TillId, sessionId.Value, authority);
    }

    private IQueryable<PosDiscountPolicySnapshot> QueryAvailablePolicies(
        Guid tenantId, Guid outletId, DateTimeOffset now)
    {
        var posChannelIds = _dbContext.SalesChannels.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == Active &&
                (x.ChannelCode.ToUpper() == "POS" || x.ChannelType.ToUpper() == "POS"))
            .Select(x => x.Id);

        return from policy in _dbContext.DiscountPolicies.AsNoTracking()
               join type in _dbContext.DiscountTypes.AsNoTracking() on policy.DiscountTypeId equals type.Id
               where policy.TenantId == tenantId && policy.Status == Active && type.Status == Active &&
                     (!policy.StartsAt.HasValue || policy.StartsAt <= now) &&
                     (!policy.EndsAt.HasValue || policy.EndsAt >= now) &&
                     (!_dbContext.DiscountPolicyOutlets.Any(x => x.TenantId == tenantId && x.DiscountPolicyId == policy.Id && x.Status == Active) ||
                      _dbContext.DiscountPolicyOutlets.Any(x => x.TenantId == tenantId && x.DiscountPolicyId == policy.Id && x.OutletId == outletId && x.Status == Active)) &&
                     (!_dbContext.DiscountPolicyChannels.Any(x => x.TenantId == tenantId && x.DiscountPolicyId == policy.Id && x.Status == Active) ||
                      _dbContext.DiscountPolicyChannels.Any(x => x.TenantId == tenantId && x.DiscountPolicyId == policy.Id && posChannelIds.Contains(x.SalesChannelId) && x.Status == Active))
               select new PosDiscountPolicySnapshot(
                   policy.Id, policy.DiscountTypeId, policy.DiscountPolicyCode,
                   policy.DiscountPolicyName, policy.Description, policy.DiscountScope,
                   type.CalculationMethod, policy.DiscountValue, policy.DiscountValue,
                   policy.CurrencyCode, policy.MaxDiscountAmount, policy.MinOrderAmount,
                   policy.MinQuantity, policy.RequiresManagerApproval, policy.IsStackable,
                   policy.StackingGroupCode, policy.Priority, policy.StartsAt, policy.EndsAt);
    }

    private async Task<bool> IsApplicableAsync(
        Guid tenantId, PosDiscountPolicySnapshot policy,
        PosDiscountApplicabilityContext context, CancellationToken cancellationToken)
    {
        if (NormalizeScope(context.Scope) is { } scope && policy.Scope != scope) return false;
        if (!string.IsNullOrWhiteSpace(context.CurrencyCode) && policy.CurrencyCode is not null &&
            !string.Equals(policy.CurrencyCode, context.CurrencyCode, StringComparison.OrdinalIgnoreCase)) return false;
        if (policy.MinOrderAmount.HasValue && context.CartSubtotal > 0 &&
            context.CartSubtotal < policy.MinOrderAmount.Value) return false;
        if (policy.MinQuantity.HasValue && context.Quantity > 0 && context.Quantity < policy.MinQuantity.Value) return false;

        if (!await ConditionsMatchAsync(tenantId, policy, context, cancellationToken)) return false;

        var targets = await _dbContext.DiscountPolicyTargets.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.DiscountPolicyId == policy.Id && x.Status == Active)
            .ToListAsync(cancellationToken);
        if (targets.Count == 0) return true;
        var variantIds = context.Scope == "LINE"
            ? (context.VariantId.HasValue && context.VariantId.Value != Guid.Empty ? [context.VariantId.Value] : [])
            : context.VariantIds.Where(x => x != Guid.Empty).Distinct().ToList();
        if (variantIds.Count == 0) return false;
        var item = await (from variant in _dbContext.ProductVariants.AsNoTracking()
                          join product in _dbContext.Products.AsNoTracking()
                              on new { variant.TenantId, Id = variant.ProductId }
                              equals new { product.TenantId, product.Id }
                          where variant.TenantId == tenantId && variantIds.Contains(variant.Id)
                          select new { VariantId = variant.Id, ProductId = product.Id, product.BrandId })
            .ToListAsync(cancellationToken);
        if (item.Count == 0) return false;
        var productIds = item.Select(x => x.ProductId).Distinct().ToList();
        var categoryIds = await _dbContext.ProductCategories.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
            .Select(x => x.CategoryId).ToListAsync(cancellationToken);
        var collectionIds = await _dbContext.ProductCollections.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
            .Select(x => x.CollectionId).ToListAsync(cancellationToken);
        bool Matches(DiscountPolicyTarget x) => x.TargetType switch
        {
            "PRODUCT" => x.ProductId.HasValue && productIds.Contains(x.ProductId.Value),
            "PRODUCT_VARIANT" => x.ProductVariantId.HasValue && variantIds.Contains(x.ProductVariantId.Value),
            "CATEGORY" => x.CategoryId.HasValue && categoryIds.Contains(x.CategoryId.Value),
            "BRAND" => x.BrandId.HasValue && item.Any(i => i.BrandId == x.BrandId),
            "COLLECTION" => x.CollectionId.HasValue && collectionIds.Contains(x.CollectionId.Value),
            _ => false
        };
        if (targets.Any(x => x.TargetMode == "EXCLUDE" && Matches(x))) return false;
        var includes = targets.Where(x => x.TargetMode == "INCLUDE").ToList();
        return includes.Count == 0 || includes.Any(Matches);
    }

    private static string? NormalizeScope(string? scope) => scope?.Trim().ToUpperInvariant() switch
    {
        "ORDER" => "ORDER", "LINE" => "LINE", _ => null
    };

    private static bool IsManualPolicyCode(string code) =>
        code is "POS_MANUAL_PERCENTAGE" or "POS_MANUAL_FIXED" or
            "POS_MANUAL_PERCENTAGE_LINE" or "POS_MANUAL_FIXED_LINE";

    private static string? ManualPolicyCode(string calculationMethod, string scope) =>
        (calculationMethod, scope) switch
        {
            ("PERCENTAGE", "ORDER") => "POS_MANUAL_PERCENTAGE",
            ("FIXED_AMOUNT", "ORDER") => "POS_MANUAL_FIXED",
            ("PERCENTAGE", "LINE") => "POS_MANUAL_PERCENTAGE_LINE",
            ("FIXED_AMOUNT", "LINE") => "POS_MANUAL_FIXED_LINE",
            _ => null
        };

    private static decimal ResolveCashierLimit(PosDiscountAuthorityDto authority, string calculationMethod) =>
        calculationMethod == "PERCENTAGE" ? authority.MaxPercentage : authority.MaxFixedAmount;

    private static int ToMoney(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private async Task<bool> ConditionsMatchAsync(
        Guid tenantId, PosDiscountPolicySnapshot policy,
        PosDiscountApplicabilityContext context, CancellationToken cancellationToken)
    {
        var conditions = await _dbContext.DiscountPolicyConditions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.DiscountPolicyId == policy.Id && x.Status == Active)
            .OrderBy(x => x.ConditionGroupNo).ThenBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        if (conditions.Count == 0) return true;

        foreach (var group in conditions.GroupBy(x => x.ConditionGroupNo))
        {
            var groupOperator = group.First().GroupOperator;
            var results = group.Select(x => ConditionMatches(x, context)).ToList();
            if (groupOperator == "OR")
            {
                if (!results.Any(x => x)) return false;
            }
            else if (results.Any(x => !x))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ConditionMatches(DiscountPolicyCondition condition, PosDiscountApplicabilityContext context)
    {
        if (condition.ConditionType == "CUSTOMER_REQUIRED")
            return context.CustomerId.HasValue && context.CustomerId.Value != Guid.Empty;

        var value = ReadConditionValue(condition.ConditionValueJson);
        if (value is null) return false;
        return condition.ConditionType switch
        {
            "MIN_ORDER_AMOUNT" or "MIN_CART_AMOUNT" or "MIN_ELIGIBLE_AMOUNT" =>
                Compare(context.CartSubtotal, value.Value, condition.ConditionOperator),
            "MIN_QUANTITY" or "QUANTITY" =>
                Compare(context.Quantity, value.Value, condition.ConditionOperator),
            _ => false
        };
    }

    private static decimal? ReadConditionValue(string json)
    {
        if (decimal.TryParse(json, out var direct)) return direct;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Number && root.TryGetDecimal(out var numeric)) return numeric;
            foreach (var name in new[] { "value", "amount", "quantity", "min", "minimum" })
            {
                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty(name, out var property) &&
                    property.ValueKind == JsonValueKind.Number &&
                    property.TryGetDecimal(out numeric))
                    return numeric;
            }
        }
        catch (JsonException)
        {
            return null;
        }
        return null;
    }

    private static bool Compare(decimal actual, decimal expected, string op) => op switch
    {
        ">" => actual > expected,
        ">=" or "GTE" or "MIN" => actual >= expected,
        "=" or "==" or "EQ" => actual == expected,
        "<=" or "LTE" => actual <= expected,
        "<" or "LT" => actual < expected,
        _ => false
    };

    private sealed record DeviceDiscountContext(
        string? ErrorCode, Guid OutletId, Guid TillId, Guid TillSessionId,
        PosDiscountAuthorityDto? Authority);
}
