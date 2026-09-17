namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

public sealed class PosOnlineOrderPackRequest
{
    public string? PackingNote { get; init; }
    public long ExpectedVersion { get; init; }
}

public sealed class PosOnlineOrderReadyRequest
{
    public long ExpectedVersion { get; init; }
}

public sealed class PosOnlineOrderPackReadyCommandResponse
{
    public Guid OrderId { get; init; }
    public Guid FulfillmentOrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalLines { get; init; }
    public int CompletedLines { get; init; }
    public bool CanPack { get; init; }
    public long FulfillmentVersion { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// One-time reveal of the raw collection QR token at MarkReady.
    /// Only the SHA-256 hash is persisted; storefront/notify may embed this once.
    /// </summary>
    public string? CollectionQrToken { get; init; }
}
