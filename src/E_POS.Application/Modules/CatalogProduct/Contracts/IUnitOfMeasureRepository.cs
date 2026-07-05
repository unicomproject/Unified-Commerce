using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IUnitOfMeasureRepository
{
    Task<IReadOnlyList<UnitOfMeasureResponse>> ListAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<bool> UomExistsAsync(Guid? tenantId, Guid uomId, CancellationToken cancellationToken);
}