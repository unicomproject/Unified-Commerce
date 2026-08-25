using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Application.Modules.Tenant.HardwareCash.Services;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using Moq;
using Xunit;

namespace E_POS.UnitTests.HardwareCash;

public sealed class PosDrawerFinalizeStatusTests
{
    [Fact]
    public async Task Finalize_RejectsInvalidStatus_WithTypedError()
    {
        var service = CreateService();
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(), [CashDrawerPermissions.Manage]);

        var result = await service.FinalizeOperationAsync(
            context,
            Guid.NewGuid(),
            new FinalizeDrawerOperationRequest("PRINTED", "SUCCESS", null, true, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_drawer.invalid_status", result.Error.Code);
    }

    [Fact]
    public async Task Finalize_AcceptsAgentAcceptedStatus()
    {
        var opId = Guid.NewGuid();
        var dto = new CashDrawerOperationDto(
            opId, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(),
            "hardwareTest", "test", null, null, null, 1, "drawerPin2", 100, 200,
            "AGENT_ACCEPTED", "SUCCESS", null, true, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var repo = new Mock<IPosDrawerRepository>();
        repo.Setup(x => x.FinalizeOperationAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), opId,
                It.IsAny<FinalizeDrawerOperationRequest>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, dto));

        var service = CreateService(repo.Object);
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(), [CashDrawerPermissions.Manage]);

        var result = await service.FinalizeOperationAsync(
            context,
            opId,
            new FinalizeDrawerOperationRequest("AGENT_ACCEPTED", "SUCCESS", null, true, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("AGENT_ACCEPTED", result.Value!.Status);
    }

    private static PosDrawerService CreateService(IPosDrawerRepository? repository = null)
    {
        var repo = repository ?? Mock.Of<IPosDrawerRepository>();
        var auth = Mock.Of<ITenantAuthRepository>();
        var hash = Mock.Of<IPasswordHashService>();
        var clock = Mock.Of<IDateTimeProvider>(c => c.UtcNow == DateTimeOffset.UtcNow);
        var tills = Mock.Of<IPosTillSessionRepository>();
        return new PosDrawerService(repo, auth, hash, clock, tills);
    }
}
