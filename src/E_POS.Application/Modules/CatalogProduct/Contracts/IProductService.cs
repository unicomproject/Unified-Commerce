using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IProductService
{
    Task<ApplicationResult<ProductResponse>> CreateAsync(TenantRequestContext context, ProductCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<ProductListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<ProductResponse>> GetByIdAsync(TenantRequestContext context, Guid productId, CancellationToken cancellationToken);
    Task<ApplicationResult<ProductResponse>> UpdateAsync(TenantRequestContext context, Guid productId, ProductUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<Guid>> DeleteAsync(TenantRequestContext context, Guid productId, CancellationToken cancellationToken);
}
