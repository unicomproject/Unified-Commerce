using E_POS.Application.Modules.ECommerce.Customer.Contracts.Interfaces;
namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerAuthRepository :
    ICustomerRegistrationRepository,
    ICustomerLoginRepository,
    ICustomerEmailVerificationRepository,
    ICustomerPasswordResetRepository,
    ICustomerExternalAuthRepository,
    ICustomerSessionRepository,
    ICustomerProfileRepository
{
}

