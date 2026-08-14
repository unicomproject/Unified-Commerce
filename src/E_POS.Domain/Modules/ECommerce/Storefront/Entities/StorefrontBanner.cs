using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Storefront.Entities;

public class StorefrontBanner : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid? SalesChannelId { get; private set; }
    public string BannerType { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public Guid? ImageMediaAssetId { get; private set; }
    public string? ActionText { get; private set; }
    public string? ActionUrl { get; private set; }
    public int SortOrder { get; private set; }
    public string Status { get; private set; } = string.Empty;

    protected StorefrontBanner() { } // EF Core

    public static StorefrontBanner Create(
        Guid tenantId,
        Guid? salesChannelId,
        string bannerType,
        string title,
        string? subtitle,
        Guid? imageMediaAssetId,
        string? actionText,
        string? actionUrl,
        int sortOrder,
        string status)
    {
        return new StorefrontBanner
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SalesChannelId = salesChannelId,
            BannerType = bannerType,
            Title = title,
            Subtitle = subtitle,
            ImageMediaAssetId = imageMediaAssetId,
            ActionText = actionText,
            ActionUrl = actionUrl,
            SortOrder = sortOrder,
            Status = status
        };
    }

    public static StorefrontBanner Create(
        Guid tenantId,
        Guid? salesChannelId,
        string bannerType,
        string title,
        string? subtitle,
        string? imageUrl,
        string? actionText,
        string? actionUrl,
        int sortOrder,
        string status)
    {
        return Create(
            tenantId,
            salesChannelId,
            bannerType,
            title,
            subtitle,
            imageMediaAssetId: null,
            actionText,
            actionUrl,
            sortOrder,
            status);
    }

    public void Update(
        string bannerType,
        string title,
        string? subtitle,
        Guid? imageMediaAssetId,
        string? actionText,
        string? actionUrl,
        int sortOrder,
        string status,
        DateTimeOffset now)
    {
        BannerType = bannerType.Trim().ToUpperInvariant();
        Title = title.Trim();
        Subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim();
        ImageMediaAssetId = imageMediaAssetId;
        ActionText = string.IsNullOrWhiteSpace(actionText) ? null : actionText.Trim();
        ActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim();
        SortOrder = sortOrder;
        Status = status.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        Status = status.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }

    public void Reorder(int sortOrder, DateTimeOffset now)
    {
        SortOrder = sortOrder;
        UpdatedAt = now;
    }
}
