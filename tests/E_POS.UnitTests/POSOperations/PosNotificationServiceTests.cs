using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosNotificationServiceTests
{
    [Fact]
    public async Task MissingCapability_FailsBeforeRepositoryQuery()
    {
        var repository = new FakeRepository();
        var result = await new PosNotificationService(repository).GetInboxAsync(
            Context(PosNotificationSourceAccess.SalesPermission), 1, 20, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_notifications.permission_denied", result.Error.Code);
        Assert.False(repository.Called);
    }

    [Theory]
    [InlineData(PosNotificationSourceAccess.SalesPermission, "POS", "Sales")]
    [InlineData(PosNotificationSourceAccess.OnlineOrderPermission, "ECommerce", null)]
    [InlineData(PosNotificationSourceAccess.ReturnPermission, "Returns", "Refunds")]
    public async Task FeaturePermission_LimitsSourceModules(
        string featurePermission,
        string expected,
        string? secondExpected)
    {
        var repository = new FakeRepository { UnreadCount = 3 };
        var result = await new PosNotificationService(repository).GetInboxAsync(
            Context(PosPermissions.Notifications.View, featurePermission), 1, 20, default);

        Assert.True(result.IsSuccess);
        Assert.Contains(expected, repository.AllowedSources);
        if (secondExpected is not null)
            Assert.Contains(secondExpected, repository.AllowedSources);
        Assert.Equal(3, result.Value!.UnreadCount);
    }

    [Fact]
    public async Task MultiDomainUser_GetsCombinedSourceFilter()
    {
        var repository = new FakeRepository();
        await new PosNotificationService(repository).GetInboxAsync(
            Context(
                PosPermissions.Notifications.View,
                PosNotificationSourceAccess.SalesPermission,
                PosNotificationSourceAccess.OnlineOrderPermission,
                PosNotificationSourceAccess.ReturnPermission),
            1, 20, default);

        Assert.Equal(5, repository.AllowedSources.Count);
    }

    [Fact]
    public async Task LegacyNotificationPermission_DoesNotAuthorizeEndpoint()
    {
        var result = await new PosNotificationService(new FakeRepository()).GetInboxAsync(
            Context("notifications.view", PosNotificationSourceAccess.SalesPermission),
            1, 20, default);
        Assert.False(result.IsSuccess);
    }

    private static TenantRequestContext Context(params string[] permissions) =>
        new(Guid.NewGuid(), Guid.NewGuid(), permissions);

    private sealed class FakeRepository : IPosNotificationRepository
    {
        public bool Called { get; private set; }
        public int UnreadCount { get; init; }
        public IReadOnlyCollection<string> AllowedSources { get; private set; } = [];

        public Task<NotificationInboxQueryResult> GetTenantUserInboxAsync(
            Guid tenantId, Guid tenantUserId,
            IReadOnlyCollection<string> allowedSourceModules,
            int page, int pageSize, CancellationToken cancellationToken)
        {
            Called = true;
            AllowedSources = allowedSourceModules;
            return Task.FromResult(new NotificationInboxQueryResult([], 0));
        }

        public Task<int> GetTenantUserUnreadCountAsync(
            Guid tenantId, Guid tenantUserId,
            IReadOnlyCollection<string> allowedSourceModules,
            CancellationToken cancellationToken)
        {
            AllowedSources = allowedSourceModules;
            return Task.FromResult(UnreadCount);
        }
    }
}
