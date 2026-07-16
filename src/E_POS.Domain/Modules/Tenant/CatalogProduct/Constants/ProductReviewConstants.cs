namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class ProductReviewConstants
{
    public const string PendingStatus = "PENDING";
    public const string ApprovedStatus = "APPROVED";
    public const string RejectedStatus = "REJECTED";
    public const string DeletedStatus = "DELETED";

    public const int MinimumRating = 1;
    public const int MaximumRating = 5;
    public const int MaximumTitleLength = 150;
    public const int MaximumTextLength = 5000;
}
