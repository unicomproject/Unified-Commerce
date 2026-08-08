using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using E_POS.Application.Modules.Tenant.TenantAuth.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Common;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.IntegrationTests.TenantAuth;

/// <summary>
/// PostgreSQL-backed Phase 5 invitation validate/accept/concurrency/isolation tests.
/// Skips silently when the local Flow4 evidence database is unavailable.
/// </summary>
public sealed class TenantAdminInvitationAcceptanceIntegrationTests
{
    private const string ConnectionString =
        "Host=127.0.0.1;Port=55436;Database=oneverz_flow4_e2e_evidence;Username=postgres;Password=postgres";

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow.AddMinutes(-2);

    [Fact]
    public async Task ValidateAndAccept_ThenReplay_AndLoginReady()
    {
        if (!await CanConnectDbAsync()) return;

        var fixture = await SeedAsync();
        try
        {
            var (service, tokens, hasher) = CreateService();
            var raw = tokens.GenerateToken();
            var hash = tokens.HashToken(raw);

            await using (var db = CreateDb())
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE user_invites SET invite_token_hash = {hash} WHERE id = {fixture.InviteId}");
            }

            var validation = await service.ValidateSetupTokenAsync(raw, CancellationToken.None);
            Assert.True(validation.Valid);
            Assert.Equal(fixture.Email, validation.Email, ignoreCase: true);

            var accept = await service.SetupPasswordAsync(
                new SetupTenantAdminPasswordRequest(raw, "Password1!", "Password1!"),
                CancellationToken.None);
            Assert.True(accept.IsSuccess);

            await using (var db = CreateDb())
            {
                var invite = await db.UserInvites.SingleAsync(x => x.Id == fixture.InviteId);
                Assert.Equal(UserInviteConstants.StatusAccepted, invite.InviteStatus);
                Assert.NotNull(invite.AcceptedAt);
                Assert.Equal(fixture.UserId, invite.AcceptedTenantUserId);

                var user = await db.TenantUsers.SingleAsync(x => x.Id == fixture.UserId);
                Assert.Equal(TenantUserConstants.StatusActive, user.AccountStatus);
                Assert.True(hasher.VerifyPassword("Password1!", user.EncryptedPassword));
            }

            var replay = await service.SetupPasswordAsync(
                new SetupTenantAdminPasswordRequest(raw, "Password1!", "Password1!"),
                CancellationToken.None);
            Assert.True(replay.IsFailure);
            Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteUsed, replay.Error.Code);
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task ConcurrentAccept_ExactlyOneSucceeds()
    {
        if (!await CanConnectDbAsync()) return;

        var fixture = await SeedAsync();
        try
        {
            var (service, tokens, _) = CreateService();
            var raw = tokens.GenerateToken();
            var hash = tokens.HashToken(raw);
            await using (var db = CreateDb())
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE user_invites SET invite_token_hash = {hash} WHERE id = {fixture.InviteId}");
            }

            var request = new SetupTenantAdminPasswordRequest(raw, "Password1!", "Password1!");
            var t1 = service.SetupPasswordAsync(request, CancellationToken.None);
            var t2 = service.SetupPasswordAsync(request, CancellationToken.None);
            var results = await Task.WhenAll(t1, t2);

            var successCount = results.Count(x => x.IsSuccess);
            var failureCount = results.Count(x => x.IsFailure);
            Assert.Equal(1, successCount);
            Assert.Equal(1, failureCount);

            await using (var db = CreateDb())
            {
                var invite = await db.UserInvites.SingleAsync(x => x.Id == fixture.InviteId);
                Assert.Equal(UserInviteConstants.StatusAccepted, invite.InviteStatus);
                var user = await db.TenantUsers.SingleAsync(x => x.Id == fixture.UserId);
                Assert.Equal(TenantUserConstants.StatusActive, user.AccountStatus);
            }
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task TenantIsolation_TokenCannotActivateOtherTenantUser()
    {
        if (!await CanConnectDbAsync()) return;

        var a = await SeedAsync();
        var b = await SeedAsync();
        try
        {
            var (service, tokens, _) = CreateService();
            var rawA = tokens.GenerateToken();
            var hashA = tokens.HashToken(rawA);
            await using (var db = CreateDb())
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE user_invites SET invite_token_hash = {hashA} WHERE id = {a.InviteId}");
            }

            var accept = await service.SetupPasswordAsync(
                new SetupTenantAdminPasswordRequest(rawA, "Password1!", "Password1!"),
                CancellationToken.None);
            Assert.True(accept.IsSuccess);

            await using (var db = CreateDb())
            {
                var userB = await db.TenantUsers.SingleAsync(x => x.Id == b.UserId);
                Assert.Equal(TenantUserConstants.StatusInvited, userB.AccountStatus);
                var inviteB = await db.UserInvites.SingleAsync(x => x.Id == b.InviteId);
                Assert.NotEqual(UserInviteConstants.StatusAccepted, inviteB.InviteStatus);
            }
        }
        finally
        {
            await CleanupAsync(a);
            await CleanupAsync(b);
        }
    }

    [Fact]
    public async Task ExpiredAndCancelled_Rejected()
    {
        if (!await CanConnectDbAsync()) return;

        var fixture = await SeedAsync();
        try
        {
            var (service, tokens, _) = CreateService();
            var raw = tokens.GenerateToken();
            var hash = tokens.HashToken(raw);
            await using (var db = CreateDb())
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE user_invites SET invite_token_hash = {hash}, expires_at = {Now.AddHours(-1)} WHERE id = {fixture.InviteId}");
            }

            var validation = await service.ValidateSetupTokenAsync(raw, CancellationToken.None);
            Assert.False(validation.Valid);
            Assert.True(validation.Expired);

            var accept = await service.SetupPasswordAsync(
                new SetupTenantAdminPasswordRequest(raw, "Password1!", "Password1!"),
                CancellationToken.None);
            Assert.True(accept.IsFailure);
            Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteExpired, accept.Error.Code);
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    private static (TenantAdminInvitationAcceptanceService Service, IInvitationTokenService Tokens, IPasswordHashService Hasher)
        CreateService()
    {
        var services = new ServiceCollection();
        services.AddDbContext<EPosDbContext>(opt => opt.UseNpgsql(ConnectionString));
        services.AddScoped<ITenantAdminInvitationAcceptanceRepository, TenantAdminInvitationAcceptanceRepository>();
        services.AddSingleton<ITokenHashService, TokenHashService>();
        services.AddSingleton(Options.Create(new TenantJwtOptions { SigningKey = "012345678901234567890123456789012" }));
        services.AddSingleton<IInvitationTokenService, InvitationTokenService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IPlatformPasswordPolicyValidator, PlatformPasswordPolicyValidator>();
        services.AddScoped<TenantAdminInvitationAcceptanceService>();
        var provider = services.BuildServiceProvider();
        return (
            provider.GetRequiredService<TenantAdminInvitationAcceptanceService>(),
            provider.GetRequiredService<IInvitationTokenService>(),
            provider.GetRequiredService<IPasswordHashService>());
    }

    private static async Task<bool> CanConnectDbAsync()
    {
        try
        {
            await using var db = CreateDb();
            return await db.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    private static EPosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new EPosDbContext(options);
    }

    private static async Task<FixtureIds> SeedAsync()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();

        var ids = FixtureIds.Create();
        var tenant = Tenant.Create(ids.TenantId, ids.TenantCode, ids.TenantSlug, ids.TenantName,
            TenantStatusConstants.Active, "LKR", "Asia/Colombo", null, null, Now);
        db.Tenants.Add(tenant);

        var user = TenantUser.CreatePendingInvite(ids.UserId, ids.TenantId, ids.Email, "Invite Admin", null, null, Now);
        db.TenantUsers.Add(user);

        var invite = UserInvite.CreatePending(
            ids.InviteId, ids.TenantId, ids.Email, user.Email, null, null,
            $"pending-hash-{ids.InviteId:N}", Now.AddHours(24), Now);
        invite.MarkSent(Now);
        db.UserInvites.Add(invite);

        await db.SaveChangesAsync();
        return ids;
    }

    private static async Task CleanupAsync(FixtureIds ids)
    {
        await using var db = CreateDb();
        db.UserInvites.RemoveRange(db.UserInvites.Where(x => x.TenantId == ids.TenantId));
        db.TenantUsers.RemoveRange(db.TenantUsers.Where(x => x.TenantId == ids.TenantId));
        db.Tenants.RemoveRange(db.Tenants.Where(x => x.Id == ids.TenantId));
        await db.SaveChangesAsync();
    }

    private sealed record FixtureIds(
        Guid TenantId, string TenantCode, string TenantSlug, string TenantName,
        Guid UserId, Guid InviteId, string Email)
    {
        public static FixtureIds Create()
        {
            var id = Guid.NewGuid().ToString("N")[..8];
            return new FixtureIds(
                Guid.NewGuid(), $"P5-{id}", $"p5-{id}", $"Phase5 Tenant {id}",
                Guid.NewGuid(), Guid.NewGuid(), $"admin-{id}@phase5.test");
        }
    }
}
