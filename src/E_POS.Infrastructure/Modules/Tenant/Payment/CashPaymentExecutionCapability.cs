using E_POS.Application.Modules.Tenant.Payment.Contracts;

namespace E_POS.Infrastructure.Modules.Tenant.Payment;

public sealed class CashPaymentExecutionCapability : IPaymentMethodExecutionCapability
{
    public string MethodCode => "CASH";

    public Task<bool> IsExecutableAsync(
        PaymentMethodCapabilityContext context,
        CancellationToken cancellationToken) => Task.FromResult(true);
}
