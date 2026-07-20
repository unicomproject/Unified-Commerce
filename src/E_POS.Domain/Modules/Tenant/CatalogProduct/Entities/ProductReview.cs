using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductReview : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid CustomerId { get; private set; }

    public int RatingValue { get; private set; }
    public string? ReviewTitle { get; private set; }
    public string? ReviewText { get; private set; }
    public string Status { get; private set; } = string.Empty;

    protected ProductReview() { } // EF Core

    public static ProductReview CreateApproved(
        Guid tenantId,
        Guid productId,
        Guid customerId,
        int ratingValue,
        string? reviewTitle,
        string? reviewText,
        DateTimeOffset now)
    {
        ValidateRating(ratingValue);
        return new ProductReview
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            CustomerId = customerId,
            RatingValue = ratingValue,
            ReviewTitle = NormalizeOptional(reviewTitle),
            ReviewText = NormalizeOptional(reviewText),
            Status = ProductReviewConstants.ApprovedStatus,
            CreatedBy = customerId,
            UpdatedBy = customerId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateApproved(
        int ratingValue,
        string? reviewTitle,
        string? reviewText,
        Guid customerId,
        DateTimeOffset now)
    {
        ValidateOwner(customerId);
        ValidateRating(ratingValue);
        RatingValue = ratingValue;
        ReviewTitle = NormalizeOptional(reviewTitle);
        ReviewText = NormalizeOptional(reviewText);
        Status = ProductReviewConstants.ApprovedStatus;
        UpdatedBy = customerId;
        UpdatedAt = now;
    }

    public void RestoreApproved(
        int ratingValue,
        string? reviewTitle,
        string? reviewText,
        Guid customerId,
        DateTimeOffset now) =>
        UpdateApproved(ratingValue, reviewTitle, reviewText, customerId, now);

    public void SoftDelete(Guid customerId, DateTimeOffset now)
    {
        ValidateOwner(customerId);
        Status = ProductReviewConstants.DeletedStatus;
        UpdatedBy = customerId;
        UpdatedAt = now;
    }

    private void ValidateOwner(Guid customerId)
    {
        if (CustomerId != customerId)
            throw new InvalidOperationException("Only the review owner can change this review.");
    }

    private static void ValidateRating(int ratingValue)
    {
        if (ratingValue is < ProductReviewConstants.MinimumRating or > ProductReviewConstants.MaximumRating)
            throw new ArgumentOutOfRangeException(
                nameof(ratingValue),
                $"Rating must be between {ProductReviewConstants.MinimumRating} and {ProductReviewConstants.MaximumRating}.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
