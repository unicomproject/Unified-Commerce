using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;

namespace E_POS.Application.Modules.Tenant.PricingTax.Services;

public sealed class TaxAggregateService : ITaxAggregateService
{
    private static readonly ApplicationError PermissionDenied = new("pricing.tax_aggregate.permission_denied", "Permission denied for tax management.");
    private static readonly ApplicationError NotFound = new("pricing.tax_aggregate.not_found", "Tax Setup was not found.");

    private readonly ITaxSetupRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TaxAggregateService(ITaxSetupRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    private static bool HasPermission(TenantRequestContext context, string required) =>
        context.Permissions.Contains(required);

    private static ApplicationError? Require(TenantRequestContext context, string required) =>
        HasPermission(context, required) ? null : PermissionDenied;

    public async Task<ApplicationResult<Guid>> CreateTaxAsync(
        TenantRequestContext context,
        TaxAggregateCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxClasses.Create);
        if (accessError is not null) return ApplicationResult<Guid>.Failure(accessError);

        var validation = ValidateCreate(request);
        if (validation is not null) return ApplicationResult<Guid>.Failure(validation);

        var code = request.Code.Trim().ToUpperInvariant();
        var existingClass = await _repository.GetTaxClassByCodeAsync(context.TenantId, code);
        if (existingClass != null)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_aggregate.code_exists", $"Tax code '{code}' already exists."));

        var treatment = TaxTreatments.Normalize(request.TaxTreatment);
        var rateValue = ResolveCreateRate(treatment, request.InitialRate);
        if (rateValue.error is not null)
            return ApplicationResult<Guid>.Failure(rateValue.error);

        var now = _dateTimeProvider.UtcNow;
        var timezone = await _repository.GetTenantTimezoneAsync(context.TenantId, cancellationToken);
        var businessToday = TaxRateResolution.ToBusinessDate(now, timezone);
        if (request.EffectiveFrom < businessToday)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_aggregate.invalid_effective_from", "Effective From cannot be before the business date."));

        Guid createdId = Guid.Empty;
        await _repository.ExecuteInTransactionAsync(async ct =>
        {
            var jurisdiction = await _repository.ResolveDefaultJurisdictionAsync(context.TenantId, context.UserId, now);
            var taxClass = TaxClass.Create(
                context.TenantId,
                code,
                request.Name,
                treatment,
                request.Description,
                false,
                context.UserId,
                now);

            var taxRate = TaxRate.Create(
                context.TenantId,
                jurisdiction.Id,
                $"{code}-RATE",
                $"{request.Name.Trim()} Rate",
                rateValue.rate,
                false,
                request.EffectiveFrom,
                null,
                context.UserId,
                now);

            await _repository.AddTaxClassAsync(taxClass);
            await _repository.AddTaxRateAsync(taxRate);
            await _repository.AddTaxClassRatesAsync(new[]
            {
                TaxClassRate.Create(context.TenantId, taxClass.Id, taxRate.Id, 1, context.UserId, now)
            });
            createdId = taxClass.Id;
        }, cancellationToken);

        return ApplicationResult<Guid>.Success(createdId);
    }

    public async Task<ApplicationResult<bool>> UpdateTaxAsync(
        TenantRequestContext context,
        Guid id,
        TaxAggregateUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxClasses.Update);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        if (string.IsNullOrWhiteSpace(request.Name))
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.name_required", "Tax Name is required."));

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, id);
        if (taxClass is null) return ApplicationResult<bool>.Failure(NotFound);

        var now = _dateTimeProvider.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code = request.Code.Trim().ToUpperInvariant();
            if (!string.Equals(code, taxClass.TaxClassCode, StringComparison.Ordinal))
            {
                var used = await _repository.HasTransactionalUsageAsync(context.TenantId, id, cancellationToken);
                if (used)
                    return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.code_locked", "Tax Code cannot be changed after the Tax Setup is in use."));

                var existing = await _repository.GetTaxClassByCodeAsync(context.TenantId, code);
                if (existing is not null && existing.Id != id)
                    return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.code_exists", $"Tax code '{code}' already exists."));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.TaxTreatment))
        {
            if (!TaxTreatments.IsValid(request.TaxTreatment))
                return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.invalid_treatment", "Tax Treatment is invalid."));

            var newTreatment = TaxTreatments.Normalize(request.TaxTreatment);
            if (!string.Equals(newTreatment, taxClass.TaxTreatment, StringComparison.Ordinal))
            {
                var used = await _repository.HasTransactionalUsageAsync(context.TenantId, id, cancellationToken);
                if (used)
                    return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.treatment_locked", "Tax Treatment cannot be changed after transactional usage."));

                if (newTreatment == TaxTreatments.ZeroRated)
                {
                    var rates = await _repository.GetRatesForClassAsync(context.TenantId, id);
                    var timezone = await _repository.GetTenantTimezoneAsync(context.TenantId, cancellationToken);
                    var businessToday = TaxRateResolution.ToBusinessDate(now, timezone);
                    var current = TaxRateResolution.ResolveCurrent(rates, businessToday);
                    if (current is not null && current.RatePercent != 0m)
                        return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.zero_rated_rate", "ZERO_RATED requires current rate to be 0%."));
                }

                taxClass.ChangeTreatment(newTreatment, context.UserId, now);
            }
        }

        taxClass.UpdateProfile(request.Name, request.Description, context.UserId, now);
        _repository.UpdateTaxClass(taxClass);
        await _repository.SaveChangesAsync();
        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<TaxAggregateResponse>> GetTaxAsync(
        TenantRequestContext context,
        Guid id,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxClasses.View);
        if (accessError is not null) return ApplicationResult<TaxAggregateResponse>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, id);
        if (taxClass is null) return ApplicationResult<TaxAggregateResponse>.Failure(NotFound);

        var rates = await _repository.GetRatesForClassAsync(context.TenantId, id);
        var counts = await _repository.GetProductCountsAsync(context.TenantId, new[] { id }, cancellationToken);
        var timezone = await _repository.GetTenantTimezoneAsync(context.TenantId, cancellationToken);
        var businessToday = TaxRateResolution.ToBusinessDate(_dateTimeProvider.UtcNow, timezone);

        var response = MapDetail(taxClass, rates, counts.GetValueOrDefault(id), businessToday, includeHistory: true);
        return ApplicationResult<TaxAggregateResponse>.Success(response);
    }

    public async Task<ApplicationResult<TaxAggregateListResponse>> GetTaxesAsync(
        TenantRequestContext context,
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxClasses.View);
        if (accessError is not null) return ApplicationResult<TaxAggregateListResponse>.Failure(accessError);

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _repository.GetTaxClassesFilteredAsync(
            context.TenantId, search, status, pageNumber, pageSize, cancellationToken);

        var ids = items.Select(x => x.Id).ToList();
        var ratesByClass = await _repository.GetRatesForClassesAsync(context.TenantId, ids, cancellationToken);
        var counts = await _repository.GetProductCountsAsync(context.TenantId, ids, cancellationToken);
        var timezone = await _repository.GetTenantTimezoneAsync(context.TenantId, cancellationToken);
        var businessToday = TaxRateResolution.ToBusinessDate(_dateTimeProvider.UtcNow, timezone);

        var responses = items.Select(tax =>
            MapDetail(
                tax,
                ratesByClass.GetValueOrDefault(tax.Id) ?? new List<TaxRate>(),
                counts.GetValueOrDefault(tax.Id),
                businessToday,
                includeHistory: false)).ToList();

        return ApplicationResult<TaxAggregateListResponse>.Success(
            new TaxAggregateListResponse(responses, pageNumber, pageSize, totalCount));
    }

    public async Task<ApplicationResult<Guid>> ScheduleRateAsync(
        TenantRequestContext context,
        Guid taxSetupId,
        TaxScheduleRateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxRates.ScheduleManage);
        if (accessError is not null) return ApplicationResult<Guid>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, taxSetupId);
        if (taxClass is null) return ApplicationResult<Guid>.Failure(NotFound);

        if (taxClass.TaxTreatment == TaxTreatments.Exempt)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_aggregate.exempt_schedule", "EXEMPT Tax Setup does not support percentage rate scheduling."));

        if (taxClass.TaxTreatment == TaxTreatments.ZeroRated && request.NewRate != 0m)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_aggregate.zero_rated_rate", "ZERO_RATED rate must be 0%."));

        if (taxClass.TaxTreatment == TaxTreatments.Taxable && (request.NewRate < 0m || request.NewRate > 100m))
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_aggregate.invalid_rate", "Rate must be between 0 and 100."));

        var now = _dateTimeProvider.UtcNow;
        var timezone = await _repository.GetTenantTimezoneAsync(context.TenantId, cancellationToken);
        var businessToday = TaxRateResolution.ToBusinessDate(now, timezone);
        if (request.EffectiveFrom <= businessToday)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_aggregate.schedule_not_future", "Scheduled Effective From must be a future business date."));

        Guid newRateId = Guid.Empty;
        try
        {
            await _repository.ExecuteInTransactionAsync(async ct =>
            {
                var rates = await _repository.GetRatesForClassAsync(context.TenantId, taxSetupId);
                if (rates.Any(r => r.Status != "DELETED" && r.ValidFrom == request.EffectiveFrom))
                    throw new TaxDomainException("pricing.tax_aggregate.duplicate_effective_from", "A rate already exists for this Effective From date.");

                var proposedUntil = (DateOnly?)null;
                foreach (var existing in rates.Where(r => r.Status != "DELETED"))
                {
                    if (TaxRateResolution.PeriodsOverlap(existing.ValidFrom, existing.ValidUntil, request.EffectiveFrom, proposedUntil))
                    {
                        // Allow overlap only if we end the current open-ended rate at EffectiveFrom-1.
                        if (existing.ValidUntil is null &&
                            existing.ValidFrom.HasValue &&
                            existing.ValidFrom.Value < request.EffectiveFrom &&
                            TaxRateResolution.ResolveCurrent(rates, businessToday)?.Id == existing.Id)
                        {
                            continue;
                        }

                        if (existing.IsFutureRelativeTo(businessToday) ||
                            existing.IsApplicableOn(businessToday))
                        {
                            if (existing.ValidFrom == request.EffectiveFrom)
                                throw new TaxDomainException("pricing.tax_aggregate.duplicate_effective_from", "A rate already exists for this Effective From date.");
                        }
                    }
                }

                var current = TaxRateResolution.ResolveCurrent(rates, businessToday);
                if (current is not null && current.ValidUntil is null)
                {
                    current.EndOn(request.EffectiveFrom, context.UserId, now);
                    _repository.UpdateTaxRate(current);
                }

                // Re-validate no remaining overlaps after ending current.
                var projectedUntil = (DateOnly?)null;
                foreach (var existing in rates.Where(r => r.Status != "DELETED" && r.Id != current?.Id))
                {
                    var until = existing.Id == current?.Id ? request.EffectiveFrom.AddDays(-1) : existing.ValidUntil;
                    if (existing.Id == current?.Id)
                        until = request.EffectiveFrom.AddDays(-1);

                    if (TaxRateResolution.PeriodsOverlap(existing.ValidFrom, until, request.EffectiveFrom, projectedUntil) &&
                        existing.ValidFrom != request.EffectiveFrom)
                    {
                        if (existing.IsFutureRelativeTo(businessToday) &&
                            existing.ValidFrom.HasValue &&
                            existing.ValidFrom.Value > request.EffectiveFrom)
                        {
                            // Future rates after this one: clamp this new rate's ValidUntil to day before next future.
                        }
                    }
                }

                var nextFuture = rates
                    .Where(r => r.Status != "DELETED" && r.ValidFrom.HasValue && r.ValidFrom.Value > request.EffectiveFrom)
                    .OrderBy(r => r.ValidFrom)
                    .FirstOrDefault();
                DateOnly? newUntil = nextFuture?.ValidFrom?.AddDays(-1);

                var jurisdiction = await _repository.ResolveDefaultJurisdictionAsync(context.TenantId, context.UserId, now);
                var newRate = TaxRate.Create(
                    context.TenantId,
                    jurisdiction.Id,
                    $"{taxClass.TaxClassCode}-RATE-{request.EffectiveFrom:yyyyMMdd}",
                    $"{taxClass.TaxClassName} Rate",
                    request.NewRate,
                    false,
                    request.EffectiveFrom,
                    newUntil,
                    context.UserId,
                    now,
                    request.Notes);

                // Unique code collision: append suffix
                var codeConflict = await _repository.GetTaxRateByCodeAsync(context.TenantId, newRate.TaxRateCode);
                if (codeConflict is not null)
                {
                    newRate = TaxRate.Create(
                        context.TenantId,
                        jurisdiction.Id,
                        $"{taxClass.TaxClassCode}-RATE-{request.EffectiveFrom:yyyyMMdd}-{now.ToUnixTimeSeconds()}",
                        $"{taxClass.TaxClassName} Rate",
                        request.NewRate,
                        false,
                        request.EffectiveFrom,
                        newUntil,
                        context.UserId,
                        now,
                        request.Notes);
                }

                await _repository.AddTaxRateAsync(newRate);
                await _repository.AddTaxClassRatesAsync(new[]
                {
                    TaxClassRate.Create(context.TenantId, taxSetupId, newRate.Id, 1, context.UserId, now)
                });
                newRateId = newRate.Id;
            }, cancellationToken);
        }
        catch (TaxDomainException ex)
        {
            return ApplicationResult<Guid>.Failure(new ApplicationError(ex.Code, ex.Message));
        }

        return ApplicationResult<Guid>.Success(newRateId);
    }

    public async Task<ApplicationResult<bool>> UpdateFutureRateAsync(
        TenantRequestContext context,
        Guid taxSetupId,
        Guid rateId,
        TaxFutureRateUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxRates.ScheduleManage);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, taxSetupId);
        if (taxClass is null) return ApplicationResult<bool>.Failure(NotFound);

        if (taxClass.TaxTreatment == TaxTreatments.Exempt)
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.exempt_schedule", "EXEMPT Tax Setup does not support percentage rate scheduling."));

        if (taxClass.TaxTreatment == TaxTreatments.ZeroRated && request.NewRate != 0m)
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.zero_rated_rate", "ZERO_RATED rate must be 0%."));

        var rates = await _repository.GetRatesForClassAsync(context.TenantId, taxSetupId);
        var rate = rates.FirstOrDefault(r => r.Id == rateId);
        if (rate is null)
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.rate_not_found", "Tax rate was not found for this Tax Setup."));

        var timezone = await _repository.GetTenantTimezoneAsync(context.TenantId, cancellationToken);
        var businessToday = TaxRateResolution.ToBusinessDate(_dateTimeProvider.UtcNow, timezone);
        if (!rate.IsFutureRelativeTo(businessToday))
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.rate_not_future", "Only future scheduled rates can be edited."));

        if (await _repository.RateReferencedByTransactionsAsync(context.TenantId, rateId, cancellationToken))
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.rate_immutable", "Rate is referenced by transactions and cannot be edited."));

        if (request.EffectiveFrom <= businessToday)
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.schedule_not_future", "Scheduled Effective From must be a future business date."));

        if (rates.Any(r => r.Id != rateId && r.Status != "DELETED" && r.ValidFrom == request.EffectiveFrom))
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.duplicate_effective_from", "A rate already exists for this Effective From date."));

        var now = _dateTimeProvider.UtcNow;
        rate.UpdateFutureSchedule(request.NewRate, request.EffectiveFrom, request.Notes, context.UserId, now);
        _repository.UpdateTaxRate(rate);

        // Keep previous open rate end aligned if this was the immediate next.
        var current = TaxRateResolution.ResolveCurrent(rates, businessToday);
        if (current is not null)
        {
            var next = TaxRateResolution.ResolveNext(rates.Where(r => r.Id != rateId).Append(rate), businessToday);
            if (next?.Id == rate.Id && current.ValidUntil != request.EffectiveFrom.AddDays(-1))
            {
                current.EndOn(request.EffectiveFrom, context.UserId, now);
                _repository.UpdateTaxRate(current);
            }
        }

        await _repository.SaveChangesAsync();
        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<bool>> DeleteFutureRateAsync(
        TenantRequestContext context,
        Guid taxSetupId,
        Guid rateId,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxRates.ScheduleManage);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, taxSetupId);
        if (taxClass is null) return ApplicationResult<bool>.Failure(NotFound);

        var rates = await _repository.GetRatesForClassAsync(context.TenantId, taxSetupId);
        var rate = rates.FirstOrDefault(r => r.Id == rateId);
        if (rate is null)
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.rate_not_found", "Tax rate was not found for this Tax Setup."));

        var timezone = await _repository.GetTenantTimezoneAsync(context.TenantId, cancellationToken);
        var businessToday = TaxRateResolution.ToBusinessDate(_dateTimeProvider.UtcNow, timezone);
        if (!rate.IsFutureRelativeTo(businessToday))
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.rate_not_future", "Only future scheduled rates can be deleted."));

        if (await _repository.RateReferencedByTransactionsAsync(context.TenantId, rateId, cancellationToken))
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_aggregate.rate_immutable", "Rate is referenced by transactions and cannot be deleted."));

        var now = _dateTimeProvider.UtcNow;
        rate.SoftDelete(context.UserId);
        _repository.UpdateTaxRate(rate);

        // Re-open current ValidUntil if this deleted rate was the next boundary.
        var remaining = rates.Where(r => r.Id != rateId && r.Status != "DELETED").ToList();
        var current = TaxRateResolution.ResolveCurrent(remaining, businessToday);
        var next = TaxRateResolution.ResolveNext(remaining, businessToday);
        if (current is not null)
        {
            if (next is null)
            {
                current.UpdateProfile(current.TaxRateName, current.RatePercent, current.IsCompound, current.ValidFrom, null, current.Status, context.UserId, current.Notes);
            }
            else if (next.ValidFrom.HasValue)
            {
                current.EndOn(next.ValidFrom.Value, context.UserId, now);
            }

            _repository.UpdateTaxRate(current);
        }

        var links = await _repository.GetTaxClassRatesAsync(context.TenantId, taxSetupId);
        var link = links.Where(x => x.TaxRateId == rateId).ToList();
        if (link.Count > 0)
            _repository.RemoveTaxClassRates(link);

        await _repository.SaveChangesAsync();
        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<TaxStatusChangeResponse>> ActivateAsync(
        TenantRequestContext context,
        Guid taxSetupId,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxClasses.StatusManage);
        if (accessError is not null) return ApplicationResult<TaxStatusChangeResponse>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, taxSetupId);
        if (taxClass is null) return ApplicationResult<TaxStatusChangeResponse>.Failure(NotFound);

        var now = _dateTimeProvider.UtcNow;
        taxClass.SetStatus("ACTIVE", context.UserId, now);
        _repository.UpdateTaxClass(taxClass);
        await _repository.SaveChangesAsync();

        var counts = await _repository.GetProductCountsAsync(context.TenantId, new[] { taxSetupId }, cancellationToken);
        return ApplicationResult<TaxStatusChangeResponse>.Success(new TaxStatusChangeResponse
        {
            Id = taxSetupId,
            Status = "ACTIVE",
            ProductCount = counts.GetValueOrDefault(taxSetupId)
        });
    }

    public async Task<ApplicationResult<TaxStatusChangeResponse>> DeactivateAsync(
        TenantRequestContext context,
        Guid taxSetupId,
        TaxStatusChangeRequest? request,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxClasses.StatusManage);
        if (accessError is not null) return ApplicationResult<TaxStatusChangeResponse>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, taxSetupId);
        if (taxClass is null) return ApplicationResult<TaxStatusChangeResponse>.Failure(NotFound);

        var counts = await _repository.GetProductCountsAsync(context.TenantId, new[] { taxSetupId }, cancellationToken);
        var productCount = counts.GetValueOrDefault(taxSetupId);

        var now = _dateTimeProvider.UtcNow;
        taxClass.SetStatus("INACTIVE", context.UserId, now);
        _repository.UpdateTaxClass(taxClass);
        await _repository.SaveChangesAsync();

        // Option B: do not clear product assignments.
        return ApplicationResult<TaxStatusChangeResponse>.Success(new TaxStatusChangeResponse
        {
            Id = taxSetupId,
            Status = "INACTIVE",
            ProductCount = productCount
        });
    }

    public async Task<ApplicationResult<TaxProductsUsingListResponse>> GetProductsUsingAsync(
        TenantRequestContext context,
        Guid taxSetupId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var accessError = Require(context, PricingTaxPermissions.TaxClasses.ProductsView);
        if (accessError is not null)
        {
            // Fallback: allow list viewers with view permission.
            if (!HasPermission(context, PricingTaxPermissions.TaxClasses.View))
                return ApplicationResult<TaxProductsUsingListResponse>.Failure(accessError);
        }

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, taxSetupId);
        if (taxClass is null) return ApplicationResult<TaxProductsUsingListResponse>.Failure(NotFound);

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _repository.GetProductsUsingTaxAsync(
            context.TenantId, taxSetupId, search, pageNumber, pageSize, cancellationToken);

        return ApplicationResult<TaxProductsUsingListResponse>.Success(
            new TaxProductsUsingListResponse(items, pageNumber, pageSize, total));
    }

    public async Task<ApplicationResult<bool>> DeleteTaxAsync(
        TenantRequestContext context,
        Guid id,
        CancellationToken cancellationToken)
    {
        // Prefer deactivate; hard soft-delete only when unused.
        var accessError = Require(context, PricingTaxPermissions.TaxClasses.StatusManage);
        if (accessError is not null && !HasPermission(context, PricingTaxPermissions.TaxClasses.LegacyDelete))
            return ApplicationResult<bool>.Failure(PermissionDenied);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, id);
        if (taxClass is null) return ApplicationResult<bool>.Failure(NotFound);

        if (await _repository.HasTransactionalUsageAsync(context.TenantId, id, cancellationToken))
            return ApplicationResult<bool>.Failure(new ApplicationError(
                "pricing.tax_aggregate.delete_restricted",
                "Tax Setup cannot be deleted once assigned to products or referenced by transactions. Deactivate instead."));

        taxClass.SoftDelete(context.UserId);
        _repository.UpdateTaxClass(taxClass);

        var rates = await _repository.GetRatesForClassAsync(context.TenantId, id);
        foreach (var rate in rates)
        {
            rate.SoftDelete(context.UserId);
            _repository.UpdateTaxRate(rate);
        }

        var classRates = await _repository.GetTaxClassRatesAsync(context.TenantId, id);
        _repository.RemoveTaxClassRates(classRates);
        await _repository.SaveChangesAsync();
        return ApplicationResult<bool>.Success(true);
    }

    private static ApplicationError? ValidateCreate(TaxAggregateCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return new ApplicationError("pricing.tax_aggregate.name_required", "Tax Name is required.");
        if (string.IsNullOrWhiteSpace(request.Code))
            return new ApplicationError("pricing.tax_aggregate.code_required", "Tax Code is required.");
        if (!TaxTreatments.IsValid(request.TaxTreatment))
            return new ApplicationError("pricing.tax_aggregate.invalid_treatment", "Tax Treatment must be TAXABLE, ZERO_RATED, or EXEMPT.");
        if (request.Name.Trim().Length > 150)
            return new ApplicationError("pricing.tax_aggregate.name_length", "Tax Name must be at most 150 characters.");
        if (request.Code.Trim().Length > 80)
            return new ApplicationError("pricing.tax_aggregate.code_length", "Tax Code must be at most 80 characters.");
        return null;
    }

    private static (decimal rate, ApplicationError? error) ResolveCreateRate(string treatment, decimal? initialRate)
    {
        if (treatment == TaxTreatments.Exempt)
            return (0m, null);

        if (treatment == TaxTreatments.ZeroRated)
        {
            if (initialRate.HasValue && initialRate.Value != 0m)
                return (0m, new ApplicationError("pricing.tax_aggregate.zero_rated_rate", "ZERO_RATED rate must be 0%."));
            return (0m, null);
        }

        if (!initialRate.HasValue)
            return (0m, new ApplicationError("pricing.tax_aggregate.rate_required", "Initial rate is required for TAXABLE."));
        if (initialRate.Value < 0m || initialRate.Value > 100m)
            return (0m, new ApplicationError("pricing.tax_aggregate.invalid_rate", "Rate must be between 0 and 100."));
        return (initialRate.Value, null);
    }

    private static TaxAggregateResponse MapDetail(
        TaxClass taxClass,
        IReadOnlyList<TaxRate> rates,
        int productCount,
        DateOnly businessToday,
        bool includeHistory)
    {
        var current = TaxRateResolution.ResolveCurrent(rates, businessToday);
        var next = TaxRateResolution.ResolveNext(rates, businessToday);

        decimal? currentRate = taxClass.TaxTreatment == TaxTreatments.Exempt
            ? null
            : current?.RatePercent;

        var response = new TaxAggregateResponse
        {
            Id = taxClass.Id,
            Name = taxClass.TaxClassName,
            Code = taxClass.TaxClassCode,
            Description = taxClass.Description,
            TaxTreatment = taxClass.TaxTreatment,
            Status = taxClass.Status,
            CurrentRate = currentRate,
            CurrentRateEffectiveFrom = current?.ValidFrom,
            NextRate = next?.RatePercent,
            NextRateEffectiveFrom = next?.ValidFrom,
            ProductCount = productCount,
            IsSeeded = taxClass.IsSeeded
        };

        if (includeHistory)
        {
            response.RateHistory = rates
                .OrderBy(r => r.ValidFrom ?? DateOnly.MinValue)
                .ThenBy(r => r.CreatedAt)
                .Select(r => new TaxRateHistoryItemResponse
                {
                    Id = r.Id,
                    Rate = r.RatePercent,
                    EffectiveFrom = r.ValidFrom,
                    EffectiveTo = r.ValidUntil,
                    State = TaxRateResolution.Classify(r, businessToday),
                    Notes = r.Notes
                })
                .ToList();
        }

        return response;
    }

    private sealed class TaxDomainException : Exception
    {
        public string Code { get; }
        public TaxDomainException(string code, string message) : base(message) => Code = code;
    }
}
