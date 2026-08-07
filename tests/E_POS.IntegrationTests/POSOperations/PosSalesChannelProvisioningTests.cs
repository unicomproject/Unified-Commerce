using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.POSOperations;

public sealed class PosSalesChannelProvisioningTests
{
    [Fact]
    public async Task EnsurePosSalesChannelAsync_CreatesThenReusesCanonicalTenantChannel()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-08-06T10:00:00Z");
        db.PlatformSalesChannels.Add(PlatformSalesChannel.Create(
            PlatformSalesChannelSeedConstants.PosChannelId,
            PlatformSalesChannelSeedConstants.PosChannelCode,
            PlatformSalesChannelSeedConstants.PosChannelName,
            PlatformSalesChannelSeedConstants.PosChannelType,
            now));
        await db.SaveChangesAsync();
        var repository = CreateRepository(db);

        var first = await repository.EnsurePosSalesChannelAsync(tenantId, now, default);
        var second = await repository.EnsurePosSalesChannelAsync(tenantId, now.AddMinutes(1), default);

        Assert.Equal(first, second);
        var channels = await db.SalesChannels.Where(x => x.TenantId == tenantId).ToListAsync();
        var channel = Assert.Single(channels);
        Assert.Equal(PlatformSalesChannelSeedConstants.PosChannelId, channel.PlatformSalesChannelId);
        Assert.Equal("ACTIVE", channel.Status);
    }

    [Fact]
    public async Task EnsurePosSalesChannelAsync_ReusesExistingActiveTenantChannel()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var existingId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-08-06T10:00:00Z");
        db.PlatformSalesChannels.Add(PlatformSalesChannel.Create(
            PlatformSalesChannelSeedConstants.PosChannelId, "POS", "Point of Sale", "POS", now));
        db.SalesChannels.Add(SalesChannel.Create(
            existingId, tenantId, PlatformSalesChannelSeedConstants.PosChannelId,
            "Main POS", "ACTIVE", 0, now));
        await db.SaveChangesAsync();

        var result = await CreateRepository(db)
            .EnsurePosSalesChannelAsync(tenantId, now, default);

        Assert.Equal(existingId, result);
        Assert.Equal(1, await db.SalesChannels.CountAsync(x => x.TenantId == tenantId));
    }

    [Fact]
    public async Task EnsurePosSalesChannelAsync_WhenCanonicalGlobalChannelMissing_ThrowsWithoutPartialTenantData()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<MissingSystemPosSalesChannelException>(() =>
            CreateRepository(db).EnsurePosSalesChannelAsync(
                tenantId, DateTimeOffset.UtcNow, default));

        Assert.Equal(tenantId, exception.TenantId);
        Assert.Empty(await db.SalesChannels.Where(x => x.TenantId == tenantId).ToListAsync());
    }

    private static PosHoldRepository CreateRepository(EPosDbContext db) =>
        new(db, null!, null!);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase($"pos-channel-{Guid.NewGuid()}")
            .Options;
        return new EPosDbContext(options);
    }
}
