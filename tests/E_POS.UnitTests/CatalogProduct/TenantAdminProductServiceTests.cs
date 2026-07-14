using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class TenantAdminProductServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task GetSummaryAsync_WithoutViewPermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetSummaryAsync(CreateContext([]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetSummaryAsync_WithTenantProductsView_ReturnsSummary()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            Summary = new TenantAdminProductSummaryResponse(12, 10, 2, 4),
        };
        var service = CreateService(repository);

        var result = await service.GetSummaryAsync(
            CreateContext([TenantAdminProductPermissions.View]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(12, result.Value.TotalProducts);
        Assert.Equal(10, result.Value.ActiveProducts);
        Assert.Equal(2, result.Value.InactiveProducts);
        Assert.Equal(4, result.Value.CategoryCount);
    }

    [Fact]
    public async Task GetSummaryAsync_WithCatalogProductsView_ReturnsSummary()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            Summary = new TenantAdminProductSummaryResponse(0, 0, 0, 0),
        };
        var service = CreateService(repository);

        var result = await service.GetSummaryAsync(
            CreateContext([ProductConstants.ViewPermission]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalProducts);
        Assert.Equal(0, result.Value.CategoryCount);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetCreateOptionsAsync(CreateContext([]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_WithTenantProductsCreate_ReturnsOptions()
    {
        var createOptions = new TenantAdminProductCreateOptionsResponse(
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var repository = new FakeTenantAdminProductRepository
        {
            CreateOptions = createOptions,
        };
        var service = CreateService(repository);

        var result = await service.GetCreateOptionsAsync(
            CreateContext([TenantAdminProductPermissions.Create]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(createOptions, result.Value);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_WithCatalogProductsCreate_ReturnsOptions()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.GetCreateOptionsAsync(
            CreateContext([ProductConstants.CreatePermission]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!.Categories);
    }

    [Fact]
    public async Task CreateAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.CreateAsync(
            CreateContext([]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutViewPermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetByIdAsync(
            CreateContext([]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ReturnsNotFound()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetByIdAsync(
            CreateContext([TenantAdminProductPermissions.View]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WithTenantProductsDetailsView_ReturnsDetail()
    {
        var productId = Guid.NewGuid();
        var detail = CreateDetailResponse(productId);
        var repository = new FakeTenantAdminProductRepository { Detail = detail };
        var service = CreateService(repository);

        var result = await service.GetByIdAsync(
            CreateContext([TenantAdminProductPermissions.DetailsView]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(detail, result.Value);
        Assert.Equal(TenantId, repository.LastDetailTenantId);
        Assert.Equal(productId, repository.LastDetailProductId);
    }

    [Fact]
    public async Task GetByIdAsync_WithTenantProductsView_ReturnsDetail()
    {
        var productId = Guid.NewGuid();
        var detail = CreateDetailResponse(productId);
        var repository = new FakeTenantAdminProductRepository { Detail = detail };
        var service = CreateService(repository);

        var result = await service.GetByIdAsync(
            CreateContext([TenantAdminProductPermissions.View]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(detail, result.Value);
    }

    [Fact]
    public async Task UpdateAsync_WithoutUpdatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateAsync(
            CreateContext([]),
            Guid.NewGuid(),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ReturnsNotFound()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            Guid.NewGuid(),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.not_found", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithDiscountAboveSellingPrice_ReturnsValidationFailed()
    {
        var productId = Guid.NewGuid();
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(new FakeTenantAdminProductRepository(), productRepository);

        var request = CreateValidRequest();
        request.SellingPrice = 10m;
        request.DiscountPrice = 15m;

        var result = await service.UpdateAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "discountPrice");
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateSkuOnOtherProduct_ReturnsDuplicateSku()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository { SkuExistsOnOtherProduct = true };
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(repository, productRepository);

        var result = await service.UpdateAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.duplicate_sku", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ReturnsUpdatedDetail()
    {
        var productId = Guid.NewGuid();
        var detail = CreateDetailResponse(productId);
        var repository = new FakeTenantAdminProductRepository { UpdateDetail = detail };
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(repository, productRepository);

        var request = CreateValidRequest();
        var result = await service.UpdateAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(detail, result.Value);
        Assert.Equal(productId, repository.LastUpdateProductId);
        Assert.Same(request, repository.LastUpdateRequest);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithoutUpdatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateStatusAsync(
            CreateContext([]),
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidStatus_ReturnsValidationFailed()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Deleted" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "status");
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenProductNotFound_ReturnsNotFound()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.not_found", result.Error.Code);
    }

    [Fact]
    public async Task UpdateStatusAsync_ActivateWithMissingFields_ReturnsValidationFailed()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ActivationSnapshot = new TenantAdminProductActivationSnapshot(
                string.Empty,
                null,
                false,
                0m,
                string.Empty),
        };
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(repository, productRepository);

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            new TenantAdminProductStatusUpdateRequest { Status = "Active" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "productName");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidInactiveStatus_ReturnsUpdatedStatus()
    {
        var productId = Guid.NewGuid();
        var response = new TenantAdminProductStatusUpdateResponse(productId, ProductConstants.InactiveStatus);
        var repository = new FakeTenantAdminProductRepository { StatusUpdateResponse = response };
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(repository, productRepository);

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(productId, result.Value!.ProductId);
        Assert.Equal(ProductConstants.InactiveStatus, result.Value.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ActivateWithCompleteProduct_ReturnsActiveStatus()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ActivationSnapshot = new TenantAdminProductActivationSnapshot(
                "Sample Product",
                "SKU-001",
                true,
                10m,
                "PIECE"),
            StatusUpdateResponse = new TenantAdminProductStatusUpdateResponse(
                productId,
                ProductConstants.ActiveStatus),
        };
        var service = CreateService(repository);

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            new TenantAdminProductStatusUpdateRequest { Status = "Active" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductConstants.ActiveStatus, result.Value!.Status);
    }

    [Fact]
    public async Task DeleteAsync_WithoutDeletePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.DeleteAsync(
            CreateContext([]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ReturnsNotFound()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(null, "product.not_found"),
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.not_found", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenAlreadyDeleted_ReturnsDeleteBlocked()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(null, "product.delete_blocked"),
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.delete_blocked", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WithHistory_ArchivesProduct()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(
                new TenantAdminProductDeleteResponse(
                    productId,
                    "Archived",
                    ProductConstants.InactiveStatus),
                null),
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Archived", result.Value!.Outcome);
        Assert.Equal(ProductConstants.InactiveStatus, result.Value.Status);
    }

    [Fact]
    public async Task DeleteAsync_WithoutHistory_SoftDeletesProduct()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(
                new TenantAdminProductDeleteResponse(
                    productId,
                    "Deleted",
                    ProductConstants.DeletedStatus),
                null),
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Deleted", result.Value!.Outcome);
        Assert.Equal(ProductConstants.DeletedStatus, result.Value.Status);
    }

    [Fact]
    public async Task DeleteAsync_WithSuccess_LogsProductDeletedAudit()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(
                new TenantAdminProductDeleteResponse(
                    productId,
                    "Deleted",
                    ProductConstants.DeletedStatus),
                null),
        };
        var auditLogger = new FakeTenantAdminProductAuditLogger();
        var service = CreateService(repository, auditLogger: auditLogger);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(auditLogger.ProductDeletedLogged);
        Assert.Equal(productId, auditLogger.LastProductId);
        Assert.Equal("Deleted", auditLogger.LastOutcome);
        Assert.Equal(ProductConstants.DeletedStatus, auditLogger.LastStatus);
    }

    [Fact]
    public async Task GetDashboardAsync_WithoutDashboardPermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetDashboardAsync(
            CreateContext([TenantAdminProductPermissions.View]),
            new TenantAdminProductDashboardQuery(
                null,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.permission_denied", result.Error?.Code);
    }

    [Fact]
    public async Task GetDashboardAsync_WithDashboardPermission_OmitsUnauthorizedSections()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            DashboardRaw = SampleDashboardRaw(),
        };
        var service = CreateService(repository);

        var result = await service.GetDashboardAsync(
            CreateContext([
                TenantAdminProductPermissions.DashboardView,
                TenantAdminProductPermissions.View,
            ]),
            new TenantAdminProductDashboardQuery(
                null,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value?.Summary.TotalProducts);
        Assert.Null(result.Value?.Summary.LowStock);
        Assert.Null(result.Value?.StockValue);
        Assert.Null(result.Value?.StockMovement);
    }

    [Fact]
    public async Task GetDashboardAsync_WithStockPermissions_ReturnsStockSections()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            DashboardRaw = SampleDashboardRaw(),
        };
        var service = CreateService(repository);

        var result = await service.GetDashboardAsync(
            CreateContext([
                TenantAdminProductPermissions.DashboardView,
                TenantAdminStockPermissions.View,
                TenantAdminStockPermissions.ValueView,
                TenantAdminStockPermissions.MovementsView,
            ]),
            new TenantAdminProductDashboardQuery(
                null,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value?.Summary.LowStock);
        Assert.NotNull(result.Value?.StockValue);
        Assert.NotNull(result.Value?.StockMovement);
    }

    [Fact]
    public async Task GetDashboardAsync_WithInvalidOutlet_ReturnsValidationFailed()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            OutletsBelong = false,
            DashboardRaw = SampleDashboardRaw(),
        };
        var service = CreateService(repository);

        var result = await service.GetDashboardAsync(
            CreateContext([TenantAdminProductPermissions.DashboardView]),
            new TenantAdminProductDashboardQuery(
                Guid.NewGuid(),
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.dashboard.invalid_outlet", result.Error?.Code);
    }

    private static TenantAdminProductDashboardRawData SampleDashboardRaw() =>
        new(
            "USD",
            new TenantAdminProductDashboardRawMetric(10, 8),
            new TenantAdminProductDashboardRawMetric(2, 1),
            new TenantAdminProductDashboardRawMetric(1, 0),
            new TenantAdminProductDashboardRawMetric(3, 2),
            new TenantAdminProductDashboardRawMetric(15, 12),
            new TenantAdminProductDashboardRawMetric(4, 3),
            1000m,
            900m,
            [new TenantAdminProductDashboardRawStockValuePoint(DateOnly.FromDateTime(DateTime.UtcNow), 1000m)],
            [new("stock_in", 5), new("stock_out", 2), new("adjustment", 1), new("transfer", 0)]);

    [Fact]
    public async Task CreateAsync_WithMissingRequiredFields_ReturnsValidationFailed()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.CreateAsync(
            CreateContext([TenantAdminProductPermissions.Create]),
            new TenantAdminProductCreateRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "productName");
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "sku");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSku_ReturnsDuplicateSku()
    {
        var productRepository = new FakeProductRepository { ExistingSkus = ["SKU-001"] };
        var service = CreateService(new FakeTenantAdminProductRepository(), productRepository);

        var result = await service.CreateAsync(
            CreateContext([TenantAdminProductPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.duplicate_sku", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsCreatedProduct()
    {
        var categoryId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ResolvedUnitId = unitId,
            CreateResponse = new TenantAdminProductCreateResponse(
                productId,
                "Sample Product",
                "SKU-001",
                ProductConstants.ActiveStatus),
        };
        var service = CreateService(repository);

        var request = CreateValidRequest(categoryId);
        var result = await service.CreateAsync(
            CreateContext([TenantAdminProductPermissions.Create]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(productId, result.Value!.ProductId);
        Assert.Equal(TenantId, repository.LastCreateTenantId);
        Assert.Equal(UserId, repository.LastCreateUserId);
        Assert.Equal(unitId, repository.LastCreateUnitId);
        Assert.Same(request, repository.LastCreateRequest);
    }

    private static TenantAdminProductCreateRequest CreateValidRequest(Guid? categoryId = null) =>
        new()
        {
            ProductName = "Sample Product",
            Sku = "SKU-001",
            CategoryId = categoryId ?? Guid.NewGuid(),
            UnitType = "PIECE",
            SellingPrice = 10m,
            Status = ProductConstants.ActiveStatus,
        };

    private static TenantAdminProductDetailResponse CreateDetailResponse(Guid productId) =>
        new(
            productId,
            "Sample Product",
            "SKU-001",
            null,
            Guid.NewGuid(),
            "Beverages",
            null,
            null,
            "PIECE",
            null,
            null,
            null,
            10m,
            null,
            null,
            null,
            ProductConstants.ActiveStatus,
            false,
            null,
            [],
            [],
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static TenantAdminProductService CreateService(
        ITenantAdminProductRepository tenantAdminProductRepository,
        FakeProductRepository? productRepository = null,
        ITenantAdminProductAuditLogger? auditLogger = null)
    {
        return new TenantAdminProductService(
            productRepository ?? new FakeProductRepository(),
            tenantAdminProductRepository,
            new TenantAdminProductRequestValidator(),
            new FakeDateTimeProvider(),
            auditLogger ?? new FakeTenantAdminProductAuditLogger());
    }

    private static TenantRequestContext CreateContext(string[] permissions) =>
        new(TenantId, UserId, permissions);

    private sealed class FakeTenantAdminProductRepository : ITenantAdminProductRepository
    {
        public TenantAdminProductSummaryResponse Summary { get; init; } =
            new(0, 0, 0, 0);

        public TenantAdminProductCreateOptionsResponse CreateOptions { get; init; } =
            new([], [], [], [], [], [], []);

        public Task<TenantAdminProductSummaryResponse> GetSummaryAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Summary);

        public Task<TenantAdminProductCreateOptionsResponse> GetCreateOptionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateOptions);

        public Task<IReadOnlyDictionary<Guid, string>> GetPrimaryCategoryNamesAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Guid? ResolvedUnitId { get; init; } = Guid.NewGuid();

        public bool CategoryBelongs { get; init; } = true;

        public bool BrandBelongs { get; init; } = true;

        public bool TaxClassBelongs { get; init; } = true;

        public bool OutletsBelong { get; init; } = true;

        public TenantAdminProductCreateResponse CreateResponse { get; init; } =
            new(Guid.NewGuid(), "Sample Product", "SKU-001", ProductConstants.ActiveStatus);

        public TenantAdminProductDetailResponse? Detail { get; init; }

        public TenantAdminProductDetailResponse? UpdateDetail { get; init; }

        public TenantAdminProductStatusUpdateResponse? StatusUpdateResponse { get; init; }

        public TenantAdminProductActivationSnapshot? ActivationSnapshot { get; init; }

        public TenantAdminProductDeleteOperationResult DeleteOperationResult { get; init; } =
            new(null, "product.not_found");

        public TenantAdminProductDashboardRawData DashboardRaw { get; init; } =
            new(
                "USD",
                new(0, 0),
                new(0, 0),
                new(0, 0),
                new(0, 0),
                new(0, 0),
                new(0, 0),
                0,
                0,
                [],
                []);

        public bool SkuExistsOnOtherProduct { get; init; }

        public bool BarcodeExistsOnOtherProduct { get; init; }

        public Guid? LastUpdateProductId { get; private set; }

        public TenantAdminProductCreateRequest? LastUpdateRequest { get; private set; }

        public Guid? LastCreateTenantId { get; private set; }

        public Guid? LastCreateUserId { get; private set; }

        public Guid? LastCreateUnitId { get; private set; }

        public TenantAdminProductCreateRequest? LastCreateRequest { get; private set; }

        public Guid? LastDetailTenantId { get; private set; }

        public Guid? LastDetailProductId { get; private set; }

        public Task<Guid?> ResolveUnitIdAsync(
            Guid tenantId,
            string unitType,
            CancellationToken cancellationToken) =>
            Task.FromResult(ResolvedUnitId);

        public Task<bool> CategoryBelongsToTenantAsync(
            Guid tenantId,
            Guid categoryId,
            Guid? parentCategoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(CategoryBelongs);

        public Task<bool> BrandBelongsToTenantAsync(
            Guid tenantId,
            Guid brandId,
            CancellationToken cancellationToken) =>
            Task.FromResult(BrandBelongs);

        public Task<bool> TaxClassBelongsToTenantAsync(
            Guid tenantId,
            Guid taxClassId,
            CancellationToken cancellationToken) =>
            Task.FromResult(TaxClassBelongs);

        public Task<bool> OutletsBelongToTenantAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> outletIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(OutletsBelong);

        public Task<TenantAdminProductCreateResponse> CreateProductAsync(
            Guid tenantId,
            Guid userId,
            TenantAdminProductCreateRequest request,
            Guid unitId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            LastCreateTenantId = tenantId;
            LastCreateUserId = userId;
            LastCreateUnitId = unitId;
            LastCreateRequest = request;
            return Task.FromResult(CreateResponse);
        }

        public Task<TenantAdminProductDetailResponse?> GetDetailAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken)
        {
            LastDetailTenantId = tenantId;
            LastDetailProductId = productId;
            return Task.FromResult(Detail);
        }

        public Task<bool> SkuExistsOnOtherProductAsync(
            Guid tenantId,
            string sku,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SkuExistsOnOtherProduct);

        public Task<bool> BarcodeExistsOnOtherProductAsync(
            Guid tenantId,
            string barcode,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(BarcodeExistsOnOtherProduct);

        public Task<TenantAdminProductDetailResponse?> UpdateProductAsync(
            Guid tenantId,
            Guid userId,
            Guid productId,
            TenantAdminProductCreateRequest request,
            Guid unitId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            LastUpdateProductId = productId;
            LastUpdateRequest = request;
            return Task.FromResult(UpdateDetail);
        }

        public Task<TenantAdminProductActivationSnapshot?> GetActivationSnapshotAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ActivationSnapshot);

        public Task<TenantAdminProductStatusUpdateResponse?> UpdateProductStatusAsync(
            Guid tenantId,
            Guid userId,
            Guid productId,
            string status,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(StatusUpdateResponse);

        public Task<TenantAdminProductDeleteOperationResult> DeleteProductAsync(
            Guid tenantId,
            Guid userId,
            Guid productId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(DeleteOperationResult);

        public Task<TenantAdminProductDeleteHistoryFlags?> GetDeleteHistoryFlagsAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<TenantAdminProductDeleteHistoryFlags?>(null);

        public Task<TenantAdminProductDashboardRawData> GetDashboardAsync(
            Guid tenantId,
            TenantAdminProductDashboardQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(DashboardRaw);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; init; } = DateTimeOffset.UtcNow;
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public IReadOnlyCollection<string> ExistingSkus { get; init; } = [];

        public IReadOnlyCollection<string> ExistingBarcodes { get; init; } = [];

        public IReadOnlyCollection<Guid> ExistingProductIds { get; init; } = [];

        public Task<bool> ProductCodeExistsAsync(
            Guid tenantId,
            string productCode,
            Guid? excludeProductId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> SkuExistsAsync(
            Guid tenantId,
            string sku,
            Guid? excludeProductId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExistingSkus.Contains(sku, StringComparer.OrdinalIgnoreCase));

        public Task<bool> BarcodeExistsAsync(
            Guid tenantId,
            string barcodeValue,
            Guid? excludeProductId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExistingBarcodes.Contains(barcodeValue, StringComparer.OrdinalIgnoreCase));

        public Task<ProductListResponse> ListAsync(
            Guid tenantId,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ProductListResponse([], pageNumber, pageSize, 0));

        public Task<ProductResponse?> GetByIdAsync(
            Guid tenantId,
            Guid productId,
            bool includeDeleted,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProductResponse?>(null);

        public Task<Product?> GetEditableAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Product?>(null);

        public Task AddAsync(Product product, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddBarcodeAsync(ProductBarcode barcode, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddCategoryLinksAsync(
            IEnumerable<ProductCategory> links,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddCollectionLinksAsync(
            IEnumerable<ProductCollection> links,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddImagesAsync(IEnumerable<ProductImage> images, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddChannelVisibilitiesAsync(
            IEnumerable<ProductChannelVisibility> visibilities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddPriceListItemAsync(PriceListItem priceListItem, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ClearProductMappingsAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetDefaultPriceListIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(null);

        public Task<ProductVariant?> GetDefaultVariantAsync(
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProductVariant?>(null);

        public Task<PriceListItem?> GetPriceListItemAsync(
            Guid priceListId,
            Guid variantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PriceListItem?>(null);

        public Task<ProductBarcode?> GetBarcodeAsync(Guid variantId, CancellationToken cancellationToken) =>
            Task.FromResult<ProductBarcode?>(null);

        public Task<bool> ProductExistsAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExistingProductIds.Contains(productId));

        public Task<bool> ProductVariantExistsAsync(
            Guid tenantId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class FakeTenantAdminProductAuditLogger : ITenantAdminProductAuditLogger
    {
        public bool ProductDeletedLogged { get; private set; }

        public Guid LastProductId { get; private set; }

        public string LastOutcome { get; private set; } = string.Empty;

        public string LastStatus { get; private set; } = string.Empty;

        public void LogProductDeleted(
            Guid tenantId,
            Guid userId,
            Guid productId,
            string outcome,
            string status)
        {
            ProductDeletedLogged = true;
            LastProductId = productId;
            LastOutcome = outcome;
            LastStatus = status;
        }
    }
}
