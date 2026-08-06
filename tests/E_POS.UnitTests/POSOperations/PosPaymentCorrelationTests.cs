using E_POS.Application.Common.Models;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosPaymentCorrelationTests
{
    [Fact]
    public void FromIdempotencyKey_IsDeterministicAndDoesNotExposeRawKey()
    {
        const string key = "pos-private-idempotency-key";

        var first = PosPaymentCorrelation.FromIdempotencyKey(key);
        var second = PosPaymentCorrelation.FromIdempotencyKey(key);

        Assert.Equal(first, second);
        Assert.Equal(12, first.Length);
        Assert.Matches("^[0-9a-f]{12}$", first);
        Assert.DoesNotContain(key, first, StringComparison.Ordinal);
    }

    [Fact]
    public void FromIdempotencyKey_WithMissingKey_StillReturnsSafeFingerprint()
    {
        var correlation = PosPaymentCorrelation.FromIdempotencyKey(null);

        Assert.Equal(12, correlation.Length);
        Assert.Matches("^[0-9a-f]{12}$", correlation);
    }
}
