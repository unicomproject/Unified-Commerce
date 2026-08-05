using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;

public sealed class CustomerPasswordResetRepository : CustomerAuthRepositoryBase, ICustomerPasswordResetRepository
{
    public CustomerPasswordResetRepository(EPosDbContext dbContext) : base(dbContext)
    {
    }

    public Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken cancellationToken) =>
        TenantIsActiveCoreAsync(tenantId, cancellationToken);

    public Task<CustomerLoginAccount?> FindAccountByEmailAsync(
        Guid tenantId,
        string normalizedEmail,
        bool trackAccount,
        CancellationToken cancellationToken) =>
        FindAccountByEmailCoreAsync(tenantId, normalizedEmail, trackAccount, cancellationToken);

    public async Task SavePasswordResetTokenAsync(
        CustomerPasswordResetToken resetToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginSerializableTransactionAsync(cancellationToken);

        var activeTokens = await DbContext.CustomerPasswordResetTokens
            .Where(x => x.TenantId == resetToken.TenantId &&
                        x.CustomerAuthAccountId == resetToken.CustomerAuthAccountId &&
                        x.Status == "ACTIVE" &&
                        x.RevokedAt == null &&
                        x.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var activeToken in activeTokens)
            activeToken.Revoke(now, "SUPERSEDED");

        DbContext.CustomerPasswordResetTokens.Add(resetToken);
        await DbContext.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
    }

    public async Task<CustomerPasswordResetContext?> FindActivePasswordResetAsync(
        Guid tenantId,
        string normalizedEmail,
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return await (
            from resetToken in DbContext.CustomerPasswordResetTokens
            join authAccount in DbContext.CustomerAuthAccounts
                on new { resetToken.TenantId, Id = resetToken.CustomerAuthAccountId }
                equals new { authAccount.TenantId, authAccount.Id }
            join customer in DbContext.Customers
                on new { authAccount.TenantId, Id = authAccount.CustomerId }
                equals new { customer.TenantId, customer.Id }
            join tenant in DbContext.Tenants.AsNoTracking()
                on authAccount.TenantId equals tenant.Id
            where resetToken.TenantId == tenantId &&
                  resetToken.TokenHash == tokenHash &&
                  resetToken.Status == "ACTIVE" &&
                  customer.NormalizedEmail == normalizedEmail &&
                  customer.Status != "DELETED"
            select new CustomerPasswordResetContext(
                resetToken,
                authAccount,
                customer.Email,
                customer.Name,
                customer.Status,
                tenant.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SavePasswordResetAsync(
        CustomerPasswordResetToken resetToken,
        CustomerAuthAccount account,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        DbContext.CustomerPasswordResetTokens.Update(resetToken);
        DbContext.CustomerAuthAccounts.Update(account);

        var sessions = await DbContext.CustomerAuthSessions
            .Where(x => x.TenantId == account.TenantId &&
                        x.CustomerAuthAccountId == account.Id &&
                        x.Status == "ACTIVE" &&
                        x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
            session.Revoke(now, "PASSWORD_RESET");

        var sessionIds = sessions.Select(x => x.Id).ToArray();
        if (sessionIds.Length > 0)
        {
            var refreshTokens = await DbContext.CustomerRefreshTokens
                .Where(x => x.TenantId == account.TenantId &&
                            sessionIds.Contains(x.CustomerAuthSessionId) &&
                            x.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var refreshToken in refreshTokens)
                refreshToken.Revoke(now, "PASSWORD_RESET");
        }

        await DbContext.SaveChangesAsync(cancellationToken);
    }
}