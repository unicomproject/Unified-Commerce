using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerAuthValidator
{
    ApplicationError? ValidateRegister(Guid tenantId, CustomerRegisterRequest request);
    ApplicationError? ValidateVerifyEmail(Guid tenantId, CustomerVerifyEmailRequest request);
    ApplicationError? ValidateResetPassword(Guid tenantId, CustomerResetPasswordRequest request);
    ApplicationError? ValidateGoogleLogin(Guid tenantId, CustomerGoogleLoginRequest request);
    ApplicationError? ValidateLogin(Guid tenantId, CustomerLoginRequest request);
    ApplicationError? ValidateEmailOnly(Guid tenantId, string? email);
    ApplicationError? ValidateExternalLoginAccount(CustomerLoginAccount account, DateTimeOffset now);
    string NormalizeEmailAddress(string? email);
    bool IsTenantActive(string status);
    bool IsCustomerActive(string status);
}
