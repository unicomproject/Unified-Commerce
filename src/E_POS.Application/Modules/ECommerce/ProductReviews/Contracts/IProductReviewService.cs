using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;

namespace E_POS.Application.Modules.ECommerce.ProductReviews.Contracts;

public interface IProductReviewService
{
    Task<ApplicationResult<ProductReviewsPageReadModel>> GetAsync(Guid tenantId, Guid productId, int page, int pageSize, string? sort, CancellationToken cancellationToken);
    Task<ApplicationResult<ProductReviewItemReadModel>> CreateAsync(Guid tenantId, Guid customerId, Guid productId, CreateProductReviewRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<ProductReviewItemReadModel>> UpdateAsync(Guid tenantId, Guid customerId, Guid reviewId, UpdateProductReviewRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(Guid tenantId, Guid customerId, Guid reviewId, CancellationToken cancellationToken);
}
