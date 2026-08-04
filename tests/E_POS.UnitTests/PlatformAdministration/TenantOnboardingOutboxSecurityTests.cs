using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class TenantOnboardingOutboxSecurityTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Lease_BlocksSecondWorkerUntilExpiry()
    {
        var message = Message();
        Assert.True(message.TryAcquire("worker-a", Now, TimeSpan.FromMinutes(1)));
        Assert.False(message.TryAcquire("worker-b", Now.AddSeconds(30), TimeSpan.FromMinutes(1)));
        Assert.True(message.TryAcquire("worker-b", Now.AddMinutes(2), TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Retry_ReleasesLeaseAndSchedulesBackoff()
    {
        var message = Message(); message.TryAcquire("worker-a", Now, TimeSpan.FromMinutes(1));
        message.MarkFailed("provider_unavailable", "Provider unavailable.", false, Now.AddMinutes(2), Now);
        Assert.Equal("FAILED_RETRYABLE", message.Status);
        Assert.Null(message.LeaseOwner);
        Assert.Equal(Now.AddMinutes(2), message.AvailableAt);
    }

    [Fact]
    public void DeliveredMessage_CannotBeClaimedAgain()
    {
        var message = Message(); message.TryAcquire("worker-a", Now, TimeSpan.FromMinutes(1)); message.MarkDelivered(Now);
        Assert.False(message.TryAcquire("worker-b", Now.AddMinutes(2), TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void TenantContact_RejectsPrimaryDuplication()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TenantContact.Create(Guid.NewGuid(), Guid.NewGuid(), "PRIMARY",
            "Primary", "primary@example.test", null, Guid.NewGuid(), Now));
    }

    [Fact]
    public void BillingContact_RequiresEmail()
    {
        Assert.Throws<ArgumentException>(() => TenantContact.Create(Guid.NewGuid(), Guid.NewGuid(), "BILLING",
            "Billing", null, "+441234567890", Guid.NewGuid(), Now));
    }

    private static IntegrationOutboxMessage Message() => IntegrationOutboxMessage.Create(Guid.NewGuid(),
        "tenant_admin.invitation_requested", "tenant_onboarding", Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), null,
        "{\"tenantId\":\"00000000-0000-0000-0000-000000000001\"}", $"invite:{Guid.NewGuid():N}", Now);
}
