using E_POS.Application.Modules.Tenant.HardwareCash;
using Xunit;

namespace E_POS.UnitTests.HardwareCash;

public sealed class CashDrawerStableRequestIdTests
{
    [Fact]
    public void ForBusinessReference_SameSaleAndPurpose_SameId()
    {
        var saleId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var a = CashDrawerStableRequestId.ForBusinessReference(saleId, "cashSale");
        var b = CashDrawerStableRequestId.ForBusinessReference(saleId, "cashSale");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ForBusinessReference_DifferentPurpose_DifferentId()
    {
        var saleId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var cash = CashDrawerStableRequestId.ForBusinessReference(saleId, "cashSale");
        var split = CashDrawerStableRequestId.ForBusinessReference(saleId, "splitPaymentCash");
        Assert.NotEqual(cash, split);
    }

    [Fact]
    public void ForBusinessReference_PurposeIsCaseInsensitive()
    {
        var saleId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var a = CashDrawerStableRequestId.ForBusinessReference(saleId, "CashSale");
        var b = CashDrawerStableRequestId.ForBusinessReference(saleId, "cashsale");
        Assert.Equal(a, b);
    }
}
