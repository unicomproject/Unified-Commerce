using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
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
            CompleteResult = new PosReturnCompleteRepositoryResult(
                CreateReturnReceipt(saleId),
                null)
        };
        var service = new PosReturnService(
            repository,
            new FakeTillSessionRepository { Result = OpenTillResult() },
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.CreateRefund]);

        var result = await service.CompleteReturnAsync(
            context,
            saleId,
            Guid.NewGuid(),
            new PosReturnCompleteRequestDto(
                " damaged ",
                " cash_refund ",
                "  Box opened  ",
                [new PosReturnCreditPreviewLineRequestDto(lineId, 1)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(saleId, repository.CompleteCommand?.SaleId);
        Assert.Equal("DAMAGED", repository.CompleteCommand?.ReasonCode);
        Assert.Equal("CASH_REFUND", repository.CompleteCommand?.SettlementMethodCode);
        Assert.Equal("Box opened", repository.CompleteCommand?.Notes);
    }

    [Fact]
    public async Task CompleteReturnAsync_WithStoreCredit_ReturnsControlledUnsupportedError()
    {
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(
            new FakeReturnRepository(),
            tillRepository,
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
                "STORE_CREDIT",
                null,
                [new PosReturnCreditPreviewLineRequestDto(Guid.NewGuid(), 1)]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_returns.settlement_not_supported", result.Error.Code);
        Assert.Equal(0, tillRepository.CallCount);
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
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(
            repository,
            tillRepository,
            new FakeDateTimeProvider());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [ReturnsPermissions.CreateRefund]);

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
        Assert.Equal(0, tillRepository.CallCount);
        Assert.Null(repository.PreviewLines);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_WithOpenTill_NormalizesInputAndCallsRepository()
    {
        var repository = new FakeReturnRepository();
        var tillRepository = new FakeTillSessionRepository { Result = OpenTillResult() };
        var service = new PosReturnService(repository, tillRepository, new FakeDateTimeProvider());
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
            0,
            500,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenantId, repository.TenantId);
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
        var service = new PosReturnService(repository, tillRepository, new FakeDateTimeProvider());

        var result = await service.SearchOriginalSalesAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []),
            Guid.NewGuid(),
            "recent",
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
    public async Task SearchOriginalSalesAsync_WithInvalidSearchType_ReturnsValidationError()
    {
        var tillRepository = new FakeTillSessionRepository();
        var service = new PosReturnService(
            new FakeReturnRepository(),
            tillRepository,
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
        public PosReturnCompleteRepositoryResult CompleteResult { get; init; } =
            new(null, "failed");
        public PosReturnCompleteCommand? CompleteCommand { get; private set; }
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
        public PosReturnSaleEligibilityDto? Eligibility { get; init; }
        public Guid EligibilitySaleId { get; private set; }
        public int CallCount { get; private set; }
        public Guid TenantId { get; private set; }
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
            Guid saleId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            EligibilitySaleId = saleId;
            return Task.FromResult(Eligibility);
        }

        public Task<PosReturnSaleSearchPageDto> SearchOriginalSalesAsync(
            Guid tenantId,
            string searchType,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            SearchType = searchType;
            Search = search;
            Page = page;
            PageSize = pageSize;
            return Task.FromResult(new PosReturnSaleSearchPageDto([], page, pageSize, 0));
        }
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
