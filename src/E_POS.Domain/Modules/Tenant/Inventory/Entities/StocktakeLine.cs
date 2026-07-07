using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StocktakeLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid StocktakeSessionId { get; protected set; }
    public int LineNumber { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? ProductBatchId { get; protected set; }
    public decimal ExpectedQuantity { get; protected set; }
    public decimal? CountedQuantity { get; protected set; }
    public decimal? VarianceQuantity { get; protected set; }
    public Guid? CountedByTenantUserId { get; protected set; }
    public DateTimeOffset? CountedAt { get; protected set; }
    public string LineStatus { get; protected set; } = string.Empty;
    public string? LineNote { get; protected set; }

    protected StocktakeLine() { }

    public static StocktakeLine Create(
        Guid id,
        Guid tenantId,
        Guid stocktakeSessionId,
        int lineNumber,
        Guid productId,
        Guid? productVariantId,
        Guid? productBatchId,
        decimal expectedQuantity,
        string? lineNote,
        DateTimeOffset now)
    {
        return new StocktakeLine
        {
            Id = id,
            TenantId = tenantId,
            StocktakeSessionId = stocktakeSessionId,
            LineNumber = lineNumber,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ProductBatchId = productBatchId,
            ExpectedQuantity = expectedQuantity,
            LineStatus = "UNCOUNTED",
            LineNote = lineNote?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Count(decimal countedQuantity, Guid countedByTenantUserId, DateTimeOffset now)
    {
        CountedQuantity = countedQuantity;
        VarianceQuantity = countedQuantity - ExpectedQuantity;
        CountedByTenantUserId = countedByTenantUserId;
        CountedAt = now;
        LineStatus = "COUNTED";
        UpdatedAt = now;
    }
}
