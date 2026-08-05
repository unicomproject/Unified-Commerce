using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;

public sealed class CustomerRegistrationRepository : CustomerAuthRepositoryBase, ICustomerRegistrationRepository
{
    public CustomerRegistrationRepository(EPosDbContext dbContext) : base(dbContext)
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

    public async Task<bool> RegisterCustomerAsync(
        CustomerEntity customer,
        CustomerAuthAccount account,
        CustomerVerificationOtp verificationOtp,
        IReadOnlyCollection<CustomerConsent> consents,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginSerializableTransactionAsync(cancellationToken);
        DbContext.Customers.Add(customer);
        DbContext.CustomerAuthAccounts.Add(account);
        DbContext.CustomerVerificationOtps.Add(verificationOtp);
        DbContext.CustomerConsents.AddRange(consents);

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
            Detach(customer, account, verificationOtp);
            foreach (var consent in consents)
                DbContext.Entry(consent).State = EntityState.Detached;
            return false;
        }
    }
}