using System.Data;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Domain.Modules.Shared.Idempotency.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Shared.Idempotency.Services;

public sealed class IdempotencyService : IIdempotencyService
{
    private const string StatusInProgress = "IN_PROGRESS";
    private const string StatusCompleted = "COMPLETED";
    private const int CreatedStatusCode = 201;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly EPosDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IdempotencyService(EPosDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<T>> ExecuteAsync<T>(
        Guid tenantId,
        Guid actorUserId,
        string operation,
        string idempotencyKey,
        string requestHash,
        Func<CancellationToken, Task<ApplicationResult<T>>> operationFunc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);
        ArgumentNullException.ThrowIfNull(operationFunc);

        var normalizedOperation = operation.Trim();
        var normalizedKey = idempotencyKey.Trim();
        var ownsTransaction = _dbContext.Database.IsRelational() && _dbContext.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;

        try
        {
            await AcquireScopeLockAsync(tenantId, actorUserId, normalizedOperation, normalizedKey, cancellationToken);

            var now = _dateTimeProvider.UtcNow;
            var existing = await _dbContext.IdempotencyRequests.SingleOrDefaultAsync(
                request =>
                    request.TenantId == tenantId &&
                    request.ActorUserId == actorUserId &&
                    request.Endpoint == normalizedOperation &&
                    request.IdempotencyKey == normalizedKey,
                cancellationToken);

            if (existing is null)
            {
                existing = IdempotencyRequest.Create(
                    Guid.NewGuid(),
                    tenantId,
                    actorUserId,
                    normalizedOperation,
                    normalizedKey,
                    requestHash,
                    now);
                _dbContext.IdempotencyRequests.Add(existing);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var replay = await TryReplayAsync<T>(existing, requestHash, now, cancellationToken);
                if (replay is not null)
                {
                    return replay;
                }
            }

            var result = await operationFunc(cancellationToken);
            var completedAt = _dateTimeProvider.UtcNow;
            if (result.IsSuccess && result.Value is not null)
            {
                existing.Complete(CreatedStatusCode, JsonSerializer.Serialize(result.Value, JsonOptions), completedAt);
                await _dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return result;
            }

            existing.Fail(result.Error.Code, completedAt);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
    }

    private async Task<ApplicationResult<T>?> TryReplayAsync<T>(
        IdempotencyRequest request,
        string requestHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return ApplicationResult<T>.Failure(new ApplicationError(
                "user.idempotency_conflict",
                "Idempotency key was already used for a different create-user request."));
        }

        if (string.Equals(request.Status, StatusCompleted, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.ResponseBody))
            {
                return ApplicationResult<T>.Failure(new ApplicationError(
                    "user.idempotency_replay_unavailable",
                    "The original idempotent response is unavailable."));
            }

            var value = JsonSerializer.Deserialize<T>(request.ResponseBody, JsonOptions);
            if (value is null)
            {
                return ApplicationResult<T>.Failure(new ApplicationError(
                    "user.idempotency_replay_unavailable",
                    "The original idempotent response is unavailable."));
            }

            return ApplicationResult<T>.Success(value);
        }

        if (string.Equals(request.Status, StatusInProgress, StringComparison.OrdinalIgnoreCase) &&
            request.ProcessingLeasedUntil.HasValue &&
            request.ProcessingLeasedUntil.Value > now)
        {
            return ApplicationResult<T>.Failure(new ApplicationError(
                "user.idempotency_in_progress",
                "A create-user request with this idempotency key is already in progress."));
        }

        request.RenewLease(now, LeaseDuration);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    private async Task AcquireScopeLockAsync(
        Guid tenantId,
        Guid actorUserId,
        string operation,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var providerName = _dbContext.Database.ProviderName ?? string.Empty;
        if (!providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var lockKey = $"idempotency:{tenantId:N}:{actorUserId:N}:{operation}:{idempotencyKey}";
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }
}
