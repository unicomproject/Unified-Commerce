using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;

public sealed class CustomerLoginRepository : CustomerAuthRepositoryBase, ICustomerLoginRepository
{
    public CustomerLoginRepository(EPosDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<CustomerLoginAccount?> FindLoginAccountAsync(
        Guid tenantId,
        string normalizedEmail,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        var row = await (
            from authAccount in DbContext.CustomerAuthAccounts
            join customer in DbContext.Customers
                on new { authAccount.TenantId, Id = authAccount.CustomerId }
                equals new { customer.TenantId, customer.Id }
            join tenant in DbContext.Tenants
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
        DbContext.CustomerAuthAccounts.Update(account);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveSuccessfulLoginAsync(
        CustomerAuthAccount account,
        CustomerAuthSession session,
        CustomerRefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        DbContext.CustomerAuthSessions.Add(session);
        DbContext.CustomerRefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}