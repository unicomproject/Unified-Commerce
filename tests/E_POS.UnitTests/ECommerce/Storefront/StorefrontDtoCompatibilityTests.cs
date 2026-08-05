using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using Xunit;

namespace E_POS.UnitTests.ECommerce.Storefront;

public sealed class StorefrontDtoCompatibilityTests
{
    [Fact]
    public void ProductDetailCompatibility_ReturnsColourAndSizeValuesFromMultipleOptionGroups()
    {
        var detail = new StorefrontProductDetailReadModel
        {
            Options =
            [
                new StorefrontProductOptionReadModel
                {
                    OptionName = "Color",
                    Values =
                    [
                        OptionValue("RED", "Red", "#FF0000"),
                        OptionValue("BLUE", "Blue", "#0000FF")
                    ]
                },
                new StorefrontProductOptionReadModel
                {
                    OptionName = "Size",
                    Values =
                    [
                        OptionValue("S", "Small"),
                        OptionValue("M", "Medium"),
                        OptionValue("L", "Large")
                    ]
                },
                new StorefrontProductOptionReadModel
                {
                    OptionName = "Unit",
                    Values =
                    [
                        OptionValue("EACH", "Each"),
                        OptionValue("BOX", "Box")
                    ]
                },
                new StorefrontProductOptionReadModel
                {
                    OptionName = "Material",
                    Values = [OptionValue("COTTON", "Cotton")]
                }
            ]
        };

        Assert.Collection(
            detail.Colours,
            first =>
            {
                Assert.Equal("Red", first.DisplayName);
                Assert.Equal("#FF0000", first.ColorHex);
            },
            second =>
            {
                Assert.Equal("Blue", second.DisplayName);
                Assert.Equal("#0000FF", second.ColorHex);
            });
        Assert.Collection(
            detail.Sizes,
            first => Assert.Equal("Small", first.DisplayName),
            second => Assert.Equal("Medium", second.DisplayName),
            third => Assert.Equal("Large", third.DisplayName));
        Assert.Equal(4, detail.Options.Count);
        Assert.DoesNotContain(detail.Colours, x => x.DisplayName is "Each" or "Box" or "Cotton");
        Assert.DoesNotContain(detail.Sizes, x => x.DisplayName is "Each" or "Box" or "Cotton");
    }

    [Fact]
    public void VariantCompatibility_ReturnsColourAndSizeForMultipleVariantsAndIgnoresUnitOptions()
    {
        var variants = new[]
        {
            new StorefrontProductVariantReadModel
            {
                Id = Guid.NewGuid(),
                VariantName = "Red / Small / Each",
                OptionValues = new Dictionary<string, string>
                {
                    ["Color"] = "Red",
                    ["Size"] = "Small",
                    ["Unit"] = "Each"
                }
            },
            new StorefrontProductVariantReadModel
            {
                Id = Guid.NewGuid(),
                VariantName = "Blue / Medium / Box",
                OptionValues = new Dictionary<string, string>
                {
                    ["Colour"] = "Blue",
                    ["Size"] = "Medium",
                    ["Unit"] = "Box"
                }
            },
            new StorefrontProductVariantReadModel
            {
                Id = Guid.NewGuid(),
                VariantName = "Large / Case",
                OptionValues = new Dictionary<string, string>
                {
                    ["Size"] = "Large",
                    ["Unit"] = "Case"
                }
            }
        };

        Assert.Collection(
            variants,
            first =>
            {
                Assert.Equal("Red", first.Colour);
                Assert.Equal("Small", first.Size);
            },
            second =>
            {
                Assert.Equal("Blue", second.Colour);
                Assert.Equal("Medium", second.Size);
            },
            third =>
            {
                Assert.Null(third.Colour);
                Assert.Equal("Large", third.Size);
            });

        Assert.All(variants, variant => Assert.DoesNotContain("Unit", variant.Colour ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        Assert.All(variants, variant => Assert.DoesNotContain("Unit", variant.Size ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CompatibilityProperties_ReturnEmptyOrNullWhenOnlyUnitOptionsExist()
    {
        var detail = new StorefrontProductDetailReadModel
        {
            Options =
            [
                new StorefrontProductOptionReadModel
                {
                    OptionName = "Unit",
                    Values =
                    [
                        OptionValue("EACH", "Each"),
                        OptionValue("CASE", "Case")
                    ]
                }
            ]
        };
        var variant = new StorefrontProductVariantReadModel
        {
            OptionValues = new Dictionary<string, string>
            {
                ["Unit"] = "Each"
            }
        };

        Assert.Empty(detail.Colours);
        Assert.Empty(detail.Sizes);
        Assert.Null(variant.Colour);
        Assert.Null(variant.Size);
    }

    [Fact]
    public void MeasurementUnitValues_DoNotLeakIntoSizeCompatibility()
    {
        var detail = new StorefrontProductDetailReadModel
        {
            Options =
            [
                new StorefrontProductOptionReadModel
                {
                    OptionName = "Size",
                    Values =
                    [
                        OptionValue("S", "Small"),
                        OptionValue("M", "Medium"),
                        OptionValue("L", "Large")
                    ]
                },
                new StorefrontProductOptionReadModel
                {
                    OptionName = "Unit",
                    Values =
                    [
                        OptionValue("250ML", "250 ml"),
                        OptionValue("500ML", "500 ml"),
                        OptionValue("1L", "1 L"),
                        OptionValue("2L", "2 L")
                    ]
                },
                new StorefrontProductOptionReadModel
                {
                    OptionName = "UOM",
                    Values =
                    [
                        OptionValue("G", "g"),
                        OptionValue("KG", "kg")
                    ]
                },
                new StorefrontProductOptionReadModel
                {
                    OptionName = "Volume",
                    Values = [OptionValue("750ML", "750 ml")]
                }
            ]
        };

        Assert.Collection(
            detail.Sizes,
            first => Assert.Equal("Small", first.DisplayName),
            second => Assert.Equal("Medium", second.DisplayName),
            third => Assert.Equal("Large", third.DisplayName));
        Assert.DoesNotContain(detail.Sizes, x => x.DisplayName is "250 ml" or "500 ml" or "1 L" or "2 L" or "g" or "kg" or "750 ml");
        Assert.Empty(detail.Colours);

        var variants = new[]
        {
            new StorefrontProductVariantReadModel
            {
                VariantName = "Large / 1 L",
                OptionValues = new Dictionary<string, string>
                {
                    ["Size"] = "Large",
                    ["Unit"] = "1 L"
                }
            },
            new StorefrontProductVariantReadModel
            {
                VariantName = "500 ml only",
                OptionValues = new Dictionary<string, string>
                {
                    ["Unit"] = "500 ml",
                    ["Volume"] = "500 ml"
                }
            },
            new StorefrontProductVariantReadModel
            {
                VariantName = "kg only",
                OptionValues = new Dictionary<string, string>
                {
                    ["UOM"] = "kg"
                }
            }
        };

        Assert.Equal("Large", variants[0].Size);
        Assert.Null(variants[1].Size);
        Assert.Null(variants[2].Size);
        Assert.All(variants, variant => Assert.Null(variant.Colour));
    }
    private static StorefrontProductOptionValueReadModel OptionValue(string name, string displayName, string? colorHex = null)
    {
        return new StorefrontProductOptionValueReadModel
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            ColorHex = colorHex
        };
    }
}