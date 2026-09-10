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

    [Theory]
    [InlineData(149, true)]
    [InlineData(150, true)]
    [InlineData(151, false)]
    public void BrandCreateValidation_EnforcesNameMaximum(int length, bool valid)
    {
        var error = new BrandRequestValidator().ValidateCreate(
            new BrandCreateRequest("CODE", new string('N', length), null, null, null, BrandConstants.ActiveStatus));

        Assert.Equal(valid, error is null);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public void BrandCreateValidation_EnforcesOptionalDescriptionMaximum(int? length, bool valid)
    {
        var description = length.HasValue ? new string('D', length.Value) : null;
        var error = new BrandRequestValidator().ValidateCreate(
            new BrandCreateRequest("CODE", "Name", null, description, null, BrandConstants.ActiveStatus));

        Assert.Equal(valid, error is null);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(25, true)]
    [InlineData(-1, false)]
    public void BrandCreateValidation_RequiresNonnegativeSortOrder(int sortOrder, bool valid)
    {
        var error = new BrandRequestValidator().ValidateCreate(
            new BrandCreateRequest("CODE", "Name", null, null, null, BrandConstants.ActiveStatus, sortOrder));

        Assert.Equal(valid, error is null);
    }

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
        var audit = new FakeBrandAuditLogger();
        var service = new BrandService(repository, new BrandRequestValidator(), new FakeDateTimeProvider(), audit);

        var result = await service.CreateAsync(
            CreateContext([BrandConstants.CreatePermission]),
            new BrandCreateRequest(" acme ", "Acme", null, null, null, BrandConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ACME", repository.AddedBrand?.BrandCode);
        Assert.Equal(TenantId, repository.AddedBrand?.TenantId);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal(("BrandCreated", TenantId, UserId, repository.AddedBrand!.Id, 1L), auditEvent);
    }

    [Fact]
    public async Task BrandGetByIdAfterMutationAsync_WithUpdateButNoViewPermission_ReturnsDetail()
    {
        var brand = Brand.Create(Guid.NewGuid(), TenantId, "ACME", "Acme", "acme", "Detail", null, BrandConstants.ActiveStatus, UserId, Now, 4);
        var service = new BrandService(new FakeBrandRepository { EditableBrand = brand }, new BrandRequestValidator(), new FakeDateTimeProvider());

        var result = await service.GetByIdAfterMutationAsync(
            CreateContext([BrandConstants.UpdatePermission]), brand.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Detail", result.Value!.Description);
        Assert.Equal(4, result.Value.SortOrder);
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
        var audit = new FakeBrandAuditLogger();
        var service = new BrandService(repository, new BrandRequestValidator(), new FakeDateTimeProvider(), audit);

        var result = await service.UpdateAsync(
            CreateContext([BrandConstants.UpdatePermission]),
            brandId,
            new BrandUpdateRequest("ACME", "Acme", "acme", null, " ", BrandConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
        var audit = new FakeBrandAuditLogger();
        var service = new BrandService(repository, new BrandRequestValidator(), new FakeDateTimeProvider(), audit);

        var result = await service.DeleteAsync(
            CreateContext([BrandConstants.DeletePermission]),
            brandId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BrandConstants.DeletedStatus, brand.Status);
        Assert.Equal([mediaAssetId], repository.InactivatedMediaAssetIds);
        Assert.Equal(("BrandDeleted", TenantId, UserId, brandId, 2L), Assert.Single(audit.Events));
    }

    [Fact]
    public async Task BrandUpdateAsync_WithStaleRowVersion_ReturnsConflictWithoutMutation()
    {
        var brand = Brand.Create(Guid.NewGuid(), TenantId, "ACME", "Current", "acme", null, BrandConstants.ActiveStatus, UserId, Now);
        brand.IncrementRowVersion();
        var audit = new FakeBrandAuditLogger();
        var service = new BrandService(new FakeBrandRepository { EditableBrand = brand }, new BrandRequestValidator(), new FakeDateTimeProvider(), audit);

        var result = await service.UpdateAsync(
            CreateContext([BrandConstants.UpdatePermission]),
            brand.Id,
            new BrandUpdateRequest("ACME", "Stale overwrite", null, null, null, BrandConstants.ActiveStatus, ExpectedRowVersion: 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("brand.concurrency_conflict", result.Error.Code);
        Assert.Equal("Current", brand.BrandName);
        Assert.Equal(2, brand.RowVersion);
        Assert.Empty(audit.Events);
    }

    [Fact]
    public async Task BrandUpdateAsync_WithCurrentRowVersion_IncrementsVersionAndAuditsUpdateAndStatus()
    {
        var brand = Brand.Create(Guid.NewGuid(), TenantId, "ACME", "Current", "acme", null, BrandConstants.ActiveStatus, UserId, Now);
        var audit = new FakeBrandAuditLogger();
        var service = new BrandService(new FakeBrandRepository { EditableBrand = brand }, new BrandRequestValidator(), new FakeDateTimeProvider(), audit);

        var result = await service.UpdateAsync(
            CreateContext([BrandConstants.UpdatePermission]),
            brand.Id,
            new BrandUpdateRequest("ACME", "Updated", null, null, null, BrandConstants.InactiveStatus, ExpectedRowVersion: 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.RowVersion);
        Assert.Contains(audit.Events, item => item.EventName == "BrandUpdated" && item.TenantId == TenantId && item.UserId == UserId && item.BrandId == brand.Id && item.RowVersion == 2);
        Assert.Contains(audit.Events, item => item.EventName == "BrandStatusChanged" && item.TenantId == TenantId && item.UserId == UserId && item.BrandId == brand.Id && item.RowVersion == 2);
    }

    [Fact]
    public async Task BrandCreateAsync_WhenDatabaseCodeRaceOccurs_ReturnsFieldConflict()
    {
        var repository = new FakeBrandRepository { AddErrorCode = "brand.code_conflict" };
        var audit = new FakeBrandAuditLogger();
        var service = new BrandService(repository, new BrandRequestValidator(), new FakeDateTimeProvider(), audit);

        var result = await service.CreateAsync(
            CreateContext([BrandConstants.CreatePermission]),
            new BrandCreateRequest("ACME", "Acme", null, null, null, BrandConstants.ActiveStatus),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("brand.code_conflict", result.Error.Code);
        Assert.Equal("brandCode", Assert.Single(result.Error.FieldErrors!).Field);
        Assert.Empty(audit.Events);
    }

    [Fact]
    public async Task BrandDeleteAsync_WhenActiveProductIsLinked_ReturnsDependencyConflict()
    {
        var brand = Brand.Create(Guid.NewGuid(), TenantId, "ACME", "Acme", "acme", null, BrandConstants.ActiveStatus, UserId, Now);
        var repository = new FakeBrandRepository { EditableBrand = brand, HasProductLinks = true };
        var audit = new FakeBrandAuditLogger();
        var service = new BrandService(repository, new BrandRequestValidator(), new FakeDateTimeProvider(), audit);

        var result = await service.DeleteAsync(CreateContext([BrandConstants.DeletePermission]), brand.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("brand.delete_conflict", result.Error.Code);
        Assert.Equal(BrandConstants.ActiveStatus, brand.Status);
        Assert.Empty(audit.Events);
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

    private sealed class FakeBrandAuditLogger : IBrandAuditLogger
    {
        public List<(string EventName, Guid TenantId, Guid UserId, Guid BrandId, long RowVersion)> Events { get; } = [];
        public void LogMutation(string eventName, Guid tenantId, Guid userId, Guid brandId, long rowVersion) => Events.Add((eventName, tenantId, userId, brandId, rowVersion));
    }

    private sealed class FakeBrandRepository : IBrandRepository
    {
        public Brand? AddedBrand { get; private set; }
        public Brand? EditableBrand { get; init; }
        public List<MediaAsset> AddedMediaAssets { get; } = [];
        public List<Guid> InactivatedMediaAssetIds { get; } = [];
        public bool HasProductLinks { get; init; }
        public string? AddErrorCode { get; init; }

        public Task<bool> BrandCodeExistsAsync(Guid tenantId, string brandCode, Guid? excludeBrandId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasProductLinksAsync(Guid tenantId, Guid brandId, CancellationToken cancellationToken)
        {
            return Task.FromResult(HasProductLinks);
        }

        public Task<BrandListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            return Task.FromResult(new BrandListResponse([], pageNumber, pageSize, 0));
        }

        public Task<BrandResponse?> GetByIdAsync(Guid tenantId, Guid brandId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var brand = AddedBrand ?? EditableBrand;
            return Task.FromResult<BrandResponse?>(new BrandResponse(brandId, brand!.BrandCode, brand.BrandName, null, brand.LogoMediaAssetId, brand.Status, brand.CreatedAt, brand.UpdatedAt, brand.Description, brand.SortOrder, brand.RowVersion));
        }

        public Task<Brand?> GetEditableAsync(Guid tenantId, Guid brandId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Brand?>(AddedBrand ?? EditableBrand);
        }

        public Task AddAsync(Brand brand, CancellationToken cancellationToken)
        {
            if (AddErrorCode is not null) throw new BrandPersistenceException(AddErrorCode);
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

        public Task<Collection?> GetByCodeAsync(Guid tenantId, string collectionCode, CancellationToken cancellationToken)
        {
            return Task.FromResult<Collection?>(null);
        }

        public Task<IReadOnlyList<CollectionProductResponseDto>> GetCollectionProductsAsync(Guid tenantId, Guid collectionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CollectionProductResponseDto>>([]);
        }

        public Task ReplaceCollectionProductsAsync(Guid tenantId, Guid collectionId, List<Guid> productIds, Guid? userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<bool> AllProductsExistAndNotDeletedAsync(Guid tenantId, List<Guid> productIds, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}

