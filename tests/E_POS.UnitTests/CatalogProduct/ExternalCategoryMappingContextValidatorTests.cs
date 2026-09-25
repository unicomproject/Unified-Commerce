using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ExternalCategoryMappingContextValidatorTests
{
    [Fact]
    public void Validate_NullContext_ReturnsNoErrors()
    {
        var errors = ExternalCategoryMappingContextValidator.Validate(null);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("openfoodfacts", "en:colas", "Colas")]
    [InlineData("OPENFOODFACTS", "EN:COLAS", "Colas")]
    [InlineData("  openfoodfacts  ", "  en:colas  ", "  Colas  ")]
    public void Validate_ValidContext_ReturnsNoErrors(string provider, string key, string name)
    {
        var context = new ExternalCategoryMappingContext(provider, key, name);

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ValidContext_WithoutName_Succeeds()
    {
        var context = new ExternalCategoryMappingContext("openfoodfacts", "en:colas", null);

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingProvider_ReturnsError(string? provider)
    {
        var context = new ExternalCategoryMappingContext(provider!, "en:colas", "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("Provider is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ProviderBoundary_Length50_Succeeds()
    {
        var provider50 = new string('p', 50);
        var context = new ExternalCategoryMappingContext(provider50, "en:colas", "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ProviderBoundary_Length51_ReturnsError()
    {
        var provider51 = new string('p', 51);
        var context = new ExternalCategoryMappingContext(provider51, "en:colas", "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("cannot exceed 50 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingKey_ReturnsError(string? key)
    {
        var context = new ExternalCategoryMappingContext("openfoodfacts", key!, "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("External category key is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_KeyBoundary_Length150_Succeeds()
    {
        var key150 = new string('k', 150);
        var context = new ExternalCategoryMappingContext("openfoodfacts", key150, "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_KeyBoundary_Length151_ReturnsError()
    {
        var key151 = new string('k', 151);
        var context = new ExternalCategoryMappingContext("openfoodfacts", key151, "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("cannot exceed 150 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_KeyWithControlCharacters_ReturnsError()
    {
        var invalidKey = "\u0000\u0001\u0002";
        var context = new ExternalCategoryMappingContext("openfoodfacts", invalidKey, "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("invalid or contains only control characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NameBoundary_Length150_Succeeds()
    {
        var name150 = new string('n', 150);
        var context = new ExternalCategoryMappingContext("openfoodfacts", "en:colas", name150);

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NameBoundary_Length151_ReturnsError()
    {
        var name151 = new string('n', 151);
        var context = new ExternalCategoryMappingContext("openfoodfacts", "en:colas", name151);

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("cannot exceed 150 characters", StringComparison.OrdinalIgnoreCase));
    }

    // --- Phase A: provider allowlist. Omitting allowedProviders (or passing an empty set)
    // preserves prior behaviour exactly (no enforcement) — see tests above, all unchanged. ---

    [Fact]
    public void Validate_AllowedProvidersOmitted_NoAllowlistEnforcement_AnyProviderAccepted()
    {
        var context = new ExternalCategoryMappingContext("totally-unknown-provider", "en:colas", "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_AllowedProvidersEmpty_NoAllowlistEnforcement_AnyProviderAccepted()
    {
        var context = new ExternalCategoryMappingContext("totally-unknown-provider", "en:colas", "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context, allowedProviders: []);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ProviderInAllowlist_ReturnsNoErrors()
    {
        var context = new ExternalCategoryMappingContext("openfoodfacts", "en:colas", "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context, allowedProviders: ["openfoodfacts"]);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ProviderInAllowlist_CaseInsensitive_ReturnsNoErrors()
    {
        var context = new ExternalCategoryMappingContext("OpenFoodFacts", "en:colas", "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context, allowedProviders: ["openfoodfacts"]);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("cache")]
    [InlineData("CACHE")]
    [InlineData("random")]
    [InlineData("foo")]
    public void Validate_ProviderNotInAllowlist_ReturnsError(string provider)
    {
        var context = new ExternalCategoryMappingContext(provider, "en:colas", "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context, allowedProviders: ["openfoodfacts"]);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("not a recognized external product-lookup provider", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ProviderTooLong_ReturnsLengthError_NotAllowlistError()
    {
        // Length validation must short-circuit before the allowlist check runs.
        var provider51 = new string('p', 51);
        var context = new ExternalCategoryMappingContext(provider51, "en:colas", "Colas");

        var errors = ExternalCategoryMappingContextValidator.Validate(context, allowedProviders: ["openfoodfacts"]);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("cannot exceed 50 characters", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(errors, e => e.Message.Contains("not a recognized", StringComparison.OrdinalIgnoreCase));
    }
}
