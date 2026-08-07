using E_POS.Application.Modules.Tenant.TenantAuth;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using Xunit;

namespace E_POS.UnitTests.TenantAuth;

public sealed class UserInviteMarkAcceptedTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MarkAccepted_PendingInvite_Succeeds()
    {
        var invite = CreateInvite(UserInviteConstants.StatusPending, Now.AddHours(24));
        var userId = Guid.NewGuid();

        invite.MarkAccepted(userId, Now);

        Assert.Equal(UserInviteConstants.StatusAccepted, invite.InviteStatus);
        Assert.Equal(Now, invite.AcceptedAt);
        Assert.Equal(userId, invite.AcceptedTenantUserId);
    }

    [Fact]
    public void MarkAccepted_SentInvite_Succeeds()
    {
        var invite = CreateInvite(UserInviteConstants.StatusPending, Now.AddHours(24));
        invite.MarkSent(Now);
        invite.MarkAccepted(Guid.NewGuid(), Now.AddMinutes(1));
        Assert.Equal(UserInviteConstants.StatusAccepted, invite.InviteStatus);
    }

    [Fact]
    public void MarkAccepted_Expired_Throws()
    {
        var invite = CreateInvite(UserInviteConstants.StatusSent, Now.AddMinutes(-1));
        Assert.Throws<InvalidOperationException>(() => invite.MarkAccepted(Guid.NewGuid(), Now));
    }

    [Fact]
    public void MarkAccepted_Cancelled_Throws()
    {
        var invite = CreateInvite(UserInviteConstants.StatusPending, Now.AddHours(1));
        invite.Cancel(Now);
        Assert.Throws<InvalidOperationException>(() => invite.MarkAccepted(Guid.NewGuid(), Now));
    }

    [Fact]
    public void MarkAccepted_AlreadyAccepted_Throws()
    {
        var invite = CreateInvite(UserInviteConstants.StatusPending, Now.AddHours(1));
        invite.MarkAccepted(Guid.NewGuid(), Now);
        Assert.Throws<InvalidOperationException>(() => invite.MarkAccepted(Guid.NewGuid(), Now.AddSeconds(1)));
    }

    [Fact]
    public void IsUsableAt_ReflectsLifecycle()
    {
        var invite = CreateInvite(UserInviteConstants.StatusSent, Now.AddHours(1));
        Assert.True(invite.IsUsableAt(Now));
        invite.Cancel(Now);
        Assert.False(invite.IsUsableAt(Now));
    }

    private static UserInvite CreateInvite(string status, DateTimeOffset expiresAt)
    {
        var invite = UserInvite.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "admin@example.test",
            "ADMIN@EXAMPLE.TEST",
            null,
            null,
            "hash-value",
            expiresAt,
            Now);

        if (status == UserInviteConstants.StatusSent)
        {
            invite.MarkSent(Now);
        }

        return invite;
    }
}

public sealed class TenantAdminInvitationUrlBuilderTests
{
    [Fact]
    public void Build_ProducesCanonicalPathTokenUrl()
    {
        var url = TenantAdminInvitationUrlBuilder.Build("https://admin.oneverz.com/", "abc+/=token");
        Assert.Equal("https://admin.oneverz.com/tenant-admin/setup/abc%2B%2F%3Dtoken", url);
    }

    [Fact]
    public void TryValidateBaseUrl_ProductionRejectsHttpAndLocalhost()
    {
        Assert.False(TenantAdminInvitationUrlBuilder.TryValidateBaseUrl("http://admin.oneverz.com", true, out _));
        Assert.False(TenantAdminInvitationUrlBuilder.TryValidateBaseUrl("https://localhost:4200", true, out _));
        Assert.False(TenantAdminInvitationUrlBuilder.TryValidateBaseUrl("", true, out _));
        Assert.True(TenantAdminInvitationUrlBuilder.TryValidateBaseUrl("https://admin.oneverz.com", true, out _));
    }

    [Fact]
    public void TryValidateBaseUrl_DevelopmentAllowsLocalHttp()
    {
        Assert.True(TenantAdminInvitationUrlBuilder.TryValidateBaseUrl("http://localhost:4200", false, out _));
    }
}
