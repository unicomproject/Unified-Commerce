using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Contracts;
namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontService :
    IStorefrontBannerService,
    IStorefrontCategoryService,
    IStorefrontProductService,
    IStorefrontFulfilmentService,
    IStorefrontTenantService
{
}
