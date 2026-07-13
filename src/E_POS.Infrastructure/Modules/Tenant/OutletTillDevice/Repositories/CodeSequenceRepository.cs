using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

public sealed class CodeSequenceRepository : ICodeSequenceRepository
{
    private readonly EPosDbContext _dbContext;

    public CodeSequenceRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetNextCodeAsync(
        Guid tenantId,
        string sequenceKey,
        string prefix,
        int paddingLength,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var normalizedPrefix = NormalizePrefix(prefix);
        var safePaddingLength = Math.Max(1, paddingLength);
        var normalizedKey = NormalizeSequenceKey(sequenceKey);

        var existingCodes = normalizedKey switch
        {
            "OUTLET_CODE" => await _dbContext.Outlets
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.OutletCode.StartsWith(normalizedPrefix))
                .Select(x => x.OutletCode)
                .ToListAsync(cancellationToken),
            "POS_DEVICE_CODE" => await _dbContext.PosDevices
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.DeviceCode.StartsWith(normalizedPrefix))
                .Select(x => x.DeviceCode)
                .ToListAsync(cancellationToken),
            "STOCK_MOVEMENT_NUMBER" => await _dbContext.StockMovements
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.MovementNumber.StartsWith(normalizedPrefix))
                .Select(x => x.MovementNumber)
                .ToListAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported sequence key '{sequenceKey}'.")
        };

        var nextValue = GetNextSequenceValue(existingCodes, normalizedPrefix) + 1;
        return FormatCode(normalizedPrefix, nextValue, safePaddingLength);
    }

    private static int GetNextSequenceValue(IReadOnlyCollection<string> existingCodes, string prefix)
    {
        var max = 0;
        foreach (var code in existingCodes)
        {
            if (!code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var suffix = code[prefix.Length..];
            if (int.TryParse(suffix, out var value) && value > max)
            {
                max = value;
            }
        }

        return max;
    }

    private static string FormatCode(string prefix, int sequence, int paddingLength) =>
        $"{prefix}{sequence.ToString().PadLeft(paddingLength, '0')}";

    private static string NormalizeSequenceKey(string sequenceKey) => sequenceKey.Trim().ToUpperInvariant();
    private static string NormalizePrefix(string prefix) => prefix.Trim().ToUpperInvariant();
}
