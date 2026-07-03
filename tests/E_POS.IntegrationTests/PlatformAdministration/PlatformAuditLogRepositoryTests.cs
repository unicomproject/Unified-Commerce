using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformAuditLogRepositoryTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserOneId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserTwoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SuccessAuditId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FailedAuditId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid LockedAuditId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task GetLoginSecurityAuditLogsAsync_ReturnsLatestFirstPaginatedItems()
    {
        await using var dbContext = CreateDbContext();
        SeedAuditLogs(dbContext);

        IPlatformAuditLogRepository repository = new PlatformAuditLogRepository(dbContext);

        var response = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.PlatformAdministration.Dtos.PlatformAuditLogListQuery
            {
                PageNumber = 1,
                PageSize = 2
            },
            CancellationToken.None);

        Assert.Equal("platform_login_security", response.AuditScope);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
        Assert.Equal(LockedAuditId, response.Items[0].Id);
        Assert.Equal(FailedAuditId, response.Items[1].Id);
        Assert.Equal("platform.login.locked", response.Items[0].Action);
        Assert.Null(response.Items[0].IpAddress);
        Assert.Null(response.Items[0].UserAgent);
    }

    [Fact]
    public async Task GetLoginSecurityAuditLogsAsync_FiltersByActionActorAndSearch()
    {
        await using var dbContext = CreateDbContext();
        SeedAuditLogs(dbContext);

        IPlatformAuditLogRepository repository = new PlatformAuditLogRepository(dbContext);

        var actionFiltered = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.PlatformAdministration.Dtos.PlatformAuditLogListQuery
            {
                Action = "platform.login.failed"
            },
            CancellationToken.None);

        Assert.Single(actionFiltered.Items);
        Assert.Equal(FailedAuditId, actionFiltered.Items[0].Id);

        var actorFiltered = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.PlatformAdministration.Dtos.PlatformAuditLogListQuery
            {
                ActorPlatformUserId = UserOneId
            },
            CancellationToken.None);

        Assert.Equal(2, actorFiltered.Items.Count);
        Assert.All(actorFiltered.Items, item => Assert.Equal(UserOneId, item.Actor.PlatformUserId));

        var searchFiltered = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.PlatformAdministration.Dtos.PlatformAuditLogListQuery
            {
                Search = "admin@nytroz.local"
            },
            CancellationToken.None);

        Assert.Equal(2, searchFiltered.Items.Count);
    }

    [Fact]
    public async Task GetLoginSecurityAuditLogsAsync_WithUnsupportedEntityType_ReturnsEmptyResponse()
    {
        await using var dbContext = CreateDbContext();
        SeedAuditLogs(dbContext);

        IPlatformAuditLogRepository repository = new PlatformAuditLogRepository(dbContext);

        var response = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.PlatformAdministration.Dtos.PlatformAuditLogListQuery
            {
                EntityType = "tenant"
            },
            CancellationToken.None);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task GetLoginSecurityAuditLogsAsync_WithNoRows_ReturnsEmptyItems()
    {
        await using var dbContext = CreateDbContext();

        IPlatformAuditLogRepository repository = new PlatformAuditLogRepository(dbContext);

        var response = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.PlatformAdministration.Dtos.PlatformAuditLogListQuery(),
            CancellationToken.None);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
        Assert.Equal(0, response.TotalPages);
    }

    private static void SeedAuditLogs(EPosDbContext dbContext)
    {
        dbContext.PlatformUsers.AddRange(
            PlatformUser.Create(
                UserOneId,
                "admin@nytroz.local",
                "hash",
                PlatformAuthConstants.ActiveStatus,
                BaseTime),
            PlatformUser.Create(
                UserTwoId,
                "support@nytroz.local",
                "hash",
                PlatformAuthConstants.ActiveStatus,
                BaseTime));

        dbContext.PlatformLoginAudits.AddRange(
            PlatformLoginAudit.Create(
                SuccessAuditId,
                UserOneId,
                PlatformAuthConstants.SuccessLoginResult,
                BaseTime),
            PlatformLoginAudit.Create(
                FailedAuditId,
                UserOneId,
                PlatformAuthConstants.FailedLoginResult,
                BaseTime.AddMinutes(1)),
            PlatformLoginAudit.Create(
                LockedAuditId,
                UserTwoId,
                PlatformAuthConstants.LockedLoginResult,
                BaseTime.AddMinutes(2)));

        dbContext.SaveChanges();
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
