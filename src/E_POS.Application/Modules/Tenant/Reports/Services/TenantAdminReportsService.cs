using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Reports.Constants;

namespace E_POS.Application.Modules.Tenant.Reports.Services;

public sealed class TenantAdminReportsService : ITenantAdminReportsService
{
    private static readonly ApplicationError InvalidContext = new(
        "reports.invalid_tenant_context",
        "Invalid tenant context.");

    private static readonly ApplicationError PermissionDenied = new(
        "reports.permission_denied",
        "You do not have permission to view this report.");

    private static readonly ApplicationError ValidationFailed = new(
        "reports.validation_failed",
        "Report query is invalid.");

    private static readonly ApplicationError NotFound = new(
        "reports.not_found",
        "Report data was not found.");

    private static readonly IReadOnlySet<string> SalesSections = new HashSet<string>(StringComparer.Ordinal)
    {
        "summary", "transactions", "products", "categories", "payments", "tax",
        "discounts", "returns", "cashiers", "daily"
    };

    private static readonly IReadOnlySet<string> StockSections = new HashSet<string>(StringComparer.Ordinal)
    {
        "current", "low-stock", "out-of-stock", "batch-expiry", "movements", "valuation"
    };

    private static readonly IReadOnlySet<string> OutletSections = new HashSet<string>(StringComparer.Ordinal)
    {
        "performance", "tills", "cashiers"
    };

    private static readonly IReadOnlySet<string> ExportFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "csv", "xlsx", "pdf"
    };

    private static readonly Dictionary<Guid, ReportExportDto> ExportJobs = new();

    private readonly ITenantAdminReportsRepository _repository;

    public TenantAdminReportsService(ITenantAdminReportsRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApplicationResult<ReportFilterOptionsResponse>> GetFilterOptionsAsync(
        TenantRequestContext context,
        ReportFilterOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportFilterOptionsResponse>.Failure(error);
        if (request.Page < 1 || request.PageSize is not (25 or 50 or 100))
        {
            return ApplicationResult<ReportFilterOptionsResponse>.Failure(ValidationFailed);
        }

        return ApplicationResult<ReportFilterOptionsResponse>.Success(
            await _repository.GetFilterOptionsAsync(context, request, cancellationToken));
    }

    public async Task<ApplicationResult<ReportResultDto>> GetDashboardAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportResultDto>.Failure(error);
        if (!HasAnyPermission(context, TenantAdminReportPermissions.DashboardView, TenantAdminReportPermissions.SalesView, "reports.sales.view"))
        {
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        }

        return ApplicationResult<ReportResultDto>.Success(
            await _repository.GetDashboardAsync(context, request, cancellationToken));
    }

    public async Task<ApplicationResult<ReportResultDto>> GetSalesAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportResultDto>.Failure(error);
        var section = NormalizeSection(request.Section);
        if (!SalesSections.Contains(section) || request.Page < 1 || request.PageSize is not (25 or 50 or 100))
        {
            return ApplicationResult<ReportResultDto>.Failure(ValidationFailed);
        }

        if (!CanViewSalesSection(context, section))
        {
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        }

        return ApplicationResult<ReportResultDto>.Success(
            await _repository.GetSalesAsync(context, request with { Section = section }, cancellationToken));
    }

    public async Task<ApplicationResult<SalesTransactionDetailDto>> GetSalesTransactionDetailAsync(
        TenantRequestContext context,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<SalesTransactionDetailDto>.Failure(error);
        if (!CanViewSalesSection(context, "transactions"))
        {
            return ApplicationResult<SalesTransactionDetailDto>.Failure(PermissionDenied);
        }

        var detail = await _repository.GetSalesTransactionDetailAsync(context, orderId, cancellationToken);
        return detail is null
            ? ApplicationResult<SalesTransactionDetailDto>.Failure(NotFound)
            : ApplicationResult<SalesTransactionDetailDto>.Success(detail);
    }

    public async Task<ApplicationResult<ReportResultDto>> GetStockAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportResultDto>.Failure(error);
        var section = NormalizeSection(request.Section) == "summary" ? "current" : NormalizeSection(request.Section);
        if (!StockSections.Contains(section) || request.Page < 1 || request.PageSize is not (25 or 50 or 100))
        {
            return ApplicationResult<ReportResultDto>.Failure(ValidationFailed);
        }

        if (!CanViewStockSection(context, section))
        {
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        }

        return ApplicationResult<ReportResultDto>.Success(
            await _repository.GetStockAsync(context, request with { Section = section }, cancellationToken));
    }

    public async Task<ApplicationResult<ReportResultDto>> GetOutletsAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportResultDto>.Failure(error);
        var section = NormalizeSection(request.Section) == "summary" ? "performance" : NormalizeSection(request.Section);
        if (!OutletSections.Contains(section) || request.Page < 1 || request.PageSize is not (25 or 50 or 100))
        {
            return ApplicationResult<ReportResultDto>.Failure(ValidationFailed);
        }

        if (!CanViewOutletSection(context, section))
        {
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        }

        return ApplicationResult<ReportResultDto>.Success(
            await _repository.GetOutletsAsync(context, request with { Section = section }, cancellationToken));
    }

    public Task<ApplicationResult<ReportExportDto>> CreateExportAsync(
        TenantRequestContext context,
        ReportExportRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return Task.FromResult(ApplicationResult<ReportExportDto>.Failure(error));
        if (!HasAnyPermission(context, TenantAdminReportPermissions.Export) ||
            !ExportFormats.Contains(request.Format) ||
            request.Filters.PageSize is not (25 or 50 or 100))
        {
            return Task.FromResult(ApplicationResult<ReportExportDto>.Failure(PermissionDenied));
        }

        if (!CanViewExportTarget(context, request.ReportType, request.Section))
        {
            return Task.FromResult(ApplicationResult<ReportExportDto>.Failure(PermissionDenied));
        }

        var now = DateTimeOffset.UtcNow;
        var job = new ReportExportDto(
            Guid.NewGuid(),
            request.ReportType.Trim().ToLowerInvariant(),
            request.Section.Trim(),
            request.Format.Trim().ToUpperInvariant(),
            "COMPLETED",
            now,
            now,
            BuildSafeFileName(request.ReportType, request.Section, request.Format),
            null,
            now.AddMinutes(15),
            "Release 1 export job metadata is created; binary file storage/download is pending.");
        lock (ExportJobs)
        {
            ExportJobs[job.JobId] = job;
        }

        return Task.FromResult(ApplicationResult<ReportExportDto>.Success(job));
    }

    public Task<ApplicationResult<ReportExportDto>> GetExportAsync(
        TenantRequestContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return Task.FromResult(ApplicationResult<ReportExportDto>.Failure(error));
        lock (ExportJobs)
        {
            return Task.FromResult(ExportJobs.TryGetValue(jobId, out var job)
                ? ApplicationResult<ReportExportDto>.Success(job)
                : ApplicationResult<ReportExportDto>.Failure(NotFound));
        }
    }

    private static ApplicationError? ValidateCommonAccess(TenantRequestContext context) =>
        context.TenantId == Guid.Empty || context.UserId == Guid.Empty ? InvalidContext : null;

    private static string NormalizeSection(string? section) =>
        string.IsNullOrWhiteSpace(section) ? "summary" : section.Trim();

    private static bool CanViewSalesSection(TenantRequestContext context, string section)
    {
        var sectionPermission = section switch
        {
            "products" or "categories" => TenantAdminReportPermissions.ProductsView,
            "payments" => TenantAdminReportPermissions.PaymentsView,
            "tax" => TenantAdminReportPermissions.TaxView,
            "discounts" => TenantAdminReportPermissions.DiscountsView,
            "returns" => TenantAdminReportPermissions.ReturnsView,
            "cashiers" => TenantAdminReportPermissions.CashiersView,
            "daily" => TenantAdminReportPermissions.DailySalesView,
            _ => TenantAdminReportPermissions.SalesView
        };

        return HasAnyPermission(context, sectionPermission, TenantAdminReportPermissions.SalesView, "reports.sales.view");
    }

    private static bool CanViewStockSection(TenantRequestContext context, string section)
    {
        var sectionPermission = section switch
        {
            "batch-expiry" => TenantAdminStockPermissions.ExpiryView,
            "movements" => TenantAdminStockPermissions.MovementsView,
            "valuation" => TenantAdminStockPermissions.ValueView,
            _ => TenantAdminStockPermissions.View
        };

        return HasAnyPermission(context, sectionPermission, TenantAdminStockPermissions.View, TenantAdminStockPermissions.LegacyInventoryView);
    }

    private static bool CanViewOutletSection(TenantRequestContext context, string section)
    {
        var sectionPermission = section switch
        {
            "tills" => TenantAdminReportPermissions.TillsView,
            "cashiers" => TenantAdminReportPermissions.CashiersView,
            _ => TenantAdminReportPermissions.OutletsView
        };

        return HasAnyPermission(context, sectionPermission, TenantAdminReportPermissions.OutletsView, "tenant.outlets.revenue.view");
    }

    private static bool CanViewExportTarget(TenantRequestContext context, string reportType, string section)
    {
        var normalizedType = reportType.Trim().ToLowerInvariant();
        return normalizedType switch
        {
            "sales" => CanViewSalesSection(context, section),
            "stock" => CanViewStockSection(context, section),
            "outlets" => CanViewOutletSection(context, section),
            _ => false
        };
    }

    private static string BuildSafeFileName(string reportType, string section, string format)
    {
        static string Clean(string value) => new(value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) || ch == '-' ? ch : '-').ToArray());
        return $"{Clean(reportType)}-{Clean(section)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.{Clean(format)}";
    }

    private static bool HasAnyPermission(TenantRequestContext context, params string[] permissions) =>
        permissions.Any(context.HasPermission);
}
