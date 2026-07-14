using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public sealed class PosReturnService : IPosReturnService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const int MaxPreviewLines = 100;

    private readonly IPosReturnRepository _repository;
    private readonly IPosTillSessionRepository _tillSessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PosReturnService(
        IPosReturnRepository repository,
        IPosTillSessionRepository tillSessionRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _tillSessionRepository = tillSessionRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PosReturnReceiptDto>> CompleteReturnAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnCompleteRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ReturnsPermissions.CreateRefund))
        {
            return CompleteFailure(
                "pos_returns.permission_denied",
                "You do not have permission to complete POS returns.");
        }

        if (saleId == Guid.Empty)
        {
            return CompleteFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return CompleteFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        var reasonCode = request.ReasonCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(reasonCode) || reasonCode.Length > 80)
        {
            return CompleteFailure(
                "pos_returns.invalid_reason_code",
                "A valid return reason code is required.");
        }

        var settlementCode = request.SettlementMethodCode?.Trim().ToUpperInvariant();
        if (settlementCode is "STORE_CREDIT" or "LOYALTY_POINTS")
        {
            return CompleteFailure(
                "pos_returns.settlement_not_supported",
                "The selected settlement requires a customer credit or loyalty ledger that is not available yet.");
        }

        if (settlementCode is not ("CASH_REFUND" or "CARD_REFUND"))
        {
            return CompleteFailure(
                "pos_returns.invalid_settlement_method",
                "Settlement method must be CASH_REFUND or CARD_REFUND.");
        }

        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        if (notes?.Length > 1000)
        {
            return CompleteFailure(
                "pos_returns.notes_too_long",
                "Return notes cannot exceed 1000 characters.");
        }

        if (request.Lines is null || request.Lines.Count == 0 ||
            request.Lines.Count > MaxPreviewLines ||
            request.Lines.Any(x => x.SaleLineId == Guid.Empty || x.ReturnQty <= 0) ||
            request.Lines.Select(x => x.SaleLineId).Distinct().Count() != request.Lines.Count)
        {
            return CompleteFailure(
                "pos_returns.invalid_lines",
                "Select unique sale lines with return quantities greater than zero.");
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess || tillContext.Snapshot is null)
        {
            return ApplicationResult<PosReturnReceiptDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var result = await _repository.CompleteReturnAsync(
            context.TenantId,
            context.UserId,
            new PosReturnCompleteCommand(
                saleId,
                deviceId.Value,
                tillContext.Snapshot.SessionId,
                tillContext.Snapshot.OutletId,
                tillContext.Snapshot.TillId,
                reasonCode,
                settlementCode,
                notes,
                request.Lines),
            _dateTimeProvider.UtcNow,
            cancellationToken);
        if (result.Receipt is not null)
        {
            return ApplicationResult<PosReturnReceiptDto>.Success(result.Receipt);
        }

        return result.ErrorCode switch
        {
            "sale_not_found" => CompleteFailure(
                "pos_returns.sale_not_found",
                "The completed original sale could not be found."),
            "reason_not_found" => CompleteFailure(
                "pos_returns.reason_not_found",
                "The selected return reason is not active or does not apply to returns."),
            "sale_line_not_found" => CompleteFailure(
                "pos_returns.sale_line_not_found",
                "One or more selected sale lines do not belong to this sale."),
            "line_not_returnable" => CompleteFailure(
                "pos_returns.line_not_returnable",
                "One or more selected sale lines are not eligible for return."),
            "quantity_exceeds_available" => CompleteFailure(
                "pos_returns.quantity_exceeds_available",
                "A requested return quantity exceeds the quantity still available."),
            "credit_exceeds_refundable" => CompleteFailure(
                "pos_returns.credit_exceeds_refundable",
                "The refund exceeds the remaining refundable sale amount."),
            "approval_required" => CompleteFailure(
                "pos_returns.approval_required",
                "Manager approval is required by the return policy."),
            "original_card_payment_required" => CompleteFailure(
                "pos_returns.original_card_payment_required",
                "A card refund requires sufficient refundable value on the original card payment."),
            "cash_payment_method_not_found" => CompleteFailure(
                "pos_returns.cash_payment_method_not_found",
                "An active POS cash payment method is required for a cash refund."),
            "concurrency_conflict" => CompleteFailure(
                "pos_returns.concurrency_conflict",
                "The sale changed while the return was being completed. Reload and try again."),
            _ => CompleteFailure(
                "pos_returns.complete_failed",
                "The return could not be completed.")
        };
    }

    public async Task<ApplicationResult<PosReturnCreditPreviewDto>> PreviewCreditAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnCreditPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ReturnsPermissions.CreateRefund))
        {
            return CreditFailure(
                "pos_returns.permission_denied",
                "You do not have permission to create POS return credits.");
        }

        if (saleId == Guid.Empty)
        {
            return CreditFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return CreditFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        var reasonCode = request.ReasonCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(reasonCode) || reasonCode.Length > 80)
        {
            return CreditFailure(
                "pos_returns.invalid_reason_code",
                "A valid return reason code is required.");
        }

        if (request.Lines is null || request.Lines.Count == 0 ||
            request.Lines.Count > MaxPreviewLines)
        {
            return CreditFailure(
                "pos_returns.invalid_lines",
                $"Select between 1 and {MaxPreviewLines} sale lines.");
        }

        if (request.Lines.Any(x => x.SaleLineId == Guid.Empty || x.ReturnQty <= 0) ||
            request.Lines.Select(x => x.SaleLineId).Distinct().Count() != request.Lines.Count)
        {
            return CreditFailure(
                "pos_returns.invalid_lines",
                "Each selected sale line must be unique and have a return quantity greater than zero.");
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess)
        {
            return ApplicationResult<PosReturnCreditPreviewDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var repositoryResult = await _repository.PreviewCreditAsync(
            context.TenantId,
            saleId,
            reasonCode,
            request.Lines,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        if (repositoryResult.Preview is not null)
        {
            return ApplicationResult<PosReturnCreditPreviewDto>.Success(repositoryResult.Preview);
        }

        return repositoryResult.ErrorCode switch
        {
            "sale_not_found" => CreditFailure(
                "pos_returns.sale_not_found",
                "The completed original sale could not be found."),
            "reason_not_found" => CreditFailure(
                "pos_returns.reason_not_found",
                "The selected return reason is not active or does not apply to returns."),
            "sale_line_not_found" => CreditFailure(
                "pos_returns.sale_line_not_found",
                "One or more selected sale lines do not belong to this sale."),
            "line_not_returnable" => CreditFailure(
                "pos_returns.line_not_returnable",
                "One or more selected sale lines are not eligible for return."),
            "quantity_exceeds_available" => CreditFailure(
                "pos_returns.quantity_exceeds_available",
                "A requested return quantity exceeds the quantity still available."),
            "credit_exceeds_refundable" => CreditFailure(
                "pos_returns.credit_exceeds_refundable",
                "The preview credit exceeds the remaining refundable sale amount."),
            _ => CreditFailure(
                "pos_returns.credit_preview_failed",
                "The return credit preview could not be calculated.")
        };
    }

    public async Task<ApplicationResult<PosReturnSaleEligibilityDto>> GetSaleEligibilityAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ReturnsPermissions.ViewReturns))
        {
            return EligibilityFailure(
                "pos_returns.permission_denied",
                "You do not have permission to view POS returns.");
        }

        if (saleId == Guid.Empty)
        {
            return EligibilityFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return EligibilityFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess)
        {
            return ApplicationResult<PosReturnSaleEligibilityDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var response = await _repository.GetSaleEligibilityAsync(
            context.TenantId,
            saleId,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        return response is null
            ? EligibilityFailure(
                "pos_returns.sale_not_found",
                "The completed original sale could not be found.")
            : ApplicationResult<PosReturnSaleEligibilityDto>.Success(response);
    }

    public async Task<ApplicationResult<PosReturnSaleSearchPageDto>> SearchOriginalSalesAsync(
        TenantRequestContext context,
        Guid? deviceId,
        string? searchType,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ReturnsPermissions.ViewReturns))
        {
            return Failure(
                "pos_returns.permission_denied",
                "You do not have permission to view POS returns.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return Failure("pos_returns.invalid_device_id", "Device id is required.");
        }

        var normalizedSearchType = NormalizeSearchType(searchType);
        if (normalizedSearchType is null)
        {
            return Failure(
                "pos_returns.invalid_search_type",
                "Search type must be invoice, mobile, customer, recent, receipt, or sale.");
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (normalizedSearchType != "recent" && normalizedSearch is null)
        {
            return Failure(
                "pos_returns.search_required",
                "A search value is required for the selected search type.");
        }

        if (normalizedSearch?.Length > 150)
        {
            return Failure(
                "pos_returns.search_too_long",
                "Search value cannot exceed 150 characters.");
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess)
        {
            return ApplicationResult<PosReturnSaleSearchPageDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var safePage = Math.Max(page, 1);
        var safePageSize = pageSize < 1
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);
        var response = await _repository.SearchOriginalSalesAsync(
            context.TenantId,
            normalizedSearchType,
            normalizedSearch,
            safePage,
            safePageSize,
            cancellationToken);

        return ApplicationResult<PosReturnSaleSearchPageDto>.Success(response);
    }

    private static string? NormalizeSearchType(string? searchType) =>
        searchType?.Trim().ToLowerInvariant() switch
        {
            "invoice" or "receipt" => "invoice",
            "sale" => "sale",
            "mobile" => "mobile",
            "customer" => "customer",
            "recent" => "recent",
            _ => null
        };

    private static ApplicationError MapTillContextError(string? errorCode) => errorCode switch
    {
        "till_session.device_not_found" => new ApplicationError(
            "pos_returns.device_not_found",
            "POS device could not be found."),
        "till_session.device_not_trusted" => new ApplicationError(
            "pos_returns.device_not_trusted",
            "This POS device is not trusted."),
        "till_session.till_not_assigned" => new ApplicationError(
            "pos_returns.till_not_assigned",
            "No till is assigned to this POS device."),
        _ => new ApplicationError(
            "pos_returns.open_till_required",
            "An open till session is required to search original sales.")
    };

    private static ApplicationResult<PosReturnSaleSearchPageDto> Failure(
        string code,
        string message) =>
        ApplicationResult<PosReturnSaleSearchPageDto>.Failure(
            new ApplicationError(code, message));

    private static ApplicationResult<PosReturnSaleEligibilityDto> EligibilityFailure(
        string code,
        string message) =>
        ApplicationResult<PosReturnSaleEligibilityDto>.Failure(
            new ApplicationError(code, message));

    private static ApplicationResult<PosReturnCreditPreviewDto> CreditFailure(
        string code,
        string message) =>
        ApplicationResult<PosReturnCreditPreviewDto>.Failure(
            new ApplicationError(code, message));

    private static ApplicationResult<PosReturnReceiptDto> CompleteFailure(
        string code,
        string message) =>
        ApplicationResult<PosReturnReceiptDto>.Failure(
            new ApplicationError(code, message));
}
