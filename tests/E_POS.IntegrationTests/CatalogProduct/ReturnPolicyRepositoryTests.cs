using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class ReturnPolicyRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TemplateListAsync_ReturnsNonDeletedTemplatesOnly()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ReturnPolicyTemplates.AddRange(
            ReturnPolicyTemplate.Create(Guid.NewGuid(), "7DAYS", "7 Days", 7, ReturnPolicyTemplateConstants.ActiveStatus, Now),
            ReturnPolicyTemplate.Create(Guid.NewGuid(), "OLD", "Old", 30, ReturnPolicyTemplateConstants.DeletedStatus, Now));
        await dbContext.SaveChangesAsync();
        var repository = new ReturnPolicyTemplateRepository(dbContext);

        var result = await repository.ListAsync(1, 50, null, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("7DAYS", Assert.Single(result.Items).TemplateCode);
    }

    [Fact]
    public async Task PolicyListAsync_ReturnsCurrentTenantNonDeletedPoliciesOnly()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.ReturnPolicies.AddRange(
            ReturnPolicy.Create(Guid.NewGuid(), tenantId, "7DAYS", "7 Days", 7, ReturnPolicyConstants.ActiveStatus, Now),
            ReturnPolicy.Create(Guid.NewGuid(), tenantId, "OLD", "Old", 30, ReturnPolicyConstants.DeletedStatus, Now),
            ReturnPolicy.Create(Guid.NewGuid(), otherTenantId, "OTHER", "Other", 14, ReturnPolicyConstants.ActiveStatus, Now));
        await dbContext.SaveChangesAsync();
        var repository = new ReturnPolicyRepository(dbContext);

        var result = await repository.ListAsync(tenantId, 1, 50, null, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("7DAYS", Assert.Single(result.Items).PolicyCode);
        Assert.DoesNotContain(result.Items, x => x.PolicyCode == "OTHER");
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}