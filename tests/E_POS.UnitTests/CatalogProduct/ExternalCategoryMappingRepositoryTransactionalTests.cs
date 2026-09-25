using E_POS.Application.Common.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ExternalCategoryMappingRepositoryTransactionalTests : IDisposable
{
    private readonly EPosDbContext _dbContext;
    private readonly ExternalCategoryMappingRepository _repository;
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _categoryA = Guid.NewGuid();
    private readonly Guid _categoryB = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ExternalCategoryMappingRepositoryTransactionalTests()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new EPosDbContext(options);
        _repository = new ExternalCategoryMappingRepository(
            _dbContext,
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            NullLogger<ExternalCategoryMappingRepository>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task UpsertAsync_WithSaveChangesFalse_StagesEntityWithoutImmediateCommit()
    {
        var mapping = await _repository.UpsertAsync(
            _tenantA,
            "openfoodfacts",
            "en:colas",
            "Colas",
            _categoryA,
            "PRODUCT_CONFIRMED",
            _userId,
            CancellationToken.None,
            saveChanges: false);

        Assert.NotNull(mapping);
        Assert.Equal(EntityState.Added, _dbContext.Entry(mapping).State);

        // Before SaveChangesAsync:
        // Querying with AsNoTracking from a fresh query or inspecting change tracker
        Assert.True(_dbContext.ChangeTracker.HasChanges());

        // When caller commits:
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var saved = await _dbContext.ExternalCategoryMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == _tenantA && m.ExternalCategoryKey == "en:colas");

        Assert.NotNull(saved);
        Assert.Equal(_categoryA, saved.TenantCategoryId);
        Assert.Equal("openfoodfacts", saved.Provider);
        Assert.Equal("PRODUCT_CONFIRMED", saved.MappingSource);
    }

    [Fact]
    public async Task UpsertAsync_UserOverride_UpdatesTargetCategoryInSameUnitOfWork()
    {
        // First mapping: categoryA
        await _repository.UpsertAsync(
            _tenantA,
            "openfoodfacts",
            "en:colas",
            "Colas",
            _categoryA,
            "PRODUCT_CONFIRMED",
            _userId,
            CancellationToken.None,
            saveChanges: true);

        // User overrides to categoryB on subsequent create with saveChanges: false
        var updated = await _repository.UpsertAsync(
            _tenantA,
            "openfoodfacts",
            "en:colas",
            "Colas",
            _categoryB,
            "PRODUCT_CONFIRMED",
            _userId,
            CancellationToken.None,
            saveChanges: false);

        Assert.Equal(_categoryB, updated.TenantCategoryId);
        Assert.Equal(EntityState.Modified, _dbContext.Entry(updated).State);

        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var count = await _dbContext.ExternalCategoryMappings
            .CountAsync(m => m.TenantId == _tenantA && m.ExternalCategoryKey == "en:colas");
        Assert.Equal(1, count);

        var saved = await _dbContext.ExternalCategoryMappings
            .AsNoTracking()
            .FirstAsync(m => m.TenantId == _tenantA && m.ExternalCategoryKey == "en:colas");
        Assert.Equal(_categoryB, saved.TenantCategoryId);
    }

    [Fact]
    public async Task UpsertAsync_TenantIsolation_MaintainsSeparateMappingsForSameExternalKey()
    {
        await _repository.UpsertAsync(
            _tenantA,
            "openfoodfacts",
            "en:colas",
            "Colas Tenant A",
            _categoryA,
            "PRODUCT_CONFIRMED",
            _userId,
            CancellationToken.None,
            saveChanges: true);

        await _repository.UpsertAsync(
            _tenantB,
            "openfoodfacts",
            "en:colas",
            "Colas Tenant B",
            _categoryB,
            "PRODUCT_CONFIRMED",
            _userId,
            CancellationToken.None,
            saveChanges: true);

        var mappingA = await _dbContext.ExternalCategoryMappings
            .AsNoTracking()
            .FirstAsync(m => m.TenantId == _tenantA && m.ExternalCategoryKey == "en:colas");
        var mappingB = await _dbContext.ExternalCategoryMappings
            .AsNoTracking()
            .FirstAsync(m => m.TenantId == _tenantB && m.ExternalCategoryKey == "en:colas");

        Assert.Equal(_categoryA, mappingA.TenantCategoryId);
        Assert.Equal(_categoryB, mappingB.TenantCategoryId);
        Assert.NotEqual(mappingA.Id, mappingB.Id);
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; }
        public FixedDateTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;
    }
}
