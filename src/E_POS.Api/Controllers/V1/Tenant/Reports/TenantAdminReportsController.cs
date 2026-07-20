using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace E_POS.Api.Controllers.V1.Tenant.Reports;

[ApiController]
[Route("api/v1/tenant-admin/reports")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminReportsController : ControllerBase
{
    private readonly ITenantAdminReportsService _reportsService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TenantAdminReportsController(
        ITenantAdminReportsService reportsService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _reportsService = reportsService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(
        [FromQuery] Guid? outletId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? optionType = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (!TryContext(out var context, out var unauthorized)) return unauthorized;
        var result = await _reportsService.GetFilterOptionsAsync(
            context,
            new ReportFilterOptionsRequest(outletId, departmentId, categoryId, productId, search, optionType, includeInactive, page, pageSize),
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? outletId = null,
        [FromQuery] Guid? salesChannelId = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryContext(out var context, out var unauthorized)) return unauthorized;
        var result = await _reportsService.GetDashboardAsync(
            context,
            new ReportQueryRequest(from, to, outletId, null, null, null, null, null, null, null, null, null, salesChannelId, null, null, null, null, null),
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSales(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? outletId = null,
        [FromQuery] Guid? tillId = null,
        [FromQuery] Guid? cashierId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? subcategoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? productVariantId = null,
        [FromQuery] Guid? salesChannelId = null,
        [FromQuery] Guid? paymentMethodId = null,
        [FromQuery] string? orderStatus = null,
        [FromQuery] string? paymentStatus = null,
        [FromQuery] string? search = null,
        [FromQuery] string? section = "summary",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        if (!TryContext(out var context, out var unauthorized)) return unauthorized;
        var result = await _reportsService.GetSalesAsync(
            context,
            new ReportQueryRequest(from, to, outletId, tillId, cashierId, customerId, departmentId, categoryId, subcategoryId,
                brandId, productId, productVariantId, salesChannelId, paymentMethodId, orderStatus, paymentStatus, search,
                section, page, pageSize, sortBy, sortDirection),
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("sales/{orderId:guid}")]
    public async Task<IActionResult> GetSalesDetail(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (!TryContext(out var context, out var unauthorized)) return unauthorized;
        var result = await _reportsService.GetSalesTransactionDetailAsync(context, orderId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStock(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? outletId = null,
        [FromQuery] Guid? inventoryLocationId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? subcategoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? productVariantId = null,
        [FromQuery] string? stockStatus = null,
        [FromQuery] string? expiryStatus = null,
        [FromQuery] string? batchNumber = null,
        [FromQuery] string? movementType = null,
        [FromQuery] string? search = null,
        [FromQuery] string? section = "current",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        if (!TryContext(out var context, out var unauthorized)) return unauthorized;
        var result = await _reportsService.GetStockAsync(
            context,
            new ReportQueryRequest(from, to, outletId, null, null, null, departmentId, categoryId, subcategoryId,
                brandId, productId, productVariantId, null, null, null, null, search, section, page, pageSize,
                sortBy, sortDirection, inventoryLocationId, stockStatus, expiryStatus, batchNumber, movementType),
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("outlets")]
    public async Task<IActionResult> GetOutlets(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? outletId = null,
        [FromQuery] Guid? salesChannelId = null,
        [FromQuery] Guid? paymentMethodId = null,
        [FromQuery] string? section = "performance",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        if (!TryContext(out var context, out var unauthorized)) return unauthorized;
        var result = await _reportsService.GetOutletsAsync(
            context,
            new ReportQueryRequest(from, to, outletId, null, null, null, null, null, null, null, null, null,
                salesChannelId, paymentMethodId, null, null, null, section, page, pageSize, sortBy, sortDirection),
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("exports")]
    public async Task<IActionResult> CreateExport([FromBody] JsonElement requestBody, CancellationToken cancellationToken = default)
    {
        if (!TryContext(out var context, out var unauthorized)) return unauthorized;
        var request = ParseExportRequest(requestBody);
        var result = await _reportsService.CreateExportAsync(context, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("exports/{jobId:guid}")]
    public async Task<IActionResult> GetExport(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (!TryContext(out var context, out var unauthorized)) return unauthorized;
        var result = await _reportsService.GetExportAsync(context, jobId, cancellationToken);
        return ToActionResult(result);
    }

    private bool TryContext(out TenantRequestContext context, out IActionResult unauthorized)
    {
        if (_tenantRequestContextFactory.TryCreate(User, out context!))
        {
            unauthorized = Ok();
            return true;
        }

        unauthorized = Unauthorized(CreateError(new ApplicationError("reports.invalid_tenant_context", "Invalid tenant context.")));
        return false;
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new { data = result.Value });
        }

        return result.Error.Code switch
        {
            "reports.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(result.Error)),
            "reports.invalid_tenant_context" => Unauthorized(CreateError(result.Error)),
            "reports.not_found" => NotFound(CreateError(result.Error)),
            _ => BadRequest(CreateError(result.Error))
        };
    }

    private object CreateError(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        details = Array.Empty<object>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };

    private static ReportExportRequest ParseExportRequest(JsonElement body)
    {
        var filters = body.TryGetProperty("filters", out var nestedFilters) && nestedFilters.ValueKind == JsonValueKind.Object
            ? nestedFilters
            : body;
        var section = GetString(body, "section") ?? "transactions";
        return new ReportExportRequest(
            GetString(body, "reportType") ?? "sales",
            section,
            GetString(body, "format") ?? "csv",
            new ReportQueryRequest(
                GetDate(filters, "from"),
                GetDate(filters, "to"),
                GetGuid(filters, "outletId"),
                GetGuid(filters, "tillId"),
                GetGuid(filters, "cashierId"),
                GetGuid(filters, "customerId"),
                GetGuid(filters, "departmentId"),
                GetGuid(filters, "categoryId"),
                GetGuid(filters, "subcategoryId"),
                GetGuid(filters, "brandId"),
                GetGuid(filters, "productId"),
                GetGuid(filters, "productVariantId"),
                GetGuid(filters, "salesChannelId"),
                GetGuid(filters, "paymentMethodId"),
                GetString(filters, "orderStatus"),
                GetString(filters, "paymentStatus"),
                GetString(filters, "search"),
                section,
                GetInt(filters, "page") ?? 1,
                GetInt(filters, "pageSize") ?? 25,
                GetString(filters, "sortBy"),
                GetString(filters, "sortDirection") ?? "asc",
                GetGuid(filters, "inventoryLocationId"),
                GetString(filters, "stockStatus"),
                GetString(filters, "expiryStatus"),
                GetString(filters, "batchNumber"),
                GetString(filters, "movementType")));
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static Guid? GetGuid(JsonElement element, string name) =>
        Guid.TryParse(GetString(element, name), out var value) ? value : null;

    private static int? GetInt(JsonElement element, string name) =>
        int.TryParse(GetString(element, name), out var value) ? value : null;

    private static DateOnly? GetDate(JsonElement element, string name) =>
        DateOnly.TryParse(GetString(element, name), out var value) ? value : null;
}
