using System.Collections.Concurrent;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using Microsoft.Extensions.Logging;

namespace E_POS.Application.Modules.Tenant.Reports.Services;

public sealed partial class TenantAdminReportsService
{
    // Bounded, short-lived snapshots. A restart/expiry requires refresh, never a silently different export.
    private static readonly ConcurrentDictionary<Guid, ReportSnapshot> Snapshots = new();
    private const int SnapshotRowLimit = 50000;
    private sealed record ReportSnapshot(Guid Tenant, Guid User, string Type, ReportQueryRequest Query,
        ReportResultDto Result, DateTimeOffset Expires, string ScopeStamp);

    private async Task<ReportResultDto?> ReadReportAsync(TenantRequestContext context, string type,
        ReportQueryRequest request, CancellationToken ct) => type switch
    {
        "sales" => await _repository.GetSalesAsync(context, request, ct),
        "stock" => await _repository.GetStockAsync(context, request, ct),
        "outlets" => await _repository.GetOutletsAsync(context, request, ct),
        _ => null
    };

    private async Task<ApplicationResult<ReportResultDto>> GetSnapshotReportAsync(TenantRequestContext context,
        string type, ReportQueryRequest query, CancellationToken ct, bool exporting = false)
    {
        var normalized = query with { Page = 1, PageSize = SnapshotRowLimit + 1, SnapshotId = null };
        ReportSnapshot snapshot;
        if (query.SnapshotId.HasValue)
        {
            if (!Snapshots.TryGetValue(query.SnapshotId.Value, out snapshot!) || snapshot.Tenant != context.TenantId
                || snapshot.User != context.UserId || snapshot.Type != type || snapshot.Query != normalized
                || snapshot.Expires <= _clock.UtcNow || snapshot.ScopeStamp != await _repository.GetScopeStampAsync(context, ct))
                return ApplicationResult<ReportResultDto>.Failure(new("reports.snapshot_expired", "Refresh the report before loading or exporting this snapshot."));
            // Revalidate all scope, including grants revoked since capture.
            if (!await _repository.CanAccessAsync(context, query.OutletId, query.TillId, ct))
                return ApplicationResult<ReportResultDto>.Failure(PermissionDenied);
        }
        else
        {
            foreach (var expired in Snapshots.Where(x => x.Value.Expires <= _clock.UtcNow).Select(x => x.Key))
                Snapshots.TryRemove(expired, out _);
            if (Snapshots.Count >= 32)
                return ApplicationResult<ReportResultDto>.Failure(new("reports.snapshot_capacity", "Report capacity is busy. Retry shortly."));
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            var report = await ReadReportAsync(context, type, normalized, ct);
            if (report is null) return ApplicationResult<ReportResultDto>.Failure(NotFound);
            var rowCount = report.Pagination?.TotalCount ?? report.Records.Count;
            _logger.LogInformation(
                "Report read {ReportId} ({ReportType}/{Section}) tenant {TenantId} user {UserId} outlet {OutletId} from {From} to {To}: {ResultCount} rows, provisional {IsProvisional}, {DurationMs} ms",
                report.ReportId, type, query.Section, context.TenantId, context.UserId, query.OutletId, query.From, query.To,
                rowCount, report.IsProvisional, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            if (rowCount > SnapshotRowLimit)
                return ApplicationResult<ReportResultDto>.Failure(new("reports.range_too_large", "Narrow the report filters before loading or exporting."));
            var id = Guid.NewGuid();
            // Keep the repository's completeness/provisional metadata; the snapshot only pins rows and filters.
            report = report with { SnapshotId = id, FiltersApplied = query with { SnapshotId = null } };
            snapshot = new(context.TenantId, context.UserId, type, normalized, report, _clock.UtcNow.AddMinutes(15), await _repository.GetScopeStampAsync(context, ct));
            Snapshots[id] = snapshot;
        }
        var full = snapshot.Result;
        if (exporting) return ApplicationResult<ReportResultDto>.Success(full);
        return ApplicationResult<ReportResultDto>.Success(full with {
            Records = full.Records.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(),
            Pagination = new(query.Page, query.PageSize, full.Records.Count, (int)Math.Ceiling(full.Records.Count / (double)query.PageSize))
        });
    }
}
