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
    private static readonly ApplicationError NotFound = new("pricing.tax_aggregate.not_found", "Tax record was not found.");
    private static readonly ApplicationError DefaultJurisdictionNotFound = new("pricing.tax_aggregate.jurisdiction_error", "Failed to resolve default jurisdiction.");

    private readonly ITaxSetupRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TaxAggregateService(ITaxSetupRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        return context.Permissions.Contains(requiredPermission) ? null : PermissionDenied;
    }

    public async Task<ApplicationResult<Guid>> CreateTaxAsync(TenantRequestContext context, TaxAggregateCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.Create);
        if (accessError is not null) return ApplicationResult<Guid>.Failure(accessError);

        var existingClass = await _repository.GetTaxClassByCodeAsync(context.TenantId, request.TaxCode);
        if (existingClass != null)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_aggregate.code_exists", $"Tax with code '{request.TaxCode}' already exists."));

        var existingRate = await _repository.GetTaxRateByCodeAsync(context.TenantId, $"{request.TaxCode}-RATE");
        if (existingRate != null)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_aggregate.rate_exists", $"Tax rate with code '{request.TaxCode}-RATE' already exists."));

        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now.Date);
        var jurisdiction = await _repository.ResolveDefaultJurisdictionAsync(context.TenantId, context.UserId, now);

        // 1. Create TaxClass
        var taxClass = TaxClass.Create(
            context.TenantId,
            request.TaxCode,
            request.TaxName,
            request.TaxType,
            request.Description,
            false,
            context.UserId,
            now);

        // 2. Create TaxRate
        var taxRate = TaxRate.Create(
            context.TenantId,
            jurisdiction.Id,
            $"{request.TaxCode}-RATE",
            $"{request.TaxName} Rate",
            request.TaxPercentage,
            false,
            today,
            null,
            context.UserId,
            now);

        await _repository.AddTaxClassAsync(taxClass);
        await _repository.AddTaxRateAsync(taxRate);

        // 3. Create TaxClassRate linking
        var taxClassRate = TaxClassRate.Create(
            context.TenantId,
            taxClass.Id,
            taxRate.Id,
            1,
            context.UserId,
            now);

        await _repository.AddTaxClassRatesAsync(new[] { taxClassRate });

        await _repository.SaveChangesAsync();

        return ApplicationResult<Guid>.Success(taxClass.Id);
    }

    public async Task<ApplicationResult<bool>> UpdateTaxAsync(TenantRequestContext context, Guid id, TaxAggregateUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.Update);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, id);
        if (taxClass == null) return ApplicationResult<bool>.Failure(NotFound);

        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now.Date);
        var jurisdiction = await _repository.ResolveDefaultJurisdictionAsync(context.TenantId, context.UserId, now);

        // Update TaxClass
        taxClass.UpdateProfile(request.TaxName, request.TaxType, request.Description, request.Status, context.UserId);
        _repository.UpdateTaxClass(taxClass);

        var classRates = await _repository.GetTaxClassRatesAsync(context.TenantId, taxClass.Id);
        
        // Find existing rate, if any, to update percentage. If missing, create new.
        // We look for the one rate. In our simplified aggregate, there's a 1-to-1-to-1 relationship.
        var rateId = classRates.FirstOrDefault()?.TaxRateId;
        if (rateId != null)
        {
            var taxRate = await _repository.GetTaxRateByIdAsync(context.TenantId, rateId.Value);
            if (taxRate != null)
            {
                if (taxRate.RatePercent != request.TaxPercentage || taxRate.Status != request.Status)
                {
                    // History Snapshotting: End-date existing, create new one if percentage changes.
                    // For simplicity, if we follow "snapshots must remain unchanged", we end-date and create V2.
                    if (taxRate.RatePercent != request.TaxPercentage)
                    {
                        taxRate.UpdateProfile(taxRate.TaxRateName, taxRate.RatePercent, taxRate.IsCompound, taxRate.ValidFrom, today, "ARCHIVED", context.UserId);
                        _repository.UpdateTaxRate(taxRate);

                        // Create V2
                        var newRate = TaxRate.Create(
                            context.TenantId,
                            jurisdiction.Id,
                            $"{taxRate.TaxRateCode}-V{now.ToUnixTimeSeconds()}",
                            $"{request.TaxName} Rate",
                            request.TaxPercentage,
                            false,
                            today,
                            null,
                            context.UserId,
                            now);
                        
                        if (request.Status != "ACTIVE")
                        {
                            newRate.UpdateProfile(newRate.TaxRateName, newRate.RatePercent, newRate.IsCompound, newRate.ValidFrom, newRate.ValidUntil, request.Status, context.UserId);
                        }

                        await _repository.AddTaxRateAsync(newRate);
                        
                        _repository.RemoveTaxClassRates(classRates);
                        
                        var newClassRate = TaxClassRate.Create(
                            context.TenantId,
                            taxClass.Id,
                            newRate.Id,
                            1,
                            context.UserId,
                            now);
                        await _repository.AddTaxClassRatesAsync(new[] { newClassRate });
                    }
                    else
                    {
                        // Just update status and name
                        taxRate.UpdateProfile($"{request.TaxName} Rate", taxRate.RatePercent, taxRate.IsCompound, taxRate.ValidFrom, taxRate.ValidUntil, request.Status, context.UserId);
                        _repository.UpdateTaxRate(taxRate);
                    }
                }
            }
        }

        if (request.Status == "DELETED")
        {
             taxClass.SoftDelete(context.UserId);
             _repository.UpdateTaxClass(taxClass);
             // Soft delete rates
             if (rateId != null)
             {
                 var rate = await _repository.GetTaxRateByIdAsync(context.TenantId, rateId.Value);
                 if (rate != null)
                 {
                     rate.SoftDelete(context.UserId);
                     _repository.UpdateTaxRate(rate);
                 }
             }
             
             _repository.RemoveTaxClassRates(classRates);
        }

        await _repository.SaveChangesAsync();
        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<TaxAggregateResponse>> GetTaxAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.View);
        if (accessError is not null) return ApplicationResult<TaxAggregateResponse>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, id);
        if (taxClass == null) return ApplicationResult<TaxAggregateResponse>.Failure(NotFound);

        var rates = await _repository.GetRatesForClassAsync(context.TenantId, taxClass.Id);
        var activeRate = rates.FirstOrDefault(x => x.Status == "ACTIVE") ?? rates.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

        var response = new TaxAggregateResponse
        {
            Id = taxClass.Id,
            TaxName = taxClass.TaxClassName,
            TaxCode = taxClass.TaxClassCode,
            TaxType = taxClass.TaxType,
            Description = taxClass.Description,
            Status = taxClass.Status,
            TaxPercentage = activeRate?.RatePercent ?? 0m
        };

        return ApplicationResult<TaxAggregateResponse>.Success(response);
    }

    public async Task<ApplicationResult<TaxAggregateListResponse>> GetTaxesAsync(TenantRequestContext context, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.View);
        if (accessError is not null) return ApplicationResult<TaxAggregateListResponse>.Failure(accessError);

        var (items, totalCount) = await _repository.GetTaxClassesAsync(context.TenantId, pageNumber, pageSize);

        var responses = new List<TaxAggregateResponse>();
        foreach (var taxClass in items)
        {
            var rates = await _repository.GetRatesForClassAsync(context.TenantId, taxClass.Id);
            var activeRate = rates.FirstOrDefault(x => x.Status == "ACTIVE") ?? rates.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

            responses.Add(new TaxAggregateResponse
            {
                Id = taxClass.Id,
                TaxName = taxClass.TaxClassName,
                TaxCode = taxClass.TaxClassCode,
                TaxType = taxClass.TaxType,
                Description = taxClass.Description,
                Status = taxClass.Status,
                TaxPercentage = activeRate?.RatePercent ?? 0m
            });
        }

        var listResponse = new TaxAggregateListResponse(
            responses,
            pageNumber,
            pageSize,
            totalCount);

        return ApplicationResult<TaxAggregateListResponse>.Success(listResponse);
    }

    public async Task<ApplicationResult<bool>> DeleteTaxAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.Delete);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, id);
        if (taxClass == null) return ApplicationResult<bool>.Failure(NotFound);

        taxClass.SoftDelete(context.UserId);
        _repository.UpdateTaxClass(taxClass);

        var rates = await _repository.GetRatesForClassAsync(context.TenantId, taxClass.Id);
        foreach (var rate in rates)
        {
            rate.SoftDelete(context.UserId);
            _repository.UpdateTaxRate(rate);
        }

        var classRates = await _repository.GetTaxClassRatesAsync(context.TenantId, taxClass.Id);
        _repository.RemoveTaxClassRates(classRates);

        await _repository.SaveChangesAsync();

        return ApplicationResult<bool>.Success(true);
    }
}
