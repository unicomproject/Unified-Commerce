using E_POS.Application.Modules.Tenant.Payment.Contracts;

namespace E_POS.Application.Modules.Tenant.Payment.Services;

public sealed class PaymentMethodCapabilityResolver(
    IEnumerable<IPaymentMethodExecutionCapability> capabilities)
    : IPaymentMethodCapabilityResolver
{
    private readonly IReadOnlyDictionary<string, IPaymentMethodExecutionCapability> _capabilities =
        capabilities.ToDictionary(
            capability => capability.MethodCode,
            StringComparer.OrdinalIgnoreCase);

    public Task<bool> IsExecutableAsync(
        string methodCode,
        PaymentMethodCapabilityContext context,
        CancellationToken cancellationToken) =>
        _capabilities.TryGetValue(methodCode, out var capability)
            ? capability.IsExecutableAsync(context, cancellationToken)
            : Task.FromResult(false);
}
