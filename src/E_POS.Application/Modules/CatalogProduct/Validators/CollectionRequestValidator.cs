using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;

namespace E_POS.Application.Modules.CatalogProduct.Validators;

public sealed class CollectionRequestValidator : ICollectionRequestValidator
{
    public ApplicationError? ValidateCreate(CollectionCreateRequest request)
    {
        return Validate(request.CollectionCode, request.Name, request.Status);
    }

    public ApplicationError? ValidateUpdate(CollectionUpdateRequest request)
    {
        return Validate(request.CollectionCode, request.Name, request.Status);
    }

    private static ApplicationError? Validate(string collectionCode, string name, string status)
    {
        if (string.IsNullOrWhiteSpace(collectionCode)) return ValidationFailed("Collection code is required.");
        if (collectionCode.Trim().Length > 80) return ValidationFailed("Collection code cannot exceed 80 characters.");
        if (string.IsNullOrWhiteSpace(name)) return ValidationFailed("Collection name is required.");
        if (name.Trim().Length > 200) return ValidationFailed("Collection name cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(status) || !CollectionConstants.IsValidWriteStatus(status)) return ValidationFailed("Collection status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message)
    {
        return new ApplicationError("collection.validation_failed", message);
    }
}