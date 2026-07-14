namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontRepository :
    IStorefrontBannerRepository,
    IStorefrontCategoryRepository,
    IStorefrontProductRepository,
    IStorefrontFulfillmentRepository,
    IStorefrontTenantRepository
{
}