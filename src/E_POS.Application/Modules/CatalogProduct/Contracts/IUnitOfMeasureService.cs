using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IUnitOfMeasureService
{
    Task<ApplicationResult<UnitOfMeasureListResponse>> ListAsync(TenantRequestContext context, CancellationToken cancellationToken);
}