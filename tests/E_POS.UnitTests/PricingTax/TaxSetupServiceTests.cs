using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Application.Modules.Tenant.PricingTax.Services;
using E_POS.Application.Modules.Tenant.PricingTax.Validators;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using Xunit;

namespace E_POS.UnitTests.PricingTax;

public sealed class TaxSetupServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task CreateTaxClass_ReturnsFailure_WhenPermissionDenied()
    {
        // Arrange
        var context = new TenantRequestContext(TenantId, UserId, []);
        var service = CreateService();

        // Act
        var result = await service.CreateTaxClassAsync(context, new TaxClassCreateRequest(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.tax_setup.permission_denied", result.Error?.Code);
    }

    [Fact]
    public async Task CreateTaxClass_ReturnsFailure_WhenValidationFails()
    {
        // Arrange
        var context = new TenantRequestContext(TenantId, UserId, [PricingTaxPermissions.TaxClasses.Create]);
        var service = CreateService();
        var request = new TaxClassCreateRequest { TaxClassCode = "" };

        // Act
        var result = await service.CreateTaxClassAsync(context, request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("validation.tax_class.invalid_code", result.Error?.Code);
    }

    [Fact]
    public async Task CreateTaxClass_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var context = new TenantRequestContext(TenantId, UserId, [PricingTaxPermissions.TaxClasses.Create]);
        var repository = new FakeTaxSetupRepository();
        var service = CreateService(repository);
        var request = new TaxClassCreateRequest
        {
            TaxClassCode = "TC-01",
            TaxClassName = "Standard Tax",
            IsDefaultTaxClass = true
        };

        // Act
        var result = await service.CreateTaxClassAsync(context, request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedTaxClass);
        Assert.Equal("TC-01", repository.AddedTaxClass.TaxClassCode);
        Assert.True(repository.AddedTaxClass.IsDefaultTaxClass);
        Assert.True(repository.ClearedDefaultClassCalled);
    }

    [Fact]
    public async Task CreateTaxRate_ReturnsFailure_WhenPermissionDenied()
    {
        // Arrange
        var context = new TenantRequestContext(TenantId, UserId, []);
        var service = CreateService();

        // Act
        var result = await service.CreateTaxRateAsync(context, new TaxRateCreateRequest(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.tax_setup.permission_denied", result.Error?.Code);
    }

    [Fact]
    public async Task CreateTaxRate_ReturnsFailure_WhenJurisdictionNotFound()
    {
        // Arrange
        var context = new TenantRequestContext(TenantId, UserId, [PricingTaxPermissions.TaxRates.ScheduleManage]);
        var repository = new FakeTaxSetupRepository { JurisdictionExists = false };
        var service = CreateService(repository);
        var request = new TaxRateCreateRequest
        {
            TaxJurisdictionId = Guid.NewGuid(),
            TaxRateCode = "TR-01",
            TaxRateName = "VAT 20%",
            RatePercent = 20
        };

        // Act
        var result = await service.CreateTaxRateAsync(context, request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.tax_rate.jurisdiction_not_found", result.Error?.Code);
    }

    [Fact]
    public async Task CreateTaxRate_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var context = new TenantRequestContext(TenantId, UserId, [PricingTaxPermissions.TaxRates.ScheduleManage]);
        var repository = new FakeTaxSetupRepository { JurisdictionExists = true };
        var service = CreateService(repository);
        var request = new TaxRateCreateRequest
        {
            TaxJurisdictionId = Guid.NewGuid(),
            TaxRateCode = "TR-01",
            TaxRateName = "VAT 20%",
            RatePercent = 20
        };

        // Act
        var result = await service.CreateTaxRateAsync(context, request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedTaxRate);
        Assert.Equal("TR-01", repository.AddedTaxRate.TaxRateCode);
    }

    private static TaxSetupService CreateService(ITaxSetupRepository? repository = null)
    {
        return new TaxSetupService(
            repository ?? new FakeTaxSetupRepository(),
            new TaxSetupRequestValidator(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeDateTimeProvider : E_POS.Application.Common.Contracts.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class FakeTaxSetupRepository : ITaxSetupRepository
    {
        public bool JurisdictionExists { get; set; } = true;
        public TaxClass? AddedTaxClass { get; private set; }
        public TaxRate? AddedTaxRate { get; private set; }
        public bool ClearedDefaultClassCalled { get; private set; }

        public Task<TaxClass?> GetTaxClassByIdAsync(Guid tenantId, Guid taxClassId) => Task.FromResult<TaxClass?>(null);
        public Task<TaxClass?> GetTaxClassByCodeAsync(Guid tenantId, string taxClassCode) => Task.FromResult<TaxClass?>(null);
        public Task<(IEnumerable<TaxClass> Items, int TotalCount)> GetTaxClassesAsync(Guid tenantId, int page, int pageSize) => Task.FromResult((Enumerable.Empty<TaxClass>(), 0));
        
        public Task<(IReadOnlyList<TaxClass> Items, int TotalCount)> GetTaxClassesFilteredAsync(
            Guid tenantId, string? search, string? status, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<TaxClass>)Array.Empty<TaxClass>(), 0));

        public Task<IReadOnlyDictionary<Guid, List<TaxRate>>> GetRatesForClassesAsync(
            Guid tenantId, IReadOnlyCollection<Guid> taxClassIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, List<TaxRate>>>(new Dictionary<Guid, List<TaxRate>>());

        public Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(
            Guid tenantId, IReadOnlyCollection<Guid> taxClassIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());

        public Task<(IReadOnlyList<TaxProductUsingResponse> Items, int TotalCount)> GetProductsUsingTaxAsync(
            Guid tenantId, Guid taxClassId, string? search, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<TaxProductUsingResponse>)Array.Empty<TaxProductUsingResponse>(), 0));

        public Task<bool> HasProductAssignmentsAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasTransactionalUsageAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> RateReferencedByTransactionsAsync(Guid tenantId, Guid taxRateId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<string?> GetTenantTimezoneAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("UTC");

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            await action(cancellationToken);
            await SaveChangesAsync();
        }

        public Task AddTaxClassAsync(TaxClass taxClass)
        {
            AddedTaxClass = taxClass;
            return Task.CompletedTask;
        }
        
        public void UpdateTaxClass(TaxClass taxClass) { }
        
        public Task<bool> IsTaxClassAssignedAsync(Guid tenantId, Guid taxClassId)
        {
            return Task.FromResult(false);
        }

        public Task<TaxJurisdiction> ResolveDefaultJurisdictionAsync(Guid tenantId, Guid? userId, DateTimeOffset now)
        {
            return Task.FromResult(TaxJurisdiction.Create(tenantId, "DEFAULT-US", "Default", "COUNTRY", "US", null, null, null, userId, now));
        }

        public Task ClearDefaultTaxClassAsync(Guid tenantId, Guid? excludeTaxClassId)
        {
            ClearedDefaultClassCalled = true;
            return Task.CompletedTask;
        }

        public Task<TaxRate?> GetTaxRateByIdAsync(Guid tenantId, Guid taxRateId) => Task.FromResult<TaxRate?>(null);
        public Task<TaxRate?> GetTaxRateByCodeAsync(Guid tenantId, string taxRateCode) => Task.FromResult<TaxRate?>(null);
        public Task<(IEnumerable<TaxRate> Items, int TotalCount)> GetTaxRatesAsync(Guid tenantId, int page, int pageSize) => Task.FromResult((Enumerable.Empty<TaxRate>(), 0));
        
        public Task AddTaxRateAsync(TaxRate taxRate)
        {
            AddedTaxRate = taxRate;
            return Task.CompletedTask;
        }
        
        public void UpdateTaxRate(TaxRate taxRate) { }

        public Task<List<TaxClassRate>> GetTaxClassRatesAsync(Guid tenantId, Guid taxClassId) => Task.FromResult(new List<TaxClassRate>());
        public Task<List<TaxRate>> GetRatesForClassAsync(Guid tenantId, Guid taxClassId) => Task.FromResult(new List<TaxRate>());
        public Task AddTaxClassRatesAsync(IEnumerable<TaxClassRate> taxClassRates) => Task.CompletedTask;
        public void RemoveTaxClassRates(IEnumerable<TaxClassRate> taxClassRates) { }
        
        public Task<bool> JurisdictionExistsAsync(Guid tenantId, Guid jurisdictionId) => Task.FromResult(JurisdictionExists);
        
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}


