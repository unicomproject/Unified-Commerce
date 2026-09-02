namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

public sealed class PosOnlineOrderPickLineRequest
{
    public decimal Quantity { get; init; }
    public string? Barcode { get; init; }
    public string InputMethod { get; init; } = string.Empty;
    public long ExpectedVersion { get; init; }
}

public sealed class PosOnlineOrderPickingIssueRequest
{
    public string Reason { get; init; } = string.Empty;
    public string? Note { get; init; }
    public long ExpectedVersion { get; init; }
}

public sealed class PosOnlineOrderPickingNoteRequest
{
    public string Note { get; init; } = string.Empty;
    public long ExpectedVersion { get; init; }
}

public sealed class PosOnlineOrderPickingNoteResponse
{
    public Guid Id { get; init; }
    public string Note { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public Guid CreatedByTenantUserId { get; init; }
    public string CreatedByDisplayName { get; init; } = string.Empty;
}

public sealed class PosOnlineOrderPickingNoteCommandResponse
{
    public Guid OrderId { get; init; }
    public Guid FulfillmentOrderId { get; init; }
    public long FulfillmentVersion { get; init; }
    public PosOnlineOrderPickingNoteResponse Note { get; init; } = new();
}

public sealed class PosOnlineOrderPickingLineResponse
{
    public Guid Id { get; init; }
    public Guid SalesOrderLineId { get; init; }
    public Guid ProductId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public int LineNumber { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public string? ImageUrl { get; init; }
    public string? AltText { get; init; }
    public string? LocationCode { get; init; }
    public string? LocationName { get; init; }
    public decimal RequestedQuantity { get; init; }
    public decimal PickedQuantity { get; init; }
    public decimal RemainingQuantity { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool HasReportedIssue { get; init; }
}

public sealed class PosOnlineOrderPickingResponse
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid FulfillmentOrderId { get; init; }
    public string FulfillmentNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedToTenantUserId { get; init; }
    public string AssignedToName { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateTimeOffset? CollectionAt { get; init; }
    public Guid OutletId { get; init; }
    public string OutletName { get; init; } = string.Empty;
    public int TotalLines { get; init; }
    public int PickedLines { get; init; }
    public decimal TotalUnits { get; init; }
    public decimal PickedUnits { get; init; }
    public decimal RemainingUnits { get; init; }
    public bool CanPack { get; init; }
    public long FulfillmentVersion { get; init; }
    public DateTimeOffset ServerTime { get; init; }
    public IReadOnlyList<PosOnlineOrderPickingLineResponse> Lines { get; init; } = [];
    public IReadOnlyList<PosOnlineOrderPickingNoteResponse> Notes { get; init; } = [];
}

public sealed class PosOnlineOrderPickingCommandResponse
{
    public Guid OrderId { get; init; }
    public Guid FulfillmentOrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalLines { get; init; }
    public int CompletedLines { get; init; }
    public bool CanPack { get; init; }
    public long FulfillmentVersion { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
