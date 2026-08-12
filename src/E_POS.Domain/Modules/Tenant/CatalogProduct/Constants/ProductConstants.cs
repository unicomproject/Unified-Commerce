namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class ProductConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";
    public const string DraftStatus = "DRAFT";
    public const string ArchivedStatus = "ARCHIVED";

    public const string ViewPermission = "catalog.products.view";
    public const string CreatePermission = "catalog.products.create";
    public const string UpdatePermission = "catalog.products.update";
    public const string DeletePermission = "catalog.products.delete";
    public const string ManagePermission = "catalog.products.manage";
    public const string PublishPermission = "catalog.products.publish";
    public const string MediaManagePermission = "catalog.product_media.manage";
    public const string ChannelManagePermission = "catalog.product_channels.manage";

    public const int MaxProductImages = 10;
    public const long MaxProductImageBytes = 5 * 1024 * 1024; // 5,242,880
    public const int ProductNameMaxLength = 200;
    public const int ProductCodeMaxLength = 80;
    public const int ShortDescriptionMaxLength = 500;
    public const int LongDescriptionMaxLength = 4000;

    public const string DesiredPublishActive = "ACTIVE";
    public const string DesiredPublishInactive = "INACTIVE";
    public const string DefaultDraftProductType = "STANDARD";
    public const string DefaultDraftProductStructure = "SIMPLE";
    public const string ProductImagePurpose = "PRODUCT_IMAGE";
    public const string StagedMediaStatus = "STAGED";

    /// <summary>
    /// Approved Save Draft placeholder when Product Name is blank (8-Step Contract).
    /// Draft-only — rejected by Save &amp; Continue validation.
    /// </summary>
    public const string DraftProductNamePlaceholder = "Untitled Product";

    public static bool IsDraftProductNamePlaceholder(string? productName) =>
        !string.IsNullOrWhiteSpace(productName) &&
        string.Equals(productName.Trim(), DraftProductNamePlaceholder, StringComparison.OrdinalIgnoreCase);

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus or DraftStatus or ArchivedStatus;
    }

    public static bool IsValidDesiredPublishStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        var normalized = NormalizeStatus(status);
        return normalized is DesiredPublishActive or DesiredPublishInactive;
    }
}
