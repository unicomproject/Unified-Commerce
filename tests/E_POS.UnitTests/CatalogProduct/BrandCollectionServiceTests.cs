using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class BrandCollectionServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BrandCreateAsync_WithoutCreateOrManagePermission_ReturnsPermissionDenied()
    {
        var service = new BrandService(new FakeBrandRepository(), new BrandRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([]),
            new BrandCreateRequest("ACME", "Acme", null, null, null, BrandConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("brand.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task BrandCreateAsync_WithCreatePermission_NormalizesCodeAndPersists()
    {
        var repository = new FakeBrandRepository();
        var service = new BrandService(repository, new BrandRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([BrandConstants.CreatePermission]),
            new BrandCreateRequest(" acme ", "Acme", null, null, null, BrandConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ACME", repository.AddedBrand?.BrandCode);
        Assert.Equal(TenantId, repository.AddedBrand?.TenantId);
    }

    [Fact]
    public async Task BrandCreateAsync_WithLegacyLogoUrl_CreatesMediaAssetAndLinksBrand()
    {
        var repository = new FakeBrandRepository();
        var service = new BrandService(repository, new BrandRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([BrandConstants.CreatePermission]),
            new BrandCreateRequest("ACME", "Acme", null, null, "https://cdn.example.test/brand.png", BrandConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var mediaAsset = Assert.Single(repository.AddedMediaAssets);
        Assert.Equal("https://cdn.example.test/brand.png", mediaAsset.PublicUrl);
        Assert.Equal(mediaAsset.Id, repository.AddedBrand?.LogoMediaAssetId);
    }
    [Fact]
    public async Task BrandUpdateAsync_WithEmptyLogoUrl_ClearsLinkedMediaAndMarksInactive()
    {
        var brandId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();
        var brand = Brand.Create(
            brandId,
            TenantId,
            "ACME",
            "Acme",
            "acme",
            null,
            "https://cdn.example.test/brand-old.png",
            BrandConstants.ActiveStatus,
            UserId,
            Now);
        brand.UpdateLogo("https://cdn.example.test/brand-old.png", mediaAssetId, UserId, Now);
        var repository = new FakeBrandRepository { EditableBrand = brand };
        var service = new BrandService(repository, new BrandRequestValidator(), new FakeDateTimeProvider());

        var result = await service.UpdateAsync(
            CreateContext([BrandConstants.UpdatePermission]),
            brandId,
            new BrandUpdateRequest("ACME", "Acme", "acme", null, " ", BrandConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(brand.LogoUrl);
        Assert.Null(brand.LogoMediaAssetId);
        Assert.Equal([mediaAssetId], repository.InactivatedMediaAssetIds);
    }

    [Fact]
    public async Task BrandDeleteAsync_WithLinkedMediaAsset_MarksMediaInactive()
    {
        var brandId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();
        var brand = Brand.Create(
            brandId,
            TenantId,
            "ACME",
            "Acme",
            "acme",
            null,
            "https://cdn.example.test/brand.png",
            BrandConstants.ActiveStatus,
            UserId,
            Now);
        brand.UpdateLogo("https://cdn.example.test/brand.png", mediaAssetId, UserId, Now);
        var repository = new FakeBrandRepository { EditableBrand = brand };
        var service = new BrandService(repository, new BrandRequestValidator(), new FakeDateTimeProvider());

        var result = await service.DeleteAsync(
            CreateContext([BrandConstants.DeletePermission]),
            brandId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BrandConstants.DeletedStatus, brand.Status);
        Assert.Equal([mediaAssetId], repository.InactivatedMediaAssetIds);
    }
    [Fact]
    public async Task CollectionCreateAsync_WithCreatePermission_NormalizesCodeAndPersists()
    {
        var repository = new FakeCollectionRepository();
        var service = new CollectionService(repository, new CollectionRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([CollectionConstants.CreatePermission]),
            new CollectionCreateRequest(" summer ", "Summer", null, null, "STANDARD", null, null, 0, CollectionConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("SUMMER", repository.AddedCollection?.CollectionCode);
        Assert.Equal(TenantId, repository.AddedCollection?.TenantId);
    }

    [Fact]
    public async Task CollectionDeleteAsync_WhenProductsAreLinked_ReturnsConflict()
    {
        var collectionId = Guid.NewGuid();
        var repository = new FakeCollectionRepository
        {
            EditableCollection = Collection.Create(collectionId, TenantId, "SUMMER", "Summer", "summer", null, "STANDARD", null, null, 0, CollectionConstants.ActiveStatus, UserId, Now),
            HasProductLinks = true
        };
        var service = new CollectionService(repository, new CollectionRequestValidator(), new FakeDateTimeProvider());

        var result = await service.DeleteAsync(CreateContext([CollectionConstants.DeletePermission]), collectionId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("collection.delete_conflict", result.Error.Code);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions)
    {
        return new TenantRequestContext(TenantId, UserId, permissions);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeBrandRepository : IBrandRepository
    {
        public Brand? AddedBrand { get; private set; }
        public Brand? EditableBrand { get; init; }
        public List<MediaAsset> AddedMediaAssets { get; } = [];
        public List<Guid> InactivatedMediaAssetIds { get; } = [];

        public Task<bool> BrandCodeExistsAsync(Guid tenantId, string brandCode, Guid? excludeBrandId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<BrandListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            return Task.FromResult(new BrandListResponse([], pageNumber, pageSize, 0));
        }

        public Task<BrandResponse?> GetByIdAsync(Guid tenantId, Guid brandId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var brand = AddedBrand ?? EditableBrand;
            return Task.FromResult<BrandResponse?>(new BrandResponse(brandId, brand!.BrandCode, brand.BrandName, brand.LogoUrl, brand.LogoMediaAssetId, brand.Status, brand.CreatedAt, brand.UpdatedAt));
        }

        public Task<Brand?> GetEditableAsync(Guid tenantId, Guid brandId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Brand?>(AddedBrand ?? EditableBrand);
        }

        public Task AddAsync(Brand brand, CancellationToken cancellationToken)
        {
            AddedBrand = brand;
            return Task.CompletedTask;
        }

        public Task AddMediaAssetAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
        {
            AddedMediaAssets.Add(mediaAsset);
            return Task.CompletedTask;
        }

        public Task MarkMediaAssetInactiveAsync(Guid tenantId, Guid mediaAssetId, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            InactivatedMediaAssetIds.Add(mediaAssetId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCollectionRepository : ICollectionRepository
    {
        public bool HasProductLinks { get; init; }
        public Collection? AddedCollection { get; private set; }
        public Collection? EditableCollection { get; init; }

        public Task<bool> CollectionCodeExistsAsync(Guid tenantId, string collectionCode, Guid? excludeCollectionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasProductLinksAsync(Guid tenantId, Guid collectionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(HasProductLinks);
        }

        public Task<CollectionListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CollectionListResponse([], pageNumber, pageSize, 0));
        }

        public Task<CollectionResponse?> GetByIdAsync(Guid tenantId, Guid collectionId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var collection = AddedCollection ?? EditableCollection;
            return Task.FromResult<CollectionResponse?>(new CollectionResponse(collectionId, collection!.CollectionCode, collection.CollectionName, collection.Status, collection.CreatedAt, collection.UpdatedAt));
        }

        public Task<Collection?> GetEditableAsync(Guid tenantId, Guid collectionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(EditableCollection);
        }

        public Task AddAsync(Collection collection, CancellationToken cancellationToken)
        {
            AddedCollection = collection;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

