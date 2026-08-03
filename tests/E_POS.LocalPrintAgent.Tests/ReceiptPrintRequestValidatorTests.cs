using E_POS.LocalPrintAgent.Models;
using E_POS.LocalPrintAgent.Validation;
using Xunit;

namespace E_POS.LocalPrintAgent.Tests;

public sealed class ReceiptPrintRequestValidatorTests
{
    private readonly ReceiptPrintRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ReturnsNoErrors()
    {
        Assert.Empty(_validator.Validate(EscPosReceiptBuilderTests.ValidRequest()));
    }

    [Fact]
    public void Validate_WithMissingIdentityItemsAndNegativeValues_ReturnsFieldErrors()
    {
        var invalid = new ReceiptPrintRequest(
            Guid.Empty, "", DateTimeOffset.UtcNow, "", null, null, null, "",
            [], -1, -1, -1, -1, "", -1, -1, null);

        var errors = _validator.Validate(invalid);

        Assert.Contains("requestId", errors.Keys);
        Assert.Contains("receiptNumber", errors.Keys);
        Assert.Contains("merchantName", errors.Keys);
        Assert.Contains("items", errors.Keys);
        Assert.Contains("total", errors.Keys);
        Assert.Contains("paymentMethod", errors.Keys);
    }

    [Fact]
    public void Validate_WithInvalidLine_ReturnsIndexedLineErrors()
    {
        var valid = EscPosReceiptBuilderTests.ValidRequest();
        var invalid = valid with { Items = [new ReceiptLineRequest("", 0, -1, -1)] };

        var errors = _validator.Validate(invalid);

        Assert.Contains("items[0].name", errors.Keys);
        Assert.Contains("items[0].quantity", errors.Keys);
        Assert.Contains("items[0].unitPrice", errors.Keys);
        Assert.Contains("items[0].lineTotal", errors.Keys);
    }

    [Fact]
    public void Validate_WithUnsupportedCode39Character_ReturnsBarcodeError()
    {
        var invalid = EscPosReceiptBuilderTests.ValidRequest() with
        {
            BarcodeValue = "RCPT_42"
        };

        var errors = _validator.Validate(invalid);

        Assert.Contains("barcodeValue", errors.Keys);
    }

    [Fact]
    public void Validate_V2TenderAmountsMustReconcile()
    {
        var invalid = EscPosReceiptBuilderTests.ValidRequest() with
        {
            Tenders = [new("CARD", "Card", "CARD", 250m, null, null, "LKR", "PAID")]
        };

        Assert.Contains("tenders", _validator.Validate(invalid).Keys);
    }

    [Theory]
    [InlineData("4242424242424242", "4242")]
    [InlineData("AUTH-1", "123456789012")]
    public void Validate_RejectsUnsafeCardData(string authorizationReference, string last4)
    {
        var invalid = EscPosReceiptBuilderTests.ValidRequest() with
        {
            Tenders =
            [
                new("CARD", "Card", "CARD", 251m, null, null, "LKR", "PAID",
                    CardBrand: "VISA",
                    MaskedCardLast4: last4,
                    AuthorizationReference: authorizationReference)
            ]
        };

        Assert.Contains(_validator.Validate(invalid).Keys,
            key => key.StartsWith("tenders[0]", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_DetailedTaxAndDiscountMustReconcile()
    {
        var invalid = EscPosReceiptBuilderTests.ValidRequest() with
        {
            DiscountTotal = 10m,
            DiscountLines = [new("TRANSACTION", null, "Offer", null, null, 9m)],
            TaxTotal = 5m,
            TaxLines = [new("VAT", "VAT", 2m, 250m, 4m)]
        };

        var errors = _validator.Validate(invalid);
        Assert.Contains("discountLines", errors.Keys);
        Assert.Contains("taxLines", errors.Keys);
    }

    [Fact]
    public void Validate_RejectsUnsupportedPurposeAndOversizedReceipt()
    {
        var invalid = EscPosReceiptBuilderTests.ValidRequest() with
        {
            ReceiptPurpose = "kitchenTicket",
            Items = Enumerable.Range(0, 501)
                .Select(_ => new ReceiptLineRequest("Item", 1, 1, 1))
                .ToArray()
        };

        var errors = _validator.Validate(invalid);
        Assert.Contains("receiptPurpose", errors.Keys);
        Assert.Contains("items", errors.Keys);
    }

    [Theory]
    [InlineData("return")]
    [InlineData("exchange")]
    [InlineData("refund")]
    public void Validate_NonSaleWithoutOriginalReference_IsRejected(string purpose)
    {
        var errors = _validator.Validate(
            EscPosReceiptBuilderTests.ValidRequest() with
            {
                ReceiptPurpose = purpose,
                OriginalReceiptReference = null
            });

        Assert.Contains("originalReceiptReference", errors.Keys);
    }
}
