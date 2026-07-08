using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public sealed class ProductRequestValidator : IProductRequestValidator
{
    public ApplicationError? ValidateCreate(ProductCreateRequest request)
    {
        return Validate(request.ProductCode, request.Name, request.Status, request.Sku, request.Barcode, request.Price);
    }

    public ApplicationError? ValidateUpdate(ProductUpdateRequest request)
    {
        return Validate(request.ProductCode, request.Name, request.Status, request.Sku, request.Barcode, request.Price);
    }

    private static ApplicationError? Validate(string productCode, string name, string status, string sku, string? barcode, decimal? price)
    {
        if (string.IsNullOrWhiteSpace(productCode)) return ValidationFailed("Product code is required.");
        if (productCode.Trim().Length > 80) return ValidationFailed("Product code cannot exceed 80 characters.");
        
        if (string.IsNullOrWhiteSpace(name)) return ValidationFailed("Product name is required.");
        if (name.Trim().Length > 200) return ValidationFailed("Product name cannot exceed 200 characters.");
        
        if (string.IsNullOrWhiteSpace(status) || !ProductConstants.IsValidWriteStatus(status)) return ValidationFailed("Product status must be ACTIVE or INACTIVE.");
        
        if (string.IsNullOrWhiteSpace(sku)) return ValidationFailed("SKU is required.");
        if (sku.Trim().Length > 255) return ValidationFailed("SKU cannot exceed 255 characters.");
        
        if (barcode != null && barcode.Trim().Length > 255) return ValidationFailed("Barcode cannot exceed 255 characters.");
        
        if (price.HasValue && price.Value < 0) return ValidationFailed("Price cannot be negative.");
        
        return null;
    }

    private static ApplicationError ValidationFailed(string message)
    {
        return new ApplicationError("product.validation_failed", message);
    }
}


