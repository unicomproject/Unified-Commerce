using System.Data;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontFulfillmentRepository : IStorefrontFulfillmentRepository
{
    private readonly EPosDbContext _dbContext;

    public StorefrontFulfillmentRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT o.id, o.outlet_name,
                       a.address_line1, a.city
                FROM outlets o
                LEFT JOIN outlet_addresses a ON a.outlet_id = o.id AND a.is_primary = true AND a.status = 'ACTIVE'
                WHERE o.tenant_id = @tenantId AND o.status = 'ACTIVE'";

            var param = cmd.CreateParameter();
            param.ParameterName = "tenantId";
            param.Value = tenantId;
            cmd.Parameters.Add(param);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var stores = new List<StorefrontStoreReadModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var addressLine1 = reader.IsDBNull(2) ? null : reader.GetString(2);
                var city = reader.IsDBNull(3) ? null : reader.GetString(3);

                stores.Add(new StorefrontStoreReadModel
                {
                    Id = reader.GetGuid(0),
                    Name = reader.GetString(1),
                    Address = addressLine1 != null ? $"{addressLine1}, {city}" : string.Empty,
                    IsAvailable = true
                });
            }

            return stores;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}