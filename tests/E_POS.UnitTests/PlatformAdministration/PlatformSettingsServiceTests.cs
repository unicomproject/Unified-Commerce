using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Services;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformSettingsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid PlatformUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task GetSettingsAsync_WithViewPermission_ReturnsSettings()
    {
        var expected = CreateResponse();
        var service = CreateService(
            expected,
            hasView: true,
            hasUpdate: false);

        var result = await service.GetSettingsAsync(PlatformUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("SCS-TIX", result.Value!.PlatformDisplayName);
        Assert.Equal("LK", result.Value.DefaultCountryCode);
    }

    [Fact]
    public async Task GetSettingsAsync_WithoutViewPermission_ReturnsForbidden()
    {
        var service = CreateService(
            CreateResponse(),
            hasView: false,
            hasUpdate: true);

        var result = await service.GetSettingsAsync(PlatformUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_settings.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithUpdatePermission_ReturnsUpdatedSettings()
    {
        var expected = CreateResponse() with
        {
            PlatformDisplayName = "TM-EPOS",
            SupportEmail = "support@example.com"
        };

        var service = CreateService(
            expected,
            hasView: true,
            hasUpdate: true);

        var result = await service.UpdateSettingsAsync(
            new UpdatePlatformSettingsRequest
            {
                PlatformDisplayName = "TM-EPOS",
                SupportEmail = "support@example.com",
                DefaultCountryCode = "LK",
                DefaultCurrencyCode = "LKR",
                DefaultTimezone = "Asia/Colombo",
                DefaultLocale = "en-LK"
            },
            PlatformUserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("TM-EPOS", result.Value!.PlatformDisplayName);
        Assert.Equal("support@example.com", result.Value.SupportEmail);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithoutUpdatePermission_ReturnsForbidden()
    {
        var service = CreateService(
            CreateResponse(),
            hasView: true,
            hasUpdate: false);

        var result = await service.UpdateSettingsAsync(
            ValidUpdateRequest(),
            PlatformUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_settings.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithInvalidCountryCode_ReturnsValidationFailed()
    {
        var service = CreateService(
            CreateResponse(),
            hasView: true,
            hasUpdate: true);

        var result = await service.UpdateSettingsAsync(
            ValidUpdateRequest() with { DefaultCountryCode = "LKR" },
            PlatformUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_settings.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "defaultCountryCode");
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithMissingDisplayName_ReturnsValidationFailed()
    {
        var service = CreateService(
            CreateResponse(),
            hasView: true,
            hasUpdate: true);

        var result = await service.UpdateSettingsAsync(
            ValidUpdateRequest() with { PlatformDisplayName = "   " },
            PlatformUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_settings.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "platformDisplayName");
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithInvalidSupportEmail_ReturnsValidationFailed()
    {
        var service = CreateService(
            CreateResponse(),
            hasView: true,
            hasUpdate: true);

        var result = await service.UpdateSettingsAsync(
            ValidUpdateRequest() with { SupportEmail = "not-an-email" },
            PlatformUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_settings.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "supportEmail");
    }

    private static PlatformSettingsService CreateService(
        PlatformSettingsResponse response,
        bool hasView,
        bool hasUpdate)
    {
        return new PlatformSettingsService(
            new FakePlatformSettingsRepository(response),
            new FakePlatformPermissionChecker(hasView, hasUpdate),
            new FakeDateTimeProvider(Now));
    }

    private static PlatformSettingsResponse CreateResponse()
    {
        return new PlatformSettingsResponse
        {
            PlatformDisplayName = "SCS-TIX",
            SupportEmail = null,
            DefaultCountryCode = "LK",
            DefaultCurrencyCode = "LKR",
            DefaultTimezone = "Asia/Colombo",
            DefaultLocale = "en-LK",
            UpdatedAt = Now,
            UpdatedByPlatformUserId = PlatformUserId
        };
    }

    private static UpdatePlatformSettingsRequest ValidUpdateRequest()
    {
        return new UpdatePlatformSettingsRequest
        {
            PlatformDisplayName = "SCS-TIX",
            DefaultCountryCode = "LK",
            DefaultCurrencyCode = "LKR",
            DefaultTimezone = "Asia/Colombo",
            DefaultLocale = "en-LK"
        };
    }

    private sealed class FakePlatformSettingsRepository : IPlatformSettingsRepository
    {
        private readonly PlatformSettingsResponse _response;

        public FakePlatformSettingsRepository(PlatformSettingsResponse response)
        {
            _response = response;
        }

        public Task<PlatformSettingsResponse> GetGeneralSettingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }

        public Task<PlatformSettingsResponse> SaveGeneralSettingsAsync(
            UpdatePlatformSettingsRequest request,
            Guid updatedByPlatformUserId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly bool _hasView;
        private readonly bool _hasUpdate;

        public FakePlatformPermissionChecker(bool hasView, bool hasUpdate)
        {
            _hasView = hasView;
            _hasUpdate = hasUpdate;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            var allowed = permissionCode switch
            {
                _ when string.Equals(permissionCode, PlatformPermissionCodes.SettingsView, StringComparison.Ordinal) =>
                    _hasView,
                _ when string.Equals(permissionCode, PlatformPermissionCodes.SettingsUpdate, StringComparison.Ordinal) =>
                    _hasUpdate,
                _ => false
            };

            return Task.FromResult(allowed);
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        private readonly DateTimeOffset _now;

        public FakeDateTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public DateTimeOffset UtcNow => _now;
    }
}
