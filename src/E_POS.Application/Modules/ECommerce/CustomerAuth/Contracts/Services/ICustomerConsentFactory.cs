using System.Net;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerConsentFactory
{
    IReadOnlyCollection<CustomerConsent> CreateRegistrationConsents(
        Guid tenantId,
        Guid customerId,
        bool sendOffers,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now);
}
