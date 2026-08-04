using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class ManualPaymentProjectionContractTests
{
    [Fact]
    public void RecipientProjection_ContainsRefreshSafeSubmissionAndInvoiceFields()
    {
        var properties = typeof(ManualPaymentStatusResponse).GetProperties().Select(x => x.Name).ToHashSet();

        Assert.Contains(nameof(ManualPaymentStatusResponse.TenantName), properties);
        Assert.Contains(nameof(ManualPaymentStatusResponse.SubscriptionStatus), properties);
        Assert.Contains(nameof(ManualPaymentStatusResponse.InvoiceStatus), properties);
        Assert.Contains(nameof(ManualPaymentStatusResponse.SubtotalAmount), properties);
        Assert.Contains(nameof(ManualPaymentStatusResponse.ReferenceSuffix), properties);
        Assert.Contains(nameof(ManualPaymentStatusResponse.SubmittedAmount), properties);
        Assert.Contains(nameof(ManualPaymentStatusResponse.Evidence), properties);
        Assert.DoesNotContain("AccessToken", properties);
        Assert.DoesNotContain("StorageKey", properties);
        Assert.DoesNotContain("Sha256", typeof(ManualPaymentEvidenceDto).GetProperties().Select(x => x.Name));
    }

    [Fact]
    public void AdminDetailProjection_ContainsLifecycleAndComparisonFields()
    {
        var properties = typeof(ManualPaymentDetailResponse).GetProperties().Select(x => x.Name).ToHashSet();

        Assert.Contains(nameof(ManualPaymentDetailResponse.SubscriptionStatus), properties);
        Assert.Contains(nameof(ManualPaymentDetailResponse.InvoiceStatus), properties);
        Assert.Contains(nameof(ManualPaymentDetailResponse.SubtotalAmount), properties);
        Assert.Contains(nameof(ManualPaymentDetailResponse.TaxAmount), properties);
        Assert.Contains(nameof(ManualPaymentDetailResponse.InvitationStatus), properties);
        Assert.Contains(nameof(ManualPaymentDetailResponse.SubmittedByType), properties);
    }
}
