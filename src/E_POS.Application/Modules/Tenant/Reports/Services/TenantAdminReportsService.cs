using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Reports.Constants;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace E_POS.Application.Modules.Tenant.Reports.Services;



public sealed partial class TenantAdminReportsService : ITenantAdminReportsService
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
        "discounts", "returns", "cashiers", "daily", "channels", "payment-transactions", "online", "collections"
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

    

        private readonly ITenantAdminReportsRepository _repository;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IDateTimeProvider _clock;
    private readonly ITenantAdminReportsAuditLogger _auditLogger;
    
    

    private static readonly ConcurrentDictionary<Guid, ExportJobEntry> ExportJobs = new();
    private readonly ILogger<TenantAdminReportsService> _logger;

    public TenantAdminReportsService(ITenantAdminReportsRepository repository, ITenantFeatureEntitlementEvaluator entitlements, IDateTimeProvider clock, ITenantAdminReportsAuditLogger auditLogger,
        ILogger<TenantAdminReportsService>? logger = null)
    {
        _repository = repository;
        _entitlements = entitlements;
        _clock = clock;
        _auditLogger = auditLogger;
        _logger = logger ?? NullLogger<TenantAdminReportsService>.Instance;
    }

    public async Task<ApplicationResult<ReportFilterOptionsResponse>> GetFilterOptionsAsync(
        TenantRequestContext context,
        ReportFilterOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportFilterOptionsResponse>.Failure(error);
        if (!await _repository.CanAccessAsync(context, request.OutletId, null, cancellationToken))
            return ApplicationResult<ReportFilterOptionsResponse>.Failure(PermissionDenied);

        if (!await ReportFeaturePolicy.IsReportsModuleEnabledAsync(_entitlements, context.TenantId, _clock.UtcNow, cancellationToken))
        {
            return ApplicationResult<ReportFilterOptionsResponse>.Failure(PermissionDenied);
        }

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
        if (!await _repository.CanAccessAsync(context, request.OutletId, request.TillId, cancellationToken))
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        if (!await ReportFeaturePolicy.IsSectionEnabledAsync("dashboard", _entitlements, context.TenantId, _clock.UtcNow, cancellationToken))
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        var metadata = await _repository.GetFilterOptionsAsync(context, new(request.OutletId, null, null, null, null, "outlets"), cancellationToken);
        // Each card is shown only when the caller could open that report itself (same entitlement + permission checks).
        var cards = new Dictionary<string, object?>();
        var cardReports = new List<ReportResultDto>();
        foreach (var target in new[] {
            ("sales", "transactions", "PERIOD: SALE POSTING DATE"),
            ("payments", "payments", "PERIOD: PAYMENT EVENT DATE"),
            ("returns", "returns", "PERIOD: RETURN POSTING DATE"),
            ("products", "products", "PERIOD: SALE POSTING DATE"),
            ("online", "online", "PERIOD: ORDER PLACEMENT DATE"),
            ("collections", "collections", "ALL DATES / CURRENT WORKLOAD") })
        {
            if (!await CanViewSalesSectionAsync(context, target.Item2, cancellationToken)) continue;
            var report = await _repository.GetSalesAsync(context, request with { Section = target.Item2, Page = 1, PageSize = 1 }, cancellationToken);
            cards[target.Item1] = new { reportId = report.ReportId, dateBasis = target.Item3, metrics = report.Summary };
            cardReports.Add(report);
        }
        if (await CanViewOutletSectionAsync(context, "tills", cancellationToken))
        {
            var report = await _repository.GetOutletsAsync(context, request with { Section = "tills", Page = 1, PageSize = 1 }, cancellationToken);
            cards["tills"] = new { reportId = report.ReportId, dateBasis = "SESSION BUSINESS DATE", metrics = report.Summary };
            cardReports.Add(report);
        }
        if (await CanViewStockSectionAsync(context, "current", cancellationToken))
        {
            var report = await _repository.GetStockAsync(context, request with { Section = "current", Page = 1, PageSize = 1, From = null, To = null }, cancellationToken);
            cards["stock"] = new { reportId = report.ReportId, dateBasis = "CURRENT AS-OF", metrics = report.Summary };
            cardReports.Add(report);
        }
        if (await CanViewStockSectionAsync(context, "movements", cancellationToken))
        {
            var report = await _repository.GetStockAsync(context, request with { Section = "movements", Page = 1, PageSize = 1 }, cancellationToken);
            cards["stockMovements"] = new { reportId = report.ReportId, dateBasis = "PERIOD: STOCK LEDGER OCCURRED AT", metrics = report.Summary };
            cardReports.Add(report);
        }
        if (cards.Count == 0) return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        var pending = cardReports.Max(x => x.KnownPendingSyncCount ?? 0);
        var now = _clock.UtcNow;
        return ApplicationResult<ReportResultDto>.Success(new("dashboard", metadata.CurrencyCode, metadata.Timezone,
            request.From, request.To, new Dictionary<string, object?>(), cards, [], null, now,
            IsProvisional: pending > 0, KnownPendingSyncCount: pending, Completeness: pending > 0 ? "PROVISIONAL" : "COMPLETE",
            FiltersApplied: request, ReportId: "REP-00", ReportName: "Reports Home", AsOf: now, LastUpdatedAt: now));
    }

    public async Task<ApplicationResult<ReportResultDto>> GetSalesAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportResultDto>.Failure(error);
        if (!await _repository.CanAccessAsync(context, request.OutletId, request.TillId, cancellationToken))
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        var section = NormalizeSection(request.Section);
        if (!SalesSections.Contains(section) || request.Page < 1 || request.PageSize is not (25 or 50 or 100))
        {
            return ApplicationResult<ReportResultDto>.Failure(ValidationFailed);
        }

        if (!await CanViewSalesSectionAsync(context, section, cancellationToken))
        {
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        }

        return await GetSnapshotReportAsync(context, "sales", request with { Section = section }, cancellationToken);
    }

    public async Task<ApplicationResult<SalesTransactionDetailDto>> GetSalesTransactionDetailAsync(
        TenantRequestContext context,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<SalesTransactionDetailDto>.Failure(error);
        if (!await _repository.CanAccessAsync(context, null, null, cancellationToken))
            return ApplicationResult<SalesTransactionDetailDto>.Failure(PermissionDenied);
        if (!await CanViewSalesSectionAsync(context, "transactions", cancellationToken))
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
        if (!await _repository.CanAccessAsync(context, request.OutletId, request.TillId, cancellationToken))
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        var section = NormalizeSection(request.Section) == "summary" ? "current" : NormalizeSection(request.Section);
        if (!StockSections.Contains(section) || request.Page < 1 || request.PageSize is not (25 or 50 or 100))
        {
            return ApplicationResult<ReportResultDto>.Failure(ValidationFailed);
        }

        if (!await CanViewStockSectionAsync(context, section, cancellationToken))
        {
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        }

        return await GetSnapshotReportAsync(context, "stock", request with { Section = section }, cancellationToken);
    }

    public async Task<ApplicationResult<ReportResultDto>> GetOutletsAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportResultDto>.Failure(error);
        if (!await _repository.CanAccessAsync(context, request.OutletId, request.TillId, cancellationToken))
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        var section = NormalizeSection(request.Section) == "summary" ? "performance" : NormalizeSection(request.Section);
        if (!OutletSections.Contains(section) || request.Page < 1 || request.PageSize is not (25 or 50 or 100))
        {
            return ApplicationResult<ReportResultDto>.Failure(ValidationFailed);
        }

        if (!await CanViewOutletSectionAsync(context, section, cancellationToken))
        {
            return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        }

        return await GetSnapshotReportAsync(context, "outlets", request with { Section = section }, cancellationToken);
    }

        public async Task<ApplicationResult<ReportExportDto>> CreateExportAsync(
        TenantRequestContext context,
        ReportExportRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportExportDto>.Failure(error);
        if (!await _repository.CanAccessAsync(context, request.Filters.OutletId, request.Filters.TillId, cancellationToken))
            return ApplicationResult<ReportExportDto>.Failure(PermissionDenied);
        if (!await ReportFeaturePolicy.IsExportEnabledAsync(_entitlements, context.TenantId, _clock.UtcNow, cancellationToken) ||
            !HasAnyPermission(context, TenantAdminReportPermissions.Export))
        {
            return ApplicationResult<ReportExportDto>.Failure(PermissionDenied);
        }
        
        if (request.Format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) || request.Format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<ReportExportDto>.Failure(new ApplicationError("reports.format_not_supported", "Format is deferred/unsupported in Release 1."));
        }
        if (!request.Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<ReportExportDto>.Failure(new ApplicationError("reports.format_invalid", "Invalid format."));
        }

        if (!await CanViewExportTargetAsync(context, request.ReportType, request.Section, cancellationToken))
        {
            return ApplicationResult<ReportExportDto>.Failure(PermissionDenied);
        }

        var filters = request.Filters with { Section = request.Section, Page = 1, PageSize = SnapshotRowLimit + 1 };
        ReportResultDto? reportResult;
        if (request.Filters.SnapshotId.HasValue)
        {
            var snapshot = await GetSnapshotReportAsync(context, request.ReportType.ToLowerInvariant(), filters, cancellationToken, exporting: true);
            if (snapshot.IsFailure) return ApplicationResult<ReportExportDto>.Failure(snapshot.Error);
            reportResult = snapshot.Value;
        }
        else
        {
            reportResult = await ReadReportAsync(context, request.ReportType.ToLowerInvariant(), filters, cancellationToken);
        }

        if (reportResult == null)
        {
            return ApplicationResult<ReportExportDto>.Failure(NotFound);
        }

        if ((reportResult.Pagination?.TotalCount ?? reportResult.Records.Count) > SnapshotRowLimit)
            return ApplicationResult<ReportExportDto>.Failure(new("reports.range_too_large", "Narrow the report filters before exporting."));
        CleanupExpiredJobs();
        if (ExportJobs.Count >= 64)
            return ApplicationResult<ReportExportDto>.Failure(new("reports.export_capacity", "Export capacity is busy. Retry shortly."));

        byte[] csvBytes;
        try
        {
            csvBytes = CsvGenerator.Generate(request, reportResult, context, _clock);
        }
        catch (ApplicationException ex)
        {
            return ApplicationResult<ReportExportDto>.Failure(new ApplicationError("reports.unsupported_section", ex.Message));
        }

        var now = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid();
        var job = new ReportExportDto(
            jobId,
            request.ReportType.Trim().ToLowerInvariant(),
            request.Section.Trim(),
            request.Format.Trim().ToUpperInvariant(),
            "COMPLETED",
            now,
            now,
            BuildSafeFileName(request.ReportType, request.Section, request.Format),
            $"/api/v1/tenant-admin/reports/exports/{jobId}/download",
            now.AddMinutes(15),
            null);
            
        var entry = new ExportJobEntry(job, context.TenantId, context.UserId, csvBytes, filters, await _repository.GetScopeStampAsync(context, cancellationToken));
        ExportJobs[jobId] = entry;

        await _auditLogger.LogExportJobCreatedAsync(context.TenantId, context.UserId, jobId, request, cancellationToken);
        _logger.LogInformation(
            "Report export {ExportJobId} created for {ReportId} tenant {TenantId} user {UserId} outlet {OutletId} from {From} to {To}: {ResultCount} rows",
            jobId, reportResult.ReportId, context.TenantId, context.UserId, filters.OutletId, filters.From, filters.To, reportResult.Records.Count);

        return ApplicationResult<ReportExportDto>.Success(job);
    }

        public async Task<ApplicationResult<ReportExportDto>> GetExportAsync(
        TenantRequestContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportExportDto>.Failure(error);
        if (!await _repository.CanAccessAsync(context, null, null, cancellationToken))
            return ApplicationResult<ReportExportDto>.Failure(PermissionDenied);
        
        CleanupExpiredJobs();
        
        if (ExportJobs.TryGetValue(jobId, out var entry) && entry.TenantId == context.TenantId && entry.UserId == context.UserId)
        {
            if (entry.Dto.ExpiresAt.HasValue && entry.Dto.ExpiresAt.Value < _clock.UtcNow)
            {
                ExportJobs.TryRemove(jobId, out _);
                return ApplicationResult<ReportExportDto>.Failure(NotFound);
            }
            
            if (entry.ScopeStamp != await _repository.GetScopeStampAsync(context, cancellationToken) ||
                !await _repository.CanAccessAsync(context, entry.Filters?.OutletId, entry.Filters?.TillId, cancellationToken) ||
                !await ReportFeaturePolicy.IsExportEnabledAsync(_entitlements, context.TenantId, _clock.UtcNow, cancellationToken) ||
                !HasAnyPermission(context, TenantAdminReportPermissions.Export) ||
                !await CanViewExportTargetAsync(context, entry.Dto.ReportType, entry.Dto.Section, cancellationToken))
            {
                return ApplicationResult<ReportExportDto>.Failure(PermissionDenied);
            }
            
            return ApplicationResult<ReportExportDto>.Success(entry.Dto);
        }
        
        return ApplicationResult<ReportExportDto>.Failure(NotFound);
    }

        public async Task<ApplicationResult<byte[]>> DownloadExportAsync(
        TenantRequestContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<byte[]>.Failure(error);
        if (!await _repository.CanAccessAsync(context, null, null, cancellationToken))
            return ApplicationResult<byte[]>.Failure(PermissionDenied);
        
        CleanupExpiredJobs();
        
        if (ExportJobs.TryGetValue(jobId, out var entry) && entry.TenantId == context.TenantId && entry.UserId == context.UserId && entry.Data != null)
        {
            if (entry.Dto.ExpiresAt.HasValue && entry.Dto.ExpiresAt.Value < _clock.UtcNow)
            {
                ExportJobs.TryRemove(jobId, out _);
                return ApplicationResult<byte[]>.Failure(NotFound);
            }
            
            if (entry.ScopeStamp != await _repository.GetScopeStampAsync(context, cancellationToken) ||
                !await _repository.CanAccessAsync(context, entry.Filters?.OutletId, entry.Filters?.TillId, cancellationToken) ||
                !await ReportFeaturePolicy.IsExportEnabledAsync(_entitlements, context.TenantId, _clock.UtcNow, cancellationToken) ||
                !HasAnyPermission(context, TenantAdminReportPermissions.Export) ||
                !await CanViewExportTargetAsync(context, entry.Dto.ReportType, entry.Dto.Section, cancellationToken))
            {
                return ApplicationResult<byte[]>.Failure(PermissionDenied);
            }
            
            await _auditLogger.LogExportDownloadedAsync(context.TenantId, context.UserId, jobId, cancellationToken);
            return ApplicationResult<byte[]>.Success(entry.Data);
        }
        
        return ApplicationResult<byte[]>.Failure(NotFound);
    }
    
    private void CleanupExpiredJobs()
    {
        
        var expiredKeys = ExportJobs.Where(x => x.Value.Dto.ExpiresAt.HasValue && x.Value.Dto.ExpiresAt.Value < _clock.UtcNow).Select(x => x.Key).ToList();
        foreach (var key in expiredKeys)
        {
            ExportJobs.TryRemove(key, out _);
        }
    }


    private static ApplicationError? ValidateCommonAccess(TenantRequestContext context) =>
        context.TenantId == Guid.Empty || context.UserId == Guid.Empty ? InvalidContext : null;

    private static string NormalizeSection(string? section) =>
        string.IsNullOrWhiteSpace(section) ? "summary" : section.Trim();

    private async Task<bool> CanViewSalesSectionAsync(TenantRequestContext context, string section, CancellationToken ct)
    {
        if (!await ReportFeaturePolicy.IsSectionEnabledAsync(section, _entitlements, context.TenantId, _clock.UtcNow, ct)) return false;

        var sectionPermission = section switch
        {
            "products" or "categories" => TenantAdminReportPermissions.ProductsView,
            "payments" or "payment-transactions" => TenantAdminReportPermissions.PaymentsView,
            "tax" => TenantAdminReportPermissions.TaxView,
            "discounts" => TenantAdminReportPermissions.DiscountsView,
            "returns" => TenantAdminReportPermissions.ReturnsView,
            "cashiers" => TenantAdminReportPermissions.CashiersView,
            "daily" => TenantAdminReportPermissions.DailySalesView,
            _ => TenantAdminReportPermissions.SalesView
        };

        return HasAnyPermission(context, sectionPermission, TenantAdminReportPermissions.SalesView, "reports.sales.view");
    }

    private async Task<bool> CanViewStockSectionAsync(TenantRequestContext context, string section, CancellationToken ct)
    {
        if (!await ReportFeaturePolicy.IsSectionEnabledAsync(section, _entitlements, context.TenantId, _clock.UtcNow, ct)) return false;

        var sectionPermission = section switch
        {
            "batch-expiry" => StockPermissions.ExpiryView,
            "movements" => StockPermissions.MovementsView,
            "valuation" => StockPermissions.ValueView,
            _ => StockPermissions.View
        };

        return HasAnyPermission(context, sectionPermission, StockPermissions.View, StockPermissions.LegacyInventoryView);
    }

    private async Task<bool> CanViewOutletSectionAsync(TenantRequestContext context, string section, CancellationToken ct)
    {
        if (!await ReportFeaturePolicy.IsSectionEnabledAsync(section, _entitlements, context.TenantId, _clock.UtcNow, ct)) return false;

        var sectionPermission = section switch
        {
            "tills" => TenantAdminReportPermissions.TillsView,
            "cashiers" => TenantAdminReportPermissions.CashiersView,
            _ => TenantAdminReportPermissions.OutletsView
        };

        return HasAnyPermission(context, sectionPermission, TenantAdminReportPermissions.OutletsView, "tenant.outlets.revenue.view");
    }

    private async Task<bool> CanViewExportTargetAsync(TenantRequestContext context, string reportType, string section, CancellationToken ct)
    {
        var normalizedType = reportType.Trim().ToLowerInvariant();
        return normalizedType switch
        {
            "sales" => SalesSections.Contains(section) && await CanViewSalesSectionAsync(context, section, ct),
            "stock" => StockSections.Contains(section) && await CanViewStockSectionAsync(context, section, ct),
            "outlets" => OutletSections.Contains(section) && await CanViewOutletSectionAsync(context, section, ct),
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














