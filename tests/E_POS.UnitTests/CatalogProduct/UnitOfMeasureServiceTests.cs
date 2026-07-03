using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Application.Modules.CatalogProduct.Services;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class UnitOfMeasureServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListAsync_WithoutViewPermission_ReturnsPermissionDenied()
    {
        var service = new UnitOfMeasureService(new FakeUnitOfMeasureRepository());

        var result = await service.ListAsync(CreateContext([]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("unit_of_measure.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task ListAsync_WithViewPermission_ReturnsGlobalAndTenantUnits()
    {
        var repository = new FakeUnitOfMeasureRepository();
        var service = new UnitOfMeasureService(repository);

        var result = await service.ListAsync(CreateContext([UnitOfMeasureConstants.ViewPermission]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Contains(result.Value.Items, x => x.UomCode == "PCS" && x.IsGlobal);
        Assert.Contains(result.Value.Items, x => x.UomCode == "BOTTLE" && !x.IsGlobal);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions)
    {
        return new TenantRequestContext(TenantId, UserId, permissions);
    }

    private sealed class FakeUnitOfMeasureRepository : IUnitOfMeasureRepository
    {
        public Guid TenantId { get; private set; }

        public Task<IReadOnlyList<UnitOfMeasureResponse>> ListAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            IReadOnlyList<UnitOfMeasureResponse> response =
            [
                new UnitOfMeasureResponse(Guid.NewGuid(), null, "PCS", "Pieces", null, true, Now, Now),
                new UnitOfMeasureResponse(Guid.NewGuid(), tenantId, "BOTTLE", "Bottle", null, false, Now, Now)
            ];

            return Task.FromResult(response);
        }
    }
}