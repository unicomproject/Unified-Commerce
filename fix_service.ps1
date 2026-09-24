$content = Get-Content "src\E_POS.Application\Modules\Tenant\Reports\Services\TenantAdminReportsService.cs" -Raw

$content = $content -replace "public sealed class TenantAdminReportsService : ITenantAdminReportsService\s*\{[\s\S]*?public TenantAdminReportsService\(ITenantAdminReportsRepository repository\)\s*\{[\s\S]*?\}", @"
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Reports.Constants;

namespace E_POS.Application.Modules.Tenant.Reports.Services;

public sealed class TenantAdminReportsService : ITenantAdminReportsService
{
    private static readonly ApplicationError InvalidContext = new(
        `"reports.invalid_tenant_context`",
        `"Invalid tenant context.`");

    private static readonly ApplicationError PermissionDenied = new(
        `"reports.permission_denied`",
        `"You do not have permission to view this report.`");

    private static readonly ApplicationError ValidationFailed = new(
        `"reports.validation_failed`",
        `"Report query is invalid.`");

    private static readonly ApplicationError NotFound = new(
        `"reports.not_found`",
        `"Report data was not found.`");

    private static readonly IReadOnlySet<string> SalesSections = new HashSet<string>(StringComparer.Ordinal)
    {
        `"summary`", `"transactions`", `"products`", `"categories`", `"payments`", `"tax`",
        `"discounts`", `"returns`", `"cashiers`", `"daily`"
    };

    private static readonly IReadOnlySet<string> StockSections = new HashSet<string>(StringComparer.Ordinal)
    {
        `"current`", `"low-stock`", `"out-of-stock`", `"batch-expiry`", `"movements`", `"valuation`"
    };

    private static readonly IReadOnlySet<string> OutletSections = new HashSet<string>(StringComparer.Ordinal)
    {
        `"performance`", `"tills`", `"cashiers`"
    };

    private static readonly IReadOnlySet<string> ExportFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        `"csv`", `"xlsx`", `"pdf`"
    };

    private static readonly Dictionary<Guid, ReportExportDto> ExportJobs = new();

    private readonly ITenantAdminReportsRepository _repository;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IDateTimeProvider _clock;

    public TenantAdminReportsService(ITenantAdminReportsRepository repository, ITenantFeatureEntitlementEvaluator entitlements, IDateTimeProvider clock)
    {
        _repository = repository;
        _entitlements = entitlements;
        _clock = clock;
    }
"@

$content = $content -replace "if \(\!ReportFeaturePolicy\.IsReportsModuleEnabled\(context\.FeatureCodes\)\)", "if (!await ReportFeaturePolicy.IsReportsModuleEnabledAsync(_entitlements, context.TenantId, _clock.UtcNow, cancellationToken))"
$content = $content -replace "if \(\!ReportFeaturePolicy\.IsSectionEnabled\(`"dashboard`", context\.FeatureCodes\)", "if (!await ReportFeaturePolicy.IsSectionEnabledAsync(`"dashboard`", _entitlements, context.TenantId, _clock.UtcNow, cancellationToken)"
$content = $content -replace "if \(\!ReportFeaturePolicy\.IsExportEnabled\(context\.FeatureCodes\)\)", "if (!await ReportFeaturePolicy.IsExportEnabledAsync(_entitlements, context.TenantId, _clock.UtcNow, cancellationToken))"

$content = $content -replace "if \(\!CanViewSalesSection\(context, section\)\)", "if (!await CanViewSalesSectionAsync(context, section, cancellationToken))"
$content = $content -replace "if \(\!CanViewSalesSection\(context, `"transactions`"\)\)", "if (!await CanViewSalesSectionAsync(context, `"transactions`", cancellationToken))"
$content = $content -replace "if \(\!CanViewStockSection\(context, section\)\)", "if (!await CanViewStockSectionAsync(context, section, cancellationToken))"
$content = $content -replace "if \(\!CanViewOutletSection\(context, section\)\)", "if (!await CanViewOutletSectionAsync(context, section, cancellationToken))"

$content = $content -replace "public Task<ApplicationResult<ReportExportDto>> CreateExportAsync\(", "public async Task<ApplicationResult<ReportExportDto>> CreateExportAsync("
$content = $content -replace "return Task.FromResult\(ApplicationResult<ReportExportDto>\.Failure\(error\)\);", "return ApplicationResult<ReportExportDto>.Failure(error);"
$content = $content -replace "return Task.FromResult\(ApplicationResult<ReportExportDto>\.Failure\(PermissionDenied\)\);", "return ApplicationResult<ReportExportDto>.Failure(PermissionDenied);"
$content = $content -replace "return Task.FromResult\(ApplicationResult<ReportExportDto>\.Success\(job\)\);", "return ApplicationResult<ReportExportDto>.Success(job);"
$content = $content -replace "if \(\!CanViewExportTarget\(context, request\.ReportType, request\.Section\)\)", "if (!await CanViewExportTargetAsync(context, request.ReportType, request.Section, cancellationToken))"

$content = $content -replace "private static bool CanViewSalesSection\(TenantRequestContext context, string section\)", "private async Task<bool> CanViewSalesSectionAsync(TenantRequestContext context, string section, CancellationToken ct)"
$content = $content -replace "private static bool CanViewStockSection\(TenantRequestContext context, string section\)", "private async Task<bool> CanViewStockSectionAsync(TenantRequestContext context, string section, CancellationToken ct)"
$content = $content -replace "private static bool CanViewOutletSection\(TenantRequestContext context, string section\)", "private async Task<bool> CanViewOutletSectionAsync(TenantRequestContext context, string section, CancellationToken ct)"
$content = $content -replace "private static bool CanViewExportTarget\(TenantRequestContext context, string reportType, string section\)", "private async Task<bool> CanViewExportTargetAsync(TenantRequestContext context, string reportType, string section, CancellationToken ct)"

$content = $content -replace "if \(\!ReportFeaturePolicy\.IsSectionEnabled\(section, context\.FeatureCodes\)\) return false;", "if (!await ReportFeaturePolicy.IsSectionEnabledAsync(section, _entitlements, context.TenantId, _clock.UtcNow, ct)) return false;"

$content = $content -replace "`"sales`" => CanViewSalesSection\(context, section\),", "`"sales`" => await CanViewSalesSectionAsync(context, section, ct),"
$content = $content -replace "`"stock`" => CanViewStockSection\(context, section\),", "`"stock`" => await CanViewStockSectionAsync(context, section, ct),"
$content = $content -replace "`"outlets`" => CanViewOutletSection\(context, section\),", "`"outlets`" => await CanViewOutletSectionAsync(context, section, ct),"

$content = $content -replace "(?s)using E_POS\.Application\.Common\.Models;.*?namespace E_POS\.Application\.Modules\.Tenant\.Reports\.Services;", "using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Reports.Constants;

namespace E_POS.Application.Modules.Tenant.Reports.Services;"

Set-Content "src\E_POS.Application\Modules\Tenant\Reports\Services\TenantAdminReportsService.cs" $content
