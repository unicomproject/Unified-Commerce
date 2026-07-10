using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosReceiptService
{
    Task<ApplicationResult<PosReceiptPrintResponseDto>> RecordPrintAsync(
        TenantRequestContext context,
        Guid saleId,
        PosReceiptPrintRequestDto request,
        CancellationToken cancellationToken);
}
