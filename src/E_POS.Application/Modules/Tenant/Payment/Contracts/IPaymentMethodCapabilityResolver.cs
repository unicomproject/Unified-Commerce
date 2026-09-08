namespace E_POS.Application.Modules.Tenant.Payment.Contracts;

public sealed record PaymentMethodCapabilityContext(
    Guid TenantId,
    Guid TenantUserId,
    Guid DeviceId);

public interface IPaymentMethodExecutionCapability
{
    string MethodCode { get; }

    Task<bool> IsExecutableAsync(
        PaymentMethodCapabilityContext context,
        CancellationToken cancellationToken);
}

public interface IPaymentMethodCapabilityResolver
{
    Task<bool> IsExecutableAsync(
        string methodCode,
        PaymentMethodCapabilityContext context,
        CancellationToken cancellationToken);
}
