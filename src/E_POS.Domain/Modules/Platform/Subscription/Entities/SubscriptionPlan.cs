using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPlan : AuditableEntity
{
    public string PlanCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string BillingInterval { get; protected set; } = string.Empty;
    public string BaseCurrency { get; protected set; } = SubscriptionPlanConstants.DefaultBaseCurrency;
    public decimal PriceAmount { get; protected set; }
    public int? MaxOutlets { get; protected set; }
    public int? MaxUsers { get; protected set; }
    public int? MaxTills { get; protected set; }

    public static SubscriptionPlan CreateDraft(
        Guid id,
        string planCode,
        string name,
        string? description,
        string billingInterval,
        string baseCurrency,
        decimal priceAmount,
        int? maxOutlets,
        int? maxUsers,
        int? maxTills,
        DateTimeOffset createdAt)
    {
        return new SubscriptionPlan
        {
            Id = id,
            PlanCode = planCode,
            Name = name,
            Description = description,
            Status = SubscriptionPlanConstants.Status.Draft,
            BillingInterval = billingInterval,
            BaseCurrency = baseCurrency,
            PriceAmount = priceAmount,
            MaxOutlets = maxOutlets,
            MaxUsers = maxUsers,
            MaxTills = maxTills,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public static SubscriptionPlan Create(
        Guid id,
        string planCode,
        string name,
        string status,
        string billingInterval,
        decimal priceAmount,
        DateTimeOffset createdAt,
        string baseCurrency = SubscriptionPlanConstants.DefaultBaseCurrency,
        int? maxOutlets = null,
        int? maxUsers = null,
        int? maxTills = null)
    {
        return new SubscriptionPlan
        {
            Id = id,
            PlanCode = planCode,
            Name = name,
            Status = status,
            BillingInterval = billingInterval,
            BaseCurrency = baseCurrency,
            PriceAmount = priceAmount,
            MaxOutlets = maxOutlets,
            MaxUsers = maxUsers,
            MaxTills = maxTills,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void UpdatePricing(string baseCurrency, decimal priceAmount, DateTimeOffset now)
    {
        BaseCurrency = baseCurrency;
        PriceAmount = priceAmount;
        UpdatedAt = now;
    }

    public void UpdateLimits(int? maxOutlets, int? maxUsers, int? maxTills, DateTimeOffset now)
    {
        MaxOutlets = maxOutlets;
        MaxUsers = maxUsers;
        MaxTills = maxTills;
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        Status = SubscriptionPlanConstants.Status.Active;
        UpdatedAt = now;
    }

    public void TouchUpdatedAt(DateTimeOffset now)
    {
        UpdatedAt = now;
    }

    public bool HasValidLimitsForPublish()
    {
        return MaxOutlets is >= 0 || MaxUsers is >= 0 || MaxTills is >= 0;
    }
}

