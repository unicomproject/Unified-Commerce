using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Repositories;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Services;
using E_POS.Application.Modules.Tenant.Inventory.Shared.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using Moq;
using Xunit;

namespace E_POS.UnitTests.Modules.Tenant.Inventory;

public sealed class StockInServiceTests
{
    private readonly Mock<ICurrentStockRepository> _mockRepository;
    private readonly Mock<IInventoryRequestValidator> _mockValidator;
    private readonly Mock<IInventoryAuditLogger> _mockAuditLogger;
    private readonly CurrentStockService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public StockInServiceTests()
    {
        _mockRepository = new Mock<ICurrentStockRepository>();
        _mockValidator = new Mock<IInventoryRequestValidator>();
        _mockAuditLogger = new Mock<IInventoryAuditLogger>();
        _service = new CurrentStockService(_mockRepository.Object, _mockValidator.Object, _mockAuditLogger.Object);
    }

    [Fact]
    public async Task StockInAsync_WithoutPermission_ReturnsFailure()
    {
        var context = new TenantRequestContext(_tenantId, _userId, ["some_other_permission"]);

        var result = await _service.StockInAsync(context, new StockInRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.permission_denied", result.Error?.Code);
    }

    [Fact]
    public async Task StockInAsync_WithValidationError_ReturnsFailure()
    {
        var context = new TenantRequestContext(_tenantId, _userId, [StockPermissions.StockIn]);
        var request = new StockInRequest();
        var validationError = new ApplicationError("validation.error", "Invalid request");
        
        _mockValidator.Setup(v => v.ValidateStockIn(request)).Returns(validationError);

        var result = await _service.StockInAsync(context, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(validationError, result.Error);
    }

    [Fact]
    public async Task StockInAsync_WithInvalidOutlet_ReturnsFailure()
    {
        var context = new TenantRequestContext(_tenantId, _userId, [StockPermissions.StockIn]);
        var request = new StockInRequest { OutletId = Guid.NewGuid() };
        
        _mockValidator.Setup(v => v.ValidateStockIn(request)).Returns((ApplicationError?)null);
        _mockRepository.Setup(r => r.OutletExistsAsync(_tenantId, request.OutletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.StockInAsync(context, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.outlet_not_found", result.Error?.Code);
    }

    [Fact]
    public async Task StockInAsync_WithDuplicateIdempotencyKey_ReturnsFailure()
    {
        var context = new TenantRequestContext(_tenantId, _userId, [StockPermissions.StockIn]);
        var request = new StockInRequest { OutletId = Guid.NewGuid(), IdempotencyKey = "key-123" };
        
        _mockValidator.Setup(v => v.ValidateStockIn(request)).Returns((ApplicationError?)null);
        _mockRepository.Setup(r => r.OutletExistsAsync(_tenantId, request.OutletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.IdempotencyKeyExistsAsync(_tenantId, request.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.StockInAsync(context, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.duplicate_request", result.Error?.Code);
    }

    [Fact]
    public async Task StockInAsync_WithValidRequest_ReturnsSuccessAndLogsAudit()
    {
        var context = new TenantRequestContext(_tenantId, _userId, [StockPermissions.StockIn]);
        var request = new StockInRequest { OutletId = Guid.NewGuid(), IdempotencyKey = "key-123" };
        var expectedResponse = new StockInResponse(Guid.NewGuid(), request.OutletId, "StockIn", null, [], DateTimeOffset.UtcNow);
        
        _mockValidator.Setup(v => v.ValidateStockIn(request)).Returns((ApplicationError?)null);
        _mockRepository.Setup(r => r.OutletExistsAsync(_tenantId, request.OutletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.IdempotencyKeyExistsAsync(_tenantId, request.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.StockInAsync(_tenantId, _userId, request, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _service.StockInAsync(context, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResponse, result.Value);
        _mockAuditLogger.Verify(a => a.LogStockInCompleted(_tenantId, _userId, expectedResponse.StockMovementId, expectedResponse.OutletId), Times.Once);
    }
}
