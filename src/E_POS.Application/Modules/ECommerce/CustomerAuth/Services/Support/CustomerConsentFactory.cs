using System.Net;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;

public sealed class CustomerConsentFactory : ICustomerConsentFactory
{
    public IReadOnlyCollection<CustomerConsent> CreateRegistrationConsents(
        Guid tenantId,
        Guid customerId,
        bool sendOffers,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now)
    {
        var consents = new List<CustomerConsent>
        {
            CustomerConsent.Grant(Guid.NewGuid(), tenantId, customerId, "TERMS", null, null, "ECOMMERCE", ipAddress, userAgent, now),
            CustomerConsent.Grant(Guid.NewGuid(), tenantId, customerId, "PRIVACY", null, null, "ECOMMERCE", ipAddress, userAgent, now)
        };

        if (sendOffers)
        {
            consents.Add(CustomerConsent.Grant(
                Guid.NewGuid(),
                tenantId,
                customerId,
                "MARKETING_EMAIL",
                null,
                null,
                "ECOMMERCE",
                ipAddress,
                userAgent,
                now));
        }

        return consents;
    }
}
