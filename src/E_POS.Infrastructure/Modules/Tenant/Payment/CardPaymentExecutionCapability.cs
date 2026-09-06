using E_POS.Application.Modules.Tenant.Payment.Contracts;

namespace E_POS.Infrastructure.Modules.Tenant.Payment;

public sealed class CardPaymentExecutionCapability(ICardPaymentGateway gateway)
    : IPaymentMethodExecutionCapability
{
    public string MethodCode => "CARD";

    public Task<bool> IsExecutableAsync(
        PaymentMethodCapabilityContext context,
        CancellationToken cancellationToken) =>
        gateway.IsExecutableAsync(context, cancellationToken);
}
