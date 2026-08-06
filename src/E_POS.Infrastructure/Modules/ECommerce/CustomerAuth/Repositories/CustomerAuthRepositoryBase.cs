using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;

public abstract class CustomerAuthRepositoryBase
{
    protected const string UniqueViolationSqlState = PostgresErrorCodes.UniqueViolation;

    protected CustomerAuthRepositoryBase(EPosDbContext dbContext)
    {
        DbContext = dbContext;
    }

    protected EPosDbContext DbContext { get; }

    protected Task<bool> TenantIsActiveCoreAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        DbContext.Tenants.AsNoTracking()
            .AnyAsync(x => x.Id == tenantId && x.Status == "active", cancellationToken);

    protected async Task<CustomerLoginAccount?> FindAccountByEmailCoreAsync(
        Guid tenantId,
        string normalizedEmail,
        bool trackAccount,
        CancellationToken cancellationToken)
    {
        var authAccounts = trackAccount
            ? DbContext.CustomerAuthAccounts
            : DbContext.CustomerAuthAccounts.AsNoTracking();
        var customers = DbContext.Customers.AsNoTracking();
        var tenants = DbContext.Tenants.AsNoTracking();

        var row = await (
            from authAccount in authAccounts
            join customer in customers
                on new { authAccount.TenantId, Id = authAccount.CustomerId }
                equals new { customer.TenantId, customer.Id }
            join tenant in tenants
                on authAccount.TenantId equals tenant.Id
            where authAccount.TenantId == tenantId &&
                  customer.NormalizedEmail == normalizedEmail &&
                  customer.Status != "DELETED"
            select new { AuthAccount = authAccount, Customer = customer, TenantStatus = tenant.Status })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new CustomerLoginAccount(
                row.AuthAccount,
                row.Customer.Id,
                row.Customer.TenantId,
                row.Customer.Name,
                row.Customer.Email,
                row.Customer.Phone,
                row.Customer.Status,
                row.TenantStatus);
    }

    protected async Task<IDbContextTransaction?> BeginSerializableTransactionAsync(
        CancellationToken cancellationToken)
    {
        if (!DbContext.Database.IsRelational())
            return null;

        return await DbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
    }

    protected static Task CommitAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken) =>
        transaction is null
            ? Task.CompletedTask
            : transaction.CommitAsync(cancellationToken);

    protected static Task RollbackAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken) =>
        transaction is null
            ? Task.CompletedTask
            : transaction.RollbackAsync(cancellationToken);

    protected void Detach(params object[] entities)
    {
        foreach (var entity in entities)
            DbContext.Entry(entity).State = EntityState.Detached;
    }
}