using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;

public class PlatformSalesChannel : AuditableEntity
{
    public string ChannelCode { get; protected set; } = string.Empty;
    public string DefaultName { get; protected set; } = string.Empty;
    public string ChannelType { get; protected set; } = string.Empty;

    public static PlatformSalesChannel Create(
        Guid id,
        string channelCode,
        string defaultName,
        string channelType,
        DateTimeOffset now)
    {
        return new PlatformSalesChannel
        {
            Id = id,
            ChannelCode = channelCode.Trim().ToUpperInvariant(),
            DefaultName = defaultName.Trim(),
            ChannelType = channelType.Trim().ToUpperInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string defaultName,
        string channelType,
        DateTimeOffset now)
    {
        DefaultName = defaultName.Trim();
        ChannelType = channelType.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }
}
