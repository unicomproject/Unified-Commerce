using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ExternalBrandMappingContextValidatorTests
{
    [Fact]
    public void Validate_NullContext_ReturnsNoErrors()
    {
        var errors = ExternalBrandMappingContextValidator.Validate(null);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("openfoodfacts", "coca cola", "Coca-Cola")]
    [InlineData("OPENFOODFACTS", "COCA COLA", "Coca-Cola")]
    [InlineData("  openfoodfacts  ", "  coca cola  ", "  Coca-Cola  ")]
    public void Validate_ValidContext_ReturnsNoErrors(string provider, string key, string name)
    {
        var context = new ExternalBrandMappingContext(provider, key, name);

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ValidContext_WithoutName_Succeeds()
    {
        var context = new ExternalBrandMappingContext("openfoodfacts", "coca cola", null);

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingProvider_ReturnsError(string? provider)
    {
        var context = new ExternalBrandMappingContext(provider!, "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("Provider is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ProviderBoundary_Length50_Succeeds()
    {
        var provider50 = new string('p', 50);
        var context = new ExternalBrandMappingContext(provider50, "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ProviderBoundary_Length51_ReturnsError()
    {
        var provider51 = new string('p', 51);
        var context = new ExternalBrandMappingContext(provider51, "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("cannot exceed 50 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingKey_ReturnsError(string? key)
    {
        var context = new ExternalBrandMappingContext("openfoodfacts", key!, "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("External brand key is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_KeyOfOnlyPunctuation_ReturnsInvalidError()
    {
        var context = new ExternalBrandMappingContext("openfoodfacts", "---,,,---", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("invalid or contains only punctuation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_KeyBoundary_Length150_Succeeds()
    {
        var key150 = new string('k', 150);
        var context = new ExternalBrandMappingContext("openfoodfacts", key150, "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_KeyBoundary_Length151_ReturnsError()
    {
        var key151 = new string('k', 151);
        var context = new ExternalBrandMappingContext("openfoodfacts", key151, "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("cannot exceed 150 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NameBoundary_Length150_Succeeds()
    {
        var name150 = new string('n', 150);
        var context = new ExternalBrandMappingContext("openfoodfacts", "coca cola", name150);

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NameBoundary_Length151_ReturnsError()
    {
        var name151 = new string('n', 151);
        var context = new ExternalBrandMappingContext("openfoodfacts", "coca cola", name151);

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("cannot exceed 150 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_AllowedProvidersOmitted_NoAllowlistEnforcement_AnyProviderAccepted()
    {
        var context = new ExternalBrandMappingContext("totally-unknown-provider", "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_AllowedProvidersEmpty_NoAllowlistEnforcement_AnyProviderAccepted()
    {
        var context = new ExternalBrandMappingContext("totally-unknown-provider", "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context, allowedProviders: []);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ProviderInAllowlist_ReturnsNoErrors()
    {
        var context = new ExternalBrandMappingContext("openfoodfacts", "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context, allowedProviders: ["openfoodfacts", "upcitemdb"]);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ProviderInAllowlist_CaseInsensitive_ReturnsNoErrors()
    {
        var context = new ExternalBrandMappingContext("OpenFoodFacts", "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context, allowedProviders: ["openfoodfacts"]);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("cache")]
    [InlineData("CACHE")]
    [InlineData("random")]
    [InlineData("foo")]
    public void Validate_ProviderNotInAllowlist_ReturnsError(string provider)
    {
        var context = new ExternalBrandMappingContext(provider, "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context, allowedProviders: ["openfoodfacts"]);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("not a recognized external product-lookup provider", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_DisabledButConfiguredProvider_StillAllowed()
    {
        // Allowlist reflects "configured" providers (see GetConfiguredProviderNames), not
        // "enabled" ones — a temporarily disabled provider's historical mappings must remain valid.
        var context = new ExternalBrandMappingContext("upcitemdb", "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context, allowedProviders: ["openfoodfacts", "upcitemdb"]);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ProviderTooLong_ReturnsLengthError_NotAllowlistError()
    {
        var provider51 = new string('p', 51);
        var context = new ExternalBrandMappingContext(provider51, "coca cola", "Coca-Cola");

        var errors = ExternalBrandMappingContextValidator.Validate(context, allowedProviders: ["openfoodfacts"]);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("cannot exceed 50 characters", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(errors, e => e.Message.Contains("not a recognized", StringComparison.OrdinalIgnoreCase));
    }
}
