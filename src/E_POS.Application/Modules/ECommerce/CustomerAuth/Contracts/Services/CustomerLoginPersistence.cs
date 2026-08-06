using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public sealed record CustomerLoginPersistence(
    CustomerAuthTokenResult TokenResult,
    CustomerAuthSession Session,
    CustomerRefreshToken RefreshToken);
