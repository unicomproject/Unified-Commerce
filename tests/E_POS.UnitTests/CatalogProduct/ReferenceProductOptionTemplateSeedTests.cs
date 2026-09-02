using E_POS.Infrastructure.Persistence.Seed;
using E_POS.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Reflection;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ReferenceProductOptionTemplateSeedTests
{
    [Fact]
    public void UpSql_IsIdempotent()
    {
        // Assert
        Assert.Contains("ON CONFLICT", ReferenceProductOptionTemplateSeedData.UpSql);
        Assert.Contains("DO UPDATE", ReferenceProductOptionTemplateSeedData.UpSql);
    }

    [Fact]
    public void UpSql_ContainsRequiredTemplates()
    {
        // Assert
        Assert.Contains(ReferenceProductOptionTemplateSeedConstants.SizeTemplateId.ToString(), ReferenceProductOptionTemplateSeedData.UpSql);
        Assert.Contains(ReferenceProductOptionTemplateSeedConstants.ColourTemplateId.ToString(), ReferenceProductOptionTemplateSeedData.UpSql);
        Assert.Contains(ReferenceProductOptionTemplateSeedConstants.MaterialTemplateId.ToString(), ReferenceProductOptionTemplateSeedData.UpSql);
    }

    [Fact]
    public void DownSql_ContainsRequiredDeletions()
    {
        // Assert
        Assert.Contains("DELETE FROM product_option_template_values", ReferenceProductOptionTemplateSeedData.DownSql);
        Assert.Contains("DELETE FROM product_option_templates", ReferenceProductOptionTemplateSeedData.DownSql);
        Assert.Contains(ReferenceProductOptionTemplateSeedConstants.SizeTemplateId.ToString(), ReferenceProductOptionTemplateSeedData.DownSql);
    }

    [Fact]
    public void CorrectiveSeedMigration_RunsBeforeDevelopmentVariableCatalogSeed()
    {
        var correctiveMigrationId = typeof(SeedReferenceProductOptionsBeforeVariableCatalog)
            .GetCustomAttribute<MigrationAttribute>()!
            .Id;
        var variableCatalogMigrationId = typeof(SeedDevelopmentVariableProductCatalog)
            .GetCustomAttribute<MigrationAttribute>()!
            .Id;

        Assert.True(string.CompareOrdinal(correctiveMigrationId, variableCatalogMigrationId) < 0);
    }
}
