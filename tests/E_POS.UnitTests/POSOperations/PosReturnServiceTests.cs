using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosReturnServiceTests
{
    [Fact]
    public async Task CompleteReturnAsync_WithCashAndOpenTill_ForwardsNormalizedCommand()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            SupportsInspectionDrafts = true,
            InspectionDraft = CreateValidatedRefundDraft(lineId),
            CompleteResult = new PosReturnCompleteRepositoryResult(
                CreateReturnReceipt(saleId),
                null)
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateRefund
            ]);

        var result = await service.CompleteReturnAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnCompleteRequestDto(
                " damaged ",
                " cash_refund ",
                "  Box opened  ",
                [new PosReturnCreditPreviewLineRequestDto(lineId, 1)],
                1,
                "test-complete-cash-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(saleId, repository.CompleteCommand?.SaleId);
        Assert.Equal("REFUND", repository.CompleteCommand?.ResolutionType);
        Assert.Equal("DAMAGED", repository.CompleteCommand?.ReasonCode);
        Assert.Equal("CASH_REFUND", repository.CompleteCommand?.SettlementMethodCode);
        Assert.Equal("Box opened", repository.CompleteCommand?.Notes);
        Assert.Equal(1, repository.CompleteCommand?.ExpectedVersion);
        Assert.Equal("test-complete-cash-1", repository.CompleteCommand?.IdempotencyKey);
    }

    [Fact]
    public async Task CompleteReturnAsync_WithStoreCredit_ReturnsControlledUnsupportedError()
    {
        var lineId = Guid.NewGuid();
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var service = new PosReturnService(
            new FakeReturnRepository
            {
                SupportsInspectionDrafts = true,
                InspectionDraft = CreateValidatedRefundDraft(lineId),
            },
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateRefund
            ]);

        var result = await service.CompleteReturnAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnCompleteRequestDto(
                "DAMAGED",
                "STORE_CREDIT",
                null,
                [new PosReturnCreditPreviewLineRequestDto(lineId, 1)],
                1,
                "test-complete-store-credit-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.invalid_settlement_method", result.Error.Code);
        Assert.Equal(1, tillRepository.CallCount);
    }

    [Fact]
    public async Task CompleteReturnAsync_WithCreateRefundAlone_ReturnsPermissionDenied()
    {
        var service = new PosReturnService(
            new FakeReturnRepository(),
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.CreateRefund]);

        var result = await service.CompleteReturnAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnCompleteRequestDto(
                "DAMAGED",
                "CASH_REFUND",
                null,
                [new PosReturnCreditPreviewLineRequestDto(Guid.NewGuid(), 1)],
                1,
                "test-complete-permission-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetCompletionAsync_WithRefundSuccessPermissions_ReturnsReceipt()
    {
        var returnId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            CompletionResult = new PosReturnCompleteRepositoryResult(
                CreateReturnReceipt(Guid.NewGuid()) with
                {
                    Resolution = "REFUND",
                    ReturnStatus = "COMPLETED",
                    CanPrint = true
                },
                null)
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateRefund
            ]);

        var result = await service.GetCompletionAsync(
            context,
            returnId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("REFUND", result.Value!.Resolution);
        Assert.Equal(returnId, repository.CompletionReturnId);
    }

    [Fact]
    public async Task GetCompletionAsync_WithCreateReturnAlone_ReturnsPermissionDenied()
    {
        var service = new PosReturnService(
            new FakeReturnRepository
            {
                CompletionResult = new PosReturnCompleteRepositoryResult(
                    CreateReturnReceipt(Guid.NewGuid()) with { Resolution = "REFUND" },
                    null)
            },
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.CreateReturn]);

        var result = await service.GetCompletionAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task PreviewCreditAsync_WithOpenTill_NormalizesReasonAndCallsRepository()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            PreviewResult = new PosReturnCreditPreviewRepositoryResult(
                CreateCreditPreview(saleId, lineId),
                null)
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.CreateRefund]);

        var result = await service.PreviewCreditAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnCreditPreviewRequestDto(
                " damaged ",
                [new PosReturnCreditPreviewLineRequestDto(lineId, 1)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("DAMAGED", repository.PreviewReasonCode);
        Assert.Equal(saleId, repository.PreviewSaleId);
        Assert.Equal(lineId, Assert.Single(repository.PreviewLines!).SaleLineId);
    }

    [Fact]
    public async Task PreviewCreditAsync_WithDuplicateLines_ReturnsValidationError()
    {
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository();
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var service = new PosReturnService(
            repository,
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = RefundProcessContext();

        var result = await service.PreviewCreditAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnCreditPreviewRequestDto(
                "DAMAGED",
                [
                    new PosReturnCreditPreviewLineRequestDto(lineId, 1),
                    new PosReturnCreditPreviewLineRequestDto(lineId, 1)
                ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.invalid_lines", result.Error.Code);
        Assert.Equal(1, tillRepository.CallCount);
        Assert.Null(repository.PreviewLines);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_WithOpenTill_NormalizesInputAndCallsRepository()
    {
        var repository = new FakeReturnRepository();
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var service = new PosReturnService(repository, tillRepository, new FakeProductCatalogRepository(), new FakeReturnInspectionMediaStorage(), new FakeDateTimeProvider());
        var tenantId = Guid.NewGuid();
        var context = new TenantRequestContext(
            tenantId,
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.SearchOriginalSalesAsync(
            context,
            Guid.NewGuid(),
            "receipt",
            " RCP-001 ",
            null,
            null,
            null,
            null,
            null,
            0,
            500,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenantId, repository.TenantId);
        Assert.NotEqual(Guid.Empty, repository.OutletId);
        Assert.Equal("invoice", repository.SearchType);
        Assert.Equal("RCP-001", repository.Search);
        Assert.Equal(1, repository.Page);
        Assert.Equal(100, repository.PageSize);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_WithoutPermission_DoesNotResolveDevice()
    {
        var repository = new FakeReturnRepository();
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(repository, tillRepository, new FakeProductCatalogRepository(), new FakeReturnInspectionMediaStorage(), new FakeDateTimeProvider());

        var result = await service.SearchOriginalSalesAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []),
            Guid.NewGuid(),
            "recent",
            null,
            null,
            null,
            null,
            null,
            null,
            1,
            20,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_WithRefundsViewOnly_IsDenied()
    {
        var repository = new FakeReturnRepository();
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var service = new PosReturnService(
            repository,
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());

        var result = await service.SearchOriginalSalesAsync(
            new TenantRequestContext(
                Guid.NewGuid(),
                Guid.NewGuid(),
                [ReturnsPermissions.ViewRefunds]),
            Guid.NewGuid(),
            "recent",
            null,
            null,
            null,
            null,
            null,
            null,
            1,
            20,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_WithInvalidDateRange_ReturnsValidationError()
    {
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(
            new FakeReturnRepository(),
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());

        var result = await service.SearchOriginalSalesAsync(
            new TenantRequestContext(
                Guid.NewGuid(),
                Guid.NewGuid(),
                [ReturnsPermissions.ViewReturns]),
            Guid.NewGuid(),
            "recent",
            null,
            DateOnly.Parse("2026-07-31"),
            DateOnly.Parse("2026-07-01"),
            null,
            null,
            null,
            1,
            20,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.invalid_date_range", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_WithInvalidSearchType_ReturnsValidationError()
    {
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(
            new FakeReturnRepository(),
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.SearchOriginalSalesAsync(
            context,
            Guid.NewGuid(),
            "barcode",
            "123",
            null,
            null,
            null,
            null,
            null,
            1,
            20,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.invalid_search_type", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
    }

    [Fact]
    public async Task GetSaleEligibilityAsync_WithOpenTill_ReturnsRepositoryResult()
    {
        var saleId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            Eligibility = new PosReturnSaleEligibilityDto(
                saleId,
                "RCP-001",
                null,
                "Walk-in Customer",
                DateTimeOffset.UtcNow,
                "Cash",
                string.Empty,
                "LKR",
                [],
                [])
        };
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var service = new PosReturnService(
            repository,
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.GetSaleEligibilityAsync(
            context,
            saleId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(saleId, result.Value?.SaleId);
        Assert.Equal(saleId, repository.EligibilitySaleId);
    }

    [Fact]
    public async Task GetSaleEligibilityAsync_WhenSaleMissing_ReturnsNotFound()
    {
        var repository = new FakeReturnRepository();
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.GetSaleEligibilityAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.sale_not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetReturnReasonsAsync_WithOpenTill_ReturnsActiveReasons()
    {
        var tenantId = Guid.NewGuid();
        var reasonId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            ActiveReasons =
            [
                new PosReturnReasonOptionDto(
                    reasonId,
                    "DAMAGED",
                    "Damaged Item",
                    "Item arrived damaged",
                    1,
                    true,
                    true,
                    false,
                    true,
                    false)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            tenantId,
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.GetReturnReasonsAsync(
            context,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("DAMAGED", result.Value![0].Code);
        Assert.Equal(tenantId, repository.ReasonsTenantId);
    }

    [Fact]
    public async Task GetReturnReasonsAsync_WithoutDeviceId_ReturnsValidationError()
    {
        var service = new PosReturnService(
            new FakeReturnRepository(),
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.GetReturnReasonsAsync(
            context,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.invalid_device_id", result.Error.Code);
    }

    [Fact]
    public async Task GetReturnReasonsAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var repository = new FakeReturnRepository();
        var service = new PosReturnService(
            repository,
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.GetReturnReasonsAsync(
            context,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WithDuplicateSaleLineIds_ReturnsDuplicateError()
    {
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var lineId = Guid.NewGuid();
        var service = new PosReturnService(
            new FakeReturnRepository(),
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.CheckSelectedSaleEligibilityAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnEligibilityCheckRequestDto(
            [
                new PosReturnCreditPreviewLineRequestDto(lineId, 1),
                new PosReturnCreditPreviewLineRequestDto(lineId, 1)
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.duplicate_sale_line_id", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WhenLineNotReturnable_MapsConflictCode()
    {
        var saleId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            EligibilityCheckErrorCode = "line_not_returnable"
        };
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var service = new PosReturnService(
            repository,
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.CheckSelectedSaleEligibilityAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnEligibilityCheckRequestDto(
            [
                new PosReturnCreditPreviewLineRequestDto(Guid.NewGuid(), 1)
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.line_not_returnable", result.Error.Code);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WithReturnsView_ReturnsRepositoryResult()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            Eligibility = new PosReturnSaleEligibilityDto(
                saleId,
                "RCP-001",
                null,
                "Walk-in Customer",
                DateTimeOffset.UtcNow,
                "Cash",
                string.Empty,
                "LKR",
                [],
                [])
        };
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var service = new PosReturnService(
            repository,
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.CheckSelectedSaleEligibilityAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnEligibilityCheckRequestDto(
            [
                new PosReturnCreditPreviewLineRequestDto(lineId, 1)
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(saleId, result.Value?.SaleId);
        Assert.Equal(1, tillRepository.CallCount);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WithoutReturnsView_ReturnsPermissionDenied()
    {
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(
            new FakeReturnRepository(),
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());

        var result = await service.CheckSelectedSaleEligibilityAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnEligibilityCheckRequestDto(
            [
                new PosReturnCreditPreviewLineRequestDto(Guid.NewGuid(), 1)
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WithRefundsViewOnly_ReturnsPermissionDenied()
    {
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(
            new FakeReturnRepository(),
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewRefunds]);

        var result = await service.CheckSelectedSaleEligibilityAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnEligibilityCheckRequestDto(
            [
                new PosReturnCreditPreviewLineRequestDto(Guid.NewGuid(), 1)
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WithExchangesViewOnly_ReturnsPermissionDenied()
    {
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(
            new FakeReturnRepository(),
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewExchanges]);

        var result = await service.CheckSelectedSaleEligibilityAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnEligibilityCheckRequestDto(
            [
                new PosReturnCreditPreviewLineRequestDto(Guid.NewGuid(), 1)
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_DoesNotRequireCreatePermission()
    {
        var saleId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            Eligibility = new PosReturnSaleEligibilityDto(
                saleId,
                "RCP-001",
                null,
                "Walk-in Customer",
                DateTimeOffset.UtcNow,
                "Cash",
                string.Empty,
                "LKR",
                [],
                [])
        };
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var service = new PosReturnService(
            repository,
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        // View only — no returns.create
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.CheckSelectedSaleEligibilityAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnEligibilityCheckRequestDto(
            [
                new PosReturnCreditPreviewLineRequestDto(Guid.NewGuid(), 1)
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateReturnReasonsAsync_WithValidReasons_ReturnsAssignments()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var reasonId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            Eligibility = new PosReturnSaleEligibilityDto(
                saleId,
                "RCP-001",
                null,
                "Walk-in Customer",
                DateTimeOffset.UtcNow,
                "Cash",
                string.Empty,
                "LKR",
                [
                    new PosReturnSaleLineEligibilityDto(
                        lineId,
                        null,
                        "Item",
                        "SKU",
                        null,
                        1,
                        0,
                        1,
                        10,
                        10,
                        true,
                        "ELIGIBLE",
                        null)
                ],
                []),
            ActiveReasons =
            [
                new PosReturnReasonOptionDto(
                    reasonId,
                    "DAMAGED",
                    "Damaged Item",
                    null,
                    1,
                    true,
                    true,
                    false,
                    true,
                    false)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateReturnReasonsAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnReasonsValidateRequestDto(
            [
                new PosReturnReasonAssignmentRequestDto(lineId, "DAMAGED", null)
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("DAMAGED", result.Value.Items[0].ReasonCode);
        Assert.Equal(reasonId, result.Value.Items[0].ReasonId);
        Assert.Equal(1000, result.Value.NotesMaxLength);
        Assert.True(result.Value.Items[0].RequiresInspection);
        Assert.False(result.Value.Items[0].RequiresManagerApproval);
    }

    [Fact]
    public async Task ValidateReturnReasonsAsync_WithDatabaseRequiresNoteOnNonOther_ReturnsValidationError()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            Eligibility = new PosReturnSaleEligibilityDto(
                saleId,
                "RCP-001",
                null,
                "Walk-in Customer",
                DateTimeOffset.UtcNow,
                "Cash",
                string.Empty,
                "LKR",
                [
                    new PosReturnSaleLineEligibilityDto(
                        lineId,
                        null,
                        "Item",
                        "SKU",
                        null,
                        1,
                        0,
                        1,
                        10,
                        10,
                        true,
                        "ELIGIBLE",
                        null)
                ],
                []),
            ActiveReasons =
            [
                new PosReturnReasonOptionDto(
                    Guid.NewGuid(),
                    "DAMAGED",
                    "Damaged Item",
                    "Damaged packaging",
                    1,
                    true,
                    true,
                    true,
                    false,
                    true)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateReturnReasonsAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnReasonsValidateRequestDto(
            [
                new PosReturnReasonAssignmentRequestDto(lineId, "DAMAGED", "   ")
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.notes_required", result.Error.Code);
    }

    [Fact]
    public async Task ValidateReturnReasonsAsync_WithOtherConfiguredWithoutNotes_Succeeds()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var reasonId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            Eligibility = new PosReturnSaleEligibilityDto(
                saleId,
                "RCP-001",
                null,
                "Walk-in Customer",
                DateTimeOffset.UtcNow,
                "Cash",
                string.Empty,
                "LKR",
                [
                    new PosReturnSaleLineEligibilityDto(
                        lineId,
                        null,
                        "Item",
                        "SKU",
                        null,
                        1,
                        0,
                        1,
                        10,
                        10,
                        true,
                        "ELIGIBLE",
                        null)
                ],
                []),
            ActiveReasons =
            [
                new PosReturnReasonOptionDto(
                    reasonId,
                    "OTHER",
                    "Other",
                    null,
                    6,
                    true,
                    true,
                    false,
                    false,
                    false)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateReturnReasonsAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnReasonsValidateRequestDto(
            [
                new PosReturnReasonAssignmentRequestDto(lineId, "OTHER", null)
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Items[0].RequiresNotes);
    }

    [Fact]
    public async Task ValidateReturnReasonsAsync_WithPerLineReasons_ReturnsAssignments()
    {
        var saleId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var damagedId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            Eligibility = new PosReturnSaleEligibilityDto(
                saleId,
                "RCP-001",
                null,
                "Walk-in Customer",
                DateTimeOffset.UtcNow,
                "Cash",
                string.Empty,
                "LKR",
                [
                    new PosReturnSaleLineEligibilityDto(
                        lineA,
                        null,
                        "Item A",
                        "SKU-A",
                        null,
                        1,
                        0,
                        1,
                        10,
                        10,
                        true,
                        "ELIGIBLE",
                        null),
                    new PosReturnSaleLineEligibilityDto(
                        lineB,
                        null,
                        "Item B",
                        "SKU-B",
                        null,
                        1,
                        0,
                        1,
                        10,
                        10,
                        true,
                        "ELIGIBLE",
                        null)
                ],
                []),
            ActiveReasons =
            [
                new PosReturnReasonOptionDto(
                    damagedId,
                    "DAMAGED",
                    "Damaged Item",
                    "Damaged packaging",
                    1,
                    true,
                    true,
                    false,
                    true,
                    true),
                new PosReturnReasonOptionDto(
                    otherId,
                    "OTHER",
                    "Other",
                    "Provide a note",
                    2,
                    true,
                    true,
                    true,
                    false,
                    false)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateReturnReasonsAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnReasonsValidateRequestDto(
            [
                new PosReturnReasonAssignmentRequestDto(lineA, "DAMAGED", null),
                new PosReturnReasonAssignmentRequestDto(lineB, "OTHER", "Custom note")
            ],
            ApplySameReasonToAll: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal(damagedId, result.Value.Items[0].ReasonId);
        Assert.True(result.Value.Items[0].RequiresInspection);
        Assert.True(result.Value.Items[0].RequiresManagerApproval);
        Assert.Equal(otherId, result.Value.Items[1].ReasonId);
        Assert.Equal("Custom note", result.Value.Items[1].Notes);
        Assert.False(result.Value.ApplySameReasonToAll);
    }

    [Fact]
    public async Task ValidateReturnReasonsAsync_WithoutCreate_ReturnsPermissionDenied()
    {
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(
            new FakeReturnRepository(),
            tillRepository,
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.ValidateReturnReasonsAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnReasonsValidateRequestDto(
            [
                new PosReturnReasonAssignmentRequestDto(Guid.NewGuid(), "DAMAGED", null)
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
    }

    [Fact]
    public async Task ValidateReturnReasonsAsync_WithUnknownReason_ReturnsValidationError()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            Eligibility = new PosReturnSaleEligibilityDto(
                saleId,
                "RCP-001",
                null,
                "Walk-in Customer",
                DateTimeOffset.UtcNow,
                "Cash",
                string.Empty,
                "LKR",
                [
                    new PosReturnSaleLineEligibilityDto(
                        lineId,
                        null,
                        "Item",
                        "SKU",
                        null,
                        1,
                        0,
                        1,
                        10,
                        10,
                        true,
                        "ELIGIBLE",
                        null)
                ],
                []),
            ActiveReasons = []
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateReturnReasonsAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnReasonsValidateRequestDto(
            [
                new PosReturnReasonAssignmentRequestDto(lineId, "UNKNOWN", null)
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.invalid_reason_code", result.Error.Code);
    }

    [Fact]
    public async Task ValidateReturnReasonsAsync_WithNotesRequiredAndEmpty_ReturnsValidationError()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            Eligibility = new PosReturnSaleEligibilityDto(
                saleId,
                "RCP-001",
                null,
                "Walk-in Customer",
                DateTimeOffset.UtcNow,
                "Cash",
                string.Empty,
                "LKR",
                [
                    new PosReturnSaleLineEligibilityDto(
                        lineId,
                        null,
                        "Item",
                        "SKU",
                        null,
                        1,
                        0,
                        1,
                        10,
                        10,
                        true,
                        "ELIGIBLE",
                        null)
                ],
                []),
            ActiveReasons =
            [
                new PosReturnReasonOptionDto(
                    Guid.NewGuid(),
                    "OTHER",
                    "Other",
                    null,
                    6,
                    true,
                    true,
                    true,
                    false,
                    false)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateReturnReasonsAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnReasonsValidateRequestDto(
            [
                new PosReturnReasonAssignmentRequestDto(lineId, "OTHER", "  ")
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.notes_required", result.Error.Code);
    }

    [Fact]
    public async Task ValidateInspectionAsync_WithValidCondition_ReturnsCanContinue()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var conditionId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            ValidSaleLineIds = [lineId],
            InspectionConditions =
            [
                new PosReturnInspectionConditionDto(
                    conditionId,
                    "OPENED_GOOD",
                    "Opened - Good",
                    null,
                    "GOOD",
                    0,
                    true,
                    "NONE",
                    false,
                    false,
                    false)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateInspectionAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnInspectionValidateRequestDto(
            [
                new PosReturnInspectionLineRequestDto(lineId, "OPENED_GOOD", null, [])
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanContinue);
        Assert.Equal(1, result.Value.InspectedItemCount);
        Assert.Equal(0, result.Value.PendingItemCount);
        Assert.False(result.Value.RequiresReview);
        Assert.Equal(200, result.Value.NotesMaxLength);
    }

    [Fact]
    public async Task ValidateInspectionAsync_WithoutCreate_ReturnsPermissionDenied()
    {
        var service = new PosReturnService(
            new FakeReturnRepository(),
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        var result = await service.ValidateInspectionAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnInspectionValidateRequestDto(
            [
                new PosReturnInspectionLineRequestDto(Guid.NewGuid(), "OPENED_GOOD", null, [])
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task ValidateInspectionAsync_WithRefundsViewOnly_ReturnsPermissionDenied()
    {
        var service = new PosReturnService(
            new FakeReturnRepository(),
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewRefunds, ReturnsPermissions.CreateRefund]);

        var result = await service.ValidateInspectionAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnInspectionValidateRequestDto(
            [
                new PosReturnInspectionLineRequestDto(Guid.NewGuid(), "OPENED_GOOD", null, [])
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task ValidateInspectionAsync_WithUnknownCondition_CannotContinue()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            ValidSaleLineIds = [lineId],
            InspectionConditions =
            [
                new PosReturnInspectionConditionDto(
                    Guid.NewGuid(),
                    "OPENED_GOOD",
                    "Opened - Good",
                    null,
                    "GOOD",
                    0,
                    true,
                    "NONE",
                    false,
                    false,
                    false)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateInspectionAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnInspectionValidateRequestDto(
            [
                new PosReturnInspectionLineRequestDto(lineId, "UNKNOWN", null, [])
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanContinue);
        Assert.Equal(1, result.Value.PendingItemCount);
    }

    [Fact]
    public async Task ValidateInspectionAsync_WithNotesRequiredAndEmpty_CannotContinue()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            ValidSaleLineIds = [lineId],
            InspectionConditions =
            [
                new PosReturnInspectionConditionDto(
                    Guid.NewGuid(),
                    "DAMAGED",
                    "Damaged",
                    null,
                    "WARNING",
                    1,
                    false,
                    "PARTIAL",
                    true,
                    true,
                    false)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateInspectionAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnInspectionValidateRequestDto(
            [
                new PosReturnInspectionLineRequestDto(lineId, "DAMAGED", "  ", [])
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanContinue);
        Assert.Equal(1, result.Value.PendingItemCount);
    }

    [Fact]
    public async Task ValidateInspectionAsync_WithForeignSaleLine_ReturnsValidationError()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            ValidSaleLineIds = [Guid.NewGuid()],
            InspectionConditions =
            [
                new PosReturnInspectionConditionDto(
                    Guid.NewGuid(),
                    "OPENED_GOOD",
                    "Opened - Good",
                    null,
                    "GOOD",
                    0,
                    true,
                    "NONE",
                    false,
                    false,
                    false)
            ]
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.ValidateInspectionAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnInspectionValidateRequestDto(
            [
                new PosReturnInspectionLineRequestDto(lineId, "OPENED_GOOD", null, [])
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.invalid_sale_line_id", result.Error.Code);
    }

    [Fact]
    public async Task UploadInspectionMediaAsync_WithoutCreate_ReturnsPermissionDenied()
    {
        var service = new PosReturnService(
            new FakeReturnRepository(),
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns]);

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await service.UploadInspectionMediaAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            stream,
            "photo.jpg",
            "image/jpeg",
            3,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task SaveResolutionAsync_RefundOnlyUser_CannotSaveExchange()
    {
        var repository = new FakeReturnRepository
        {
            SupportsInspectionDrafts = true,
            InspectionDraft = new PosReturnInspectionDraftRecord(
                Guid.NewGuid(),
                "VALIDATED",
                DateTimeOffset.UtcNow,
                []),
        };
        var service = CreateResolutionService(repository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateRefund,
            ]);

        var result = await service.SaveResolutionAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnResolutionSaveRequestDto("EXCHANGE", 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task SaveResolutionAsync_ExchangeOnlyUser_CannotSaveRefund()
    {
        var repository = new FakeReturnRepository
        {
            SupportsInspectionDrafts = true,
            InspectionDraft = new PosReturnInspectionDraftRecord(
                Guid.NewGuid(),
                "VALIDATED",
                DateTimeOffset.UtcNow,
                []),
        };
        var service = CreateResolutionService(repository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateExchange,
            ]);

        var result = await service.SaveResolutionAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnResolutionSaveRequestDto("REFUND", 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task SaveResolutionAsync_RefundUser_SavesRefundResolution()
    {
        var saleId = Guid.NewGuid();
        var draftId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            SupportsInspectionDrafts = true,
            InspectionDraft = new PosReturnInspectionDraftRecord(
                draftId,
                "VALIDATED",
                DateTimeOffset.UtcNow,
                [],
                Version: 1,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(1)),
        };
        var service = CreateResolutionService(repository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateRefund,
            ]);

        var result = await service.SaveResolutionAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnResolutionSaveRequestDto("REFUND", 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("REFUND", result.Value!.ResolutionType);
        Assert.Equal(saleId, result.Value.SaleId);
        Assert.Equal("REFUND", repository.SavedResolutionType);
    }

    [Fact]
    public async Task SaveResolutionAsync_InvalidResolution_ReturnsValidationError()
    {
        var service = CreateResolutionService(new FakeReturnRepository());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateRefund,
            ]);

        var result = await service.SaveResolutionAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnResolutionSaveRequestDto("STORE_CREDIT", 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.invalid_resolution", result.Error.Code);
    }

    [Fact]
    public async Task SaveResolutionAsync_WithoutValidatedDraft_ReturnsWorkflowError()
    {
        var repository = new FakeReturnRepository
        {
            SupportsInspectionDrafts = true,
            InspectionDraft = new PosReturnInspectionDraftRecord(
                Guid.NewGuid(),
                "DRAFT",
                null,
                []),
            ResolutionSaveErrorCode = "inspection_not_validated",
        };
        var service = CreateResolutionService(repository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateRefund,
            ]);

        var result = await service.SaveResolutionAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnResolutionSaveRequestDto("REFUND", 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.inspection_not_validated", result.Error.Code);
    }

    [Fact]
    public async Task GetRefundMethodsAsync_ReturnsCreateOnlyUser_IsDenied()
    {
        var service = CreateResolutionService(new FakeReturnRepository());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.GetRefundMethodsAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetRefundMethodsAsync_StrictRefundUser_ReturnsBackendMethods()
    {
        var methods = new PosReturnRefundMethodsResponseDto(
            [
                new PosReturnRefundMethodOptionDto(
                    "ORIGINAL_PAYMENT",
                    "Original Payment Method",
                    true,
                    null,
                    "VISA",
                    "•••• 4242",
                    false,
                    true,
                    false),
                new PosReturnRefundMethodOptionDto(
                    "CASH",
                    "Cash",
                    true,
                    null,
                    null,
                    null,
                    true,
                    false,
                    false),
            ],
            null,
            null,
            null);
        var repository = new FakeReturnRepository
        {
            RefundMethodsResult = new PosReturnRefundMethodsRepositoryResult(methods, null),
        };
        var service = CreateResolutionService(repository);
        var context = RefundProcessContext();

        var result = await service.GetRefundMethodsAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.DoesNotContain(result.Value!.Items, x => x.Code == "STORE_CREDIT");
        Assert.Contains(result.Value!.Items, x => x.Code == "ORIGINAL_PAYMENT" && x.Enabled);
        Assert.Contains(result.Value!.Items, x => x.Code == "CASH" && x.Enabled);
    }

    [Fact]
    public async Task SaveRefundMethodAsync_StoreCredit_IsRejected()
    {
        var service = CreateResolutionService(new FakeReturnRepository());
        var context = RefundProcessContext();

        var result = await service.SaveRefundMethodAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReturnRefundMethodSaveRequestDto("STORE_CREDIT"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.invalid_refund_method", result.Error.Code);
    }

    [Fact]
    public async Task SaveRefundMethodAsync_StrictRefundUser_PersistsMethod()
    {
        var repository = new FakeReturnRepository();
        var service = CreateResolutionService(repository);
        var context = RefundProcessContext();
        var saleId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var result = await service.SaveRefundMethodAsync(
            context,
            saleId,
            deviceId,
            new PosReturnRefundMethodSaveRequestDto("CASH"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("CASH", result.Value!.MethodCode);
        Assert.Equal("CASH", repository.SavedRefundMethodCode);
    }

    private static TenantRequestContext ExchangeProcessContext() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateExchange,
            ]);

    [Fact]
    public async Task SearchExchangeProductsAsync_ReturnsCreateOnlyUser_IsDenied()
    {
        var service = CreateResolutionService(new FakeReturnRepository());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.SearchExchangeProductsAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            search: null,
            page: 1,
            pageSize: 20,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task SearchExchangeProductsAsync_StrictExchangeUser_ReturnsProducts()
    {
        var saleId = Guid.NewGuid();
        var repository = new FakeReturnRepository
        {
            ResolutionSeed = CreateResolutionRecord(saleId, "EXCHANGE"),
            SaleCurrencyCode = "LKR",
        };
        var catalog = new FakeProductCatalogRepository
        {
            Products =
            [
                new PosProductSummaryResponseDto(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Replacement Tee",
                    null,
                    null,
                    null,
                    "Apparel",
                    1500,
                    false,
                    "InStock",
                    5m),
            ],
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            catalog,
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());

        var result = await service.SearchExchangeProductsAsync(
            ExchangeProcessContext(),
            saleId,
            Guid.NewGuid(),
            search: "tee",
            page: 1,
            pageSize: 20,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Replacement Tee", result.Value.Items[0].Name);
        Assert.Equal("LKR", result.Value.CurrencyCode);
    }

    [Fact]
    public async Task PreviewExchangeAsync_ReturnsCreateOnlyUser_IsDenied()
    {
        var service = CreateResolutionService(new FakeReturnRepository());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.ViewReturns, ReturnsPermissions.CreateReturn]);

        var result = await service.PreviewExchangeAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosExchangePreviewRequestDto("DAMAGED", []),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task PreviewExchangeAsync_StrictExchangeUser_ReturnsBackendDifference()
    {
        var repository = new FakeReturnRepository
        {
            ExchangePreviewResult = new PosExchangePreviewRepositoryResult(
                new PosExchangePreviewDto(
                    Guid.NewGuid(),
                    "LKR",
                    1,
                    100m,
                    250m,
                    0m,
                    0m,
                    150m,
                    "CUSTOMER_PAYS",
                    true,
                    false,
                    [],
                    []),
                null),
        };
        var service = CreateResolutionService(repository);

        var result = await service.PreviewExchangeAsync(
            ExchangeProcessContext(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosExchangePreviewRequestDto(
                "DAMAGED",
                [new PosReturnCreditPreviewLineRequestDto(Guid.NewGuid(), 1)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("CUSTOMER_PAYS", result.Value!.DifferenceDirection);
        Assert.Equal(150m, result.Value.DifferenceAmount);
    }

    private static TenantRequestContext RefundProcessContext() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                ReturnsPermissions.ViewReturns,
                ReturnsPermissions.CreateReturn,
                ReturnsPermissions.CreateRefund,
            ]);

    private static PosReturnService CreateResolutionService(FakeReturnRepository repository) =>
        new(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeProductCatalogRepository(),
            new FakeReturnInspectionMediaStorage(),
            new FakeDateTimeProvider());

    private static PosReturnInspectionDraftRecord CreateValidatedRefundDraft(Guid saleLineId) =>
        new(
            Guid.NewGuid(),
            "VALIDATED",
            DateTimeOffset.UtcNow,
            [
                new PosReturnInspectionDraftLineRecord(
                    Guid.NewGuid(),
                    saleLineId,
                    null,
                    "GOOD",
                    null,
                    "COMPLETED",
                    Array.Empty<Guid>())
            ],
            ResolutionType: "REFUND",
            Version: 1,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1));

    private static PosReturnResolutionRecord CreateResolutionRecord(
        Guid saleId,
        string? resolutionType,
        Guid? draftId = null,
        DateTimeOffset? selectedAt = null,
        bool canChange = true) =>
        new(
            saleId,
            draftId ?? Guid.NewGuid(),
            resolutionType,
            selectedAt ?? DateTimeOffset.UtcNow,
            null,
            1,
            "VALIDATED",
            DateTimeOffset.UtcNow.AddHours(1),
            false,
            false,
            canChange);

    private static CurrentTillSessionResolveResult OpenTillResult() => new(
        true,
        null,
        new CurrentTillSessionDbSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1000m,
            "OPEN",
            DateTimeOffset.UtcNow,
            null));

    private static PosReturnCreditPreviewDto CreateCreditPreview(Guid saleId, Guid lineId) =>
        new(
            saleId,
            "RCP-001",
            null,
            "Walk-in Customer",
            string.Empty,
            DateTimeOffset.UtcNow,
            "Cash",
            string.Empty,
            "LKR",
            1000m,
            1,
            "DAMAGED",
            "Damaged",
            [
                new PosReturnCreditPreviewItemDto(
                    lineId,
                    "Product",
                    "SKU-1",
                    string.Empty,
                    null,
                    1,
                    1000m,
                    1000m)
            ],
            new PosReturnCreditCalculationDto(
                1000m,
                "Discount adjustment",
                0,
                "Tax refund",
                0,
                1000m),
            "PREVIEW-RCP-001",
            0,
            null,
            1);

    private static PosReturnReceiptDto CreateReturnReceipt(Guid saleId) =>
        new(
            saleId,
            "RRF-000001",
            "RCP-000001",
            1,
            "CASH_REFUND",
            "Cash Refund",
            "Cash",
            "Cash Refund completed",
            "LKR",
            1000m,
            0,
            DateTimeOffset.UtcNow,
            "COMPLETED",
            "Walk-in Customer",
            "Cashier",
            "Till 1",
            "NOT_REQUIRED",
            "NOT_CAPTURED");

    private sealed class FakeReturnRepository : IPosReturnRepository
    {
        public bool SupportsInspectionDrafts { get; init; }
        public PosReturnInspectionDraftRecord? InspectionDraft { get; init; }
        public string? SavedResolutionType { get; private set; }
        public string? ResolutionSaveErrorCode { get; init; }
        public PosReturnResolutionRecord? ResolutionSeed { get; init; }
        public PosReturnResolutionRecord? SavedResolution { get; private set; }

        public Task<PosReturnInspectionDraftRecord?> GetInspectionDraftBySaleAsync(
            Guid tenantId, Guid outletId, Guid saleId, CancellationToken cancellationToken) =>
            Task.FromResult(InspectionDraft);

        public Task<PosReturnResolutionSaveRepositoryResult> SaveResolutionAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            Guid userId,
            string resolutionType,
            int expectedVersion,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (ResolutionSaveErrorCode is not null)
            {
                return Task.FromResult(
                    new PosReturnResolutionSaveRepositoryResult(null, ResolutionSaveErrorCode));
            }

            if (InspectionDraft is null)
            {
                return Task.FromResult(
                    new PosReturnResolutionSaveRepositoryResult(null, "draft_not_found"));
            }

            if (!string.Equals(InspectionDraft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(
                    new PosReturnResolutionSaveRepositoryResult(null, "inspection_not_validated"));
            }

            if (InspectionDraft.Version != expectedVersion)
            {
                return Task.FromResult(
                    new PosReturnResolutionSaveRepositoryResult(null, "conflict"));
            }

            SavedResolutionType = resolutionType;
            SavedResolution = CreateResolutionRecord(
                saleId,
                resolutionType,
                InspectionDraft.DraftId,
                now,
                canChange: true);
            return Task.FromResult(
                new PosReturnResolutionSaveRepositoryResult(SavedResolution, null));
        }

        public Task<PosReturnResolutionRecord?> GetResolutionAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                ResolutionSeed is not null && ResolutionSeed.SaleId == saleId
                    ? ResolutionSeed
                    : SavedResolution is not null && SavedResolution.SaleId == saleId
                        ? SavedResolution
                        : null);

        public PosReturnRefundMethodsRepositoryResult RefundMethodsResult { get; init; } =
            new(null, "failed");
        public string? SavedRefundMethodCode { get; private set; }

        public Task<PosReturnRefundMethodsRepositoryResult> GetRefundMethodsAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            bool hasOpenTillSession,
            CancellationToken cancellationToken) =>
            Task.FromResult(RefundMethodsResult);

        public Task<PosReturnRefundMethodSaveRepositoryResult> SaveRefundMethodAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            Guid userId,
            string methodCode,
            bool hasOpenTillSession,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            SavedRefundMethodCode = methodCode;
            return Task.FromResult(
                new PosReturnRefundMethodSaveRepositoryResult(
                    new PosReturnRefundMethodRecord(saleId, methodCode, now),
                    null));
        }

        public Task<PosReturnRefundMethodRecord?> GetSavedRefundMethodAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                SavedRefundMethodCode is not null
                    ? new PosReturnRefundMethodRecord(
                        saleId,
                        SavedRefundMethodCode,
                        DateTimeOffset.UtcNow)
                    : null);

        public string? SaleCurrencyCode { get; init; } = "LKR";
        public PosExchangePreviewRepositoryResult ExchangePreviewResult { get; init; } =
            new(null, "failed");
        public PosExchangeReplacementSaveRepositoryResult ExchangeReplacementSaveResult { get; init; } =
            new(null, "failed");

        public Task<string?> GetSaleCurrencyCodeAsync(
            Guid tenantId,
            Guid saleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SaleCurrencyCode);

        public Task<PosExchangeReplacementSaveRepositoryResult> SaveExchangeReplacementAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            Guid userId,
            IReadOnlyList<PosExchangeReplacementItemRequestDto> items,
            int expectedVersion,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExchangeReplacementSaveResult);

        public Task<PosExchangeReplacementRepositoryResult> GetExchangeReplacementAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            CancellationToken cancellationToken)
        {
            if (ExchangeReplacementSaveResult.Replacement is null)
            {
                return Task.FromResult(new PosExchangeReplacementRepositoryResult(null, "replacement_not_found"));
            }

            return Task.FromResult(
                new PosExchangeReplacementRepositoryResult(
                    ExchangeReplacementSaveResult.Replacement,
                    null));
        }

        public Task<PosExchangePreviewRepositoryResult> PreviewExchangeAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            string reasonCode,
            IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExchangePreviewResult);

        public PosReturnCompleteRepositoryResult CompleteResult { get; init; } =
            new(null, "failed");
        public PosReturnCompleteCommand? CompleteCommand { get; private set; }
        public PosReturnCompleteRepositoryResult CompletionResult { get; init; } =
            new(null, "completion_not_found");
        public Guid CompletionReturnId { get; private set; }
        public PosReturnCreditPreviewRepositoryResult PreviewResult { get; init; } =
            new(null, "failed");
        public Guid PreviewSaleId { get; private set; }
        public string? PreviewReasonCode { get; private set; }
        public IReadOnlyList<PosReturnCreditPreviewLineRequestDto>? PreviewLines { get; private set; }

        public Task<PosReturnCompleteRepositoryResult> CompleteReturnAsync(
            Guid tenantId,
            Guid tenantUserId,
            PosReturnCompleteCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CompleteCommand = command;
            return Task.FromResult(CompleteResult);
        }

        public Task<PosReturnCompleteRepositoryResult> GetCompletionAsync(
            Guid tenantId,
            Guid outletId,
            Guid returnId,
            CancellationToken cancellationToken)
        {
            CompletionReturnId = returnId;
            return Task.FromResult(CompletionResult);
        }
        public PosReturnSaleEligibilityDto? Eligibility { get; init; }
        public Guid EligibilitySaleId { get; private set; }
        public int CallCount { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid OutletId { get; private set; }
        public string? SearchType { get; private set; }
        public string? Search { get; private set; }
        public int Page { get; private set; }
        public int PageSize { get; private set; }

        public Task<PosReturnCreditPreviewRepositoryResult> PreviewCreditAsync(
            Guid tenantId,
            Guid saleId,
            string reasonCode,
            IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            PreviewSaleId = saleId;
            PreviewReasonCode = reasonCode;
            PreviewLines = lines;
            return Task.FromResult(PreviewResult);
        }

        public Task<PosReturnSaleEligibilityDto?> GetSaleEligibilityAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            EligibilitySaleId = saleId;
            return Task.FromResult(Eligibility);
        }

        public string? EligibilityCheckErrorCode { get; init; }

        public Task<PosReturnSaleEligibilityCheckResult> CheckSelectedSaleEligibilityAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PosReturnSaleEligibilityCheckResult(
                EligibilityCheckErrorCode is null ? Eligibility : null,
                EligibilityCheckErrorCode));

        public Task<PosReturnSaleSearchPageDto> SearchOriginalSalesAsync(
            Guid tenantId,
            Guid outletId,
            string searchType,
            string? search,
            PosReturnSaleSearchFilterDto filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            OutletId = outletId;
            SearchType = searchType;
            Search = search;
            Page = page;
            PageSize = pageSize;
            return Task.FromResult(new PosReturnSaleSearchPageDto([], page, pageSize, 0));
        }

        public Task<bool> IsActivePaymentMethodAsync(
            Guid tenantId,
            string paymentMethodCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public IReadOnlyList<PosReturnReasonOptionDto> ActiveReasons { get; init; } = [];
        public Guid ReasonsTenantId { get; private set; }

        public Task<IReadOnlyList<PosReturnReasonOptionDto>> GetActiveReturnReasonsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            ReasonsTenantId = tenantId;
            return Task.FromResult(ActiveReasons);
        }

        public IReadOnlyList<PosReturnInspectionConditionDto> InspectionConditions { get; init; } = [];
        public HashSet<Guid>? ValidSaleLineIds { get; init; }
        public Dictionary<Guid, PosReturnInspectionMediaStagingRecord> StagedMedia { get; init; } = new();

        public Task<IReadOnlyList<PosReturnInspectionConditionDto>> GetActiveInspectionConditionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(InspectionConditions);

        public Task<bool> SaleLineBelongsToSaleAsync(
            Guid tenantId,
            Guid saleId,
            Guid saleLineId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ValidSaleLineIds is null || ValidSaleLineIds.Contains(saleLineId));

        public Task<bool> SaleBelongsToOutletAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<PosReturnInspectionDraftSaveResult> SaveInspectionDraftAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            Guid userId,
            IReadOnlyList<PosReturnInspectionDraftLineDto> lines,
            DateTimeOffset now,
            int? expectedVersion,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PosReturnInspectionDraftSaveResult(InspectionDraft, null));

        public Task<bool> MarkInspectionDraftValidatedAsync(
            Guid tenantId,
            Guid draftId,
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(InspectionDraft is not null);

        public Task<PosReturnInspectionDraftRecord?> GetValidatedInspectionDraftForCompletionAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(InspectionDraft);

        public Task<PosReturnInspectionMediaStagingResult> SaveInspectionMediaStagingAsync(
            Guid tenantId,
            Guid outletId,
            Guid tenantUserId,
            Guid saleId,
            Guid saleLineId,
            Guid mediaId,
            string storageKey,
            string fileName,
            string contentType,
            long sizeBytes,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            var record = new PosReturnInspectionMediaStagingRecord(
                mediaId,
                saleId,
                saleLineId,
                storageKey,
                fileName,
                contentType,
                sizeBytes,
                "STAGED",
                null,
                null,
                now.AddHours(24),
                outletId);
            StagedMedia[mediaId] = record;
            return Task.FromResult(new PosReturnInspectionMediaStagingResult(record, null));
        }

        public Task<PosReturnInspectionMediaStagingRecord?> GetInspectionMediaStagingAsync(
            Guid tenantId,
            Guid outletId,
            Guid mediaId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                StagedMedia.TryGetValue(mediaId, out var media) &&
                (!media.OutletId.HasValue || media.OutletId == outletId)
                    ? media
                    : null);

        public Task<IReadOnlyList<PosReturnInspectionMediaStagingRecord>> GetInspectionMediaForSaleLineAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            Guid saleLineId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PosReturnInspectionMediaStagingRecord>>(
                StagedMedia.Values
                    .Where(x => x.SaleId == saleId &&
                                x.SaleLineId == saleLineId &&
                                (!x.OutletId.HasValue || x.OutletId == outletId))
                    .ToList());

        public Task<PosReturnInspectionMediaDeleteResult> DeleteInspectionMediaStagingAsync(
            Guid tenantId,
            Guid outletId,
            Guid mediaId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (!StagedMedia.TryGetValue(mediaId, out var media) ||
                (media.OutletId.HasValue && media.OutletId != outletId))
            {
                return Task.FromResult(
                    new PosReturnInspectionMediaDeleteResult(false, null, "pos_returns.media_not_found"));
            }

            StagedMedia.Remove(mediaId);
            return Task.FromResult(new PosReturnInspectionMediaDeleteResult(true, media.StorageKey, null));
        }
    }

    private sealed class FakeProductCatalogRepository : IPosProductCatalogRepository
    {
        public Task<PosBarcodeProductRepositoryResult> GetProductByBarcodeAsync(
            Guid tenantId,
            Guid deviceId,
            string barcode,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PosBarcodeProductRepositoryResult("pos_barcode.not_found", null));
        public IReadOnlyList<PosProductSummaryResponseDto> Products { get; init; } = [];

        public Task<PosProductCatalogRepositoryResult> ListProductsAsync(
            Guid tenantId,
            Guid deviceId,
            Guid? categoryId,
            string? search,
            CancellationToken cancellationToken,
            Guid? outletId = null) =>
            Task.FromResult(new PosProductCatalogRepositoryResult(null, Products));

        public Task<PosProductCatalogCategoriesRepositoryResult> ListCategoriesAsync(
            Guid tenantId,
            Guid deviceId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PosProductCatalogCategoriesRepositoryResult(null, []));

        public Task<PosProductDetailRepositoryResult> GetProductDetailAsync(
            Guid tenantId,
            Guid deviceId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PosProductDetailRepositoryResult("not_supported", null));

        public Task<PosBarcodeProductRepositoryResult> GetProductByBarcodeAsync(
            Guid tenantId,
            Guid deviceId,
            string barcode,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PosBarcodeProductRepositoryResult("not_supported", null));
    }

    private sealed class FakeReturnInspectionMediaStorage : IReturnInspectionMediaStorage
    {
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(null);

        public Task<ReturnInspectionMediaSaveResult> SaveAsync(
            Guid tenantId,
            Guid saleId,
            Guid saleLineId,
            Guid mediaId,
            Stream content,
            string contentType,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ReturnInspectionMediaSaveResult(
                $"{tenantId}/{saleId}/{saleLineId}/{mediaId}.jpg",
                content.Length));
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } =
            new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeTillSessionRepository : IPosTillSessionRepository
    {
        public int CallCount { get; private set; }
        public CurrentTillSessionResolveResult Result { get; init; } =
            new(false, "till_session.not_found", null);

        public Task<CurrentTillSessionResolveResult> ResolveCurrentSessionAsync(
            Guid tenantId,
            Guid deviceId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(Result);
        }

        public Task<OpenTillRepositoryResult> OpenTillAsync(
            Guid tenantId,
            Guid tenantUserId,
            OpenTillCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CloseTillRepositoryResult> CloseTillAsync(
            Guid tenantId,
            Guid tenantUserId,
            CloseTillCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
