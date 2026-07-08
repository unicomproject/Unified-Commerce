using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
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
            new Application.Modules.Platform.PlatformAdmin.Dtos.PlatformAuditLogListQuery
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
            new Application.Modules.Platform.PlatformAdmin.Dtos.PlatformAuditLogListQuery
            {
                Action = "platform.login.failed"
            },
            CancellationToken.None);

        Assert.Single(actionFiltered.Items);
        Assert.Equal(FailedAuditId, actionFiltered.Items[0].Id);

        var actorFiltered = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.Platform.PlatformAdmin.Dtos.PlatformAuditLogListQuery
            {
                ActorPlatformUserId = UserOneId
            },
            CancellationToken.None);

        Assert.Equal(2, actorFiltered.Items.Count);
        Assert.All(actorFiltered.Items, item => Assert.Equal(UserOneId, item.Actor.PlatformUserId));

        var searchFiltered = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.Platform.PlatformAdmin.Dtos.PlatformAuditLogListQuery
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
            new Application.Modules.Platform.PlatformAdmin.Dtos.PlatformAuditLogListQuery
            {
                EntityType = "tenant"
            },
            CancellationToken.None);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task GetLoginSecurityAuditLogsAsync_SecondPage_ReturnsRemainingLatestFirstItem()
    {
        await using var dbContext = CreateDbContext();
        SeedAuditLogs(dbContext);

        IPlatformAuditLogRepository repository = new PlatformAuditLogRepository(dbContext);

        var response = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.Platform.PlatformAdmin.Dtos.PlatformAuditLogListQuery
            {
                PageNumber = 2,
                PageSize = 2
            },
            CancellationToken.None);

        Assert.Single(response.Items);
        Assert.Equal(SuccessAuditId, response.Items[0].Id);
        Assert.Equal("platform.login.success", response.Items[0].Action);
    }

    [Fact]
    public void GetLoginSecurityAuditLogsAsync_QueryOrdersByEntityColumnsBeforeProjection()
    {
        using var dbContext = CreatePostgreSqlDbContext();

        var query = BuildPaginatedAuditQuery(dbContext, new Application.Modules.Platform.PlatformAdmin.Dtos.PlatformAuditLogListQuery
        {
            PageNumber = 1,
            PageSize = 10
        });

        var sql = query.ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("created_at", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OccurredAt", sql, StringComparison.Ordinal);
    }

    private static IQueryable<LoginAuditRow> BuildPaginatedAuditQuery(
        EPosDbContext dbContext,
        Application.Modules.Platform.PlatformAdmin.Dtos.PlatformAuditLogListQuery listQuery)
    {
        var audits = dbContext.PlatformLoginAudits.AsNoTracking();
        var users = dbContext.PlatformUsers.AsNoTracking();

        return (
            from audit in audits
            join user in users on audit.PlatformUserId equals user.Id into matchedUsers
            from user in matchedUsers.DefaultIfEmpty()
            select new { audit, Email = user != null ? user.Email : null })
            .OrderByDescending(x => x.audit.CreatedAt)
            .ThenByDescending(x => x.audit.Id)
            .Skip((listQuery.PageNumber - 1) * listQuery.PageSize)
            .Take(listQuery.PageSize)
            .Select(x => new LoginAuditRow(
                x.audit.Id,
                x.audit.CreatedAt,
                x.audit.PlatformUserId,
                x.Email,
                x.audit.LoginResult));
    }

    private sealed record LoginAuditRow(
        Guid Id,
        DateTimeOffset OccurredAt,
        Guid? PlatformUserId,
        string? Email,
        string LoginResult);

    private static EPosDbContext CreatePostgreSqlDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=e_pos_audit_query_test;Username=postgres;Password=admin")
            .Options;

        return new EPosDbContext(options);
    }

    [Fact]
    public async Task GetLoginSecurityAuditLogsAsync_WithNoRows_ReturnsEmptyItems()
    {
        await using var dbContext = CreateDbContext();

        IPlatformAuditLogRepository repository = new PlatformAuditLogRepository(dbContext);

        var response = await repository.GetLoginSecurityAuditLogsAsync(
            new Application.Modules.Platform.PlatformAdmin.Dtos.PlatformAuditLogListQuery(),
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




