using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosCartCalculationServiceTests
{
    [Fact]
    public async Task CalculateCartAsync_WithUpdateItemPermission_UsesCalculationRepository()
    {
        var repository = new FakeRepository();
        var service = new PosCheckoutService(repository, new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [SalesPermissions.Cart.UpdateItem]);

        var result = await service.CalculateCartAsync(context, CreateRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.CalculateCalled);
    }

    [Fact]
    public async Task CalculateCartAsync_WithoutUpdateItemPermission_ReturnsPermissionDenied()
    {
        var repository = new FakeRepository();
        var service = new PosCheckoutService(repository, new FakeDateTimeProvider());
        var context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Sale.Checkout]);

        var result = await service.CalculateCartAsync(context, CreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_cart.permission_denied", result.Error.Code);
        Assert.False(repository.CalculateCalled);
    }

    private static PosCheckoutSummaryRequestDto CreateRequest() =>
        new(
            Guid.NewGuid(),
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)]);

    private sealed class FakeRepository : IPosCheckoutRepository
    {
        public bool CalculateCalled { get; private set; }

        public Task<PosCheckoutCalculationResult> CalculateSummaryAsync(
            Guid tenantId,
            Guid tenantUserId,
            IReadOnlyCollection<string> permissions,
            PosCheckoutSummaryRequestDto request,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CalculateCalled = true;
            var summary = new PosCheckoutSummaryResponseDto(
                new PosCheckoutBillingSummaryDto(1, 100, 0, 0, 100, "LKR"),
                new PosCheckoutSaleDetailsDto("New Sale", 1, now, "Cashier"),
                ["cash"],
                []);
            return Task.FromResult(new PosCheckoutCalculationResult(null, summary));
        }

        public Task<PosCheckoutStartPaymentResult> StartPaymentAsync(
            Guid tenantId,
            Guid tenantUserId,
            IReadOnlyCollection<string> permissions,
            PosCheckoutStartPaymentRequestDto request,
            DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 12, 0, 0, 0, TimeSpan.Zero);
    }
}
