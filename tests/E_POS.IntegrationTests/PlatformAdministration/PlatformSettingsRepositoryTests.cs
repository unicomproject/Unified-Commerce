using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformSettingsRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid PlatformUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task GetGeneralSettingsAsync_ReturnsSeededDefaults()
    {
        await using var dbContext = CreateDbContext();
        SeedDefaults(dbContext);

        IPlatformSettingsRepository repository = new PlatformSettingsRepository(dbContext);

        var settings = await repository.GetGeneralSettingsAsync(CancellationToken.None);

        Assert.Equal("SCS-TIX", settings.PlatformDisplayName);
        Assert.Equal("LK", settings.DefaultCountryCode);
        Assert.Equal("LKR", settings.DefaultCurrencyCode);
        Assert.Equal("Asia/Colombo", settings.DefaultTimezone);
        Assert.Equal("en-LK", settings.DefaultLocale);
    }

    [Fact]
    public async Task SaveGeneralSettingsAsync_UpdatesExistingValues()
    {
        await using var dbContext = CreateDbContext();
        SeedDefaults(dbContext);

        IPlatformSettingsRepository repository = new PlatformSettingsRepository(dbContext);

        var updated = await repository.SaveGeneralSettingsAsync(
            new UpdatePlatformSettingsRequest
            {
                PlatformDisplayName = "TM-EPOS",
                SupportEmail = "support@example.com",
                DefaultCountryCode = "lk",
                DefaultCurrencyCode = "lkr",
                DefaultTimezone = "Asia/Colombo",
                DefaultLocale = "en-LK"
            },
            PlatformUserId,
            Now.AddMinutes(5),
            CancellationToken.None);

        Assert.Equal("TM-EPOS", updated.PlatformDisplayName);
        Assert.Equal("support@example.com", updated.SupportEmail);
        Assert.Equal("LK", updated.DefaultCountryCode);
        Assert.Equal("LKR", updated.DefaultCurrencyCode);
        Assert.Equal(PlatformUserId, updated.UpdatedByPlatformUserId);

        var persisted = await repository.GetGeneralSettingsAsync(CancellationToken.None);
        Assert.Equal("TM-EPOS", persisted.PlatformDisplayName);
    }

    [Fact]
    public async Task SaveGeneralSettingsAsync_CreatesMissingKeys()
    {
        await using var dbContext = CreateDbContext();

        IPlatformSettingsRepository repository = new PlatformSettingsRepository(dbContext);

        var saved = await repository.SaveGeneralSettingsAsync(
            new UpdatePlatformSettingsRequest
            {
                PlatformDisplayName = "SCS-TIX",
                DefaultCountryCode = "LK"
            },
            PlatformUserId,
            Now,
            CancellationToken.None);

        Assert.Equal("SCS-TIX", saved.PlatformDisplayName);
        Assert.Equal("LK", saved.DefaultCountryCode);

        var count = await dbContext.PlatformSettings.CountAsync();
        Assert.Equal(PlatformSettingKeys.GeneralSettings.Count, count);
    }

    private static void SeedDefaults(EPosDbContext dbContext)
    {
        dbContext.PlatformSettings.AddRange(
            PlatformSetting.Create(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                PlatformSettingKeys.PlatformDisplayName,
                "SCS-TIX",
                isSecret: false,
                "Platform display name",
                Now),
            PlatformSetting.Create(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                PlatformSettingKeys.DefaultCountryCode,
                "LK",
                isSecret: false,
                "Default country code",
                Now),
            PlatformSetting.Create(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                PlatformSettingKeys.DefaultCurrencyCode,
                "LKR",
                isSecret: false,
                "Default currency code",
                Now),
            PlatformSetting.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                PlatformSettingKeys.DefaultTimezone,
                "Asia/Colombo",
                isSecret: false,
                "Default timezone",
                Now),
            PlatformSetting.Create(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                PlatformSettingKeys.DefaultLocale,
                "en-LK",
                isSecret: false,
                "Default locale",
                Now));

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



