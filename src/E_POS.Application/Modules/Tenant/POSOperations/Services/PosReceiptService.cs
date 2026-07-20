using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public sealed class PosReceiptService : IPosReceiptService
{
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

    public async Task<ApplicationResult<PosReceiptPrintResponseDto>> RecordPrintAsync(
        TenantRequestContext context,
        Guid saleId,
        PosReceiptPrintRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ReceiptPermissions.Print))
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
