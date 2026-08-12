using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantAdminProductRequestValidator
{
    ApplicationError? ValidateCreate(TenantAdminProductCreateRequest request);

    ApplicationError? ValidateUpdate(TenantAdminProductCreateRequest request);

    ApplicationError? ValidateStatusUpdate(TenantAdminProductStatusUpdateRequest request);

    ApplicationError? ValidateListQuery(
        string? productStatus,
        string? stockStatus,
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection);

    /// <summary>
    /// Permissive Step 1 Save Draft validation (Category optional; blank name allowed for placeholder).
    /// </summary>
    ApplicationError? ValidateSaveDraft(SaveProductDraftRequest request);

    /// <summary>
    /// Strict Step 1 Save &amp; Continue validation (Product Name + Category required).
    /// </summary>
    ApplicationError? ValidateSaveAndContinue(SaveProductDraftRequest request);

    /// <summary>
    /// Step-aware validation for Save Draft across any step (Step 1 or Step 2).
    /// </summary>
    ApplicationError? ValidateStepSaveDraft(SaveProductDraftRequest request);

    /// <summary>
    /// Step-aware validation for Save &amp; Continue across any step (Step 1 or Step 2).
    /// </summary>
    ApplicationError? ValidateStepSaveAndContinue(SaveProductDraftRequest request);

    [Obsolete("Use ValidateSaveDraft or ValidateSaveAndContinue.")]
    ApplicationError? ValidateDraft(SaveProductDraftRequest request, bool requireCategory);
}
