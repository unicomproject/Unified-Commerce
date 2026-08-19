using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Contracts;
namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontRepository :
    IStorefrontBannerRepository,
    IStorefrontCategoryRepository,
    IStorefrontProductRepository,
    IStorefrontFulfilmentRepository,
    IStorefrontTenantRepository
{
}
