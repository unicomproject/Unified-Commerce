using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;

public sealed class CustomerExternalAuthRepository : CustomerAuthRepositoryBase, ICustomerExternalAuthRepository
{
    public CustomerExternalAuthRepository(EPosDbContext dbContext) : base(dbContext)
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

    public async Task<CustomerExternalLoginAccount?> FindExternalLoginAccountAsync(
        Guid tenantId,
        string providerCode,
        string providerSubject,
        bool trackAccount,
        bool trackExternalAccount,
        CancellationToken cancellationToken)
    {
        var authAccounts = trackAccount
            ? DbContext.CustomerAuthAccounts
            : DbContext.CustomerAuthAccounts.AsNoTracking();
        var externalAccounts = trackExternalAccount
            ? DbContext.CustomerExternalAuthAccounts
            : DbContext.CustomerExternalAuthAccounts.AsNoTracking();

        var row = await (
            from externalAccount in externalAccounts
            join authAccount in authAccounts
                on new { externalAccount.TenantId, Id = externalAccount.CustomerAuthAccountId }
                equals new { authAccount.TenantId, authAccount.Id }
            join customer in DbContext.Customers.AsNoTracking()
                on new { authAccount.TenantId, Id = authAccount.CustomerId }
                equals new { customer.TenantId, customer.Id }
            join tenant in DbContext.Tenants.AsNoTracking()
                on authAccount.TenantId equals tenant.Id
            where externalAccount.TenantId == tenantId &&
                  externalAccount.ProviderCode == providerCode &&
                  externalAccount.ProviderSubject == providerSubject &&
                  externalAccount.Status != "DELETED" &&
                  customer.Status != "DELETED"
            select new
            {
                ExternalAccount = externalAccount,
                AuthAccount = authAccount,
                Customer = customer,
                TenantStatus = tenant.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        var account = new CustomerLoginAccount(
            row.AuthAccount,
            row.Customer.Id,
            row.Customer.TenantId,
            row.Customer.Name,
            row.Customer.Email,
            row.Customer.Phone,
            row.Customer.Status,
            row.TenantStatus);

        return new CustomerExternalLoginAccount(account, row.ExternalAccount);
    }

    public async Task<bool> RegisterExternalCustomerAsync(
        CustomerEntity customer,
        CustomerAuthAccount account,
        CustomerExternalAuthAccount externalAccount,
        IReadOnlyCollection<CustomerConsent> consents,
        CustomerAuthSession session,
        CustomerRefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginSerializableTransactionAsync(cancellationToken);
        DbContext.Customers.Add(customer);
        DbContext.CustomerAuthAccounts.Add(account);
        DbContext.CustomerExternalAuthAccounts.Add(externalAccount);
        DbContext.CustomerConsents.AddRange(consents);
        DbContext.CustomerAuthSessions.Add(session);
        DbContext.CustomerRefreshTokens.Add(refreshToken);

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            await RollbackAsync(transaction, cancellationToken);
            Detach(customer, account, externalAccount, session, refreshToken);
            foreach (var consent in consents)
                DbContext.Entry(consent).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> LinkExternalAccountAndSaveLoginAsync(
        CustomerAuthAccount account,
        CustomerExternalAuthAccount externalAccount,
        CustomerAuthSession session,
        CustomerRefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginSerializableTransactionAsync(cancellationToken);
        DbContext.CustomerAuthAccounts.Update(account);
        DbContext.CustomerExternalAuthAccounts.Add(externalAccount);
        DbContext.CustomerAuthSessions.Add(session);
        DbContext.CustomerRefreshTokens.Add(refreshToken);

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            await RollbackAsync(transaction, cancellationToken);
            Detach(externalAccount, session, refreshToken);
            return false;
        }
    }

    public async Task SaveSuccessfulExternalLoginAsync(
        CustomerAuthAccount account,
        CustomerExternalAuthAccount externalAccount,
        CustomerAuthSession session,
        CustomerRefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        DbContext.CustomerAuthAccounts.Update(account);
        DbContext.CustomerExternalAuthAccounts.Update(externalAccount);
        DbContext.CustomerAuthSessions.Add(session);
        DbContext.CustomerRefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}