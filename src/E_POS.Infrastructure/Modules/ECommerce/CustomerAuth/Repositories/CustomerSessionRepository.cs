using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;

public sealed class CustomerSessionRepository : CustomerAuthRepositoryBase, ICustomerSessionRepository
{
    public CustomerSessionRepository(EPosDbContext dbContext) : base(dbContext)
    {
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

        var current = await DbContext.CustomerRefreshTokens
            .SingleOrDefaultAsync(
                x => x.TenantId == tenantId && x.TokenHash == currentTokenHash,
                cancellationToken);
        if (current is null)
        {
            await RollbackAsync(transaction, cancellationToken);
            return new(CustomerRefreshRotationStatus.Invalid, null, null);
        }

        var session = await DbContext.CustomerAuthSessions
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
            await DbContext.SaveChangesAsync(cancellationToken);
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
            await DbContext.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return new(CustomerRefreshRotationStatus.AccountUnavailable, null, null);
        }

        current.MarkRotated(replacementTokenId, now);
        session.Extend(replacementExpiresAt, now);
        DbContext.CustomerRefreshTokens.Add(CustomerRefreshToken.Create(
            replacementTokenId,
            tenantId,
            current.CustomerAuthSessionId,
            replacementTokenHash,
            current.TokenFamilyId,
            replacementExpiresAt,
            now));

        await DbContext.SaveChangesAsync(cancellationToken);
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
            from authSession in DbContext.CustomerAuthSessions
            join authAccount in DbContext.CustomerAuthAccounts
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
        var refreshTokens = await DbContext.CustomerRefreshTokens
            .Where(x => x.TenantId == tenantId &&
                        x.CustomerAuthSessionId == sessionId &&
                        x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var refreshToken in refreshTokens)
            refreshToken.Revoke(now, "CUSTOMER_LOGOUT");

        await DbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<CustomerLoginAccount?> FindRefreshAccountAsync(
        Guid tenantId,
        Guid authAccountId,
        CancellationToken cancellationToken)
    {
        return (
            from authAccount in DbContext.CustomerAuthAccounts.AsNoTracking()
            join customer in DbContext.Customers.AsNoTracking()
                on new { authAccount.TenantId, Id = authAccount.CustomerId }
                equals new { customer.TenantId, customer.Id }
            join tenant in DbContext.Tenants.AsNoTracking()
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
        var tokens = await DbContext.CustomerRefreshTokens
            .Where(x => x.TenantId == tenantId &&
                        x.TokenFamilyId == tokenFamilyId &&
                        x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.Revoke(now, reason);
    }
}