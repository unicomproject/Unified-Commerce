using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class UnitOfMeasureService : IUnitOfMeasureService
{
    private static readonly ApplicationError PermissionDenied = new("unit_of_measure.permission_denied", "Permission denied for unit of measure reference data.");
    private readonly IUnitOfMeasureRepository _repository;

    public UnitOfMeasureService(IUnitOfMeasureRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApplicationResult<UnitOfMeasureListResponse>> ListAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<UnitOfMeasureListResponse>.Failure(accessError);
        }

        var items = await _repository.ListAsync(context.TenantId, cancellationToken);
        return ApplicationResult<UnitOfMeasureListResponse>.Success(new UnitOfMeasureListResponse(items));
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("unit_of_measure.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(UnitOfMeasureConstants.ViewPermission) ? null : PermissionDenied;
    }
}

