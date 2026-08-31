using System.Text.Json;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Services;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class CategoryAuditLoggerTests
{
    [Fact]
    public void LogCreated_WithQuotesSlashesNewlinesAndUnicode_PersistsValidJson()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new EPosDbContext(options);
        var logger = new CategoryAuditLogger(NullLogger<CategoryAuditLogger>.Instance, db);
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        const string code = "Q\"\\uote\nPath/品";

        logger.LogCreated(tenantId, actorId, categoryId, code, "ACTIVE");

        var audit = Assert.Single(db.ChangeTracker.Entries<AuditLog>().Select(x => x.Entity));
        Assert.Equal("CATEGORY", audit.EntityType);
        Assert.Equal("category.created", audit.Action);
        Assert.Equal(tenantId, audit.TenantId);
        Assert.Equal(actorId, audit.ActorUserId);
        Assert.Equal(categoryId, audit.EntityId);
        Assert.False(string.IsNullOrWhiteSpace(audit.NewValues));
        using var document = JsonDocument.Parse(audit.NewValues!);
        Assert.Equal(code, document.RootElement.GetProperty("categoryCode").GetString());
        Assert.Equal("ACTIVE", document.RootElement.GetProperty("status").GetString());
        Assert.DoesNotContain("SAS", audit.NewValues, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection", audit.NewValues, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogImageEvents_RecordActorCategoryAndSafeResultOnly()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new EPosDbContext(options);
        var logger = new CategoryAuditLogger(NullLogger<CategoryAuditLogger>.Instance, db);
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();

        logger.LogImageUploaded(tenantId, actorId, categoryId, mediaAssetId);
        logger.LogImageRemoved(tenantId, actorId, categoryId, mediaAssetId, noOp: false);

        var audits = db.ChangeTracker.Entries<AuditLog>().Select(x => x.Entity).ToList();
        Assert.Equal(2, audits.Count);
        Assert.Contains(audits, x => x.Action == "category.image_uploaded");
        Assert.Contains(audits, x => x.Action == "category.image_removed");
        foreach (var audit in audits)
        {
            using var document = JsonDocument.Parse(audit.NewValues!);
            Assert.Equal(JsonValueKind.String, document.RootElement.GetProperty("result").ValueKind);
            Assert.DoesNotContain("token", audit.NewValues, StringComparison.OrdinalIgnoreCase);
        }
    }
}
