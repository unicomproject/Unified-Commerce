using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;

namespace E_POS.Application.Modules.Tenant.Reports.Contracts;

public interface ITenantAdminReportsService
{
    Task<ApplicationResult<ReportFilterOptionsResponse>> GetFilterOptionsAsync(
        TenantRequestContext context,
        ReportFilterOptionsRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ReportResultDto>> GetDashboardAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ReportResultDto>> GetSalesAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ReportResultDto>> GetStockAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ReportResultDto>> GetOutletsAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SalesTransactionDetailDto>> GetSalesTransactionDetailAsync(
        TenantRequestContext context,
        Guid orderId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ReportExportDto>> CreateExportAsync(
        TenantRequestContext context,
        ReportExportRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ReportExportDto>> GetExportAsync(
        TenantRequestContext context,
        Guid jobId,
        CancellationToken cancellationToken);
}
