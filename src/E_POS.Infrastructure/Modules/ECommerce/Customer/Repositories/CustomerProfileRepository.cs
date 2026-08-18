using E_POS.Application.Modules.ECommerce.Customer.Contracts.Interfaces;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Repositories;

public sealed class CustomerProfileRepository : CustomerAuthRepositoryBase, ICustomerProfileRepository
{
    public CustomerProfileRepository(EPosDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<CustomerEntity?> GetCustomerByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await DbContext.Customers
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == customerId, cancellationToken);
    }

    public async Task UpdateCustomerAsync(
        CustomerEntity customer,
        CancellationToken cancellationToken)
    {
        DbContext.Customers.Update(customer);
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
