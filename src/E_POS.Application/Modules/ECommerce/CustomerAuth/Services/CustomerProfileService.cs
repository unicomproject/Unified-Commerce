using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Mappers;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerProfileService : ICustomerProfileService
{
    private readonly ICustomerProfileRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CustomerProfileService(
        ICustomerProfileRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<CustomerProfileResponse>> GetProfileAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await _repository.GetCustomerByIdAsync(tenantId, customerId, cancellationToken);

        if (customer is null)
            return ApplicationResult<CustomerProfileResponse>.Failure(CustomerAuthErrors.CustomerNotFound);

        return ApplicationResult<CustomerProfileResponse>.Success(CustomerProfileMapper.ToResponse(customer));
    }

    public async Task<ApplicationResult> UpdateProfileAsync(
        Guid tenantId,
        Guid customerId,
        CustomerProfileUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _repository.GetCustomerByIdAsync(tenantId, customerId, cancellationToken);

        if (customer is null)
            return ApplicationResult.Failure(CustomerAuthErrors.CustomerNotFound);

        if (string.IsNullOrWhiteSpace(request.FirstName))
            return ApplicationResult.Failure(CustomerAuthErrors.InvalidFirstName);

        customer.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            _dateTimeProvider.UtcNow);

        await _repository.UpdateCustomerAsync(customer, cancellationToken);

        return ApplicationResult.Success();
    }
}
