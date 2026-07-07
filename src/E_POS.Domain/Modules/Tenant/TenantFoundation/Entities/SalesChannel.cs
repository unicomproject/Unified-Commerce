using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class SalesChannel : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ChannelCode { get; protected set; } = string.Empty;
    public string ChannelName { get; protected set; } = string.Empty;
    public string ChannelType { get; protected set; } = string.Empty;
    public string? ChannelMode { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }

    public static SalesChannel Create(
        Guid id,
        Guid tenantId,
        string channelCode,
        string channelName,
        string channelType,
        string? channelMode,
        string status,
        int sortOrder,
        DateTimeOffset now)
    {
        return new SalesChannel
        {
            Id = id,
            TenantId = tenantId,
            ChannelCode = channelCode.Trim(),
            ChannelName = channelName.Trim(),
            ChannelType = channelType.Trim(),
            ChannelMode = channelMode?.Trim(),
            Status = status.Trim(),
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
