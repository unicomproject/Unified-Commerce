using E_POS.Application.Modules.PlatformAdministration;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;

public sealed class PlatformAuditLogRepository : IPlatformAuditLogRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformAuditLogRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformAuditLogListResponse> GetLoginSecurityAuditLogsAsync(
        PlatformAuditLogListQuery query,
        CancellationToken cancellationToken)
    {
        if (HasUnsupportedEntityTypeFilter(query.EntityType))
        {
            return EmptyResponse(query);
        }

        var audits = _dbContext.PlatformLoginAudits.AsNoTracking();
        var users = _dbContext.PlatformUsers.AsNoTracking();

        var filteredAudits = ApplyFilters(
            from audit in audits
            join user in users on audit.PlatformUserId equals user.Id into matchedUsers
            from user in matchedUsers.DefaultIfEmpty()
            select new AuditUserJoinRow(audit, user != null ? user.Email : null),
            query);

        var totalCount = await filteredAudits.CountAsync(cancellationToken);
        var pageRows = await filteredAudits
            .OrderByDescending(x => x.Audit.CreatedAt)
            .ThenByDescending(x => x.Audit.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new LoginAuditRow(
                x.Audit.Id,
                x.Audit.CreatedAt,
                x.Audit.PlatformUserId,
                x.Email,
                x.Audit.LoginResult))
            .ToListAsync(cancellationToken);

        var items = pageRows
            .Select(row => PlatformAuditLogMapper.MapLoginAudit(
                row.Id,
                row.OccurredAt,
                row.PlatformUserId,
                row.Email,
                row.LoginResult))
            .ToList();

        return new PlatformAuditLogListResponse(
            PlatformAuditLogMapper.AuditScope,
            PlatformAuditLogMapper.AuditScopeDescription,
            items,
            query.PageNumber,
            query.PageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }

    private static bool HasUnsupportedEntityTypeFilter(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return false;
        }

        return !string.Equals(entityType, PlatformAuditLogMapper.EntityType, StringComparison.OrdinalIgnoreCase);
    }

    private static PlatformAuditLogListResponse EmptyResponse(PlatformAuditLogListQuery query)
    {
        return new PlatformAuditLogListResponse(
            PlatformAuditLogMapper.AuditScope,
            PlatformAuditLogMapper.AuditScopeDescription,
            [],
            query.PageNumber,
            query.PageSize,
            0,
            0);
    }

    private static IQueryable<AuditUserJoinRow> ApplyFilters(
        IQueryable<AuditUserJoinRow> rows,
        PlatformAuditLogListQuery query)
    {
        if (query.ActorPlatformUserId is not null)
        {
            rows = rows.Where(x => x.Audit.PlatformUserId == query.ActorPlatformUserId);
        }

        if (query.From is not null)
        {
            rows = rows.Where(x => x.Audit.CreatedAt >= query.From);
        }

        if (query.To is not null)
        {
            rows = rows.Where(x => x.Audit.CreatedAt <= query.To);
        }

        var loginResultFilter = PlatformAuditLogMapper.ResolveLoginResultFilter(query.Action);
        if (loginResultFilter is not null)
        {
            rows = rows.Where(x => x.Audit.LoginResult == loginResultFilter);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var mappedLoginResult = PlatformAuditLogMapper.ResolveLoginResultFilter(term);

            if (mappedLoginResult is not null)
            {
                rows = rows.Where(x =>
                    x.Audit.LoginResult == mappedLoginResult ||
                    (x.Email != null && x.Email.Contains(term)));
            }
            else
            {
                rows = rows.Where(x =>
                    (x.Email != null && x.Email.Contains(term)) ||
                    x.Audit.LoginResult.Contains(term));
            }
        }

        return rows;
    }

    private sealed record AuditUserJoinRow(PlatformLoginAudit Audit, string? Email);

    private sealed record LoginAuditRow(
        Guid Id,
        DateTimeOffset OccurredAt,
        Guid? PlatformUserId,
        string? Email,
        string LoginResult);
}
