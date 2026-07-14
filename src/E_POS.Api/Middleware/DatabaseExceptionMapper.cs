using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace E_POS.Api.Middleware;

public static class DatabaseExceptionMapper
{
    private const string UniqueViolationSqlState = "23505";
    private const string ForeignKeyViolationSqlState = "23503";

    public static MappedDatabaseError Map(Exception exception)
    {
        var postgres = FindPostgresException(exception);
        if (postgres is not null)
        {
            return postgres.SqlState switch
            {
                UniqueViolationSqlState => MapUniqueViolation(postgres),
                ForeignKeyViolationSqlState => new MappedDatabaseError(
                    StatusCodes.Status400BadRequest,
                    "data_constraint_violation",
                    "One or more referenced records are missing or invalid."),
                _ => MappedDatabaseError.InternalServerError,
            };
        }

        if (exception is DbUpdateException)
        {
            return new MappedDatabaseError(
                StatusCodes.Status500InternalServerError,
                "data_update_failed",
                "A database error occurred while saving changes.");
        }

        return MappedDatabaseError.InternalServerError;
    }

    private static MappedDatabaseError MapUniqueViolation(PostgresException postgres)
    {
        var constraint = postgres.ConstraintName ?? string.Empty;
        if (constraint.Contains("till_code", StringComparison.OrdinalIgnoreCase) ||
            constraint.Contains("tills", StringComparison.OrdinalIgnoreCase))
        {
            return new MappedDatabaseError(
                StatusCodes.Status409Conflict,
                "till.duplicate_code",
                "Till code already exists for this tenant.");
        }

        return new MappedDatabaseError(
            StatusCodes.Status409Conflict,
            "data_conflict",
            "A record with the same unique value already exists.");
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }
}

public readonly record struct MappedDatabaseError(int StatusCode, string Code, string Message)
{
    public static MappedDatabaseError InternalServerError { get; } = new(
        StatusCodes.Status500InternalServerError,
        "internal_server_error",
        "An unexpected error occurred.");
}
