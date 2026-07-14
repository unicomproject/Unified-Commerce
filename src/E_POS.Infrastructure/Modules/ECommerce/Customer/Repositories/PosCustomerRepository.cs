using E_POS.Application.Modules.ECommerce.Customer.Contracts;
using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Repositories;

public sealed class PosCustomerRepository : IPosCustomerRepository
{
    private const string UniqueViolationSqlState = PostgresErrorCodes.UniqueViolation;
    private readonly EPosDbContext _dbContext;

    public PosCustomerRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> NormalizedPhoneExistsAsync(
        Guid tenantId,
        string normalizedPhone,
        CancellationToken cancellationToken) =>
        _dbContext.Customers.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.NormalizedPhone == normalizedPhone &&
                 x.Status != "DELETED",
            cancellationToken);

    public Task<bool> NormalizedEmailExistsAsync(
        Guid tenantId,
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        _dbContext.Customers.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.NormalizedEmail == normalizedEmail &&
                 x.Status != "DELETED",
            cancellationToken);

    public async Task<bool> AddAsync(
        CustomerEntity customer,
        CancellationToken cancellationToken)
    {
        _dbContext.Customers.Add(customer);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            _dbContext.Entry(customer).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<PosCustomerListResponseDto> ListAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE");

        var searchTerm = search?.Trim();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, pattern) ||
                (x.Phone != null && EF.Functions.ILike(x.Phone, pattern)) ||
                (x.NormalizedPhone != null && EF.Functions.ILike(x.NormalizedPhone, pattern)) ||
                (x.Email != null && EF.Functions.ILike(x.Email, pattern)) ||
                (x.NormalizedEmail != null && EF.Functions.ILike(x.NormalizedEmail, pattern)) ||
                EF.Functions.ILike(x.CustomerCode, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PosCustomerListItemResponseDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.Status))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PosCustomerListResponseDto(
            items,
            page,
            pageSize,
            totalCount,
            totalPages);
    }
}
