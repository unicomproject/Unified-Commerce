using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Repositories;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Services;
using E_POS.Application.Modules.Tenant.Inventory.Shared.Contracts;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using Moq;
using Xunit;

namespace E_POS.UnitTests.Modules.Tenant.Inventory;

public sealed class CurrentStockServiceTests
{
    private readonly Mock<ICurrentStockRepository> _mockRepository;
    private readonly Mock<IInventoryRequestValidator> _mockValidator;
    private readonly Mock<IInventoryAuditLogger> _mockAuditLogger;
    private readonly CurrentStockService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public CurrentStockServiceTests()
    {
        _mockRepository = new Mock<ICurrentStockRepository>();
        _mockValidator = new Mock<IInventoryRequestValidator>();
        _mockAuditLogger = new Mock<IInventoryAuditLogger>();
        _service = new CurrentStockService(_mockRepository.Object, _mockValidator.Object, _mockAuditLogger.Object);
    }

    [Fact]
    public async Task GetCurrentStockSummaryAsync_WithoutPermission_ReturnsFailure()
    {
        var context = new TenantRequestContext(_tenantId, _userId, ["some_other_permission"]);

        var result = await _service.GetCurrentStockSummaryAsync(context, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.permission_denied", result.Error?.Code);
    }

    [Fact]
    public async Task GetCurrentStockSummaryAsync_WithPermission_ReturnsSuccess()
    {
        var context = new TenantRequestContext(_tenantId, _userId, [StockPermissions.View]);
        var expectedSummary = new CurrentStockSummaryResponse(10, 5, 2, 5000);
        
        _mockRepository.Setup(r => r.GetCurrentStockSummaryAsync(_tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        var result = await _service.GetCurrentStockSummaryAsync(context, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedSummary, result.Value);
    }

    [Fact]
    public async Task GetCurrentStockAsync_WithValidationError_ReturnsFailure()
    {
        var context = new TenantRequestContext(_tenantId, _userId, [StockPermissions.View]);
        var query = new CurrentStockQuery();
        var validationError = new ApplicationError("validation.error", "Invalid query");
        
        _mockValidator.Setup(v => v.ValidateCurrentStockQuery(query)).Returns(validationError);

        var result = await _service.GetCurrentStockAsync(context, query, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(validationError, result.Error);
    }

    [Fact]
    public async Task GetCurrentStockAsync_WithValidQuery_ReturnsSuccess()
    {
        var context = new TenantRequestContext(_tenantId, _userId, [StockPermissions.View]);
        var query = new CurrentStockQuery();
        var expectedList = new CurrentStockListResponse([], 0, 1, 10);
        
        _mockValidator.Setup(v => v.ValidateCurrentStockQuery(query)).Returns((ApplicationError?)null);
        _mockRepository.Setup(r => r.GetCurrentStockAsync(_tenantId, query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        var result = await _service.GetCurrentStockAsync(context, query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedList, result.Value);
    }
}
