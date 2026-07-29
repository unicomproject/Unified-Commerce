using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public sealed class PosReceiptService : IPosReceiptService
{
    private static readonly HashSet<string> ReprintReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "CUSTOMER_REQUEST",
        "ORIGINAL_DAMAGED",
        "ORIGINAL_LOST",
        "PRINTER_FAILURE",
        "OTHER"
    };

    private static readonly ApplicationError PermissionDenied = new(
        "pos_receipts.permission_denied",
        "You do not have permission to print receipts.");

    private static readonly ApplicationError InvalidSaleId = new(
        "pos_receipts.invalid_sale_id",
        "Sale id is required.");

    private static readonly ApplicationError InvalidCopies = new(
        "pos_receipts.invalid_copies",
        "At least one receipt copy is required.");

    private static readonly ApplicationError ReceiptNotFound = new(
        "pos_receipts.receipt_not_found",
        "Receipt could not be found for the sale.");

    private static readonly ApplicationError InvalidPrintStatus = new(
        "pos_receipts.invalid_print_status",
        "Print status is not supported.");

    private readonly IPosReceiptRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PosReceiptService(
        IPosReceiptRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PosReceiptSearchResponseDto>> SearchAsync(
        TenantRequestContext context,
        PosReceiptSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ReceiptPermissions.View))
        {
            return ApplicationResult<PosReceiptSearchResponseDto>.Failure(
                new ApplicationError("pos_receipts.permission_denied", "You do not have permission to view receipts."));
        }

        if (request.PageNumber < 1 || request.PageSize is < 1 or > 100 ||
            request.MinAmount < 0 || request.MaxAmount < 0 ||
            (request.MinAmount.HasValue && request.MaxAmount.HasValue &&
             request.MinAmount > request.MaxAmount) ||
            (request.DateFrom.HasValue && request.DateTo.HasValue &&
             request.DateFrom > request.DateTo))
        {
            return ApplicationResult<PosReceiptSearchResponseDto>.Failure(
                new ApplicationError("pos_receipts.invalid_search", "Receipt search filters are invalid."));
        }

        return ApplicationResult<PosReceiptSearchResponseDto>.Success(
            await _repository.SearchAsync(context.TenantId, request, cancellationToken));
    }

    public async Task<ApplicationResult<PosReceiptDetailDto>> GetDetailAsync(
        TenantRequestContext context,
        Guid receiptId,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ReceiptPermissions.View))
        {
            return ApplicationResult<PosReceiptDetailDto>.Failure(
                new ApplicationError("pos_receipts.permission_denied", "You do not have permission to view receipts."));
        }

        if (receiptId == Guid.Empty)
        {
            return ApplicationResult<PosReceiptDetailDto>.Failure(
                new ApplicationError("pos_receipts.invalid_receipt_id", "Receipt id is required."));
        }

        var detail = await _repository.GetDetailAsync(context.TenantId, receiptId, cancellationToken);
        return detail is null
            ? ApplicationResult<PosReceiptDetailDto>.Failure(ReceiptNotFound)
            : ApplicationResult<PosReceiptDetailDto>.Success(detail);
    }

    public async Task<ApplicationResult<PosReceiptReprintAuthorizationResponseDto>> AuthorizeReprintAsync(
        TenantRequestContext context,
        Guid receiptId,
        PosReceiptReprintAuthorizationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ReceiptPermissions.Reprint))
        {
            return ApplicationResult<PosReceiptReprintAuthorizationResponseDto>.Failure(
                new ApplicationError("pos_receipts.reprint_permission_denied", "You do not have permission to reprint receipts."));
        }

        var reason = request.ReasonCode?.Trim().ToUpperInvariant();
        var note = request.ReasonNote?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || !ReprintReasons.Contains(reason) ||
            (reason == "OTHER" && string.IsNullOrWhiteSpace(note)) ||
            (note?.Length ?? 0) > 500)
        {
            return ApplicationResult<PosReceiptReprintAuthorizationResponseDto>.Failure(
                new ApplicationError("pos_receipts.invalid_reprint_reason", "Select a valid reprint reason; Other requires a note."));
        }

        var detail = await _repository.GetDetailAsync(context.TenantId, receiptId, cancellationToken);
        if (detail is null)
        {
            return ApplicationResult<PosReceiptReprintAuthorizationResponseDto>.Failure(ReceiptNotFound);
        }

        if (!string.Equals(detail.ReceiptStatus, "ISSUED", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PosReceiptReprintAuthorizationResponseDto>.Failure(
                new ApplicationError("pos_receipts.reprint_not_allowed", "Only issued receipts can be reprinted."));
        }

        var authorization = await _repository.AuthorizeReprintAsync(
            context.TenantId,
            context.UserId,
            receiptId,
            reason,
            note,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        return authorization is null
            ? ApplicationResult<PosReceiptReprintAuthorizationResponseDto>.Failure(ReceiptNotFound)
            : ApplicationResult<PosReceiptReprintAuthorizationResponseDto>.Success(authorization);
    }

    public async Task<ApplicationResult<PosReceiptPrintResponseDto>> RecordPrintAsync(
        TenantRequestContext context,
        Guid saleId,
        PosReceiptPrintRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.IsReprint
            ? !context.HasPermission(ReceiptPermissions.Reprint)
            : !context.HasPermission(ReceiptPermissions.Print))
        {
            return ApplicationResult<PosReceiptPrintResponseDto>.Failure(PermissionDenied);
        }

        if (saleId == Guid.Empty)
        {
            return ApplicationResult<PosReceiptPrintResponseDto>.Failure(InvalidSaleId);
        }

        if (request.Copies < 1)
        {
            return ApplicationResult<PosReceiptPrintResponseDto>.Failure(InvalidCopies);
        }

        if (!TryNormalizePrintStatus(request.Status, out _))
        {
            return ApplicationResult<PosReceiptPrintResponseDto>.Failure(InvalidPrintStatus);
        }

        if (request.IsReprint &&
            (request.ReprintOperationId is null ||
             request.ReprintOperationId == Guid.Empty ||
             string.IsNullOrWhiteSpace(request.ReprintReasonCode)))
        {
            return ApplicationResult<PosReceiptPrintResponseDto>.Failure(
                new ApplicationError("pos_receipts.invalid_reprint_audit", "Authorized reprint operation and reason are required."));
        }

        var result = await _repository.RecordPrintAsync(
            context.TenantId,
            context.UserId,
            saleId,
            request,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccess || result.Print is null)
        {
            return ApplicationResult<PosReceiptPrintResponseDto>.Failure(
                result.ErrorCode switch
                {
                    "pos_receipts.receipt_not_found" => ReceiptNotFound,
                    "pos_receipts.receipt_not_completed" => new ApplicationError(
                        "pos_receipts.receipt_not_completed",
                        "Only completed receipts can be printed."),
                    "pos_receipts.invalid_copies" => InvalidCopies,
                    "pos_receipts.invalid_print_status" => InvalidPrintStatus,
                    "pos_receipts.invalid_receipt_purpose" => new ApplicationError(
                        "pos_receipts.invalid_receipt_purpose",
                        "Receipt print purpose is invalid."),
                    "pos_receipts.invalid_printer_configuration" => new ApplicationError(
                        "pos_receipts.invalid_printer_configuration",
                        "The selected receipt printer configuration is invalid or stale."),
                    "pos_receipts.reprint_not_authorized" => new ApplicationError(
                        "pos_receipts.reprint_not_authorized",
                        "This reprint operation was not authorized."),
                    "pos_receipts.duplicate_reprint_operation" => new ApplicationError(
                        "pos_receipts.duplicate_reprint_operation",
                        "This reprint operation has already been completed."),
                    _ => new ApplicationError(
                        result.ErrorCode ?? "pos_receipts.print_failed",
                        "Receipt print audit could not be recorded.")
                });
        }

        return ApplicationResult<PosReceiptPrintResponseDto>.Success(result.Print);
    }

    public static bool TryNormalizePrintStatus(string? status, out string normalizedStatus)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            normalizedStatus = "PRINTED";
            return true;
        }

        normalizedStatus = status.Trim().ToLowerInvariant() switch
        {
            "success" => "PRINTED",
            "printed" => "PRINTED",
            "failed" => "FAILED",
            "failure" => "FAILED",
            "cancelled" => "CANCELLED",
            "canceled" => "CANCELLED",
            "pending" => "PENDING",
            _ => string.Empty
        };

        return normalizedStatus.Length > 0;
    }
}
