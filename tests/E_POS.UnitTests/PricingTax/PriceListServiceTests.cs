using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Application.Modules.Tenant.PricingTax.Services;
using E_POS.Application.Modules.Tenant.PricingTax.Validators;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using Xunit;

namespace E_POS.UnitTests.PricingTax;

public sealed class PriceListServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_WithoutPermission_ReturnsPermissionDenied()
    {
        var service = new PriceListService(
            new FakePriceListRepository(),
            new FakeOutletRepository(),
            new FakeTenantLookupRepository(),
            new PriceListRequestValidator(),
            new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([]),
            CreateValidRequest("PL-1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithPermission_Succeeds()
    {
        var repository = new FakePriceListRepository();
        var service = new PriceListService(
            repository,
            new FakeOutletRepository(),
            new FakeTenantLookupRepository(),
            new PriceListRequestValidator(),
            new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([PricingTaxConstants.CreatePermission]),
            CreateValidRequest("pl-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedPriceList);
        Assert.Equal("PL-1", repository.AddedPriceList.PriceListCode);
        Assert.Equal("LKR", repository.AddedPriceList.CurrencyCode);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ReturnsConflict()
    {
        var repository = new FakePriceListRepository { CodeExists = true };
        var service = new PriceListService(
            repository,
            new FakeOutletRepository(),
            new FakeTenantLookupRepository(),
            new PriceListRequestValidator(),
            new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([PricingTaxConstants.CreatePermission]),
            CreateValidRequest("PL-1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list.duplicate_code", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCurrency_ReturnsError()
    {
        var lookupRepository = new FakeTenantLookupRepository { CurrencyExists = false };
        var service = new PriceListService(
            new FakePriceListRepository(),
            new FakeOutletRepository(),
            lookupRepository,
            new PriceListRequestValidator(),
            new FakeDateTimeProvider());

        var result = await service.CreateAsync(
            CreateContext([PricingTaxConstants.CreatePermission]),
            CreateValidRequest("PL-1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list.invalid_currency", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidOutlets_ReturnsError()
    {
        var outletRepository = new FakeOutletRepository { OutletsBelongToTenant = false };
        var service = new PriceListService(
            new FakePriceListRepository(),
            outletRepository,
            new FakeTenantLookupRepository(),
            new PriceListRequestValidator(),
            new FakeDateTimeProvider());

        var request = CreateValidRequest("PL-1") with
        {
            AssignedOutletIds = [Guid.NewGuid()]
        };

        var result = await service.CreateAsync(
            CreateContext([PricingTaxConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list.invalid_outlets", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsPriceList()
    {
        var repository = new FakePriceListRepository();
        var service = new PriceListService(
            repository,
            new FakeOutletRepository(),
            new FakeTenantLookupRepository(),
            new PriceListRequestValidator(),
            new FakeDateTimeProvider());

        var priceListId = Guid.NewGuid();
        repository.ExistingResponse = new PriceListResponse(
            priceListId, "PL-1", "Standard Price List", "POS", "LKR", false, true, 0, null, null, "ACTIVE", [], [], Now, Now);

        var result = await service.GetByIdAsync(
            CreateContext([PricingTaxConstants.ViewPermission]),
            priceListId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("PL-1", result.Value.PriceListCode);
    }

    [Fact]
    public async Task DeleteAsync_SetsStatusToDeleted()
    {
        var repository = new FakePriceListRepository();
        var service = new PriceListService(
            repository,
            new FakeOutletRepository(),
            new FakeTenantLookupRepository(),
            new PriceListRequestValidator(),
            new FakeDateTimeProvider());

        var priceListId = Guid.NewGuid();
        var priceList = PriceList.Create(
            priceListId, TenantId, "PL-1", "Standard", "POS", "LKR", false, true, 0, null, null, "ACTIVE", UserId, Now);
        repository.EditablePriceList = priceList;

        var result = await service.DeleteAsync(
            CreateContext([PricingTaxConstants.DeletePermission]),
            priceListId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PricingTaxConstants.DeletedStatus, priceList.Status);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions)
    {
        return new TenantRequestContext(TenantId, UserId, permissions);
    }

    private static PriceListCreateRequest CreateValidRequest(string code)
    {
        return new PriceListCreateRequest(
            PriceListCode: code,
            PriceListName: "Standard Price List",
            PriceListType: "POS",
            CurrencyCode: "LKR",
            PriceIncludesTax: false,
            IsDefaultPriceList: true,
            Priority: 0,
            ValidFrom: null,
            ValidUntil: null,
            Status: "ACTIVE",
            AssignedOutletIds: null,
            AssignedSalesChannelIds: null);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeOutletRepository : IOutletRepository
    {
        public bool OutletsBelongToTenant { get; set; } = true;

        public Task<bool> AllOutletsBelongToTenantAsync(Guid tenantId, Guid[] outletIds, CancellationToken cancellationToken)
        {
            return Task.FromResult(OutletsBelongToTenant);
        }

        public Task<bool> OutletCodeExistsAsync(Guid tenantId, string outletCode, Guid? excludeOutletId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Guid?> GetActivePickupFulfillmentMethodIdAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);
        public Task<OutletListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(new OutletListResponse([], pageNumber, pageSize, 0));
        public Task<OutletResponse?> GetByIdAsync(Guid tenantId, Guid outletId, bool includeDeleted, CancellationToken cancellationToken) => Task.FromResult<OutletResponse?>(null);
        public Task<OutletEditAggregate?> GetEditAggregateAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) => Task.FromResult<OutletEditAggregate?>(null);
        public Task<bool> HasActiveTillOrDeviceAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> AddAsync(Outlet outlet, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? pickupMapping, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> SaveUpdatedAsync(OutletEditAggregate aggregate, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? newPickupMapping, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTenantLookupRepository : ITenantLookupRepository
    {
        public bool CurrencyExists { get; set; } = true;
        public bool ChannelsExist { get; set; } = true;

        public Task<bool> CurrencyExistsAsync(string currencyCode, CancellationToken cancellationToken)
        {
            return Task.FromResult(CurrencyExists);
        }

        public Task<bool> AllSalesChannelsExistAsync(Guid tenantId, Guid[] channelIds, CancellationToken cancellationToken)
        {
            return Task.FromResult(ChannelsExist);
        }
    }

    private sealed class FakePriceListRepository : IPriceListRepository
    {
        public bool CodeExists { get; set; }

        public PriceList? AddedPriceList { get; private set; }
        public List<PriceListOutlet> AddedOutlets { get; } = [];
        public List<PriceListChannel> AddedChannels { get; } = [];
        public PriceListResponse? ExistingResponse { get; set; }
        public PriceList? EditablePriceList { get; set; }

        public Task<bool> PriceListCodeExistsAsync(Guid tenantId, string code, Guid? excludePriceListId, CancellationToken cancellationToken)
        {
            return Task.FromResult(CodeExists);
        }

        public Task<PriceList?> GetEditableAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(EditablePriceList);
        }

        public Task<PriceListResponse?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        {
            if (ExistingResponse != null) return Task.FromResult<PriceListResponse?>(ExistingResponse);

            if (AddedPriceList != null)
            {
                return Task.FromResult<PriceListResponse?>(new PriceListResponse(
                    AddedPriceList.Id,
                    AddedPriceList.PriceListCode,
                    AddedPriceList.PriceListName,
                    AddedPriceList.PriceListType,
                    AddedPriceList.CurrencyCode,
                    AddedPriceList.PriceIncludesTax,
                    AddedPriceList.IsDefaultPriceList,
                    AddedPriceList.Priority,
                    AddedPriceList.ValidFrom,
                    AddedPriceList.ValidUntil,
                    AddedPriceList.Status,
                    AddedOutlets.Select(x => x.OutletId).ToArray(),
                    AddedChannels.Select(x => x.SalesChannelId).ToArray(),
                    Now,
                    Now));
            }

            return Task.FromResult<PriceListResponse?>(null);
        }

        public Task<PriceListListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PriceListListResponse([], pageNumber, pageSize, 0));
        }

        public Task AddAsync(PriceList priceList, CancellationToken cancellationToken)
        {
            AddedPriceList = priceList;
            return Task.CompletedTask;
        }

        public Task AddOutletAssignmentsAsync(IEnumerable<PriceListOutlet> assignments, CancellationToken cancellationToken)
        {
            AddedOutlets.AddRange(assignments);
            return Task.CompletedTask;
        }

        public Task AddChannelAssignmentsAsync(IEnumerable<PriceListChannel> assignments, CancellationToken cancellationToken)
        {
            AddedChannels.AddRange(assignments);
            return Task.CompletedTask;
        }

        public Task ClearAssignmentsAsync(Guid priceListId, CancellationToken cancellationToken)
        {
            AddedOutlets.Clear();
            AddedChannels.Clear();
            return Task.CompletedTask;
        }

        public Task ClearOtherDefaultsAsync(Guid tenantId, Guid defaultPriceListId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}


