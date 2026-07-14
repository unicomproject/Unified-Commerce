using System.Reflection;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using E_POS.Infrastructure.Modules.ECommerce.Customer.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.Customer;

public sealed class PosCustomerRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsCustomerAndMakesNormalizedContactsDiscoverable()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var repository = new PosCustomerRepository(dbContext);
        var customer = CustomerEntity.CreatePosCustomer(
            Guid.NewGuid(),
            tenantId,
            "CUS000001",
            "Kamal Perera",
            "+94 77-123-4567",
            "Kamal@Example.com",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var added = await repository.AddAsync(customer, CancellationToken.None);

        Assert.True(added);
        Assert.True(await repository.NormalizedPhoneExistsAsync(
            tenantId,
            "+94771234567",
            CancellationToken.None));
        Assert.True(await repository.NormalizedEmailExistsAsync(
            tenantId,
            "KAMAL@EXAMPLE.COM",
            CancellationToken.None));
        var persisted = await dbContext.Customers.SingleAsync(x => x.Id == customer.Id);
        Assert.Equal("POS", persisted.SourceType);
        Assert.Equal("ACTIVE", persisted.Status);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyActiveTenantCustomersWithPagination()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.Customers.Add(CreateCustomer(tenantId, "CUS-001", "Alice", "ACTIVE"));
        dbContext.Customers.Add(CreateCustomer(tenantId, "CUS-002", "Bob", "ACTIVE"));
        dbContext.Customers.Add(CreateCustomer(tenantId, "CUS-003", "Charlie", "INACTIVE"));
        dbContext.Customers.Add(CreateCustomer(otherTenantId, "CUS-001", "Other Tenant", "ACTIVE"));
        await dbContext.SaveChangesAsync();

        var repository = new PosCustomerRepository(dbContext);
        var result = await repository.ListAsync(
            tenantId,
            null,
            2,
            1,
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Page);
        var customer = Assert.Single(result.Items);
        Assert.Equal("Bob", customer.FullName);
    }

    private static CustomerEntity CreateCustomer(
        Guid tenantId,
        string customerCode,
        string name,
        string status)
    {
        var customer = new CustomerEntity();
        Set(customer, "Id", Guid.NewGuid());
        Set(customer, "TenantId", tenantId);
        Set(customer, "CustomerCode", customerCode);
        Set(customer, "Name", name);
        Set(customer, "Phone", "+94770000000");
        Set(customer, "NormalizedPhone", "+94770000000");
        Set(customer, "SourceType", "POS");
        Set(customer, "Status", status);
        Set(customer, "CreatedAt", DateTimeOffset.UtcNow);
        Set(customer, "UpdatedAt", DateTimeOffset.UtcNow);
        return customer;
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var property = entity.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(entity, value);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
