using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class TenantOnboardingProgressEvaluatorTests
{
    [Fact]
    public void EmptyDraft_HasNoCompletedSteps()
    {
        var result = TenantOnboardingProgressEvaluator.Evaluate(new(null, null, null, null, null, null));

        Assert.Equal(0, result.Mask);
        Assert.Equal(0, result.Percent);
        Assert.Empty(result.Steps);
    }

    [Fact]
    public void SixValidDataSteps_ProduceEightyFivePercentUntilReviewConfirmation()
    {
        var result = TenantOnboardingProgressEvaluator.Evaluate(CompletePayload(reviewConfirmed: false));

        Assert.Equal(63, result.Mask);
        Assert.Equal(85, result.Percent);
        Assert.Equal([1, 2, 3, 4, 5, 6], result.Steps);
    }

    [Fact]
    public void ConfirmedReview_CompletesExactlySevenSteps()
    {
        var result = TenantOnboardingProgressEvaluator.Evaluate(CompletePayload(reviewConfirmed: true));

        Assert.Equal(127, result.Mask);
        Assert.Equal(100, result.Percent);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], result.Steps);
    }

    [Fact]
    public void PaidDraftWithoutPaymentInputs_DoesNotCompleteBillingStep()
    {
        var payload = CompletePayload(true) with
        {
            Billing = new(null, null, null, null, null, null, true, null, null, null, null, null)
        };

        var result = TenantOnboardingProgressEvaluator.Evaluate(payload);

        Assert.DoesNotContain(4, result.Steps);
        Assert.Contains(result.Errors, error => error.Field == "billing");
    }

    private static TenantOnboardingPayloadDto CompletePayload(bool reviewConfirmed) => new(
        new("Acme", "Acme Legal", "ACME-01", "acme-01", null, null, null, "RETAIL", "unified_epos", "GB", "GBP", "Europe/London", "en-GB"),
        new(new("1 Main Street", null, "London", null, null, "GB"),
            new("Ada", "ada@acme.test", "+442071234567"), null, true, null, true, null, null),
        new(Guid.Parse("81111111-1111-4111-8111-111111111111"), "PAID", "monthly", [], new(5, 10, 20)),
        new("billing@acme.test", "payment_link", null, null, null, null, true, null, null, 0m, null, null),
        new([]),
        new("Ada", "Lovelace", "ada@acme.test", "+442071234567"),
        reviewConfirmed);
}
