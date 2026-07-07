using E_POS.Application.Common.Models;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;

namespace E_POS.Application.Modules.Tenant.PricingTax.Services;

public sealed class TaxSetupService : ITaxSetupService
{
    private static readonly ApplicationError PermissionDenied = new("pricing.tax_setup.permission_denied", "Permission denied for tax setup management.");
    private static readonly ApplicationError NotFound = new("pricing.tax_setup.not_found", "Tax setup record was not found.");
    
    private readonly ITaxSetupRepository _repository;
    private readonly ITaxSetupRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TaxSetupService(
        ITaxSetupRepository repository,
        ITaxSetupRequestValidator validator,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        return context.Permissions.Contains(requiredPermission) ? null : PermissionDenied;
    }

    public async Task<ApplicationResult<Guid>> CreateTaxClassAsync(TenantRequestContext context, TaxClassCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.Create);
        if (accessError is not null) return ApplicationResult<Guid>.Failure(accessError);

        var validationError = _validator.ValidateTaxClassCreate(request);
        if (validationError is not null) return ApplicationResult<Guid>.Failure(validationError);

        var existing = await _repository.GetTaxClassByCodeAsync(context.TenantId, request.TaxClassCode);
        if (existing != null)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_class.code_exists", $"Tax Class with code '{request.TaxClassCode}' already exists."));

        var taxClass = TaxClass.Create(context.TenantId, request.TaxClassCode, request.TaxClassName, request.Description, request.IsDefaultTaxClass, context.UserId, _dateTimeProvider.UtcNow);

        if (request.IsDefaultTaxClass)
        {
            await _repository.ClearDefaultTaxClassAsync(context.TenantId, null);
        }

        await _repository.AddTaxClassAsync(taxClass);

        if (request.AssignedRateIds != null && request.AssignedRateIds.Any())
        {
            var rates = new List<TaxClassRate>();
            int order = 1;
            foreach (var rateId in request.AssignedRateIds)
            {
                var rate = await _repository.GetTaxRateByIdAsync(context.TenantId, rateId);
                if (rate == null)
                    return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_rate.not_found", $"Tax Rate with ID '{rateId}' not found."));

                rates.Add(TaxClassRate.Create(context.TenantId, taxClass.Id, rateId, order++, context.UserId, _dateTimeProvider.UtcNow));
            }
            await _repository.AddTaxClassRatesAsync(rates);
        }

        await _repository.SaveChangesAsync();
        return ApplicationResult<Guid>.Success(taxClass.Id);
    }

    public async Task<ApplicationResult<bool>> UpdateTaxClassAsync(TenantRequestContext context, Guid id, TaxClassUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.Update);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var validationError = _validator.ValidateTaxClassUpdate(request);
        if (validationError is not null) return ApplicationResult<bool>.Failure(validationError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, id);
        if (taxClass == null)
            return ApplicationResult<bool>.Failure(NotFound);

        taxClass.UpdateProfile(request.TaxClassName, request.Description, context.UserId);

        if (request.IsDefaultTaxClass && !taxClass.IsDefaultTaxClass)
        {
            await _repository.ClearDefaultTaxClassAsync(context.TenantId, taxClass.Id);
            taxClass.SetDefault(true, context.UserId);
        }
        else if (!request.IsDefaultTaxClass && taxClass.IsDefaultTaxClass)
        {
            taxClass.SetDefault(false, context.UserId);
        }

        _repository.UpdateTaxClass(taxClass);

        if (request.AssignedRateIds != null)
        {
            var existingRates = await _repository.GetTaxClassRatesAsync(context.TenantId, taxClass.Id);
            _repository.RemoveTaxClassRates(existingRates);

            var newRates = new List<TaxClassRate>();
            int order = 1;
            foreach (var rateId in request.AssignedRateIds)
            {
                var rate = await _repository.GetTaxRateByIdAsync(context.TenantId, rateId);
                if (rate == null)
                    return ApplicationResult<bool>.Failure(new ApplicationError("pricing.tax_rate.not_found", $"Tax Rate with ID '{rateId}' not found."));

                newRates.Add(TaxClassRate.Create(context.TenantId, taxClass.Id, rateId, order++, context.UserId, _dateTimeProvider.UtcNow));
            }
            await _repository.AddTaxClassRatesAsync(newRates);
        }

        await _repository.SaveChangesAsync();
        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<TaxClassResponse>> GetTaxClassAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.View);
        if (accessError is not null) return ApplicationResult<TaxClassResponse>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, id);
        if (taxClass == null)
            return ApplicationResult<TaxClassResponse>.Failure(NotFound);

        var response = new TaxClassResponse
        {
            Id = taxClass.Id,
            TaxClassCode = taxClass.TaxClassCode,
            TaxClassName = taxClass.TaxClassName,
            Description = taxClass.Description,
            IsDefaultTaxClass = taxClass.IsDefaultTaxClass,
            Status = taxClass.Status
        };

        var classRates = await _repository.GetTaxClassRatesAsync(context.TenantId, id);
        var rateDetails = new List<TaxClassRateDetailResponse>();
        
        foreach (var cr in classRates.OrderBy(x => x.SortOrder))
        {
            var rate = await _repository.GetTaxRateByIdAsync(context.TenantId, cr.TaxRateId);
            if (rate != null)
            {
                rateDetails.Add(new TaxClassRateDetailResponse
                {
                    TaxRateId = rate.Id,
                    TaxRateCode = rate.TaxRateCode,
                    TaxRateName = rate.TaxRateName,
                    RatePercent = rate.RatePercent,
                    IsCompound = rate.IsCompound,
                    SortOrder = cr.SortOrder
                });
            }
        }
        
        response.AssignedRates = rateDetails;
        return ApplicationResult<TaxClassResponse>.Success(response);
    }

    public async Task<ApplicationResult<TaxClassListResponse>> GetTaxClassesAsync(TenantRequestContext context, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.View);
        if (accessError is not null) return ApplicationResult<TaxClassListResponse>.Failure(accessError);

        var (items, totalCount) = await _repository.GetTaxClassesAsync(context.TenantId, pageNumber, pageSize);
        
        var responses = items.Select(x => new TaxClassResponse
        {
            Id = x.Id,
            TaxClassCode = x.TaxClassCode,
            TaxClassName = x.TaxClassName,
            Description = x.Description,
            IsDefaultTaxClass = x.IsDefaultTaxClass,
            Status = x.Status
        }).ToList();
        
        var listResponse = new TaxClassListResponse(
            responses,
            pageNumber,
            pageSize,
            totalCount);
        
        return ApplicationResult<TaxClassListResponse>.Success(listResponse);
    }

    public async Task<ApplicationResult<bool>> DeleteTaxClassAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxClasses.Delete);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var taxClass = await _repository.GetTaxClassByIdAsync(context.TenantId, id);
        if (taxClass == null)
            return ApplicationResult<bool>.Failure(NotFound);

        taxClass.SoftDelete(context.UserId);
        _repository.UpdateTaxClass(taxClass);
        await _repository.SaveChangesAsync();

        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<Guid>> CreateTaxRateAsync(TenantRequestContext context, TaxRateCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxRates.Create);
        if (accessError is not null) return ApplicationResult<Guid>.Failure(accessError);

        var validationError = _validator.ValidateTaxRateCreate(request);
        if (validationError is not null) return ApplicationResult<Guid>.Failure(validationError);

        if (!await _repository.JurisdictionExistsAsync(context.TenantId, request.TaxJurisdictionId))
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_rate.jurisdiction_not_found", $"Tax Jurisdiction with ID '{request.TaxJurisdictionId}' not found."));

        var existing = await _repository.GetTaxRateByCodeAsync(context.TenantId, request.TaxRateCode);
        if (existing != null)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.tax_rate.code_exists", $"Tax Rate with code '{request.TaxRateCode}' already exists."));

        var taxRate = TaxRate.Create(context.TenantId, request.TaxJurisdictionId, request.TaxRateCode, request.TaxRateName, request.RatePercent, request.IsCompound, request.ValidFrom, request.ValidUntil, context.UserId, _dateTimeProvider.UtcNow);
        
        await _repository.AddTaxRateAsync(taxRate);
        await _repository.SaveChangesAsync();

        return ApplicationResult<Guid>.Success(taxRate.Id);
    }

    public async Task<ApplicationResult<bool>> UpdateTaxRateAsync(TenantRequestContext context, Guid id, TaxRateUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxRates.Update);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var validationError = _validator.ValidateTaxRateUpdate(request);
        if (validationError is not null) return ApplicationResult<bool>.Failure(validationError);

        var taxRate = await _repository.GetTaxRateByIdAsync(context.TenantId, id);
        if (taxRate == null)
            return ApplicationResult<bool>.Failure(NotFound);

        taxRate.UpdateProfile(request.TaxRateName, request.RatePercent, request.IsCompound, request.ValidFrom, request.ValidUntil, request.Status, context.UserId);
        
        _repository.UpdateTaxRate(taxRate);
        await _repository.SaveChangesAsync();

        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<TaxRateResponse>> GetTaxRateAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxRates.View);
        if (accessError is not null) return ApplicationResult<TaxRateResponse>.Failure(accessError);

        var taxRate = await _repository.GetTaxRateByIdAsync(context.TenantId, id);
        if (taxRate == null)
            return ApplicationResult<TaxRateResponse>.Failure(NotFound);

        var response = new TaxRateResponse
        {
            Id = taxRate.Id,
            TaxJurisdictionId = taxRate.TaxJurisdictionId,
            TaxRateCode = taxRate.TaxRateCode,
            TaxRateName = taxRate.TaxRateName,
            RatePercent = taxRate.RatePercent,
            IsCompound = taxRate.IsCompound,
            ValidFrom = taxRate.ValidFrom,
            ValidUntil = taxRate.ValidUntil,
            Status = taxRate.Status
        };

        return ApplicationResult<TaxRateResponse>.Success(response);
    }

    public async Task<ApplicationResult<TaxRateListResponse>> GetTaxRatesAsync(TenantRequestContext context, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxRates.View);
        if (accessError is not null) return ApplicationResult<TaxRateListResponse>.Failure(accessError);

        var (items, totalCount) = await _repository.GetTaxRatesAsync(context.TenantId, pageNumber, pageSize);
        
        var responses = items.Select(x => new TaxRateResponse
        {
            Id = x.Id,
            TaxJurisdictionId = x.TaxJurisdictionId,
            TaxRateCode = x.TaxRateCode,
            TaxRateName = x.TaxRateName,
            RatePercent = x.RatePercent,
            IsCompound = x.IsCompound,
            ValidFrom = x.ValidFrom,
            ValidUntil = x.ValidUntil,
            Status = x.Status
        }).ToList();
        
        var listResponse = new TaxRateListResponse(
            responses,
            pageNumber,
            pageSize,
            totalCount);
        
        return ApplicationResult<TaxRateListResponse>.Success(listResponse);
    }

    public async Task<ApplicationResult<bool>> DeleteTaxRateAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.TaxRates.Delete);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var taxRate = await _repository.GetTaxRateByIdAsync(context.TenantId, id);
        if (taxRate == null)
            return ApplicationResult<bool>.Failure(NotFound);

        taxRate.SoftDelete(context.UserId);
        _repository.UpdateTaxRate(taxRate);
        await _repository.SaveChangesAsync();

        return ApplicationResult<bool>.Success(true);
    }
}


