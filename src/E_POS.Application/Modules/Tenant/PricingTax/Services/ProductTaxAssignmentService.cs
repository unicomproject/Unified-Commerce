using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;

namespace E_POS.Application.Modules.Tenant.PricingTax.Services;

public sealed class ProductTaxAssignmentService : IProductTaxAssignmentService
{
    private static readonly ApplicationError PermissionDenied = new("pricing.product_tax_assignment.permission_denied", "Permission denied for product tax assignments.");
    private static readonly ApplicationError NotFound = new("pricing.product_tax_assignment.not_found", "Product tax assignment not found.");

    private readonly IProductTaxAssignmentRepository _repository;
    private readonly ITaxSetupRepository _taxSetupRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductTaxAssignmentRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProductTaxAssignmentService(
        IProductTaxAssignmentRepository repository,
        ITaxSetupRepository taxSetupRepository,
        IProductRepository productRepository,
        IProductTaxAssignmentRequestValidator validator,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _taxSetupRepository = taxSetupRepository;
        _productRepository = productRepository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        // Manage implies they can do anything, or specific perm. 
        if (context.Permissions.Contains(PricingTaxPermissions.ProductTaxAssignments.Manage))
            return null;
            
        return context.Permissions.Contains(requiredPermission) ? null : PermissionDenied;
    }

    public async Task<ApplicationResult<Guid>> CreateAsync(TenantRequestContext context, ProductTaxAssignmentCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.ProductTaxAssignments.Create);
        if (accessError is not null) return ApplicationResult<Guid>.Failure(accessError);

        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<Guid>.Failure(validationError);

        // Validate Product
        var productExists = await _productRepository.ProductExistsAsync(context.TenantId, request.ProductId, cancellationToken);
        if (!productExists)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.product_tax_assignment.product_not_found", "Product not found."));

        // Validate Tax Class
        var taxClass = await _taxSetupRepository.GetTaxClassByIdAsync(context.TenantId, request.TaxClassId);
        if (taxClass == null)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.product_tax_assignment.tax_class_not_found", "Tax class not found."));

        // Validate Overlap
        var hasOverlap = await _repository.HasOverlappingAssignmentAsync(context.TenantId, request.ProductId, request.ProductVariantId, request.TaxClassId, request.AppliesFrom, request.AppliesUntil);
        if (hasOverlap)
            return ApplicationResult<Guid>.Failure(new ApplicationError("pricing.product_tax_assignment.overlap", "An overlapping active tax assignment already exists for this product/variant."));

        var assignment = ProductTaxAssignment.Create(
            context.TenantId,
            request.ProductId,
            request.ProductVariantId,
            request.TaxClassId,
            request.AppliesFrom,
            request.AppliesUntil,
            context.UserId,
            _dateTimeProvider.UtcNow);

        await _repository.AddAsync(assignment);
        await _repository.SaveChangesAsync();

        return ApplicationResult<Guid>.Success(assignment.Id);
    }

    public async Task<ApplicationResult<bool>> UpdateAsync(TenantRequestContext context, Guid id, ProductTaxAssignmentUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.ProductTaxAssignments.Update);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<bool>.Failure(validationError);

        var assignment = await _repository.GetByIdAsync(context.TenantId, id);
        if (assignment == null)
            return ApplicationResult<bool>.Failure(NotFound);

        var taxClass = await _taxSetupRepository.GetTaxClassByIdAsync(context.TenantId, request.TaxClassId);
        if (taxClass == null)
            return ApplicationResult<bool>.Failure(new ApplicationError("pricing.product_tax_assignment.tax_class_not_found", "Tax class not found."));

        if (PricingTaxConstants.IsValidWriteStatus(request.Status))
        {
            var hasOverlap = await _repository.HasOverlappingAssignmentAsync(context.TenantId, assignment.ProductId, assignment.ProductVariantId, request.TaxClassId, request.AppliesFrom, request.AppliesUntil, assignment.Id);
            if (hasOverlap)
                return ApplicationResult<bool>.Failure(new ApplicationError("pricing.product_tax_assignment.overlap", "An overlapping active tax assignment already exists for this product/variant."));
        }

        assignment.UpdateAssignment(
            request.TaxClassId,
            request.AppliesFrom,
            request.AppliesUntil,
            request.Status,
            context.UserId,
            _dateTimeProvider.UtcNow);

        _repository.Update(assignment);
        await _repository.SaveChangesAsync();

        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<ProductTaxAssignmentResponse>> GetAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.ProductTaxAssignments.View);
        if (accessError is not null) return ApplicationResult<ProductTaxAssignmentResponse>.Failure(accessError);

        var assignment = await _repository.GetByIdAsync(context.TenantId, id);
        if (assignment == null)
            return ApplicationResult<ProductTaxAssignmentResponse>.Failure(NotFound);

        return ApplicationResult<ProductTaxAssignmentResponse>.Success(MapToResponse(assignment));
    }

    public async Task<ApplicationResult<ProductTaxAssignmentListResponse>> GetByProductAsync(TenantRequestContext context, Guid productId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.ProductTaxAssignments.View);
        if (accessError is not null) return ApplicationResult<ProductTaxAssignmentListResponse>.Failure(accessError);

        var (items, totalCount) = await _repository.GetByProductAsync(context.TenantId, productId, pageNumber, pageSize);
        
        var responses = items.Select(MapToResponse).ToList();
        
        return ApplicationResult<ProductTaxAssignmentListResponse>.Success(new ProductTaxAssignmentListResponse(responses, pageNumber, pageSize, totalCount));
    }

    public async Task<ApplicationResult<bool>> DeleteAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxPermissions.ProductTaxAssignments.Delete);
        if (accessError is not null) return ApplicationResult<bool>.Failure(accessError);

        var assignment = await _repository.GetByIdAsync(context.TenantId, id);
        if (assignment == null)
            return ApplicationResult<bool>.Failure(NotFound);

        assignment.SoftDelete(context.UserId, _dateTimeProvider.UtcNow);
        
        _repository.Update(assignment);
        await _repository.SaveChangesAsync();

        return ApplicationResult<bool>.Success(true);
    }

    private static ProductTaxAssignmentResponse MapToResponse(ProductTaxAssignment entity)
    {
        return new ProductTaxAssignmentResponse
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            ProductVariantId = entity.ProductVariantId,
            TaxClassId = entity.TaxClassId,
            AppliesFrom = entity.AppliesFrom,
            AppliesUntil = entity.AppliesUntil,
            Status = entity.Status
        };
    }
}


