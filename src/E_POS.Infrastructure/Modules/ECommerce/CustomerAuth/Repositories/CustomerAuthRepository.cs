using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;

public sealed class CustomerAuthRepository : ICustomerAuthRepository
{
    private readonly EPosDbContext _dbContext;

    public CustomerAuthRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerLoginAccount?> FindLoginAccountAsync(
        Guid tenantId,
        string normalizedEmail,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        var row = await (
            from authAccount in _dbContext.CustomerAuthAccounts
            join customer in _dbContext.Customers
                on new { authAccount.TenantId, Id = authAccount.CustomerId }
                equals new { customer.TenantId, customer.Id }
            join tenant in _dbContext.Tenants
                on authAccount.TenantId equals tenant.Id
            where authAccount.TenantId == tenantId &&
                  ((!string.IsNullOrEmpty(normalizedEmail) && customer.NormalizedEmail == normalizedEmail) ||
                   (!string.IsNullOrEmpty(normalizedPhone) && customer.NormalizedPhone == normalizedPhone))
            select new { AuthAccount = authAccount, Customer = customer, TenantStatus = tenant.Status })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new CustomerLoginAccount(
                row.AuthAccount,
                row.Customer.Id,
                row.Customer.TenantId,
                row.Customer.Name,
                row.Customer.Email,
                row.Customer.Phone,
                row.Customer.Status,
                row.TenantStatus);
    }

    public async Task SaveFailedLoginAsync(
        CustomerAuthAccount account,
        CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveSuccessfulLoginAsync(
        CustomerAuthAccount account,
        CustomerAuthSession session,
        CustomerRefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        _dbContext.CustomerAuthSessions.Add(session);
        _dbContext.CustomerRefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CustomerRefreshRotationResult> RotateRefreshTokenAsync(
        Guid tenantId,
        string currentTokenHash,
        Guid replacementTokenId,
        string replacementTokenHash,
        DateTimeOffset replacementExpiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginSerializableTransactionAsync(cancellationToken);

        var current = await _dbContext.CustomerRefreshTokens
            .SingleOrDefaultAsync(
                x => x.TenantId == tenantId && x.TokenHash == currentTokenHash,
                cancellationToken);
        if (current is null)
        {
            await RollbackAsync(transaction, cancellationToken);
            return new(CustomerRefreshRotationStatus.Invalid, null, null);
        }

        var session = await _dbContext.CustomerAuthSessions
            .SingleOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == current.CustomerAuthSessionId,
                cancellationToken);

        var tokenWasConsumed =
            current.UsedAt.HasValue ||
            current.ReplacedByTokenId.HasValue ||
            string.Equals(current.Status, "USED", StringComparison.OrdinalIgnoreCase);
        if (tokenWasConsumed)
        {
            if (session is not null)
                session.Revoke(now, "REFRESH_TOKEN_REUSE");

            await RevokeTokenFamilyAsync(
                tenantId,
                current.TokenFamilyId,
                now,
                "REFRESH_TOKEN_REUSE",
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return new(CustomerRefreshRotationStatus.Reused, null, null);
        }

        if (current.RevokedAt.HasValue ||
            !string.Equals(current.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            current.ExpiresAt <= now ||
            session is null ||
            session.RevokedAt.HasValue ||
            !string.Equals(session.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            session.ExpiresAt <= now)
        {
            await RollbackAsync(transaction, cancellationToken);
            return new(CustomerRefreshRotationStatus.Invalid, null, null);
        }

        var account = await FindRefreshAccountAsync(
            tenantId,
            session.CustomerAuthAccountId,
            cancellationToken);
        if (account is null ||
            !string.Equals(account.Account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(account.CustomerStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(account.TenantStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            session.Revoke(now, "ACCOUNT_UNAVAILABLE");
            await RevokeTokenFamilyAsync(
                tenantId,
                current.TokenFamilyId,
                now,
                "ACCOUNT_UNAVAILABLE",
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return new(CustomerRefreshRotationStatus.AccountUnavailable, null, null);
        }

        current.MarkRotated(replacementTokenId, now);
        session.Extend(replacementExpiresAt, now);
        _dbContext.CustomerRefreshTokens.Add(CustomerRefreshToken.Create(
            replacementTokenId,
            tenantId,
            current.CustomerAuthSessionId,
            replacementTokenHash,
            current.TokenFamilyId,
            replacementExpiresAt,
            now));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return new(CustomerRefreshRotationStatus.Succeeded, account, session.Id);
    }

    public async Task<bool> RevokeSessionAsync(
        Guid tenantId,
        Guid customerId,
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var session = await (
            from authSession in _dbContext.CustomerAuthSessions
            join authAccount in _dbContext.CustomerAuthAccounts
                on new { authSession.TenantId, Id = authSession.CustomerAuthAccountId }
                equals new { authAccount.TenantId, authAccount.Id }
            where authSession.TenantId == tenantId &&
                  authSession.Id == sessionId &&
                  authAccount.CustomerId == customerId &&
                  authSession.Status == "ACTIVE" &&
                  authSession.RevokedAt == null
            select authSession)
            .FirstOrDefaultAsync(cancellationToken);
        if (session is null) return false;

        session.Revoke(now, "CUSTOMER_LOGOUT");
        var refreshTokens = await _dbContext.CustomerRefreshTokens
            .Where(x => x.TenantId == tenantId &&
                        x.CustomerAuthSessionId == sessionId &&
                        x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var refreshToken in refreshTokens)
            refreshToken.Revoke(now, "CUSTOMER_LOGOUT");

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<CustomerLoginAccount?> FindRefreshAccountAsync(
        Guid tenantId,
        Guid authAccountId,
        CancellationToken cancellationToken)
    {
        return (
            from authAccount in _dbContext.CustomerAuthAccounts.AsNoTracking()
            join customer in _dbContext.Customers.AsNoTracking()
                on new { authAccount.TenantId, Id = authAccount.CustomerId }
                equals new { customer.TenantId, customer.Id }
            join tenant in _dbContext.Tenants.AsNoTracking()
                on authAccount.TenantId equals tenant.Id
            where authAccount.TenantId == tenantId &&
                  authAccount.Id == authAccountId
            select new CustomerLoginAccount(
                authAccount,
                customer.Id,
                customer.TenantId,
                customer.Name,
                customer.Email,
                customer.Phone,
                customer.Status,
                tenant.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task RevokeTokenFamilyAsync(
        Guid tenantId,
        Guid tokenFamilyId,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken)
    {
        var tokens = await _dbContext.CustomerRefreshTokens
            .Where(x => x.TenantId == tenantId &&
                        x.TokenFamilyId == tokenFamilyId &&
                        x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.Revoke(now, reason);
    }

    private async Task<IDbContextTransaction?> BeginSerializableTransactionAsync(
        CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
            return null;

        return await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
    }

    private static Task CommitAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken) =>
        transaction is null
            ? Task.CompletedTask
            : transaction.CommitAsync(cancellationToken);

    private static Task RollbackAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken) =>
        transaction is null
            ? Task.CompletedTask
            : transaction.RollbackAsync(cancellationToken);
}
