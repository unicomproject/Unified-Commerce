using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerPasswordResetLinkBuilder
{
    string BuildResetUrl(string email, string rawToken);
}
