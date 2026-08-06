using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using Xunit;

namespace E_POS.UnitTests.OutletTillDevice;

public sealed class TenantAdminOutletServiceOverviewTests
{
    [Fact]
    public async Task GetOverviewAsync_MissingCorePermission_ReturnsPermissionDenied()
    {
        var repository = new FakeTenantAdminOutletRepository();
        var service = new TenantAdminOutletService(repository);
        var context = new TenantRequestContext(
            TenantId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Permissions: new HashSet<string>());

        var result = await service.GetOverviewAsync(context, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetOverviewAsync_OutletNotFound_ReturnsNotFound()
    {
        var repository = new FakeTenantAdminOutletRepository { Exists = false };
        var service = new TenantAdminOutletService(repository);
        var context = new TenantRequestContext(
            TenantId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Permissions: new HashSet<string> { TenantAdminOutletPermissions.View });

        var result = await service.GetOverviewAsync(context, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetOverviewAsync_PartialPermissions_MasksUnauthorizedSections()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var repository = new FakeTenantAdminOutletRepository
        {
            Exists = true,
            InfoResponse = new OutletOverviewInfoResponse(outletId, "Main Store", "S001", "STORE", "ACTIVE", null, "123 Main St", "Colombo"),
            TillsResponse = new TenantAdminOutletTillsResponse(
                new TenantAdminOutletTillsSummaryResponse(1, 1, 1, 0),
                new List<TenantAdminOutletTillItemResponse>
                {
                    new(Guid.NewGuid(), "Till 1", "T01", "ACTIVE", 100m, 100m, DateTimeOffset.UtcNow, null, "Cashier", "Online")
                }),
            TillHealthInputs = new List<OutletOperationalHealthCalculator.TillHealthInput>
            {
                new(Guid.NewGuid(), "T01", "Till 1", "ACTIVE", "Online", DateTimeOffset.UtcNow)
            }
        };

        var service = new TenantAdminOutletService(repository);
        var context = new TenantRequestContext(
            TenantId: tenantId,
            UserId: Guid.NewGuid(),
            Permissions: new HashSet<string>
            {
                TenantAdminOutletPermissions.View,
                TenantAdminOutletPermissions.TillsView
                // Sales, Inventory, Orders permissions omitted intentionally
            });

        var result = await service.GetOverviewAsync(context, outletId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var overview = result.Value!;
        Assert.NotNull(overview.Outlet);
        Assert.NotNull(overview.Tills);
        Assert.True(overview.Access.CanViewTills);

        // Masked sections
        Assert.Null(overview.Sales);
        Assert.False(overview.Access.CanViewSales);
        Assert.Null(overview.Inventory);
        Assert.False(overview.Access.CanViewInventory);
        Assert.Null(overview.Orders);
        Assert.False(overview.Access.CanViewOrders);
    }

    [Fact]
    public async Task SetManagerAsync_Success_AssignsPrimaryManager()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var repository = new FakeTenantAdminOutletRepository { Exists = true, TenantUserActive = true };
        var service = new TenantAdminOutletService(repository);
        var context = new TenantRequestContext(tenantId, userId, new HashSet<string> { TenantAdminOutletPermissions.Manage });

        var result = await service.SetManagerAsync(context, outletId, new TenantAdminOutletManagerUpdateRequest(managerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(managerId, repository.AssignedManagerId);
    }

    [Fact]
    public async Task SetImageAsync_Success_AssignsOutletImage()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();

        var repository = new FakeTenantAdminOutletRepository { Exists = true, MediaAssetActive = true };
        var service = new TenantAdminOutletService(repository);
        var context = new TenantRequestContext(tenantId, userId, new HashSet<string> { TenantAdminOutletPermissions.Update });

        var result = await service.SetImageAsync(context, outletId, new TenantAdminOutletImageUpdateRequest(mediaAssetId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(mediaAssetId, repository.AssignedMediaAssetId);
    }

    [Fact]
    public async Task ListAsync_MissingCorePermission_ReturnsPermissionDenied()
    {
        var service = new TenantAdminOutletService(new FakeTenantAdminOutletRepository());
        var result = await service.ListAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), new HashSet<string>()),
            new TenantAdminOutletListQuery(1, 20, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task UpdateStatusAsync_DefaultOutletCannotBeDisabled()
    {
        var repository = new FakeTenantAdminOutletRepository
        {
            LifecycleState = new TenantAdminOutletLifecycleState(true, false, false, false, false)
        };
        var service = new TenantAdminOutletService(repository);
        var context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), new HashSet<string> { TenantAdminOutletPermissions.Manage });

        var result = await service.UpdateStatusAsync(context, Guid.NewGuid(), new TenantAdminOutletStatusUpdateRequest("INACTIVE"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.default_cannot_disable", result.Error.Code);
        Assert.Null(repository.UpdatedStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_ActiveOutletUpdatesLifecycleAndAudits()
    {
        var repository = new FakeTenantAdminOutletRepository
        {
            LifecycleState = new TenantAdminOutletLifecycleState(false, false, false, false, false)
        };
        var auditLogger = new FakeOutletAuditLogger();
        var service = new TenantAdminOutletService(repository, auditLogger);
        var context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), new HashSet<string> { TenantAdminOutletPermissions.Manage });

        var result = await service.UpdateStatusAsync(context, Guid.NewGuid(), new TenantAdminOutletStatusUpdateRequest("inactive"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("INACTIVE", repository.UpdatedStatus);
        Assert.Equal(1, auditLogger.StatusChangeCount);
    }

    private sealed class FakeTenantAdminOutletRepository : ITenantAdminOutletRepository
    {
        public bool Exists { get; set; } = true;
        public bool TenantUserActive { get; set; } = true;
        public bool MediaAssetActive { get; set; } = true;
        public Guid? AssignedManagerId { get; set; }
        public Guid? AssignedMediaAssetId { get; set; }
        public string? UpdatedStatus { get; private set; }
        public TenantAdminOutletLifecycleState? LifecycleState { get; set; }

        public OutletOverviewInfoResponse? InfoResponse { get; set; }
        public TenantAdminOutletTillsResponse TillsResponse { get; set; } = new(new(0, 0, 0, 0), Array.Empty<TenantAdminOutletTillItemResponse>());
        public IReadOnlyList<OutletOperationalHealthCalculator.TillHealthInput> TillHealthInputs { get; set; } = Array.Empty<OutletOperationalHealthCalculator.TillHealthInput>();

        public Task<TenantAdminOutletListResponse> ListAsync(Guid tenantId, TenantAdminOutletListQuery query, CancellationToken cancellationToken)
            => Task.FromResult(new TenantAdminOutletListResponse(Array.Empty<TenantAdminOutletListItemResponse>(), query.PageNumber, query.PageSize, 0));

        public Task<bool> OutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(Exists);

        public Task<TenantAdminOutletLifecycleState?> GetLifecycleStateAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(LifecycleState);

        public Task<bool> UpdateStatusAsync(Guid tenantId, Guid outletId, string status, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            UpdatedStatus = status;
            return Task.FromResult(Exists);
        }

        public Task<TenantAdminOutletDetailResponse?> GetDetailAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult<TenantAdminOutletDetailResponse?>(null);

        public Task<TenantAdminOutletRevenueSummaryResponse> GetRevenueSummaryAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<TenantAdminOutletUsersResponse> GetUsersAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<TenantAdminOutletTillsResponse> GetTillsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(TillsResponse);

        public Task<OutletOverviewInfoResponse?> GetOverviewInfoAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(InfoResponse);

        public Task<OutletOverviewManagerResponse?> GetOverviewManagerAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult<OutletOverviewManagerResponse?>(null);

        public Task<OutletOverviewSalesSummaryResponse> GetOverviewSalesAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(new OutletOverviewSalesSummaryResponse(100m, 5m, "LKR"));

        public Task<decimal> GetOverviewStockValueAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(500m);

        public Task<int> GetOverviewOpenOrdersCountAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(2);

        public Task<IReadOnlyList<OutletOperationalHealthCalculator.TillHealthInput>> GetOverviewTillHealthInputsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(TillHealthInputs);

        public Task<string> GetTenantCurrencyCodeAsync(Guid tenantId, CancellationToken cancellationToken)
            => Task.FromResult("LKR");

        public Task<bool> TenantUserExistsAndActiveAsync(Guid tenantId, Guid tenantUserId, CancellationToken cancellationToken)
            => Task.FromResult(TenantUserActive);

        public Task<bool> MediaAssetExistsAndActiveAsync(Guid tenantId, Guid mediaAssetId, CancellationToken cancellationToken)
            => Task.FromResult(MediaAssetActive);

        public Task<bool> SetPrimaryManagerAsync(Guid tenantId, Guid outletId, Guid tenantUserId, Guid? assignedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            AssignedManagerId = tenantUserId;
            return Task.FromResult(true);
        }

        public Task<bool> RemovePrimaryManagerAsync(Guid tenantId, Guid outletId, Guid? revokedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            AssignedManagerId = null;
            return Task.FromResult(true);
        }

        public Task<bool> SetOutletImageAsync(Guid tenantId, Guid outletId, Guid mediaAssetId, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            AssignedMediaAssetId = mediaAssetId;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveOutletImageAsync(Guid tenantId, Guid outletId, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            AssignedMediaAssetId = null;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeOutletAuditLogger : IOutletAuditLogger
    {
        public int StatusChangeCount { get; private set; }

        public void LogOutletCreated(Guid tenantId, Guid actorTenantUserId, Guid outletId, string outletCode, string outletType, string status) { }
        public void LogManagerAssigned(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid managerTenantUserId) { }
        public void LogManagerRemoved(Guid tenantId, Guid actorTenantUserId, Guid outletId) { }
        public void LogImageAssociated(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid mediaAssetId) { }
        public void LogImageRemoved(Guid tenantId, Guid actorTenantUserId, Guid outletId) { }
        public void LogStatusChanged(Guid tenantId, Guid actorTenantUserId, Guid outletId, string status) => StatusChangeCount++;
    }
}
