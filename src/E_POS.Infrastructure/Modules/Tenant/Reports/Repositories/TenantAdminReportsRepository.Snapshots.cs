using System.Data;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;

public sealed partial class TenantAdminReportsRepository
{
    // All component queries in one report observe a single committed database snapshot.
    private async Task<ReportResultDto> ConsistentReadAsync(TenantRequestContext context, ReportQueryRequest request,
        Func<Task<ReportResultDto>> read, CancellationToken ct)
    {
        if (!_dbContext.Database.IsRelational() || _dbContext.Database.CurrentTransaction is not null)
            return await WithSyncMetadataAsync(context, request, await read(), ct);
        return await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () => {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            var result = await WithSyncMetadataAsync(context, request, await read(), ct);
            await transaction.CommitAsync(ct);
            return result;
        });
    }

    /// <summary>
    /// Offline activity that has reached the server but is not yet applied (unprocessed sync items or
    /// unresolved conflicts) for devices at the caller's outlets makes the result provisional. Missing
    /// amounts are never estimated; the count is reported so the client can label REP-S05.
    /// </summary>
    private async Task<ReportResultDto> WithSyncMetadataAsync(TenantRequestContext context, ReportQueryRequest request,
        ReportResultDto result, CancellationToken ct)
    {
        var outletIds = await GetAccessibleOutletIdsAsync(context, ct);
        if (request.OutletId.HasValue) outletIds = outletIds.Where(x => x == request.OutletId.Value).ToList();
        var clients = _dbContext.OfflineClients.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && outletIds.Contains(x.OutletId)).Select(x => x.Id);
        var pendingItems = await _dbContext.SyncItems.AsNoTracking()
            .CountAsync(x => x.TenantId == context.TenantId && x.ProcessedAt == null && clients.Contains(x.OfflineClientId), ct);
        var openConflicts = await _dbContext.SyncConflicts.AsNoTracking()
            .CountAsync(x => x.TenantId == context.TenantId && x.ResolvedAt == null && clients.Contains(x.OfflineClientId), ct);
        var pending = pendingItems + openConflicts;
        var descriptor = ReportCatalog.Describe(result.Section);
        return result with
        {
            ReportId = descriptor?.Id,
            ReportName = descriptor?.Name,
            AsOf = result.GeneratedAt,
            LastUpdatedAt = result.GeneratedAt,
            KnownPendingSyncCount = pending,
            IsProvisional = pending > 0,
            Completeness = pending > 0 ? "PROVISIONAL" : "COMPLETE"
        };
    }

    public Task<ReportResultDto> GetSalesAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken ct)
        => ConsistentReadAsync(context, request, () => GetSalesCoreAsync(context, request, ct), ct);
    public Task<ReportResultDto> GetStockAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken ct)
        => ConsistentReadAsync(context, request, () => GetStockCoreAsync(context, request, ct), ct);
    public Task<ReportResultDto> GetOutletsAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken ct)
        => ConsistentReadAsync(context, request, () => GetOutletsCoreAsync(context, request, ct), ct);
    public Task<ReportResultDto> GetDashboardAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken ct)
        => ConsistentReadAsync(context, request, () => GetDashboardCoreAsync(context, request, ct), ct);
}
