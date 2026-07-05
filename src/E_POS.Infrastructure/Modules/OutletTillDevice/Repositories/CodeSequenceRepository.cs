using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Repositories;

public sealed class CodeSequenceRepository : ICodeSequenceRepository
{
    private const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
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
        var normalizedKey = NormalizeSequenceKey(sequenceKey);
        var normalizedPrefix = NormalizePrefix(prefix);
        var safePaddingLength = Math.Max(1, paddingLength);

        var nextValue = _dbContext.Database.ProviderName == NpgsqlProviderName
            ? await GetNextPostgresValueAsync(tenantId, normalizedKey, normalizedPrefix, safePaddingLength, now, cancellationToken)
            : await GetNextTrackedValueAsync(tenantId, normalizedKey, normalizedPrefix, safePaddingLength, now, cancellationToken);

        return FormatCode(normalizedPrefix, nextValue, safePaddingLength);
    }

    private async Task<int> GetNextPostgresValueAsync(
        Guid tenantId,
        string sequenceKey,
        string prefix,
        int paddingLength,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sequenceId = Guid.NewGuid();

        // EF Core's SqlQuery with RETURNING is non-composable; use raw ADO.NET for atomic upsert.
        var conn = (Npgsql.NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO code_sequences (id, tenant_id, sequence_key, prefix, current_value, padding_length, created_at, updated_at)
            VALUES (@id, @tenantId, @sequenceKey, @prefix, 1, @paddingLength, @now, @now)
            ON CONFLICT (tenant_id, sequence_key)
            DO UPDATE SET current_value = code_sequences.current_value + 1,
                          prefix = EXCLUDED.prefix,
                          padding_length = EXCLUDED.padding_length,
                          updated_at = EXCLUDED.updated_at
            RETURNING current_value
            """;

        cmd.Parameters.AddWithValue("id", sequenceId);
        cmd.Parameters.AddWithValue("tenantId", tenantId);
        cmd.Parameters.AddWithValue("sequenceKey", sequenceKey);
        cmd.Parameters.AddWithValue("prefix", prefix);
        cmd.Parameters.AddWithValue("paddingLength", paddingLength);
        cmd.Parameters.AddWithValue("now", now.UtcDateTime);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private async Task<int> GetNextTrackedValueAsync(
        Guid tenantId,
        string sequenceKey,
        string prefix,
        int paddingLength,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sequence = await _dbContext.CodeSequences
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.SequenceKey == sequenceKey,
                cancellationToken);

        if (sequence is null)
        {
            sequence = CodeSequence.Create(Guid.NewGuid(), tenantId, sequenceKey, prefix, 1, paddingLength, now);
            _dbContext.CodeSequences.Add(sequence);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return sequence.CurrentValue;
        }

        var nextValue = sequence.Advance(prefix, paddingLength, now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return nextValue;
    }

    private static string FormatCode(string prefix, int sequence, int paddingLength)
    {
        return $"{prefix}{sequence.ToString().PadLeft(paddingLength, '0')}";
    }

    private static string NormalizeSequenceKey(string sequenceKey) => sequenceKey.Trim().ToUpperInvariant();
    private static string NormalizePrefix(string prefix) => prefix.Trim().ToUpperInvariant();
}