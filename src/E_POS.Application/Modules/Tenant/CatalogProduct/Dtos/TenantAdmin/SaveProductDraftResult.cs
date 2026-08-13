using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed record SaveProductDraftResult(
    ProductDraftResponse? Response,
    ApplicationError? Error)
{
    public bool IsSuccess => Error is null && Response is not null;

    public static SaveProductDraftResult Success(ProductDraftResponse response) => new(response, null);

    public static SaveProductDraftResult Failure(ApplicationError error) => new(null, error);
}
