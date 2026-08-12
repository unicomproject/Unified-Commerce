using System.Data;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Services;

public sealed class TenantUserStaffCodeService(EPosDbContext db) : ITenantUserStaffCodeService
{
    private const string SequenceType = "TENANT_USER_STAFF_CODE";

    public async Task<string> GenerateAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var year = now.UtcDateTime.Year;
        var connection = db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            if (db.Database.CurrentTransaction is not null)
            {
                command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
            }

            command.CommandText = """
                INSERT INTO tenant_user_code_sequences (id, tenant_id, sequence_type, year, current_value, created_at, updated_at)
                VALUES (@id, @tenant_id, @sequence_type, @year, 1, @created_at, @updated_at)
                ON CONFLICT (tenant_id, sequence_type, year)
                DO UPDATE SET current_value = tenant_user_code_sequences.current_value + 1,
                              updated_at = EXCLUDED.updated_at
                RETURNING current_value
                """;
            AddParameter(command, "id", Guid.NewGuid());
            AddParameter(command, "tenant_id", tenantId);
            AddParameter(command, "sequence_type", SequenceType);
            AddParameter(command, "year", year);
            AddParameter(command, "created_at", now);
            AddParameter(command, "updated_at", now);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var value = Convert.ToInt64(result);
            return $"USR-{year}-{value:00000}";
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
