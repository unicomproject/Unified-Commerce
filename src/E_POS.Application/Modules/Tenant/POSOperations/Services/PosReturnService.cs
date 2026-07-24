using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Access;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public sealed class PosReturnService : IPosReturnService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const int MaxPreviewLines = 100;
    private const int ReturnNotesMaxLength = 1000;
    private const int InspectionNotesMaxLength = 200;
    private const int InspectionMaxPhotosPerLine = 5;
    private const long InspectionMaxPhotoSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedInspectionContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
    };

    private readonly IPosReturnRepository _repository;
    private readonly IPosTillSessionRepository _tillSessionRepository;
    private readonly IPosProductCatalogRepository _productCatalogRepository;
    private readonly IReturnInspectionMediaStorage _inspectionMediaStorage;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PosReturnService(
        IPosReturnRepository repository,
        IPosTillSessionRepository tillSessionRepository,
        IPosProductCatalogRepository productCatalogRepository,
        IReturnInspectionMediaStorage inspectionMediaStorage,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _tillSessionRepository = tillSessionRepository;
        _productCatalogRepository = productCatalogRepository;
        _inspectionMediaStorage = inspectionMediaStorage;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PosReturnReceiptDto>> CompleteReturnAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnCompleteRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return CompleteFailure(
                "pos_returns.permission_denied",
                "You do not have permission to complete POS returns or exchanges.");
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

        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        if (notes?.Length > ReturnNotesMaxLength)
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

        if (request.ExpectedVersion < 1)
        {
            return CompleteFailure(
                "pos_returns.invalid_version",
                "A valid expected draft version is required.");
        }

        var idempotencyKey = request.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 120)
        {
            return CompleteFailure(
                "pos_returns.invalid_idempotency_key",
                "A valid idempotency key is required.");
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

        var inspectionDraft = _repository.SupportsInspectionDrafts
            ? await _repository.GetInspectionDraftBySaleAsync(context.TenantId, tillContext.Snapshot.OutletId, saleId, cancellationToken)
            : null;
        if (_repository.SupportsInspectionDrafts && inspectionDraft is null)
            return CompleteFailure("pos_returns.inspection_draft_required", "A validated inspection draft is required before completing the return.");
        if (inspectionDraft is not null &&
            inspectionDraft.ExpiresAt.HasValue &&
            inspectionDraft.ExpiresAt.Value <= _dateTimeProvider.UtcNow &&
            !string.Equals(inspectionDraft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
        {
            return CompleteFailure(
                "pos_returns.inspection_draft_expired",
                "The inspection draft has expired. Restart inspection for this sale.");
        }
        // CONSUMED drafts are allowed through so the repository can replay an idempotent completion.
        if (inspectionDraft is not null &&
            !string.Equals(inspectionDraft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(inspectionDraft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
        {
            return CompleteFailure(
                "pos_returns.inspection_not_validated",
                "The inspection draft must be validated before completing the return.");
        }
        if (inspectionDraft is not null &&
            string.Equals(inspectionDraft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase) &&
            inspectionDraft.Version != request.ExpectedVersion)
        {
            return CompleteFailure(
                "pos_returns.inspection_draft_conflict",
                "The return workflow changed. Reload and review the latest details.");
        }
        if (inspectionDraft is not null &&
            string.Equals(inspectionDraft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase) &&
            request.Lines.Any(line => !inspectionDraft.Lines.Any(draftLine => draftLine.SaleLineId == line.SaleLineId)))
        {
            return CompleteFailure("pos_returns.inspection_draft_required",
                "Every returned sale line must have an inspection draft line.");
        }

        var resolutionType = inspectionDraft?.ResolutionType?.Trim().ToUpperInvariant();
        if (resolutionType is not ("REFUND" or "EXCHANGE"))
        {
            return CompleteFailure(
                "pos_returns.invalid_resolution",
                "A saved Refund or Exchange resolution is required before completion.");
        }

        if (resolutionType == "REFUND")
        {
            if (!ReturnsAccess.CanProcessRefund(context))
            {
                return CompleteFailure(
                    "pos_returns.permission_denied",
                    "You do not have permission to complete POS refunds.");
            }
            if (settlementCode is not ("CASH_REFUND" or "CARD_REFUND"))
            {
                return CompleteFailure(
                    "pos_returns.invalid_settlement_method",
                    "Refund settlement must be CASH_REFUND or CARD_REFUND.");
            }
        }
        else
        {
            if (!ReturnsAccess.CanProcessExchange(context))
            {
                return CompleteFailure(
                    "pos_returns.permission_denied",
                    "You do not have permission to complete POS exchanges.");
            }
            if (settlementCode is not ("NO_SETTLEMENT" or "CASH_PAYMENT" or "CASH_REFUND" or "CARD_REFUND"))
            {
                return CompleteFailure(
                    "pos_returns.invalid_settlement_method",
                    "Exchange settlement must match the authoritative exchange difference.");
            }
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
                resolutionType,
                reasonCode,
                settlementCode,
                notes,
                request.Lines,
                request.ExpectedVersion,
                idempotencyKey),
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
            "inspection_draft_required" => CompleteFailure(
                "pos_returns.inspection_draft_required",
                "A validated inspection draft is required before completing the return."),
            "inspection_draft_expired" => CompleteFailure(
                "pos_returns.inspection_draft_expired",
                "The inspection draft has expired. Restart inspection for this sale."),
            "inspection_not_validated" => CompleteFailure(
                "pos_returns.inspection_not_validated",
                "The inspection draft must be validated before completing the return."),
            "inspection_stale" => CompleteFailure(
                "pos_returns.inspection_stale",
                "The inspection draft is no longer valid for completion."),
            "inspection_draft_consumed" => CompleteFailure(
                "pos_returns.inspection_draft_consumed",
                "This return was already completed. Reload the receipt or retry with the same idempotency key."),
            "inspection_draft_conflict" => CompleteFailure(
                "pos_returns.inspection_draft_conflict",
                "The return workflow changed. Reload and review the latest details."),
            "invalid_resolution" => CompleteFailure(
                "pos_returns.invalid_resolution",
                "A saved Refund or Exchange resolution is required before completion."),
            "idempotency_conflict" => CompleteFailure(
                "pos_returns.idempotency_conflict",
                "This completion request conflicts with a previous attempt."),
            "duplicate_completion" => CompleteFailure(
                "pos_returns.duplicate_completion",
                "A completion already exists for this return workflow."),
            "refund_preview_stale" => CompleteFailure(
                "pos_returns.refund_preview_stale",
                "The refund preview is stale. Reload and review before completing."),
            "exchange_cannot_proceed" => CompleteFailure(
                "pos_returns.exchange_cannot_proceed",
                "The exchange cannot proceed with the current replacement selection."),
            "exchange_cash_payment_required" => CompleteFailure(
                "pos_returns.exchange_cash_payment_required",
                "Customer-pays exchanges currently require an in-till cash payment."),
            "exchange_settlement_mismatch" => CompleteFailure(
                "pos_returns.invalid_settlement_method",
                "Exchange settlement must match the authoritative exchange difference."),
            "insufficient_stock" or "insufficient_outlet_stock" => CompleteFailure(
                "pos_returns.insufficient_outlet_stock",
                "One or more replacement items no longer have enough stock at this outlet."),
            "exchange_price_changed" => CompleteFailure(
                "pos_returns.exchange_price_changed",
                "A replacement price changed. Reload the exchange and try again."),
            "exchange_tax_changed" => CompleteFailure(
                "pos_returns.exchange_tax_changed",
                "Replacement tax changed. Reload the exchange and try again."),
            "exchange_discount_changed" => CompleteFailure(
                "pos_returns.exchange_discount_changed",
                "Replacement discount changed. Reload the exchange and try again."),
            "exchange_preview_stale" => CompleteFailure(
                "pos_returns.exchange_preview_stale",
                "The exchange preview is stale. Reload and review before completing."),
            "replacement_not_found" => CompleteFailure(
                "pos_returns.exchange_incomplete",
                "Saved exchange replacement items are required before completion."),
            "expired" => CompleteFailure(
                "pos_returns.inspection_draft_expired",
                "The inspection draft has expired. Restart inspection for this sale."),
            "conflict" => CompleteFailure(
                "pos_returns.inspection_draft_conflict",
                "The return workflow changed. Reload and review the latest details."),
            _ => CompleteFailure(
                "pos_returns.complete_failed",
                "The return could not be completed.")
        };
    }

    public async Task<ApplicationResult<PosReturnReceiptDto>> GetCompletionAsync(
        TenantRequestContext context,
        Guid returnId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (returnId == Guid.Empty)
        {
            return CompleteFailure("pos_returns.invalid_return_id", "Return id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return CompleteFailure("pos_returns.invalid_device_id", "Device id is required.");
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

        var result = await _repository.GetCompletionAsync(
            context.TenantId,
            tillContext.Snapshot.OutletId,
            returnId,
            cancellationToken);
        if (result.Receipt is null)
        {
            return result.ErrorCode switch
            {
                "completion_not_found" or "receipt_not_found" => CompleteFailure(
                    "pos_returns.completion_not_found",
                    "The completed return or receipt could not be found."),
                "completion_not_ready" => CompleteFailure(
                    "pos_returns.completion_not_ready",
                    "The return completion is not ready to display."),
                "exchange_incomplete" => CompleteFailure(
                    "pos_returns.exchange_incomplete",
                    "Exchange completion records are not available for this return."),
                _ => CompleteFailure(
                    "pos_returns.completion_load_failed",
                    "The completed return could not be loaded.")
            };
        }

        var resolution = result.Receipt.Resolution?.Trim().ToUpperInvariant() ?? "REFUND";
        var allowed = resolution == "EXCHANGE"
            ? ReturnsAccess.CanViewExchangeSuccess(context)
            : ReturnsAccess.CanViewRefundSuccess(context);
        if (!allowed)
        {
            return CompleteFailure(
                "pos_returns.permission_denied",
                "You do not have permission to view this completion.");
        }

        return ApplicationResult<PosReturnReceiptDto>.Success(result.Receipt);
    }

    public async Task<ApplicationResult<PosReturnCreditPreviewDto>> PreviewCreditAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnCreditPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return CreditFailure("pos_returns.invalid_device_id", "Device id is required.");
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

        var resolution = _repository.SupportsInspectionDrafts && tillContext.Snapshot is not null
            ? await _repository.GetResolutionAsync(
                context.TenantId,
                tillContext.Snapshot.OutletId,
                saleId,
                cancellationToken)
            : null;

        if (resolution is not null &&
            string.Equals(resolution.ResolutionType, "REFUND", StringComparison.OrdinalIgnoreCase))
        {
            if (!ReturnsAccess.CanProcessRefund(context))
            {
                return CreditFailure(
                    "pos_returns.permission_denied",
                    "You do not have permission to create POS refunds.");
            }
        }
        else if (resolution is not null &&
                 string.Equals(resolution.ResolutionType, "EXCHANGE", StringComparison.OrdinalIgnoreCase))
        {
            if (!ReturnsAccess.CanProcessExchange(context))
            {
                return CreditFailure(
                    "pos_returns.permission_denied",
                    "You do not have permission to create POS exchanges.");
            }
        }
        else if (!ReturnsAccess.CanCompleteReturnOrExchange(context))
        {
            return CreditFailure(
                "pos_returns.permission_denied",
                "You do not have permission to create POS return credits.");
        }

        if (saleId == Guid.Empty)
        {
            return CreditFailure("pos_returns.invalid_sale_id", "Sale id is required.");
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

        var repositoryResult = await _repository.PreviewCreditAsync(
            context.TenantId,
            saleId,
            reasonCode,
            request.Lines,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        if (repositoryResult.Preview is not null)
        {
            var preview = repositoryResult.Preview;
            var requiresApproval = await RequiresRefundApprovalAsync(
                context.TenantId,
                saleId,
                reasonCode,
                tillContext.Snapshot!.OutletId,
                cancellationToken);
            var enriched = preview with
            {
                CanProceed = !requiresApproval,
                RequiresApproval = requiresApproval,
                PolicyMessage = requiresApproval
                    ? "Manager approval is required before this refund can be completed."
                    : null,
                DraftVersion = resolution?.Version
            };
            return ApplicationResult<PosReturnCreditPreviewDto>.Success(enriched);
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
        if (!ReturnsAccess.CanViewReturns(context))
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
        if (!tillContext.IsSuccess || tillContext.Snapshot is null)
        {
            return ApplicationResult<PosReturnSaleEligibilityDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var response = await _repository.GetSaleEligibilityAsync(
            context.TenantId,
            tillContext.Snapshot.OutletId,
            saleId,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        return response is null
            ? EligibilityFailure(
                "pos_returns.sale_not_found",
                "The completed original sale could not be found.")
            : ApplicationResult<PosReturnSaleEligibilityDto>.Success(response);
    }

    public async Task<ApplicationResult<PosReturnSaleEligibilityDto>> CheckSelectedSaleEligibilityAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnEligibilityCheckRequestDto request,
        CancellationToken cancellationToken)
    {
        // Step 4 eligibility evaluation is non-mutating (policy calculation only).
        // Shared Returns workflow requires returns.view — not refunds.view / exchanges.view.
        if (!ReturnsAccess.CanViewReturns(context))
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

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return EligibilityFailure(
                "pos_returns.lines_required",
                "At least one sale line must be selected for eligibility validation.");
        }

        var seenLineIds = new HashSet<Guid>();
        foreach (var line in request.Lines)
        {
            if (line.SaleLineId == Guid.Empty)
            {
                return EligibilityFailure(
                    "pos_returns.invalid_sale_line_id",
                    "Each selected sale line must include a valid sale line id.");
            }

            if (!seenLineIds.Add(line.SaleLineId))
            {
                return EligibilityFailure(
                    "pos_returns.duplicate_sale_line_id",
                    "Each sale line may only appear once in the eligibility check request.");
            }

            if (line.ReturnQty <= 0)
            {
                return EligibilityFailure(
                    "pos_returns.invalid_return_qty",
                    "Each selected sale line must include a return quantity greater than zero.");
            }
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess || tillContext.Snapshot is null)
        {
            return ApplicationResult<PosReturnSaleEligibilityDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var checkResult = await _repository.CheckSelectedSaleEligibilityAsync(
            context.TenantId,
            tillContext.Snapshot.OutletId,
            saleId,
            request.Lines,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return checkResult.ErrorCode switch
        {
            "sale_not_found" => EligibilityFailure(
                "pos_returns.sale_not_found",
                "The completed original sale could not be found."),
            "invalid_sale_line" => EligibilityFailure(
                "pos_returns.invalid_sale_line_id",
                "One or more selected sale lines could not be found on the original sale."),
            "line_not_returnable" => EligibilityFailure(
                "pos_returns.line_not_returnable",
                "One or more selected sale lines are not returnable."),
            "quantity_exceeds_available" => EligibilityFailure(
                "pos_returns.quantity_exceeds_available",
                "The requested return quantity exceeds the remaining returnable quantity."),
            null when checkResult.Eligibility is not null =>
                ApplicationResult<PosReturnSaleEligibilityDto>.Success(checkResult.Eligibility),
            _ => EligibilityFailure(
                "pos_returns.eligibility_check_failed",
                "The return eligibility check could not be completed.")
        };
    }

    public async Task<ApplicationResult<PosReturnReasonsValidateResponseDto>> ValidateReturnReasonsAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnReasonsValidateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return ReasonsValidateFailure(
                "pos_returns.permission_denied",
                "You do not have permission to continue the POS return.");
        }

        if (saleId == Guid.Empty)
        {
            return ReasonsValidateFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ReasonsValidateFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return ReasonsValidateFailure(
                "pos_returns.lines_required",
                "At least one selected return item must include a reason.");
        }

        if (request.Items.Count > MaxPreviewLines ||
            request.Items.Select(x => x.SaleLineId).Distinct().Count() != request.Items.Count)
        {
            return ReasonsValidateFailure(
                "pos_returns.invalid_lines",
                "Select unique sale lines with return reasons.");
        }

        try
        {
            var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
                context.TenantId,
                deviceId.Value,
                cancellationToken);
            if (!tillContext.IsSuccess || tillContext.Snapshot is null)
            {
                return ApplicationResult<PosReturnReasonsValidateResponseDto>.Failure(
                    MapTillContextError(tillContext.ErrorCode));
            }

            var eligibility = await _repository.GetSaleEligibilityAsync(
                context.TenantId,
                tillContext.Snapshot.OutletId,
                saleId,
                _dateTimeProvider.UtcNow,
                cancellationToken);
            if (eligibility is null)
            {
                return ReasonsValidateFailure(
                    "pos_returns.sale_not_found",
                    "The completed original sale could not be found.");
            }

            var saleLineIds = eligibility.Items.Select(x => x.SaleLineId).ToHashSet();
            var reasons = await _repository.GetActiveReturnReasonsAsync(
                context.TenantId,
                cancellationToken);
            var reasonsByCode = reasons.ToDictionary(
                x => x.Code,
                StringComparer.OrdinalIgnoreCase);

            var results = new List<PosReturnReasonAssignmentResultDto>(request.Items.Count);
            foreach (var item in request.Items)
            {
                if (item.SaleLineId == Guid.Empty || !saleLineIds.Contains(item.SaleLineId))
                {
                    return ReasonsValidateFailure(
                        "pos_returns.invalid_sale_line_id",
                        "One or more selected sale lines could not be found on the original sale.");
                }

                var reasonCode = item.ReasonCode?.Trim().ToUpperInvariant() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(reasonCode) ||
                    !reasonsByCode.TryGetValue(reasonCode, out var reason))
                {
                    return ReasonsValidateFailure(
                        "pos_returns.invalid_reason_code",
                        "A valid active return reason is required for every selected item.");
                }

                var notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim();
                if (notes?.Length > ReturnNotesMaxLength)
                {
                    return ReasonsValidateFailure(
                        "pos_returns.notes_too_long",
                        $"Return notes cannot exceed {ReturnNotesMaxLength} characters.");
                }

                if (reason.RequiresNotes && string.IsNullOrWhiteSpace(notes))
                {
                    return ReasonsValidateFailure(
                        "pos_returns.notes_required",
                        "Notes are required for the selected return reason.");
                }

                results.Add(new PosReturnReasonAssignmentResultDto(
                    item.SaleLineId,
                    reason.Id,
                    reason.Code,
                    reason.DisplayName,
                    notes,
                    reason.RequiresNotes,
                    reason.RequiresInspection,
                    reason.RequiresManagerApproval));
            }

            return ApplicationResult<PosReturnReasonsValidateResponseDto>.Success(
                new PosReturnReasonsValidateResponseDto(
                    saleId,
                    request.ApplySameReasonToAll,
                    ReturnNotesMaxLength,
                    results));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return ReasonsValidateFailure(
                "pos_returns.reasons_validate_failed",
                "Return reason validation could not be completed.");
        }
    }

    public async Task<ApplicationResult<IReadOnlyList<PosReturnReasonOptionDto>>> GetReturnReasonsAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        // Return Reason is a create-path step; loading reasons advances past Eligibility Check.
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return ApplicationResult<IReadOnlyList<PosReturnReasonOptionDto>>.Failure(
                new ApplicationError(
                    "pos_returns.permission_denied",
                    "You do not have permission to continue the POS return."));
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<IReadOnlyList<PosReturnReasonOptionDto>>.Failure(
                new ApplicationError("pos_returns.invalid_device_id", "Device id is required."));
        }

        try
        {
            var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
                context.TenantId,
                deviceId.Value,
                cancellationToken);
            if (!tillContext.IsSuccess)
            {
                return ApplicationResult<IReadOnlyList<PosReturnReasonOptionDto>>.Failure(
                    MapTillContextError(tillContext.ErrorCode));
            }

            var reasons = await _repository.GetActiveReturnReasonsAsync(
                context.TenantId,
                cancellationToken);
            return ApplicationResult<IReadOnlyList<PosReturnReasonOptionDto>>.Success(reasons);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<IReadOnlyList<PosReturnReasonOptionDto>>.Failure(
                new ApplicationError(
                    "pos_returns.reasons_load_failed",
                    "Return reasons could not be loaded."));
        }
    }

    public async Task<ApplicationResult<IReadOnlyList<PosReturnInspectionConditionDto>>> GetInspectionConditionsAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        // Inspect Items is a create-path step after reasons are saved.
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return ApplicationResult<IReadOnlyList<PosReturnInspectionConditionDto>>.Failure(
                new ApplicationError(
                    "pos_returns.permission_denied",
                    "You do not have permission to continue the POS return."));
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<IReadOnlyList<PosReturnInspectionConditionDto>>.Failure(
                new ApplicationError("pos_returns.invalid_device_id", "Device id is required."));
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess)
        {
            return ApplicationResult<IReadOnlyList<PosReturnInspectionConditionDto>>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var conditions = await _repository.GetActiveInspectionConditionsAsync(
            context.TenantId,
            cancellationToken);
        return ApplicationResult<IReadOnlyList<PosReturnInspectionConditionDto>>.Success(conditions);
    }

    public async Task<ApplicationResult<PosReturnInspectionValidateResponseDto>> ValidateInspectionAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnInspectionValidateRequestDto request,
        CancellationToken cancellationToken)
    {
        // Step 6 is a shared Returns create-path step (not refund/exchange branch).
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                new ApplicationError(
                    "pos_returns.permission_denied",
                    "You do not have permission to continue the POS return."));
        }

        if (saleId == Guid.Empty)
        {
            return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                new ApplicationError("pos_returns.invalid_sale_id", "Sale id is required."));
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                new ApplicationError("pos_returns.invalid_device_id", "Device id is required."));
        }

        if (request.Lines is not null && (request.Lines.Count > MaxPreviewLines ||
            request.Lines.Select(x => x.SaleLineId).Distinct().Count() != request.Lines.Count)
        )
        {
            return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                new ApplicationError(
                    "pos_returns.invalid_lines",
                    "Select unique sale lines with inspection results."));
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess)
        {
            return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var outletId = tillContext.Snapshot!.OutletId;
        var saleOk = await _repository.SaleBelongsToOutletAsync(
            context.TenantId, outletId, saleId, cancellationToken);
        if (!saleOk)
        {
            return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                new ApplicationError("pos_returns.sale_not_found", "The original sale could not be found."));
        }

        // A validate request may carry edits for backwards compatibility, but validation
        // always evaluates the persisted draft that completion will later consume.
        PosReturnInspectionDraftRecord? persistedDraft = null;
        if (_repository.SupportsInspectionDrafts && request.Lines is { Count: > 0 })
        {
            var saved = await _repository.SaveInspectionDraftAsync(
                context.TenantId, outletId, saleId, context.UserId,
                request.Lines.Select(x => new PosReturnInspectionDraftLineDto(
                    x.SaleLineId, x.ConditionCode, x.Notes, x.MediaIds)).ToList(),
                _dateTimeProvider.UtcNow, request.Version, cancellationToken);
            if (saved.Draft is null)
            {
                return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                    MapInspectionDraftError(saved.ErrorCode));
            }

            request = new PosReturnInspectionValidateRequestDto(
                saved.Draft.Lines
                    .Select(x => new PosReturnInspectionLineRequestDto(x.SaleLineId, x.ConditionCode, x.Notes, x.MediaIds)).ToList(),
                request.ReasonRefs,
                saved.Draft.Version);
        }
        if (_repository.SupportsInspectionDrafts)
        {
            persistedDraft = await _repository.GetInspectionDraftBySaleAsync(
                context.TenantId, outletId, saleId, cancellationToken);
            if (persistedDraft is null)
            {
                return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                    new ApplicationError("pos_returns.inspection_draft_required", "Save an inspection draft before validating it."));
            }

            if (persistedDraft.ExpiresAt.HasValue &&
                persistedDraft.ExpiresAt.Value <= _dateTimeProvider.UtcNow)
            {
                return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                    new ApplicationError(
                        "pos_returns.inspection_draft_expired",
                        "The inspection draft has expired. Restart inspection for this sale."));
            }

            if (string.Equals(persistedDraft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
            {
                return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                    new ApplicationError(
                        "pos_returns.inspection_draft_consumed",
                        "The inspection draft has already been consumed."));
            }

            request = new PosReturnInspectionValidateRequestDto(
                persistedDraft.Lines
                    .Select(x => new PosReturnInspectionLineRequestDto(x.SaleLineId, x.ConditionCode, x.Notes, x.MediaIds)).ToList(),
                request.ReasonRefs,
                persistedDraft.Version);
        }

        var conditions = await _repository.GetActiveInspectionConditionsAsync(
            context.TenantId,
            cancellationToken);
        if (conditions.Count == 0)
        {
            return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                new ApplicationError(
                    "pos_returns.inspection_conditions_unavailable",
                    "Inspection conditions are not configured for this tenant."));
        }

        var conditionByCode = conditions.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var inspectionLines = request.Lines ?? Array.Empty<PosReturnInspectionLineRequestDto>();
        var selectedCount = inspectionLines.Count;
        var inspectedCount = 0;
        var breakdown = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var policyMessages = new List<PosReturnInspectionPolicyMessageDto>();
        var canContinue = true;
        var step6RequiresManagerApproval = false;
        var step6RequiresInspection = false;

        foreach (var pendingCode in conditions.Select(x => x.Code))
        {
            breakdown[pendingCode] = 0;
        }
        breakdown["PENDING"] = 0;

        foreach (var line in inspectionLines)
        {
            if (line.SaleLineId == Guid.Empty)
            {
                canContinue = false;
                breakdown["PENDING"] += 1;
                continue;
            }

            var belongs = await _repository.SaleLineBelongsToSaleAsync(
                context.TenantId,
                saleId,
                line.SaleLineId,
                cancellationToken);
            if (!belongs)
            {
                return ApplicationResult<PosReturnInspectionValidateResponseDto>.Failure(
                    new ApplicationError(
                        "pos_returns.invalid_sale_line_id",
                        "One or more selected sale lines could not be found on the original sale."));
            }

            var conditionCode = line.ConditionCode?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!conditionByCode.TryGetValue(conditionCode, out var condition))
            {
                canContinue = false;
                breakdown["PENDING"] += 1;
                continue;
            }

            var notes = line.Notes?.Trim() ?? string.Empty;
            var lineComplete = true;
            if (notes.Length > InspectionNotesMaxLength)
            {
                lineComplete = false;
            }

            if (lineComplete && condition.RequiresNotes && string.IsNullOrWhiteSpace(notes))
            {
                lineComplete = false;
            }

            var mediaIds = line.MediaIds ?? Array.Empty<Guid>();
            if (mediaIds.Count > InspectionMaxPhotosPerLine)
            {
                lineComplete = false;
            }

            if (lineComplete && condition.RequiresPhoto && mediaIds.Count == 0)
            {
                lineComplete = false;
            }

            if (lineComplete)
            {
                foreach (var mediaId in mediaIds)
                {
                    var media = await _repository.GetInspectionMediaStagingAsync(
                        context.TenantId,
                        outletId,
                        mediaId,
                        cancellationToken);
                    if (media is null ||
                        media.SaleId != saleId ||
                        media.SaleLineId != line.SaleLineId)
                    {
                        lineComplete = false;
                        break;
                    }
                }
            }

            if (!lineComplete)
            {
                canContinue = false;
                breakdown["PENDING"] += 1;
                continue;
            }

            breakdown[condition.Code] += 1;
            inspectedCount += 1;

            if (condition.RequiresApproval)
            {
                step6RequiresManagerApproval = true;
            }

            // Canonical Step 6 inspection/review: photo-required conditions or non-NONE refund impact.
            if (condition.RequiresPhoto ||
                !string.Equals(condition.RefundImpact, "NONE", StringComparison.OrdinalIgnoreCase))
            {
                step6RequiresInspection = true;
            }

            if (!string.Equals(condition.RefundImpact, "NONE", StringComparison.OrdinalIgnoreCase))
            {
                policyMessages.Add(new PosReturnInspectionPolicyMessageDto(
                    condition.StatusCategory == "DANGER" ? "WARNING" : "INFO",
                    condition.DisplayName,
                    BuildRefundImpactMessage(condition),
                    [line.SaleLineId],
                    condition.RequiresApproval,
                    condition.RefundImpact));
            }
        }

        var pendingCount = Math.Max(0, selectedCount - inspectedCount);
        breakdown["PENDING"] = pendingCount;

        var distinctMessages = policyMessages
            .GroupBy(x => x.Message, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        // Merge Step 4 (policy) + Step 5 (reason codes re-read from DB) + Step 6 (conditions).
        // Never trust client-supplied approval/inspection booleans.
        var step4RequiresInspection = false;
        var step4RequiresManagerApproval = false;
        if (inspectionLines.Count > 0)
        {
            var eligibilityCheck = await _repository.CheckSelectedSaleEligibilityAsync(
                context.TenantId,
                outletId,
                saleId,
                inspectionLines.Select(x => new PosReturnCreditPreviewLineRequestDto(x.SaleLineId, 1m)).ToList(),
                _dateTimeProvider.UtcNow,
                cancellationToken);
            if (eligibilityCheck.Eligibility is not null)
            {
                step4RequiresInspection = eligibilityCheck.Eligibility.RequiresInspection;
                step4RequiresManagerApproval = eligibilityCheck.Eligibility.RequiresManagerApproval;
            }
        }

        var step5RequiresInspection = false;
        var step5RequiresManagerApproval = false;
        if (request.ReasonRefs is { Count: > 0 })
        {
            var reasons = await _repository.GetActiveReturnReasonsAsync(context.TenantId, cancellationToken);
            var reasonByCode = reasons.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
            foreach (var reasonRef in request.ReasonRefs)
            {
                var code = reasonRef.ReasonCode?.Trim() ?? string.Empty;
                if (!reasonByCode.TryGetValue(code, out var reason))
                {
                    continue;
                }

                if (reason.RequiresInspection)
                {
                    step5RequiresInspection = true;
                }

                if (reason.RequiresManagerApproval)
                {
                    step5RequiresManagerApproval = true;
                }
            }
        }

        var finalRequiresInspection =
            step4RequiresInspection || step5RequiresInspection || step6RequiresInspection;
        var finalRequiresManagerApproval =
            step4RequiresManagerApproval || step5RequiresManagerApproval || step6RequiresManagerApproval;
        // RequiresReview remains the manager-approval signal for existing Flutter clients.
        var requiresReview = finalRequiresManagerApproval;

        if (persistedDraft is not null && canContinue && pendingCount == 0)
        {
            await _repository.MarkInspectionDraftValidatedAsync(
                context.TenantId,
                persistedDraft.DraftId,
                context.UserId,
                _dateTimeProvider.UtcNow,
                finalRequiresInspection,
                finalRequiresManagerApproval,
                cancellationToken);
            persistedDraft = await _repository.GetInspectionDraftBySaleAsync(
                context.TenantId, outletId, saleId, cancellationToken);
        }

        return ApplicationResult<PosReturnInspectionValidateResponseDto>.Success(
            new PosReturnInspectionValidateResponseDto(
                canContinue && pendingCount == 0,
                selectedCount,
                inspectedCount,
                pendingCount,
                breakdown,
                distinctMessages,
                requiresReview,
                InspectionNotesMaxLength,
                InspectionMaxPhotosPerLine,
                InspectionMaxPhotoSizeBytes,
                persistedDraft?.DraftId,
                canContinue && pendingCount == 0 ? "VALIDATED" : "DRAFT",
                persistedDraft?.Version,
                persistedDraft?.ExpiresAt,
                finalRequiresInspection,
                finalRequiresManagerApproval));
    }

    public async Task<ApplicationResult<PosReturnInspectionDraftResponseDto>> SaveInspectionDraftAsync(
        TenantRequestContext context, Guid saleId, Guid? deviceId,
        PosReturnInspectionDraftSaveRequestDto request, CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError("pos_returns.permission_denied", "You do not have permission to save an inspection draft."));
        if (saleId == Guid.Empty || request.Lines is null || request.Lines.Count == 0)
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError("pos_returns.lines_required", "At least one inspection line is required."));
        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError("pos_returns.invalid_device_id", "Device id is required."));
        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(context.TenantId, deviceId.Value, cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(MapTillContextError(till.ErrorCode));

        var saleOk = await _repository.SaleBelongsToOutletAsync(
            context.TenantId, till.Snapshot.OutletId, saleId, cancellationToken);
        if (!saleOk)
        {
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError("pos_returns.sale_not_found", "The original sale could not be found."));
        }

        var saved = await _repository.SaveInspectionDraftAsync(context.TenantId, till.Snapshot.OutletId, saleId,
            context.UserId, request.Lines, _dateTimeProvider.UtcNow, request.Version, cancellationToken);
        if (saved.Draft is null)
        {
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                MapInspectionDraftError(saved.ErrorCode));
        }

        return ApplicationResult<PosReturnInspectionDraftResponseDto>.Success(
            new PosReturnInspectionDraftResponseDto(
                saved.Draft.DraftId,
                saved.Draft.Status,
                saved.Draft.Lines
                    .Select(x => new PosReturnInspectionDraftLineDto(x.SaleLineId, x.ConditionCode, x.Notes, x.MediaIds)).ToList(),
                saved.Draft.Version,
                saved.Draft.ExpiresAt));
    }

    public async Task<ApplicationResult<PosReturnInspectionDraftResponseDto>> GetInspectionDraftAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError(
                    "pos_returns.permission_denied",
                    "You do not have permission to view the inspection draft."));
        }

        if (saleId == Guid.Empty)
        {
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError("pos_returns.invalid_sale_id", "Sale id is required."));
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError("pos_returns.invalid_device_id", "Device id is required."));
        }

        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
        {
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                MapTillContextError(till.ErrorCode));
        }

        if (!_repository.SupportsInspectionDrafts)
        {
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError(
                    "pos_returns.inspection_draft_not_found",
                    "No inspection draft was found for this sale."));
        }

        var draft = await _repository.GetInspectionDraftBySaleAsync(
            context.TenantId,
            till.Snapshot.OutletId,
            saleId,
            cancellationToken);
        if (draft is null)
        {
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError(
                    "pos_returns.inspection_draft_not_found",
                    "No inspection draft was found for this sale."));
        }

        if (draft.ExpiresAt.HasValue && draft.ExpiresAt.Value <= _dateTimeProvider.UtcNow)
        {
            return ApplicationResult<PosReturnInspectionDraftResponseDto>.Failure(
                new ApplicationError(
                    "pos_returns.inspection_draft_expired",
                    "The inspection draft has expired. Restart inspection for this sale."));
        }

        return ApplicationResult<PosReturnInspectionDraftResponseDto>.Success(
            new PosReturnInspectionDraftResponseDto(
                draft.DraftId,
                draft.Status,
                draft.Lines
                    .Select(x => new PosReturnInspectionDraftLineDto(
                        x.SaleLineId,
                        x.ConditionCode,
                        x.Notes,
                        x.MediaIds))
                    .ToList(),
                draft.Version,
                draft.ExpiresAt));
    }

    public async Task<ApplicationResult<PosReturnResolutionResponseDto>> SaveResolutionAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnResolutionSaveRequestDto request,
        CancellationToken cancellationToken)
    {
        var resolution = request.ResolutionType?.Trim().ToUpperInvariant();
        if (resolution is not ("REFUND" or "EXCHANGE"))
        {
            return ResolutionFailure(
                "pos_returns.invalid_resolution",
                "Resolution must be REFUND or EXCHANGE.");
        }

        var hasBranchPermission = resolution == "REFUND"
            ? ReturnsAccess.CanSaveRefundResolution(context)
            : ReturnsAccess.CanSaveExchangeResolution(context);
        if (!hasBranchPermission)
        {
            return ResolutionFailure(
                "pos_returns.permission_denied",
                "You do not have permission to save the selected resolution.");
        }

        if (saleId == Guid.Empty)
        {
            return ResolutionFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ResolutionFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        if (request.ExpectedVersion < 1)
        {
            return ResolutionFailure(
                "pos_returns.invalid_version",
                "A valid expected draft version is required.");
        }

        if (!_repository.SupportsInspectionDrafts)
        {
            return ResolutionFailure(
                "pos_returns.inspection_draft_not_found",
                "No validated inspection draft was found for this sale.");
        }

        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
        {
            return ApplicationResult<PosReturnResolutionResponseDto>.Failure(
                MapTillContextError(till.ErrorCode));
        }

        if (!await _repository.SaleBelongsToOutletAsync(
                context.TenantId,
                till.Snapshot.OutletId,
                saleId,
                cancellationToken))
        {
            return ResolutionFailure(
                "pos_returns.sale_not_found",
                "The original sale could not be found.");
        }

        var result = await _repository.SaveResolutionAsync(
            context.TenantId,
            till.Snapshot.OutletId,
            saleId,
            context.UserId,
            resolution,
            request.ExpectedVersion,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return result.ErrorCode switch
        {
            null when result.Resolution is not null =>
                ApplicationResult<PosReturnResolutionResponseDto>.Success(
                    BuildResolutionResponse(context, result.Resolution)),
            "draft_not_found" => ResolutionFailure(
                "pos_returns.inspection_draft_not_found",
                "No validated inspection draft was found for this sale."),
            "inspection_not_validated" => ResolutionFailure(
                "pos_returns.inspection_not_validated",
                "The inspection draft must be validated before choosing a resolution."),
            "expired" => ResolutionFailure(
                "pos_returns.inspection_draft_expired",
                "The inspection draft has expired. Restart inspection for this sale."),
            "conflict" => ResolutionFailure(
                "pos_returns.inspection_draft_conflict",
                "The return workflow changed. Reload the current resolution and try again."),
            "stale" => ResolutionFailure(
                "pos_returns.concurrency_conflict",
                "The return workflow has already been completed or cancelled."),
            _ => ResolutionFailure(
                "pos_returns.resolution_save_failed",
                "The selected resolution could not be saved."),
        };
    }

    public async Task<ApplicationResult<PosReturnResolutionResponseDto>> GetResolutionAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return ResolutionFailure(
                "pos_returns.permission_denied",
                "You do not have permission to view the saved resolution.");
        }

        if (saleId == Guid.Empty)
        {
            return ResolutionFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ResolutionFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        if (!_repository.SupportsInspectionDrafts)
        {
            return ResolutionFailure(
                "pos_returns.resolution_not_found",
                "No saved resolution was found for this sale.");
        }

        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
        {
            return ApplicationResult<PosReturnResolutionResponseDto>.Failure(
                MapTillContextError(till.ErrorCode));
        }

        if (!await _repository.SaleBelongsToOutletAsync(
                context.TenantId,
                till.Snapshot.OutletId,
                saleId,
                cancellationToken))
        {
            return ResolutionFailure(
                "pos_returns.sale_not_found",
                "The original sale could not be found.");
        }

        var resolution = await _repository.GetResolutionAsync(
            context.TenantId,
            till.Snapshot.OutletId,
            saleId,
            cancellationToken);
        if (resolution is null)
        {
            return ResolutionFailure(
                "pos_returns.resolution_not_found",
                "No saved resolution was found for this sale.");
        }

        if (resolution.ExpiresAt <= _dateTimeProvider.UtcNow)
        {
            return ResolutionFailure(
                "pos_returns.inspection_draft_expired",
                "The inspection draft has expired. Restart inspection for this sale.");
        }

        if (string.Equals(resolution.DraftStatus, "CONSUMED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(resolution.DraftStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            return ResolutionFailure(
                "pos_returns.inspection_draft_consumed",
                "The return workflow is no longer available for resolution changes.");
        }

        if (!string.Equals(resolution.DraftStatus, "VALIDATED", StringComparison.OrdinalIgnoreCase))
        {
            return ResolutionFailure(
                "pos_returns.inspection_not_validated",
                "The inspection draft must be validated before choosing a resolution.");
        }

        return ApplicationResult<PosReturnResolutionResponseDto>.Success(
            BuildResolutionResponse(context, resolution));
    }

    public async Task<ApplicationResult<PosReturnRefundMethodsResponseDto>> GetRefundMethodsAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanProcessRefund(context))
        {
            return RefundMethodsFailure(
                "pos_returns.permission_denied",
                "You do not have permission to view refund methods.");
        }

        if (saleId == Guid.Empty)
        {
            return RefundMethodsFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return RefundMethodsFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
        {
            return ApplicationResult<PosReturnRefundMethodsResponseDto>.Failure(
                MapTillContextError(till.ErrorCode));
        }

        var result = await _repository.GetRefundMethodsAsync(
            context.TenantId,
            till.Snapshot.OutletId,
            saleId,
            till.Snapshot.Status == "OPEN",
            cancellationToken);

        return MapRefundMethodsResult(result);
    }

    public async Task<ApplicationResult<PosReturnRefundMethodSaveResponseDto>> SaveRefundMethodAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnRefundMethodSaveRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanProcessRefund(context))
        {
            return RefundMethodSaveFailure(
                "pos_returns.permission_denied",
                "You do not have permission to save a refund method.");
        }

        var methodCode = request.MethodCode?.Trim().ToUpperInvariant();
        if (methodCode is not ("ORIGINAL_PAYMENT" or "CASH"))
        {
            return RefundMethodSaveFailure(
                "pos_returns.invalid_refund_method",
                "Refund method must be ORIGINAL_PAYMENT or CASH.");
        }

        if (saleId == Guid.Empty)
        {
            return RefundMethodSaveFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return RefundMethodSaveFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
        {
            return ApplicationResult<PosReturnRefundMethodSaveResponseDto>.Failure(
                MapTillContextError(till.ErrorCode));
        }

        var result = await _repository.SaveRefundMethodAsync(
            context.TenantId,
            till.Snapshot.OutletId,
            saleId,
            context.UserId,
            methodCode,
            till.Snapshot.Status == "OPEN",
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return result.ErrorCode switch
        {
            null when result.Method is not null =>
                ApplicationResult<PosReturnRefundMethodSaveResponseDto>.Success(
                    new PosReturnRefundMethodSaveResponseDto(
                        result.Method.SaleId,
                        result.Method.MethodCode,
                        result.Method.SelectedAt)),
            "draft_not_found" => RefundMethodSaveFailure(
                "pos_returns.inspection_draft_not_found",
                "No validated inspection draft was found for this sale."),
            "inspection_not_validated" => RefundMethodSaveFailure(
                "pos_returns.inspection_not_validated",
                "The inspection draft must be validated before selecting a refund method."),
            "invalid_resolution" => RefundMethodSaveFailure(
                "pos_returns.invalid_resolution",
                "A saved Refund resolution is required before selecting a refund method."),
            "stale" => RefundMethodSaveFailure(
                "pos_returns.concurrency_conflict",
                "The return workflow has already been completed or cancelled."),
            "invalid_refund_method" or "refund_method_not_allowed" or "method_not_allowed" =>
                RefundMethodSaveFailure(
                    "pos_returns.refund_method_not_allowed",
                    "The selected refund method is not allowed for this return."),
            _ => RefundMethodSaveFailure(
                "pos_returns.refund_method_save_failed",
                "The selected refund method could not be saved.")
        };
    }

    public async Task<ApplicationResult<PosReturnInspectionMediaDto>> UploadInspectionMediaAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid saleLineId,
        Guid? deviceId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                new ApplicationError(
                    "pos_returns.permission_denied",
                    "You do not have permission to upload inspection media."));
        }

        if (saleId == Guid.Empty || saleLineId == Guid.Empty)
        {
            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                new ApplicationError(
                    "pos_returns.invalid_sale_line",
                    "Sale id and sale line id are required."));
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                new ApplicationError("pos_returns.invalid_device_id", "Device id is required."));
        }

        if (fileSizeBytes <= 0 || fileSizeBytes > InspectionMaxPhotoSizeBytes)
        {
            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                new ApplicationError(
                    "pos_returns.inspection_media_too_large",
                    "Inspection photo exceeds the allowed file size."));
        }

        var normalizedContentType = contentType.Trim().ToLowerInvariant();
        if (!AllowedInspectionContentTypes.Contains(normalizedContentType))
        {
            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                new ApplicationError(
                    "pos_returns.inspection_media_invalid_type",
                    "Only JPEG, PNG, and WebP images are supported."));
        }
        if (!HasValidInspectionImageSignature(fileStream, normalizedContentType))
        {
            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                new ApplicationError("pos_returns.inspection_media_invalid_type",
                    "The uploaded file does not match the declared image type."));
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess || tillContext.Snapshot is null)
        {
            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var outletId = tillContext.Snapshot.OutletId;
        var saleOk = await _repository.SaleBelongsToOutletAsync(
            context.TenantId, outletId, saleId, cancellationToken);
        if (!saleOk)
        {
            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                new ApplicationError("pos_returns.sale_not_found", "The original sale could not be found."));
        }

        var belongs = await _repository.SaleLineBelongsToSaleAsync(
            context.TenantId,
            saleId,
            saleLineId,
            cancellationToken);
        if (!belongs)
        {
            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                new ApplicationError(
                    "pos_returns.invalid_sale_line_id",
                    "The selected sale line could not be found on the original sale."));
        }

        var mediaId = Guid.NewGuid();
        string? storageKey = null;
        try
        {
            var saveResult = await _inspectionMediaStorage.SaveAsync(
                context.TenantId,
                saleId,
                saleLineId,
                mediaId,
                fileStream,
                normalizedContentType,
                cancellationToken);
            storageKey = saveResult.StorageKey;

            var stagingResult = await _repository.SaveInspectionMediaStagingAsync(
                context.TenantId,
                outletId,
                context.UserId,
                saleId,
                saleLineId,
                mediaId,
                saveResult.StorageKey,
                fileName,
                normalizedContentType,
                saveResult.SizeBytes,
                _dateTimeProvider.UtcNow,
                cancellationToken);
            if (stagingResult.Media is null)
            {
                await _inspectionMediaStorage.DeleteAsync(saveResult.StorageKey, cancellationToken);
                return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                    MapInspectionMediaError(stagingResult.ErrorCode));
            }

            return ApplicationResult<PosReturnInspectionMediaDto>.Success(
                new PosReturnInspectionMediaDto(
                    stagingResult.Media.MediaId,
                    stagingResult.Media.SaleLineId,
                    stagingResult.Media.FileName,
                    stagingResult.Media.ContentType,
                    stagingResult.Media.SizeBytes,
                    BuildInspectionMediaUrl(stagingResult.Media.MediaId)));
        }
        catch (Exception)
        {
            if (!string.IsNullOrWhiteSpace(storageKey))
            {
                await _inspectionMediaStorage.DeleteAsync(storageKey, cancellationToken);
            }

            return ApplicationResult<PosReturnInspectionMediaDto>.Failure(
                new ApplicationError(
                    "pos_returns.inspection_media_storage_failed",
                    "Inspection photo storage failed."));
        }
    }

    public async Task<ApplicationResult> DeleteInspectionMediaAsync(
        TenantRequestContext context,
        Guid mediaId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return ApplicationResult.Failure(
                new ApplicationError(
                    "pos_returns.permission_denied",
                    "You do not have permission to delete inspection media."));
        }

        if (mediaId == Guid.Empty)
        {
            return ApplicationResult.Failure(
                new ApplicationError("pos_returns.invalid_media_id", "Media id is required."));
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult.Failure(
                new ApplicationError("pos_returns.invalid_device_id", "Device id is required."));
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess || tillContext.Snapshot is null)
        {
            return ApplicationResult.Failure(MapTillContextError(tillContext.ErrorCode));
        }

        var deleteResult = await _repository.DeleteInspectionMediaStagingAsync(
            context.TenantId,
            tillContext.Snapshot.OutletId,
            mediaId,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        if (!deleteResult.Deleted)
        {
            return ApplicationResult.Failure(
                MapInspectionMediaError(deleteResult.ErrorCode ?? "pos_returns.media_not_found"));
        }

        if (!string.IsNullOrWhiteSpace(deleteResult.StorageKey))
        {
            await _inspectionMediaStorage.DeleteAsync(deleteResult.StorageKey, cancellationToken);
        }

        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<PosReturnInspectionMediaContentDto>> GetInspectionMediaAsync(
        TenantRequestContext context,
        Guid mediaId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanViewReturns(context) || !ReturnsAccess.CanCreateReturn(context))
        {
            return ApplicationResult<PosReturnInspectionMediaContentDto>.Failure(
                new ApplicationError(
                    "pos_returns.permission_denied",
                    "You do not have permission to view inspection media."));
        }

        if (mediaId == Guid.Empty)
        {
            return ApplicationResult<PosReturnInspectionMediaContentDto>.Failure(
                new ApplicationError("pos_returns.invalid_media_id", "Media id is required."));
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<PosReturnInspectionMediaContentDto>.Failure(
                new ApplicationError("pos_returns.invalid_device_id", "Device id is required."));
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess || tillContext.Snapshot is null)
        {
            return ApplicationResult<PosReturnInspectionMediaContentDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var media = await _repository.GetInspectionMediaStagingAsync(
            context.TenantId,
            tillContext.Snapshot.OutletId,
            mediaId,
            cancellationToken);
        if (media is null)
        {
            // Cross-outlet / missing media: safe 404 — do not reveal existence elsewhere.
            return ApplicationResult<PosReturnInspectionMediaContentDto>.Failure(
                new ApplicationError("pos_returns.media_not_found", "Inspection media was not found."));
        }

        var saleOk = await _repository.SaleBelongsToOutletAsync(
            context.TenantId, tillContext.Snapshot.OutletId, media.SaleId, cancellationToken);
        if (!saleOk)
        {
            return ApplicationResult<PosReturnInspectionMediaContentDto>.Failure(
                new ApplicationError("pos_returns.media_not_found", "Inspection media was not found."));
        }

        Stream? stream;
        try
        {
            stream = await _inspectionMediaStorage.OpenReadAsync(media.StorageKey, cancellationToken);
        }
        catch (Exception)
        {
            return ApplicationResult<PosReturnInspectionMediaContentDto>.Failure(
                new ApplicationError(
                    "pos_returns.inspection_media_storage_failed",
                    "Inspection photo storage failed."));
        }

        if (stream is null)
        {
            return ApplicationResult<PosReturnInspectionMediaContentDto>.Failure(
                new ApplicationError("pos_returns.media_not_found", "Inspection media file is unavailable."));
        }

        return ApplicationResult<PosReturnInspectionMediaContentDto>.Success(
            new PosReturnInspectionMediaContentDto(
                stream,
                media.ContentType,
                media.FileName));
    }

    public async Task<ApplicationResult<PosReturnSaleSearchPageDto>> SearchOriginalSalesAsync(
        TenantRequestContext context,
        Guid? deviceId,
        string? searchType,
        string? search,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? paymentMethodCode,
        decimal? minAmount,
        decimal? maxAmount,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanViewReturns(context))
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

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return Failure(
                "pos_returns.invalid_date_range",
                "From date cannot be later than to date.");
        }

        if (minAmount < 0 || maxAmount < 0 ||
            (minAmount.HasValue && maxAmount.HasValue && minAmount.Value > maxAmount.Value))
        {
            return Failure(
                "pos_returns.invalid_amount_range",
                "Amounts must be non-negative and minimum amount cannot exceed maximum amount.");
        }

        var normalizedPaymentMethodCode = string.IsNullOrWhiteSpace(paymentMethodCode)
            ? null
            : paymentMethodCode.Trim().ToUpperInvariant();
        if (normalizedPaymentMethodCode is not null &&
            !await _repository.IsActivePaymentMethodAsync(
                context.TenantId,
                normalizedPaymentMethodCode,
                cancellationToken))
        {
            return Failure(
                "pos_returns.payment_method_not_found",
                "The selected payment method is not available.");
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);
        if (!tillContext.IsSuccess || tillContext.Snapshot is null)
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
            tillContext.Snapshot.OutletId,
            normalizedSearchType,
            normalizedSearch,
            new PosReturnSaleSearchFilterDto(
                fromDate,
                toDate,
                normalizedPaymentMethodCode,
                minAmount,
                maxAmount),
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

    private static bool HasValidInspectionImageSignature(Stream stream, string contentType)
    {
        if (!stream.CanSeek) return false;
        var position = stream.Position;
        try
        {
            Span<byte> bytes = stackalloc byte[12];
            var read = stream.Read(bytes);
            return contentType switch
            {
                "image/jpeg" or "image/jpg" => read >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
                "image/png" => read >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E &&
                    bytes[3] == 0x47 && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A,
                "image/webp" => read >= 12 && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' &&
                    bytes[2] == (byte)'F' && bytes[3] == (byte)'F' && bytes[8] == (byte)'W' &&
                    bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P',
                _ => false
            };
        }
        finally { stream.Position = position; }
    }

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

    private static ApplicationResult<PosReturnReasonsValidateResponseDto> ReasonsValidateFailure(
        string code,
        string message) =>
        ApplicationResult<PosReturnReasonsValidateResponseDto>.Failure(
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

    private static PosReturnResolutionResponseDto BuildResolutionResponse(
        TenantRequestContext context,
        PosReturnResolutionRecord resolution)
    {
        var refundAllowed = ReturnsAccess.CanSaveRefundResolution(context);
        var exchangeAllowed = ReturnsAccess.CanSaveExchangeResolution(context);
        var options = new[]
        {
            new PosReturnResolutionOptionDto(
                "REFUND",
                refundAllowed,
                refundAllowed ? null : "permission_denied"),
            new PosReturnResolutionOptionDto(
                "EXCHANGE",
                exchangeAllowed,
                exchangeAllowed ? null : "permission_denied"),
        };

        var selectedPermitted = resolution.ResolutionType switch
        {
            "REFUND" => refundAllowed,
            "EXCHANGE" => exchangeAllowed,
            null => true,
            _ => false,
        };
        var nextStep = resolution.ResolutionType switch
        {
            "REFUND" when refundAllowed => "REFUND_DETAILS",
            "EXCHANGE" when exchangeAllowed => "EXCHANGE",
            _ => "CHOOSE_OPTION",
        };

        return new PosReturnResolutionResponseDto(
            resolution.SaleId,
            resolution.DraftId,
            selectedPermitted ? resolution.ResolutionType : null,
            selectedPermitted ? resolution.ResolutionSelectedAt : null,
            selectedPermitted ? resolution.ResolutionSelectedByTenantUserId : null,
            resolution.Version,
            resolution.DraftStatus,
            resolution.ExpiresAt,
            options,
            refundAllowed,
            exchangeAllowed,
            resolution.RequiresManagerApproval,
            resolution.RequiresInspection,
            resolution.CanChange && selectedPermitted,
            nextStep);
    }

    private static ApplicationResult<PosReturnResolutionResponseDto> ResolutionFailure(
        string code,
        string message) =>
        ApplicationResult<PosReturnResolutionResponseDto>.Failure(
            new ApplicationError(code, message));

    private static ApplicationResult<PosReturnRefundMethodsResponseDto> RefundMethodsFailure(
        string code,
        string message) =>
        ApplicationResult<PosReturnRefundMethodsResponseDto>.Failure(
            new ApplicationError(code, message));

    private static ApplicationResult<PosReturnRefundMethodSaveResponseDto> RefundMethodSaveFailure(
        string code,
        string message) =>
        ApplicationResult<PosReturnRefundMethodSaveResponseDto>.Failure(
            new ApplicationError(code, message));

    private ApplicationResult<PosReturnRefundMethodsResponseDto> MapRefundMethodsResult(
        PosReturnRefundMethodsRepositoryResult result) =>
        result.ErrorCode switch
        {
            null when result.Methods is not null =>
                ApplicationResult<PosReturnRefundMethodsResponseDto>.Success(result.Methods),
            "draft_not_found" => RefundMethodsFailure(
                "pos_returns.inspection_draft_not_found",
                "No validated inspection draft was found for this sale."),
            "inspection_not_validated" => RefundMethodsFailure(
                "pos_returns.inspection_not_validated",
                "The inspection draft must be validated before loading refund methods."),
            "invalid_resolution" => RefundMethodsFailure(
                "pos_returns.invalid_resolution",
                "A saved Refund resolution is required before loading refund methods."),
            "stale" => RefundMethodsFailure(
                "pos_returns.concurrency_conflict",
                "The return workflow has already been completed or cancelled."),
            _ => RefundMethodsFailure(
                "pos_returns.refund_methods_failed",
                "Refund methods could not be loaded.")
        };

    private async Task<bool> RequiresRefundApprovalAsync(
        Guid tenantId,
        Guid saleId,
        string reasonCode,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var reasons = await _repository.GetActiveReturnReasonsAsync(tenantId, cancellationToken);
        if (reasons.Any(x =>
                string.Equals(x.Code, reasonCode, StringComparison.OrdinalIgnoreCase) &&
                x.RequiresManagerApproval))
        {
            return true;
        }

        if (!_repository.SupportsInspectionDrafts)
        {
            return false;
        }

        var draft = await _repository.GetInspectionDraftBySaleAsync(
            tenantId, outletId, saleId, cancellationToken);
        if (draft is null || draft.Lines.Count == 0)
        {
            return false;
        }

        var conditions = await _repository.GetActiveInspectionConditionsAsync(tenantId, cancellationToken);
        var approvalCodes = conditions
            .Where(x => x.RequiresApproval)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return draft.Lines.Any(line =>
            approvalCodes.Contains(line.ConditionCode));
    }

    private static string BuildRefundImpactMessage(PosReturnInspectionConditionDto condition) =>
        condition.RefundImpact.ToUpperInvariant() switch
        {
            "FULL_DENIAL" =>
                $"Items marked as {condition.DisplayName} may be ineligible for a full refund.",
            "PARTIAL" =>
                $"Items marked as {condition.DisplayName} may receive a partial refund after review.",
            _ => condition.Description ?? string.Empty,
        };

    private static string BuildInspectionMediaUrl(Guid mediaId) =>
        $"/api/v1/pos/returns/inspection/media/{mediaId:D}";

    private static ApplicationError MapInspectionDraftError(string? errorCode) => errorCode switch
    {
        "pos_returns.inspection_draft_conflict" => new ApplicationError(
            "pos_returns.inspection_draft_conflict",
            "The inspection draft was updated elsewhere. Reload and try again."),
        "pos_returns.inspection_draft_expired" => new ApplicationError(
            "pos_returns.inspection_draft_expired",
            "The inspection draft has expired. Restart inspection for this sale."),
        "pos_returns.inspection_draft_consumed" => new ApplicationError(
            "pos_returns.inspection_draft_consumed",
            "The inspection draft has already been consumed."),
        "pos_returns.sale_not_found" => new ApplicationError(
            "pos_returns.sale_not_found",
            "The original sale could not be found."),
        "pos_returns.invalid_sale_line_id" => new ApplicationError(
            "pos_returns.invalid_sale_line_id",
            "One or more selected sale lines could not be found on the original sale."),
        _ => new ApplicationError(
            "pos_returns.inspection_operation_failed",
            "The inspection draft could not be saved."),
    };

    private static ApplicationError MapInspectionMediaError(string? errorCode) => errorCode switch
    {
        "pos_returns.sale_line_not_found" or "pos_returns.sale_not_found" => new ApplicationError(
            "pos_returns.sale_not_found",
            "The selected sale line could not be found."),
        "pos_returns.inspection_media_limit_reached" => new ApplicationError(
            "pos_returns.inspection_media_limit_reached",
            "The maximum number of inspection photos has been reached for this item."),
        "pos_returns.inspection_draft_expired" => new ApplicationError(
            "pos_returns.inspection_draft_expired",
            "The inspection draft has expired. Restart inspection for this sale."),
        "pos_returns.inspection_draft_consumed" => new ApplicationError(
            "pos_returns.inspection_draft_consumed",
            "The inspection draft has already been consumed."),
        "pos_returns.inspection_media_consumed" => new ApplicationError(
            "pos_returns.inspection_media_consumed",
            "Finalized inspection media cannot be deleted."),
        "pos_returns.media_not_found" => new ApplicationError(
            "pos_returns.media_not_found",
            "Inspection media was not found."),
        "pos_returns.inspection_media_storage_failed" => new ApplicationError(
            "pos_returns.inspection_media_storage_failed",
            "Inspection photo storage failed."),
        _ => new ApplicationError(
            "pos_returns.inspection_media_failed",
            "Inspection photo could not be saved."),
    };

    public async Task<ApplicationResult<PosExchangeProductsResponseDto>> SearchExchangeProductsAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanProcessExchange(context))
        {
            return ExchangeProductsFailure(
                "pos_returns.permission_denied",
                "You do not have permission to search exchange products.");
        }

        if (saleId == Guid.Empty)
        {
            return ExchangeProductsFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ExchangeProductsFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId, deviceId.Value, cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
        {
            return ApplicationResult<PosExchangeProductsResponseDto>.Failure(
                MapTillContextError(till.ErrorCode));
        }

        var resolution = await _repository.GetResolutionAsync(
            context.TenantId, till.Snapshot.OutletId, saleId, cancellationToken);
        if (resolution is null ||
            !string.Equals(resolution.ResolutionType, "EXCHANGE", StringComparison.OrdinalIgnoreCase))
        {
            return ExchangeProductsFailure(
                "pos_returns.invalid_resolution",
                "A saved Exchange resolution is required before searching replacement products.");
        }

        var currency = await _repository.GetSaleCurrencyCodeAsync(
            context.TenantId, saleId, cancellationToken) ?? string.Empty;

        var catalogResult = await _productCatalogRepository.ListProductsAsync(
            context.TenantId,
            deviceId.Value,
            categoryId: null,
            search,
            cancellationToken,
            till.Snapshot.OutletId);
        if (!catalogResult.IsSuccess)
        {
            return ExchangeProductsFailure(
                catalogResult.ErrorCode ?? "pos_returns.exchange_products_failed",
                "Exchange products could not be loaded.");
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);
        var allItems = catalogResult.Products
            .Select(product => MapExchangeProduct(product, currency))
            .ToList();
        var total = allItems.Count;
        var pageItems = allItems
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return ApplicationResult<PosExchangeProductsResponseDto>.Success(
            new PosExchangeProductsResponseDto(
                pageItems,
                normalizedPage,
                normalizedPageSize,
                total,
                currency));
    }

    public async Task<ApplicationResult<PosExchangeReplacementSaveResponseDto>> SaveExchangeReplacementAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosExchangeReplacementSaveRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanProcessExchange(context))
        {
            return ExchangeReplacementFailure(
                "pos_returns.permission_denied",
                "You do not have permission to save exchange replacement items.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return ExchangeReplacementFailure(
                "pos_returns.invalid_replacement",
                "At least one replacement item is required.");
        }

        if (saleId == Guid.Empty)
        {
            return ExchangeReplacementFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ExchangeReplacementFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        if (request.ExpectedVersion < 1)
        {
            return ExchangeReplacementFailure(
                "pos_returns.invalid_expected_version",
                "A valid expectedVersion is required to save exchange replacement items.");
        }

        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId, deviceId.Value, cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
        {
            return ApplicationResult<PosExchangeReplacementSaveResponseDto>.Failure(
                MapTillContextError(till.ErrorCode));
        }

        var result = await _repository.SaveExchangeReplacementAsync(
            context.TenantId,
            till.Snapshot.OutletId,
            saleId,
            context.UserId,
            request.Items,
            request.ExpectedVersion,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return MapExchangeReplacementSaveResult(result);
    }

    public async Task<ApplicationResult<PosExchangeReplacementSaveResponseDto>> GetExchangeReplacementAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanProcessExchange(context))
        {
            return ExchangeReplacementFailure(
                "pos_returns.permission_denied",
                "You do not have permission to view exchange replacement items.");
        }

        if (saleId == Guid.Empty)
        {
            return ExchangeReplacementFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ExchangeReplacementFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId, deviceId.Value, cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
        {
            return ApplicationResult<PosExchangeReplacementSaveResponseDto>.Failure(
                MapTillContextError(till.ErrorCode));
        }

        var result = await _repository.GetExchangeReplacementAsync(
            context.TenantId, till.Snapshot.OutletId, saleId, cancellationToken);

        return result.ErrorCode switch
        {
            null when result.Replacement is not null =>
                ApplicationResult<PosExchangeReplacementSaveResponseDto>.Success(result.Replacement),
            "replacement_not_found" => ExchangeReplacementFailure(
                "pos_returns.replacement_not_found",
                "No saved exchange replacement was found for this sale."),
            "draft_not_found" => ExchangeReplacementFailure(
                "pos_returns.inspection_draft_not_found",
                "No validated inspection draft was found for this sale."),
            "inspection_not_validated" => ExchangeReplacementFailure(
                "pos_returns.inspection_not_validated",
                "The inspection draft must be validated before selecting replacement items."),
            "invalid_resolution" => ExchangeReplacementFailure(
                "pos_returns.invalid_resolution",
                "A saved Exchange resolution is required before selecting replacement items."),
            "stale" => ExchangeReplacementFailure(
                "pos_returns.concurrency_conflict",
                "The return workflow has already been completed or cancelled."),
            "expired" => ExchangeReplacementFailure(
                "pos_returns.inspection_draft_expired",
                "The inspection draft has expired. Restart inspection for this sale."),
            "conflict" => ExchangeReplacementFailure(
                "pos_returns.inspection_draft_conflict",
                "The replacement draft changed. Reload and try again."),
            _ => ExchangeReplacementFailure(
                "pos_returns.replacement_save_failed",
                "Exchange replacement could not be loaded."),
        };
    }

    public async Task<ApplicationResult<PosExchangePreviewDto>> PreviewExchangeAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosExchangePreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ReturnsAccess.CanProcessExchange(context))
        {
            return ExchangePreviewFailure(
                "pos_returns.permission_denied",
                "You do not have permission to preview an exchange.");
        }

        if (saleId == Guid.Empty)
        {
            return ExchangePreviewFailure("pos_returns.invalid_sale_id", "Sale id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ExchangePreviewFailure("pos_returns.invalid_device_id", "Device id is required.");
        }

        var reasonCode = request.ReasonCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            return ExchangePreviewFailure(
                "pos_returns.invalid_reason_code",
                "A valid return reason code is required.");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return ExchangePreviewFailure(
                "pos_returns.invalid_lines",
                "At least one return line is required.");
        }

        var till = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId, deviceId.Value, cancellationToken);
        if (!till.IsSuccess || till.Snapshot is null)
        {
            return ApplicationResult<PosExchangePreviewDto>.Failure(
                MapTillContextError(till.ErrorCode));
        }

        var result = await _repository.PreviewExchangeAsync(
            context.TenantId,
            till.Snapshot.OutletId,
            saleId,
            reasonCode,
            request.Lines,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return result.ErrorCode switch
        {
            null when result.Preview is not null =>
                ApplicationResult<PosExchangePreviewDto>.Success(result.Preview),
            "replacement_not_found" => ExchangePreviewFailure(
                "pos_returns.replacement_not_found",
                "Save a replacement item before previewing the exchange."),
            "insufficient_stock" or "insufficient_outlet_stock" => ExchangePreviewFailure(
                "pos_returns.insufficient_outlet_stock",
                "Replacement stock is no longer sufficient at this outlet."),
            "draft_not_found" => ExchangePreviewFailure(
                "pos_returns.inspection_draft_not_found",
                "No validated inspection draft was found for this sale."),
            "inspection_not_validated" => ExchangePreviewFailure(
                "pos_returns.inspection_not_validated",
                "The inspection draft must be validated before previewing the exchange."),
            "invalid_resolution" => ExchangePreviewFailure(
                "pos_returns.invalid_resolution",
                "A saved Exchange resolution is required before previewing the exchange."),
            "stale" => ExchangePreviewFailure(
                "pos_returns.concurrency_conflict",
                "The return workflow has already been completed or cancelled."),
            "expired" => ExchangePreviewFailure(
                "pos_returns.inspection_draft_expired",
                "The inspection draft has expired. Restart inspection for this sale."),
            "conflict" => ExchangePreviewFailure(
                "pos_returns.inspection_draft_conflict",
                "The replacement draft changed. Reload and try again."),
            _ => ExchangePreviewFailure(
                result.ErrorCode ?? "pos_returns.exchange_preview_failed",
                "Exchange preview could not be calculated."),
        };
    }

    private static PosExchangeProductDto MapExchangeProduct(
        PosProductSummaryResponseDto product,
        string currencyCode)
    {
        var isOutOfStock =
            string.Equals(product.StockStatus, "OutOfStock", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(product.StockStatus, "out_of_stock", StringComparison.OrdinalIgnoreCase) ||
            (product.AvailableQuantity.HasValue && product.AvailableQuantity.Value <= 0);
        var enabled = !isOutOfStock;
        return new PosExchangeProductDto(
            product.Id,
            product.VariantId,
            product.Name,
            Sku: product.Sku ?? string.Empty,
            Barcode: product.Barcode,
            VariantDisplayName: product.HasVariants ? null : product.CategoryName,
            product.ImageStorageKey,
            isOutOfStock ? "OutOfStock" : product.StockStatus,
            product.AvailableQuantity,
            product.BasePrice,
            currencyCode,
            product.HasVariants,
            enabled,
            enabled ? null : "This product is out of stock for exchange.");
    }

    private static ApplicationResult<PosExchangeReplacementSaveResponseDto> MapExchangeReplacementSaveResult(
        PosExchangeReplacementSaveRepositoryResult result) =>
        result.ErrorCode switch
        {
            null when result.Replacement is not null =>
                ApplicationResult<PosExchangeReplacementSaveResponseDto>.Success(result.Replacement),
            "invalid_replacement" => ExchangeReplacementFailure(
                "pos_returns.invalid_replacement",
                "The replacement selection is invalid."),
            "sale_line_not_found" => ExchangeReplacementFailure(
                "pos_returns.sale_line_not_found",
                "The selected sale line could not be found."),
            "variant_not_found" or "product_not_found" => ExchangeReplacementFailure(
                "pos_returns.product_not_found",
                "The selected replacement product could not be found."),
            "product_not_sellable" => ExchangeReplacementFailure(
                "pos_returns.product_not_sellable",
                "The selected replacement product is not sellable."),
            "price_not_found" => ExchangeReplacementFailure(
                "pos_returns.price_not_found",
                "A price could not be resolved for the replacement product."),
            "insufficient_stock" or "insufficient_outlet_stock" => ExchangeReplacementFailure(
                "pos_returns.insufficient_outlet_stock",
                "Insufficient outlet stock is available for the selected replacement."),
            "draft_not_found" => ExchangeReplacementFailure(
                "pos_returns.inspection_draft_not_found",
                "No validated inspection draft was found for this sale."),
            "inspection_not_validated" => ExchangeReplacementFailure(
                "pos_returns.inspection_not_validated",
                "The inspection draft must be validated before saving replacement items."),
            "invalid_resolution" => ExchangeReplacementFailure(
                "pos_returns.invalid_resolution",
                "A saved Exchange resolution is required before saving replacement items."),
            "stale" => ExchangeReplacementFailure(
                "pos_returns.concurrency_conflict",
                "The return workflow has already been completed or cancelled."),
            "expired" => ExchangeReplacementFailure(
                "pos_returns.inspection_draft_expired",
                "The inspection draft has expired. Restart inspection for this sale."),
            "conflict" => ExchangeReplacementFailure(
                "pos_returns.inspection_draft_conflict",
                "The replacement draft changed. Reload and try again."),
            _ => ExchangeReplacementFailure(
                "pos_returns.replacement_save_failed",
                "Exchange replacement could not be saved."),
        };

    private static ApplicationResult<PosExchangeProductsResponseDto> ExchangeProductsFailure(
        string code,
        string message) =>
        ApplicationResult<PosExchangeProductsResponseDto>.Failure(new ApplicationError(code, message));

    private static ApplicationResult<PosExchangeReplacementSaveResponseDto> ExchangeReplacementFailure(
        string code,
        string message) =>
        ApplicationResult<PosExchangeReplacementSaveResponseDto>.Failure(new ApplicationError(code, message));

    private static ApplicationResult<PosExchangePreviewDto> ExchangePreviewFailure(
        string code,
        string message) =>
        ApplicationResult<PosExchangePreviewDto>.Failure(new ApplicationError(code, message));
}
