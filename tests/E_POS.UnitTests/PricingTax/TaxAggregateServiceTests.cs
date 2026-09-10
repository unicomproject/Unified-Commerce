using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Application.Modules.Tenant.PricingTax.Services;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using Xunit;

namespace E_POS.UnitTests.PricingTax;

public sealed class TaxAggregateServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid OtherTenantId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");

    [Fact]
    public async Task CreateTax_Taxable_Succeeds()
    {
        var repo = new FakeRepo();
        var service = CreateService(repo);
        var context = Ctx(PricingTaxPermissions.TaxClasses.Create);

        var result = await service.CreateTaxAsync(context, new TaxAggregateCreateRequest
        {
            Name = "Standard Tax",
            Code = "STD-TAX",
            TaxTreatment = TaxTreatments.Taxable,
            InitialRate = 18m,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repo.LastClass);
        Assert.Equal(TaxTreatments.Taxable, repo.LastClass!.TaxTreatment);
        Assert.NotNull(repo.LastRate);
        Assert.Equal(18m, repo.LastRate!.RatePercent);
    }

    [Fact]
    public async Task CreateTax_ZeroRated_ForcesZero()
    {
        var repo = new FakeRepo();
        var service = CreateService(repo);
        var context = Ctx(PricingTaxPermissions.TaxClasses.Create);

        var result = await service.CreateTaxAsync(context, new TaxAggregateCreateRequest
        {
            Name = "Zero Rated",
            Code = "ZR",
            TaxTreatment = TaxTreatments.ZeroRated,
            InitialRate = 0m,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, repo.LastRate!.RatePercent);
        Assert.Equal(TaxTreatments.ZeroRated, repo.LastClass!.TaxTreatment);
    }

    [Fact]
    public async Task CreateTax_Exempt_SucceedsWithoutPercentage()
    {
        var repo = new FakeRepo();
        var service = CreateService(repo);
        var context = Ctx(PricingTaxPermissions.TaxClasses.Create);

        var result = await service.CreateTaxAsync(context, new TaxAggregateCreateRequest
        {
            Name = "Tax Exempt",
            Code = "EX",
            TaxTreatment = TaxTreatments.Exempt,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TaxTreatments.Exempt, repo.LastClass!.TaxTreatment);
        Assert.Equal(0m, repo.LastRate!.RatePercent);
    }

    [Fact]
    public async Task CreateTax_DuplicateCode_Rejected()
    {
        var existing = TaxClass.Create(TenantId, "STD", "Existing", TaxTreatments.Taxable, null, false, UserId, DateTimeOffset.UtcNow);
        var repo = new FakeRepo { ExistingByCode = existing };
        var service = CreateService(repo);
        var context = Ctx(PricingTaxPermissions.TaxClasses.Create);

        var result = await service.CreateTaxAsync(context, new TaxAggregateCreateRequest
        {
            Name = "Other",
            Code = "std",
            TaxTreatment = TaxTreatments.Taxable,
            InitialRate = 10m,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.tax_aggregate.code_exists", result.Error?.Code);
    }

    [Fact]
    public async Task CreateTax_MissingName_Rejected()
    {
        var service = CreateService(new FakeRepo());
        var result = await service.CreateTaxAsync(Ctx(PricingTaxPermissions.TaxClasses.Create), new TaxAggregateCreateRequest
        {
            Code = "X",
            TaxTreatment = TaxTreatments.Taxable,
            InitialRate = 5,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.tax_aggregate.name_required", result.Error?.Code);
    }

    [Fact]
    public async Task UpdateTreatment_AfterUsage_Rejected()
    {
        var tax = TaxClass.Create(TenantId, "STD", "Standard", TaxTreatments.Taxable, null, false, UserId, DateTimeOffset.UtcNow);
        var repo = new FakeRepo
        {
            ExistingById = tax,
            HasUsage = true
        };
        var service = CreateService(repo);

        var result = await service.UpdateTaxAsync(Ctx(PricingTaxPermissions.TaxClasses.Update), tax.Id, new TaxAggregateUpdateRequest
        {
            Name = "Standard",
            TaxTreatment = TaxTreatments.Exempt
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.tax_aggregate.treatment_locked", result.Error?.Code);
    }

    [Fact]
    public async Task ScheduleRate_DoesNotOverwriteCurrentEarly()
    {
        var tax = TaxClass.Create(TenantId, "STD", "Standard", TaxTreatments.Taxable, null, false, UserId, DateTimeOffset.UtcNow);
        var jurisdictionId = Guid.NewGuid();
        var current = TaxRate.Create(TenantId, jurisdictionId, "STD-RATE", "Rate", 18m, false,
            DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-1)), null, UserId, DateTimeOffset.UtcNow);
        var repo = new FakeRepo
        {
            ExistingById = tax,
            Rates = [current],
            ClassRates = [TaxClassRate.Create(TenantId, tax.Id, current.Id, 1, UserId, DateTimeOffset.UtcNow)]
        };
        var service = CreateService(repo);
        var future = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30));

        var result = await service.ScheduleRateAsync(Ctx(PricingTaxPermissions.TaxRates.ScheduleManage), tax.Id, new TaxScheduleRateRequest
        {
            NewRate = 20m,
            EffectiveFrom = future
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(18m, current.RatePercent);
        Assert.Equal(future.AddDays(-1), current.ValidUntil);
        Assert.NotNull(repo.LastRate);
        Assert.Equal(20m, repo.LastRate!.RatePercent);
        Assert.Equal(future, repo.LastRate.ValidFrom);
    }

    [Fact]
    public async Task ScheduleRate_Exempt_Rejected()
    {
        var tax = TaxClass.Create(TenantId, "EX", "Exempt", TaxTreatments.Exempt, null, false, UserId, DateTimeOffset.UtcNow);
        var repo = new FakeRepo { ExistingById = tax };
        var service = CreateService(repo);

        var result = await service.ScheduleRateAsync(Ctx(PricingTaxPermissions.TaxRates.ScheduleManage), tax.Id, new TaxScheduleRateRequest
        {
            NewRate = 5m,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(10))
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.tax_aggregate.exempt_schedule", result.Error?.Code);
    }

    [Fact]
    public async Task Deactivate_KeepsProductAssignments_OptionB()
    {
        var tax = TaxClass.Create(TenantId, "STD", "Standard", TaxTreatments.Taxable, null, false, UserId, DateTimeOffset.UtcNow);
        var repo = new FakeRepo { ExistingById = tax, ProductCounts = new Dictionary<Guid, int> { [tax.Id] = 3 } };
        var service = CreateService(repo);

        var result = await service.DeactivateAsync(Ctx(PricingTaxPermissions.TaxClasses.StatusManage), tax.Id, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("INACTIVE", tax.Status);
        Assert.Equal(3, result.Value!.ProductCount);
        Assert.False(repo.ClearedAssignments);
    }

    [Fact]
    public async Task GetTaxes_PermissionDenied()
    {
        var service = CreateService(new FakeRepo());
        var result = await service.GetTaxesAsync(Ctx(), null, null, 1, 20, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.tax_aggregate.permission_denied", result.Error?.Code);
    }

    [Fact]
    public async Task ResolveCurrent_PicksLatestApplicable()
    {
        var rates = new List<TaxRate>
        {
            TaxRate.Create(TenantId, Guid.NewGuid(), "R1", "R1", 15m, false, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), UserId, DateTimeOffset.UtcNow),
            TaxRate.Create(TenantId, Guid.NewGuid(), "R2", "R2", 17m, false, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), UserId, DateTimeOffset.UtcNow),
            TaxRate.Create(TenantId, Guid.NewGuid(), "R3", "R3", 18m, false, new DateOnly(2026, 1, 1), null, UserId, DateTimeOffset.UtcNow),
            TaxRate.Create(TenantId, Guid.NewGuid(), "R4", "R4", 20m, false, new DateOnly(2027, 1, 1), null, UserId, DateTimeOffset.UtcNow),
        };

        var current = TaxRateResolution.ResolveCurrent(rates, new DateOnly(2026, 6, 1));
        var next = TaxRateResolution.ResolveNext(rates, new DateOnly(2026, 6, 1));

        Assert.Equal(18m, current!.RatePercent);
        Assert.Equal(20m, next!.RatePercent);
    }

    [Fact]
    public void TaxTreatments_MapLegacy_DoesNotForceExemptOnOther()
    {
        Assert.Equal(TaxTreatments.Taxable, TaxTreatments.MapFromLegacyTaxType("OTHER", 0m));
        Assert.Equal(TaxTreatments.Exempt, TaxTreatments.MapFromLegacyTaxType("EXEMPT"));
        Assert.Equal(TaxTreatments.ZeroRated, TaxTreatments.MapFromLegacyTaxType("ZERO_RATED"));
    }

    [Fact]
    public async Task CrossTenant_GetReturnsNotFound()
    {
        var tax = TaxClass.Create(OtherTenantId, "STD", "Standard", TaxTreatments.Taxable, null, false, UserId, DateTimeOffset.UtcNow);
        var repo = new FakeRepo(); // no ExistingById for this tenant
        var service = CreateService(repo);
        var result = await service.GetTaxAsync(Ctx(PricingTaxPermissions.TaxClasses.View), tax.Id, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.tax_aggregate.not_found", result.Error?.Code);
    }

    private static TenantRequestContext Ctx(params string[] permissions) =>
        new(TenantId, UserId, permissions);

    private static TaxAggregateService CreateService(FakeRepo repo) =>
        new(repo, new FakeClock());

    private sealed class FakeClock : E_POS.Application.Common.Contracts.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeRepo : ITaxSetupRepository
    {
        public TaxClass? ExistingById { get; set; }
        public TaxClass? ExistingByCode { get; set; }
        public TaxClass? LastClass { get; private set; }
        public TaxRate? LastRate { get; private set; }
        public List<TaxRate> Rates { get; set; } = [];
        public List<TaxClassRate> ClassRates { get; set; } = [];
        public bool HasUsage { get; set; }
        public bool ClearedAssignments { get; private set; }
        public Dictionary<Guid, int> ProductCounts { get; set; } = new();

        public Task<TaxClass?> GetTaxClassByIdAsync(Guid tenantId, Guid taxClassId) =>
            Task.FromResult(ExistingById is not null && ExistingById.TenantId == tenantId && ExistingById.Id == taxClassId ? ExistingById : null);

        public Task<TaxClass?> GetTaxClassByCodeAsync(Guid tenantId, string taxClassCode) =>
            Task.FromResult(ExistingByCode is not null && ExistingByCode.TenantId == tenantId ? ExistingByCode : null);

        public Task<(IEnumerable<TaxClass> Items, int TotalCount)> GetTaxClassesAsync(Guid tenantId, int page, int pageSize) =>
            Task.FromResult((Enumerable.Empty<TaxClass>(), 0));

        public Task<(IReadOnlyList<TaxClass> Items, int TotalCount)> GetTaxClassesFilteredAsync(
            Guid tenantId, string? search, string? status, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<TaxClass>)Array.Empty<TaxClass>(), 0));

        public Task AddTaxClassAsync(TaxClass taxClass)
        {
            LastClass = taxClass;
            ExistingById = taxClass;
            return Task.CompletedTask;
        }

        public void UpdateTaxClass(TaxClass taxClass) { }
        public Task ClearDefaultTaxClassAsync(Guid tenantId, Guid? excludeTaxClassId) => Task.CompletedTask;
        public Task<TaxRate?> GetTaxRateByIdAsync(Guid tenantId, Guid taxRateId) =>
            Task.FromResult(Rates.FirstOrDefault(r => r.Id == taxRateId));
        public Task<TaxRate?> GetTaxRateByCodeAsync(Guid tenantId, string taxRateCode) => Task.FromResult<TaxRate?>(null);
        public Task<(IEnumerable<TaxRate> Items, int TotalCount)> GetTaxRatesAsync(Guid tenantId, int page, int pageSize) =>
            Task.FromResult((Enumerable.Empty<TaxRate>(), 0));

        public Task AddTaxRateAsync(TaxRate taxRate)
        {
            LastRate = taxRate;
            Rates.Add(taxRate);
            return Task.CompletedTask;
        }

        public void UpdateTaxRate(TaxRate taxRate) { }
        public Task<List<TaxClassRate>> GetTaxClassRatesAsync(Guid tenantId, Guid taxClassId) => Task.FromResult(ClassRates);
        public Task<List<TaxRate>> GetRatesForClassAsync(Guid tenantId, Guid taxClassId) => Task.FromResult(Rates);

        public Task<IReadOnlyDictionary<Guid, List<TaxRate>>> GetRatesForClassesAsync(
            Guid tenantId, IReadOnlyCollection<Guid> taxClassIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, List<TaxRate>>>(
                taxClassIds.ToDictionary(id => id, _ => Rates));

        public Task AddTaxClassRatesAsync(IEnumerable<TaxClassRate> taxClassRates)
        {
            ClassRates.AddRange(taxClassRates);
            return Task.CompletedTask;
        }

        public void RemoveTaxClassRates(IEnumerable<TaxClassRate> taxClassRates) { }

        public Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(
            Guid tenantId, IReadOnlyCollection<Guid> taxClassIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(ProductCounts);

        public Task<(IReadOnlyList<TaxProductUsingResponse> Items, int TotalCount)> GetProductsUsingTaxAsync(
            Guid tenantId, Guid taxClassId, string? search, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<TaxProductUsingResponse>)Array.Empty<TaxProductUsingResponse>(), 0));

        public Task<bool> HasProductAssignmentsAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken) =>
            Task.FromResult(HasUsage);

        public Task<bool> HasTransactionalUsageAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken) =>
            Task.FromResult(HasUsage);

        public Task<bool> RateReferencedByTransactionsAsync(Guid tenantId, Guid taxRateId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> JurisdictionExistsAsync(Guid tenantId, Guid jurisdictionId) => Task.FromResult(true);

        public Task<TaxJurisdiction> ResolveDefaultJurisdictionAsync(Guid tenantId, Guid? userId, DateTimeOffset now) =>
            Task.FromResult(TaxJurisdiction.Create(tenantId, "DEFAULT-US", "Default", "COUNTRY", "US", null, null, null, userId, now));

        public Task<string?> GetTenantTimezoneAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("UTC");

        public Task SaveChangesAsync() => Task.CompletedTask;

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            await action(cancellationToken);
            await SaveChangesAsync();
        }
    }
}
