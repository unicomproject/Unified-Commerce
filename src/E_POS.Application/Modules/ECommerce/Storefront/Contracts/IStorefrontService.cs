namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontService :
    IStorefrontBannerService,
    IStorefrontCategoryService,
    IStorefrontProductService,
    IStorefrontFulfillmentService,
    IStorefrontTenantService
{
}