using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ProductSetupScanBootstrapNormalizerTests
{
    [Fact]
    public void TryNormalize_ScanContinueWithBarcode_PreservesLeadingZeros()
    {
        var error = ProductSetupScanBootstrapNormalizer.TryNormalize(
            new ProductSetupScanBootstrapRequest
            {
                AcquisitionMode = "SCAN",
                CreationAction = "CONTINUE_WITH_BARCODE",
                CandidateIdentifier = "012345678905",
                ExternalLookupStatus = "NOT_STARTED"
            },
            out var persistence);

        Assert.Null(error);
        Assert.NotNull(persistence);
        Assert.Equal("012345678905", persistence!.CandidateIdentifier);
        Assert.Equal("GTIN12", persistence.IdentifierStandard);
        Assert.Equal("UNKNOWN", persistence.SymbologyHint);
        Assert.Equal("NOT_STARTED", persistence.ExternalLookupStatus);
        Assert.False(persistence.ApplyPrefillToDraft);
        Assert.Null(persistence.NormalizedPrefillJson);
    }

    [Fact]
    public void TryNormalize_UseThisProduct_RequiresFoundAndPrefill()
    {
        var missing = ProductSetupScanBootstrapNormalizer.TryNormalize(
            new ProductSetupScanBootstrapRequest
            {
                AcquisitionMode = "SCAN",
                CreationAction = "USE_THIS_PRODUCT",
                CandidateIdentifier = "4006381333931",
                ExternalLookupStatus = "FOUND"
            },
            out _);
        Assert.NotNull(missing);

        var ok = ProductSetupScanBootstrapNormalizer.TryNormalize(
            new ProductSetupScanBootstrapRequest
            {
                AcquisitionMode = "SCAN",
                CreationAction = "USE_THIS_PRODUCT",
                CandidateIdentifier = "4006381333931",
                ExternalLookupStatus = "FOUND",
                ExternalSourceReference = "src-1",
                NormalizedPrefill = new ExternalProductSuggestion(
                    "External Name", null, "Brand", null, null, null, "Short", null, null, null, "GTIN13")
            },
            out var persistence);

        Assert.Null(ok);
        Assert.True(persistence!.ApplyPrefillToDraft);
        Assert.Contains("External Name", persistence.NormalizedPrefillJson);
        Assert.Equal("FOUND", persistence.ExternalLookupStatus);
    }

    [Fact]
    public void TryNormalize_CreateManually_DiscardsPrefill()
    {
        var error = ProductSetupScanBootstrapNormalizer.TryNormalize(
            new ProductSetupScanBootstrapRequest
            {
                AcquisitionMode = "MANUAL",
                CreationAction = "CREATE_MANUALLY",
                CandidateIdentifier = "4006381333931",
                ExternalLookupStatus = "FOUND",
                NormalizedPrefill = new ExternalProductSuggestion(
                    "Ignore Me", null, null, null, null, null, null, null, null, null, null)
            },
            out var persistence);

        Assert.Null(error);
        Assert.Equal("4006381333931", persistence!.CandidateIdentifier);
        Assert.False(persistence.ApplyPrefillToDraft);
        Assert.Null(persistence.NormalizedPrefillJson);
    }

    [Theory]
    [InlineData("OWN_MADE")]
    [InlineData("SERVICE_FEE")]
    [InlineData("UNLABELLED")]
    public void TryNormalize_NoBarcode_Reasons(string reason)
    {
        var error = ProductSetupScanBootstrapNormalizer.TryNormalize(
            new ProductSetupScanBootstrapRequest
            {
                AcquisitionMode = "NO_BARCODE",
                CreationAction = "CONTINUE_TO_BASIC_DETAILS",
                NoBarcodeReason = reason,
                GeneratedSkuCandidate = "SKU-NB"
            },
            out var persistence);

        Assert.Null(error);
        Assert.Equal("NO_BARCODE", persistence!.AcquisitionMode);
        Assert.Equal(reason, persistence.NoBarcodeReason);
        Assert.Equal("SKU-NB", persistence.GeneratedSkuCandidate);
        Assert.Equal("NOT_STARTED", persistence.ExternalLookupStatus);
        Assert.Null(persistence.CandidateIdentifier);
    }

    [Fact]
    public void TryNormalize_NoBarcode_WithCandidate_Rejected()
    {
        var error = ProductSetupScanBootstrapNormalizer.TryNormalize(
            new ProductSetupScanBootstrapRequest
            {
                AcquisitionMode = "NO_BARCODE",
                CreationAction = "CONTINUE_TO_BASIC_DETAILS",
                NoBarcodeReason = "OWN_MADE",
                CandidateIdentifier = "4006381333931"
            },
            out _);

        Assert.NotNull(error);
        Assert.Equal("product.validation_failed", error!.Code);
    }

    [Fact]
    public void TryNormalize_Scan_MissingIdentifier_Rejected()
    {
        var error = ProductSetupScanBootstrapNormalizer.TryNormalize(
            new ProductSetupScanBootstrapRequest
            {
                AcquisitionMode = "SCAN",
                CreationAction = "CONTINUE_WITH_BARCODE"
            },
            out _);

        Assert.NotNull(error);
    }

    [Fact]
    public void TryNormalize_LegacyMode_Rejected()
    {
        var error = ProductSetupScanBootstrapNormalizer.TryNormalize(
            new ProductSetupScanBootstrapRequest
            {
                AcquisitionMode = "LEGACY",
                CreationAction = "CONTINUE_WITH_BARCODE",
                CandidateIdentifier = "4006381333931"
            },
            out _);

        Assert.NotNull(error);
    }

    [Fact]
    public void ApplyPrefillToDraftRequest_FillsEmptyFieldsOnly()
    {
        var request = new SaveProductDraftRequest { ProductName = "Keep Me" };
        ProductSetupScanBootstrapNormalizer.ApplyPrefillToDraftRequest(
            request,
            new ExternalProductSuggestion(
                "External", "Short", null, null, null, null, "SD", "LD", null, null, null));

        Assert.Equal("Keep Me", request.ProductName);
        Assert.Equal("Short", request.ShortName);
        Assert.Equal("SD", request.ShortDescription);
        Assert.Equal("LD", request.LongDescription);
    }
}
