using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformAuditLogServiceTests
{
    private static readonly Guid PlatformUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAuditLogsAsync_WithPermission_ReturnsPaginatedResponse()
    {
        var service = CreateService(
            hasAuditView: true,
            new PlatformAuditLogListResponse(
                "platform_login_security",
                "Platform login and authentication security events from platform_login_audits. Generic business audit logs are not available in Release 1.",
                [
                    new PlatformAuditLogListItemDto(
                        Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Now,
                        new PlatformAuditLogActorDto(PlatformUserId, "admin@nytroz.local"),
                        "platform.login.success",
                        "platform_auth",
                        "platform_user",
                        PlatformUserId,
                        "Platform login succeeded.",
                        null,
                        null)
                ],
                1,
                20,
                1,
                1));

        var result = await service.GetAuditLogsAsync(new PlatformAuditLogListQuery(), PlatformUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("platform_login_security", result.Value!.AuditScope);
        Assert.Single(result.Value.Items);
        Assert.Equal("platform.login.success", result.Value.Items[0].Action);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithoutPermission_ReturnsForbidden()
    {
        var service = CreateService(hasAuditView: false, EmptyResponse());

        var result = await service.GetAuditLogsAsync(new PlatformAuditLogListQuery(), PlatformUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_audit.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithInvalidDateRange_ReturnsValidationFailed()
    {
        var service = CreateService(hasAuditView: true, EmptyResponse());

        var result = await service.GetAuditLogsAsync(
            new PlatformAuditLogListQuery
            {
                From = Now,
                To = Now.AddDays(-1)
            },
            PlatformUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_audit.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithEmptyRepository_ReturnsEmptyItems()
    {
        var service = CreateService(hasAuditView: true, EmptyResponse());

        var result = await service.GetAuditLogsAsync(new PlatformAuditLogListQuery(), PlatformUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    private static PlatformAuditLogService CreateService(
        bool hasAuditView,
        PlatformAuditLogListResponse response)
    {
        return new PlatformAuditLogService(
            new FakePlatformAuditLogRepository(response),
            new FakePlatformPermissionChecker(hasAuditView));
    }

    private static PlatformAuditLogListResponse EmptyResponse()
    {
        return new PlatformAuditLogListResponse(
            "platform_login_security",
            "Platform login and authentication security events from platform_login_audits. Generic business audit logs are not available in Release 1.",
            [],
            1,
            20,
            0,
            0);
    }

    private sealed class FakePlatformAuditLogRepository : IPlatformAuditLogRepository
    {
        private readonly PlatformAuditLogListResponse _response;

        public FakePlatformAuditLogRepository(PlatformAuditLogListResponse response)
        {
            _response = response;
        }

        public Task<PlatformAuditLogListResponse> GetLoginSecurityAuditLogsAsync(
            PlatformAuditLogListQuery query,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly bool _hasAuditView;

        public FakePlatformPermissionChecker(bool hasAuditView)
        {
            _hasAuditView = hasAuditView;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                _hasAuditView &&
                string.Equals(permissionCode, PlatformPermissionCodes.AuditView, StringComparison.Ordinal));
        }
    }
}


