using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerAddressService : ICustomerAddressService
{
    private readonly ICustomerAddressRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CustomerAddressService(ICustomerAddressRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<List<CustomerAddressDto>>> GetAddressesAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken)
    {
        var addresses = await _repository.GetAddressesAsync(tenantId, customerId, cancellationToken);
        
        var dtos = addresses.Select(x => new CustomerAddressDto
        {
            Id = x.Id,
            ContactName = x.ContactName,
            ContactPhone = x.ContactPhone,
            AddressLine1 = x.AddressLine1,
            AddressLine2 = x.AddressLine2,
            City = x.City,
            State = x.State,
            PostalCode = x.PostalCode,
            CountryCode = x.CountryCode,
            AddressType = x.AddressType,
            IsDefaultShipping = x.IsDefaultShipping,
            IsDefaultBilling = x.IsDefaultBilling
        }).ToList();

        return ApplicationResult<List<CustomerAddressDto>>.Success(dtos);
    }

    public async Task<ApplicationResult<CustomerAddressDto>> GetAddressByIdAsync(Guid tenantId, Guid customerId, Guid addressId, CancellationToken cancellationToken)
    {
        var address = await _repository.GetAddressByIdAsync(tenantId, customerId, addressId, cancellationToken);

        if (address is null)
            return ApplicationResult<CustomerAddressDto>.Failure(new ApplicationError("address.not_found", "Address not found"));

        var dto = new CustomerAddressDto
        {
            Id = address.Id,
            ContactName = address.ContactName,
            ContactPhone = address.ContactPhone,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            CountryCode = address.CountryCode,
            AddressType = address.AddressType,
            IsDefaultShipping = address.IsDefaultShipping,
            IsDefaultBilling = address.IsDefaultBilling
        };

        return ApplicationResult<CustomerAddressDto>.Success(dto);
    }

    public async Task<ApplicationResult<CustomerAddressDto>> CreateAddressAsync(Guid tenantId, Guid customerId, CreateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        if (request.IsDefaultShipping)
        {
            var existingDefaultShipping = await _repository.GetDefaultShippingAddressesAsync(tenantId, customerId, cancellationToken);
            foreach (var addr in existingDefaultShipping)
                addr.SetDefaultShipping(false);
        }

        if (request.IsDefaultBilling)
        {
            var existingDefaultBilling = await _repository.GetDefaultBillingAddressesAsync(tenantId, customerId, cancellationToken);
            foreach (var addr in existingDefaultBilling)
                addr.SetDefaultBilling(false);
        }

        var address = CustomerAddress.Create(
            Guid.NewGuid(),
            tenantId,
            customerId,
            request.ContactName,
            request.ContactPhone,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.PostalCode,
            request.CountryCode,
            request.AddressType,
            request.IsDefaultShipping,
            request.IsDefaultBilling,
            _dateTimeProvider.UtcNow);

        _repository.AddAddress(address);
        await _repository.SaveChangesAsync(cancellationToken);

        return await GetAddressByIdAsync(tenantId, customerId, address.Id, cancellationToken);
    }

    public async Task<ApplicationResult<CustomerAddressDto>> UpdateAddressAsync(Guid tenantId, Guid customerId, Guid addressId, UpdateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await _repository.GetAddressByIdAsync(tenantId, customerId, addressId, cancellationToken);

        if (address is null)
            return ApplicationResult<CustomerAddressDto>.Failure(new ApplicationError("address.not_found", "Address not found"));

        if (request.IsDefaultShipping && !address.IsDefaultShipping)
        {
            var existingDefaultShipping = await _repository.GetDefaultShippingAddressesAsync(tenantId, customerId, cancellationToken);
            foreach (var addr in existingDefaultShipping.Where(a => a.Id != addressId))
                addr.SetDefaultShipping(false);
        }

        if (request.IsDefaultBilling && !address.IsDefaultBilling)
        {
            var existingDefaultBilling = await _repository.GetDefaultBillingAddressesAsync(tenantId, customerId, cancellationToken);
            foreach (var addr in existingDefaultBilling.Where(a => a.Id != addressId))
                addr.SetDefaultBilling(false);
        }

        address.Update(
            request.ContactName,
            request.ContactPhone,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.PostalCode,
            request.CountryCode,
            request.AddressType,
            request.IsDefaultShipping,
            request.IsDefaultBilling,
            _dateTimeProvider.UtcNow);

        await _repository.SaveChangesAsync(cancellationToken);

        return await GetAddressByIdAsync(tenantId, customerId, address.Id, cancellationToken);
    }

    public async Task<ApplicationResult> DeleteAddressAsync(Guid tenantId, Guid customerId, Guid addressId, CancellationToken cancellationToken)
    {
        var address = await _repository.GetAddressByIdAsync(tenantId, customerId, addressId, cancellationToken);

        if (address is null)
            return ApplicationResult.Failure(new ApplicationError("address.not_found", "Address not found"));

        _repository.RemoveAddress(address);
        await _repository.SaveChangesAsync(cancellationToken);

        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> SetDefaultAddressAsync(Guid tenantId, Guid customerId, Guid addressId, string type, CancellationToken cancellationToken)
    {
        var address = await _repository.GetAddressByIdAsync(tenantId, customerId, addressId, cancellationToken);

        if (address is null)
            return ApplicationResult.Failure(new ApplicationError("address.not_found", "Address not found"));

        if (type.Equals("SHIPPING", StringComparison.OrdinalIgnoreCase))
        {
            var existingDefaultShipping = await _repository.GetDefaultShippingAddressesAsync(tenantId, customerId, cancellationToken);
            foreach (var addr in existingDefaultShipping.Where(a => a.Id != addressId))
                addr.SetDefaultShipping(false);
                
            address.SetDefaultShipping(true);
        }
        else if (type.Equals("BILLING", StringComparison.OrdinalIgnoreCase))
        {
            var existingDefaultBilling = await _repository.GetDefaultBillingAddressesAsync(tenantId, customerId, cancellationToken);
            foreach (var addr in existingDefaultBilling.Where(a => a.Id != addressId))
                addr.SetDefaultBilling(false);
                
            address.SetDefaultBilling(true);
        }
        else
        {
            return ApplicationResult.Failure(new ApplicationError("address.invalid_type", "Type must be SHIPPING or BILLING"));
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}
