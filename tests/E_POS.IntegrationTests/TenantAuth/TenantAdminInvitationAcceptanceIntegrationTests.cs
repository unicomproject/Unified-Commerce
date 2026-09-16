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
using System.Net;
using System.Net.Http.Json;

namespace E_POS.IntegrationTests.TenantAuth;

/// <summary>
/// PostgreSQL-backed Phase 5 invitation validate/accept/concurrency/isolation tests.
/// Requires an explicitly configured, isolated local Phase B test database.
/// </summary>
public sealed class TenantAdminInvitationAcceptanceIntegrationTests
{
    private static string ConnectionString
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("PHASE_B_TEST_CONNECTION")
                ?? throw new InvalidOperationException("Set PHASE_B_TEST_CONNECTION to an isolated local oneverz_phase_b_test_* database.");
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(value);
            if (builder.Host is not ("localhost" or "127.0.0.1") ||
                builder.Database?.StartsWith("oneverz_phase_b_test_", StringComparison.Ordinal) != true)
                throw new InvalidOperationException("Phase B integration tests require an isolated local test database.");
            return value;
        }
    }

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow.AddMinutes(-2);

    [Fact]
    public async Task Http_ValidateActivateLoginAndReplay()
    {
        await CanConnectDbAsync();
        var endpoint = Environment.GetEnvironmentVariable("PHASE_B_TEST_API_BASE_URL")
            ?? throw new InvalidOperationException("Start the real API on the isolated test database and set PHASE_B_TEST_API_BASE_URL.");
        var uri = new Uri(endpoint);
        Assert.True(uri.Scheme == "http" && uri.Host is "127.0.0.1" or "localhost");
        using var client = new HttpClient { BaseAddress = uri };
        var fixture = await SeedAsync();
        try
        {
            var (_, tokens, _) = CreateService();
            var raw = tokens.GenerateToken();
            var hash = tokens.HashToken(raw);
            await using (var db = CreateDb())
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE user_invites SET invite_token_hash = {hash} WHERE id = {fixture.InviteId}");
            }
            var credentials = new TenantLoginRequest(fixture.Email, "Password1!");
            using var before = await client.PostAsJsonAsync("/api/v1/tenant-auth/login", credentials);
            Assert.Equal(HttpStatusCode.Unauthorized, before.StatusCode);

            var validated = await client.GetFromJsonAsync<ValidateTenantAdminSetupTokenResponse>(
                $"/api/tenant-admin/onboarding/setup-token/{Uri.EscapeDataString(raw)}/validate");
            Assert.True(validated?.Valid);
            Assert.Equal(fixture.Email, validated!.Email, ignoreCase: true);

            var request = new SetupTenantAdminPasswordRequest(raw, credentials.Password, credentials.Password);
            using var activation = await client.PostAsJsonAsync("/api/tenant-admin/onboarding/setup-password", request);
            Assert.Equal(HttpStatusCode.OK, activation.StatusCode);
            Assert.True((await activation.Content.ReadFromJsonAsync<SetupTenantAdminPasswordResponse>())?.Success);

            using var login = await client.PostAsJsonAsync("/api/v1/tenant-auth/login", credentials);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var session = await login.Content.ReadFromJsonAsync<TenantLoginResponse>();
            Assert.NotNull(session);
            Assert.Equal(fixture.TenantId, session.User.TenantId);
            Assert.Equal(fixture.UserId, session.User.TenantUserId);
            Assert.False(string.IsNullOrEmpty(session.AccessToken));
            // This fixture has no assigned role. Activation must not grant permissions.
            Assert.Empty(session.Permissions);

            using var replay = await client.PostAsJsonAsync("/api/tenant-admin/onboarding/setup-password", request);
            Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
            var used = await client.GetFromJsonAsync<ValidateTenantAdminSetupTokenResponse>(
                $"/api/tenant-admin/onboarding/setup-token/{Uri.EscapeDataString(raw)}/validate");
            Assert.False(used!.Valid);
            Assert.Equal("INVITE_USED", used.Code);
        }
        finally { await CleanupAsync(fixture); }
    }

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
            // Concurrent HTTP requests have independent DbContexts.
            var (secondService, _, _) = CreateService();
            var t2 = secondService.SetupPasswordAsync(request, CancellationToken.None);
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

    [Theory]
    [InlineData("expired", "INVITE_EXPIRED")]
    [InlineData("cancelled", "INVITE_CANCELLED")]
    [InlineData("revoked", "INVITE_CANCELLED")]
    public async Task UnusableInvitation_Rejected(string state, string expectedCode)
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
                var expiry = state == "expired" ? Now.AddHours(-1) : Now.AddHours(24);
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE user_invites SET invite_token_hash = {hash}, expires_at = {expiry} WHERE id = {fixture.InviteId}");
                if (state != "expired")
                {
                    var invite = await db.UserInvites.SingleAsync(x => x.Id == fixture.InviteId);
                    if (state == "revoked") invite.Revoke(Now); else invite.Cancel(Now);
                    await db.SaveChangesAsync();
                }
            }

            var validation = await service.ValidateSetupTokenAsync(raw, CancellationToken.None);
            Assert.False(validation.Valid);
            Assert.Equal(state == "expired", validation.Expired);
            Assert.Equal(expectedCode, validation.Code);

            var accept = await service.SetupPasswordAsync(
                new SetupTenantAdminPasswordRequest(raw, "Password1!", "Password1!"),
                CancellationToken.None);
            Assert.True(accept.IsFailure);
            Assert.Equal(expectedCode, accept.Error.Code);
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
        services.AddLogging();
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
        await using var db = CreateDb();
        Assert.True(await db.Database.CanConnectAsync(), "Phase B test database is unavailable; no integration verification occurred.");
        return true;
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
        // Model/repository integration only; migration-history validation is separate.
        await db.Database.EnsureCreatedAsync();
        if (!await db.Currencies.AnyAsync(x => x.CurrencyCode == "LKR"))
        {
            db.Currencies.Add(Currency.Create(Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 1, Now));
            await db.SaveChangesAsync();
        }

        var ids = FixtureIds.Create();
        var tenant = Tenant.Create(ids.TenantId, ids.TenantCode, ids.TenantSlug, ids.TenantName,
            TenantStatusConstants.Active, "LKR", "Asia/Colombo", null, null, Now);
        db.Tenants.Add(tenant);

        var user = TenantUser.CreatePendingInvite(ids.UserId, ids.TenantId, ids.Email, "Invite Admin", null, null, Now, "USR-2026-91001");
        db.TenantUsers.Add(user);

        var invite = UserInvite.CreatePending(
            ids.InviteId, ids.TenantId, ids.Email, user.Email, null, null,
            $"pending-hash-{ids.InviteId:N}", Now.AddHours(24), Now, tenantUserId: ids.UserId);
        invite.MarkSent(Now);
        db.UserInvites.Add(invite);

        await db.SaveChangesAsync();
        return ids;
    }

    private static async Task CleanupAsync(FixtureIds ids)
    {
        await using var db = CreateDb();
        await db.TenantRefreshTokens.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
        await db.TenantLoginAudits.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
        await db.TenantAuthSessions.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
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
