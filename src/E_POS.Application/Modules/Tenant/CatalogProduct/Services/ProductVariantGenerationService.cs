using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class ProductVariantGenerationService
{
    public Task<ApplicationResult<VariantConfigurationDto>> GenerateAndReconcileVariantsAsync(
        Guid tenantId,
        Guid productId,
        VariantConfigurationDto variantConfiguration,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        _ = productId;
        _ = cancellationToken;

        var result = VariantConfigurationCombinationGenerator.GenerateAndReconcile(variantConfiguration);
        if (!result.Succeeded || result.Configuration is null)
        {
            return Task.FromResult(ApplicationResult<VariantConfigurationDto>.Failure(new ApplicationError(
                result.ErrorCode ?? "product.variant_generation_failed",
                result.ErrorMessage ?? "Variant combination generation failed.")));
        }

        return Task.FromResult(ApplicationResult<VariantConfigurationDto>.Success(result.Configuration));
    }
}
