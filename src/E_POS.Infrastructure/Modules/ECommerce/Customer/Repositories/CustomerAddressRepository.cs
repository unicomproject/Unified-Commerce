using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using Microsoft.EntityFrameworkCore;

using E_POS.Infrastructure.Persistence;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Repositories;

public class CustomerAddressRepository : ICustomerAddressRepository
{
    private readonly EPosDbContext _context;

    public CustomerAddressRepository(EPosDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerAddress>> GetAddressesAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken)
    {
        return await _context.CustomerAddresses
            .Where(x => x.TenantId == tenantId && x.CustomerId == customerId)
            .OrderByDescending(x => x.IsDefaultShipping)
            .ThenByDescending(x => x.IsDefaultBilling)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerAddress?> GetAddressByIdAsync(Guid tenantId, Guid customerId, Guid addressId, CancellationToken cancellationToken)
    {
        return await _context.CustomerAddresses
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.CustomerId == customerId && x.Id == addressId, cancellationToken);
    }

    public async Task<List<CustomerAddress>> GetAddressesByTypeAsync(Guid tenantId, Guid customerId, string type, CancellationToken cancellationToken)
    {
        return await _context.CustomerAddresses
            .Where(x => x.TenantId == tenantId && x.CustomerId == customerId && x.AddressType == type)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CustomerAddress>> GetDefaultShippingAddressesAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken)
    {
        return await _context.CustomerAddresses
            .Where(x => x.TenantId == tenantId && x.CustomerId == customerId && x.IsDefaultShipping)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CustomerAddress>> GetDefaultBillingAddressesAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken)
    {
        return await _context.CustomerAddresses
            .Where(x => x.TenantId == tenantId && x.CustomerId == customerId && x.IsDefaultBilling)
            .ToListAsync(cancellationToken);
    }

    public void AddAddress(CustomerAddress address)
    {
        _context.CustomerAddresses.Add(address);
    }

    public void RemoveAddress(CustomerAddress address)
    {
        _context.CustomerAddresses.Remove(address);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
