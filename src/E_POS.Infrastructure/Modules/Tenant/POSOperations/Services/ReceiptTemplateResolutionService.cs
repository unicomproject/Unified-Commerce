using Microsoft.EntityFrameworkCore;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Infrastructure.Persistence;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Services;

public sealed class ReceiptTemplateResolutionService : IReceiptTemplateResolutionService
{
    private readonly EPosDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReceiptTemplateResolutionService(EPosDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ResolvedReceiptTemplateDto?> ResolveTemplateAsync(
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        
        var assignments = await _dbContext.ReceiptTemplateAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId &&
                        a.Status == "ACTIVE" &&
                        a.EffectiveFrom <= now &&
                        (a.EffectiveTo == null || a.EffectiveTo >= now) &&
                        (a.PosDeviceId == deviceId || a.TillId == tillId || a.OutletId == outletId || a.IsDefault))
            .ToListAsync(cancellationToken);

        var resolvedAssignment = assignments
            .Where(a => a.PosDeviceId == deviceId).FirstOrDefault()
            ?? assignments.Where(a => a.TillId == tillId).FirstOrDefault()
            ?? assignments.Where(a => a.OutletId == outletId).FirstOrDefault()
            ?? assignments.Where(a => a.IsDefault).FirstOrDefault();

        if (resolvedAssignment != null)
        {
            var version = await _dbContext.ReceiptTemplateVersions.AsNoTracking()
                .Where(v => v.Id == resolvedAssignment.ReceiptTemplateVersionId && v.TenantId == tenantId && v.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (version != null)
            {
                return new ResolvedReceiptTemplateDto(version.Id, version.TemplateData);
            }
        }

        var fallbackJson = "{\"type\":\"system_fallback\",\"components\":[]}";
        return new ResolvedReceiptTemplateDto(Guid.Empty, fallbackJson);
    }
}
