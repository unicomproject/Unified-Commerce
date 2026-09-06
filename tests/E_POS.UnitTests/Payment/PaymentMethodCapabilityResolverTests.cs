using E_POS.Application.Modules.Tenant.Payment.Contracts;
using E_POS.Application.Modules.Tenant.Payment.Services;
using Xunit;

namespace E_POS.UnitTests.Payment;

public sealed class PaymentMethodCapabilityResolverTests
{
    private static readonly PaymentMethodCapabilityContext Context =
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Theory]
    [InlineData("CASH")]
    [InlineData("CARD")]
    [InlineData("QR")]
    [InlineData("SPLIT")]
    public async Task RegisteredExecutableCapability_ReturnsTrue(string methodCode)
    {
        var resolver = new PaymentMethodCapabilityResolver(
            [new StubCapability(methodCode, true)]);

        Assert.True(await resolver.IsExecutableAsync(
            methodCode, Context, CancellationToken.None));
    }

    [Fact]
    public async Task RegisteredUnavailableCapability_ReturnsFalse()
    {
        var resolver = new PaymentMethodCapabilityResolver(
            [new StubCapability("CARD", false)]);

        Assert.False(await resolver.IsExecutableAsync(
            "CARD", Context, CancellationToken.None));
    }

    [Theory]
    [InlineData("CARD")]
    [InlineData("QR")]
    [InlineData("SPLIT")]
    [InlineData("UNEXPECTED")]
    public async Task UnregisteredCapability_FailsClosed(string methodCode)
    {
        var resolver = new PaymentMethodCapabilityResolver(
            [new StubCapability("CASH", true)]);

        Assert.False(await resolver.IsExecutableAsync(
            methodCode, Context, CancellationToken.None));
    }

    [Fact]
    public async Task MethodCodeMatching_IsCaseInsensitive()
    {
        var resolver = new PaymentMethodCapabilityResolver(
            [new StubCapability("CASH", true)]);

        Assert.True(await resolver.IsExecutableAsync(
            "cash", Context, CancellationToken.None));
    }

    private sealed class StubCapability(string methodCode, bool executable)
        : IPaymentMethodExecutionCapability
    {
        public string MethodCode => methodCode;

        public Task<bool> IsExecutableAsync(
            PaymentMethodCapabilityContext context,
            CancellationToken cancellationToken) => Task.FromResult(executable);
    }
}
