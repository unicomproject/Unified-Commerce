using E_POS.Application.Modules.Platform.PlatformAdmin;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformAuditLogMapperTests
{
    [Theory]
    [InlineData("SUCCESS", "platform.login.success", "Platform login succeeded.")]
    [InlineData("FAILED", "platform.login.failed", "Platform login failed.")]
    [InlineData("LOCKED", "platform.login.locked", "Platform login blocked because the account is locked.")]
    public void MapLoginAudit_MapsKnownLoginStatuses(string loginStatus, string action, string summary)
    {
        var item = PlatformAuditLogMapper.MapLoginAudit(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new DateTimeOffset(2026, 7, 3, 12, 0, 0, TimeSpan.Zero),
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "admin@nytroz.local",
            loginStatus);

        Assert.Equal(action, item.Action);
        Assert.Equal(summary, item.Summary);
        Assert.Equal(PlatformAuditLogMapper.Area, item.Area);
        Assert.Equal(PlatformAuditLogMapper.EntityType, item.EntityType);
        Assert.Null(item.IpAddress);
        Assert.Null(item.UserAgent);
    }

    [Theory]
    [InlineData("platform.login.success", "SUCCESS")]
    [InlineData("FAILED", "FAILED")]
    public void ResolveLoginResultFilter_AcceptsActionCodesAndRawResults(string action, string expected)
    {
        var resolved = PlatformAuditLogMapper.ResolveLoginResultFilter(action);
        Assert.Equal(expected, resolved);
    }

}


