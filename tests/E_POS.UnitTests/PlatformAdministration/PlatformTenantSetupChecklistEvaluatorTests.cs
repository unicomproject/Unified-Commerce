using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantSetupChecklistEvaluatorTests
{
    [Fact]
    public void Evaluate_AllComplete_Returns100()
    {
        var result = PlatformTenantSetupChecklistEvaluator.Evaluate(
            new(true, true, true, true, true));

        Assert.True(result.IsComplete);
        Assert.Equal(100, result.ProgressPercent);
        Assert.Empty(result.MissingSteps);
        Assert.Equal(5, result.CompletedSteps.Count);
        Assert.Null(PlatformTenantSetupChecklistEvaluator.FirstMissingMandatoryStep(result));
    }

    [Theory]
    [InlineData(false, true, true, true, true, PlatformTenantSetupChecklistEvaluator.StepBusinessProfile)]
    [InlineData(true, false, true, true, true, PlatformTenantSetupChecklistEvaluator.StepSubscriptionPlan)]
    [InlineData(true, true, false, true, true, PlatformTenantSetupChecklistEvaluator.StepEntitlements)]
    [InlineData(true, true, true, false, true, PlatformTenantSetupChecklistEvaluator.StepBillingCondition)]
    [InlineData(true, true, true, true, false, PlatformTenantSetupChecklistEvaluator.StepTenantAdmin)]
    public void Evaluate_FirstMissingMandatoryStep_FollowsApprovedOrder(
        bool profile,
        bool plan,
        bool entitlements,
        bool billing,
        bool admin,
        string expectedFirstMissing)
    {
        var result = PlatformTenantSetupChecklistEvaluator.Evaluate(
            new(profile, plan, entitlements, billing, admin));

        Assert.False(result.IsComplete);
        Assert.Equal(expectedFirstMissing, PlatformTenantSetupChecklistEvaluator.FirstMissingMandatoryStep(result));
        Assert.Equal(expectedFirstMissing, result.MissingSteps[0]);
    }

    [Fact]
    public void Evaluate_MissingSubscriptionAndAdmin_ReportsStepsInOrder()
    {
        var result = PlatformTenantSetupChecklistEvaluator.Evaluate(
            new(true, false, true, true, false));

        Assert.False(result.IsComplete);
        Assert.Equal(60, result.ProgressPercent);
        Assert.Equal(
            [
                PlatformTenantSetupChecklistEvaluator.StepSubscriptionPlan,
                PlatformTenantSetupChecklistEvaluator.StepTenantAdmin
            ],
            result.MissingSteps);
        Assert.Equal(
            PlatformTenantSetupChecklistEvaluator.StepSubscriptionPlan,
            PlatformTenantSetupChecklistEvaluator.FirstMissingMandatoryStep(result));
    }

    [Fact]
    public void ContinueSetupPath_AlwaysTargetsTenantDetailSurface()
    {
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
        var path = PlatformTenantSetupChecklistEvaluator.ContinueSetupPath(tenantId);

        Assert.Equal($"/admin/tenants/{tenantId}", path);
    }

    [Theory]
    [InlineData("TRIAL", true)]
    [InlineData("PAID", true)]
    [InlineData("WAIVED", true)]
    [InlineData("CURRENT", true)]
    [InlineData("PENDING", false)]
    public void IsBillingConditionSatisfied_UsesApprovedStatuses(string status, bool expected)
    {
        var ok = status is "TRIAL"
            ? PlatformTenantSetupChecklistEvaluator.IsBillingConditionSatisfied(null, "TRIAL")
            : PlatformTenantSetupChecklistEvaluator.IsBillingConditionSatisfied(status, "ACTIVE");
        Assert.Equal(expected, ok);
    }

    [Fact]
    public void IsSetupBillingSatisfied_PendingPayment_FailsEvenWhenTrial()
    {
        var ok = PlatformTenantSetupChecklistEvaluator.IsSetupBillingSatisfied(
            null,
            "TRIAL",
            hasPendingInvoice: false,
            isPendingPaymentStatus: true);

        Assert.False(ok);
        var result = PlatformTenantSetupChecklistEvaluator.Evaluate(
            new(true, true, true, ok, true));
        Assert.Contains(PlatformTenantSetupChecklistEvaluator.StepBillingCondition, result.MissingSteps);
        Assert.Equal(
            PlatformTenantSetupChecklistEvaluator.StepBillingCondition,
            PlatformTenantSetupChecklistEvaluator.FirstMissingMandatoryStep(result));
    }

    [Fact]
    public void IsSetupBillingSatisfied_PendingInvoice_FailsEvenWhenTrial()
    {
        Assert.False(PlatformTenantSetupChecklistEvaluator.IsSetupBillingSatisfied(
            null, "TRIAL", hasPendingInvoice: true, isPendingPaymentStatus: false));
    }

    [Fact]
    public void OptionalOutletTill_DoNotAppearInMandatorySteps()
    {
        Assert.DoesNotContain("outlet", PlatformTenantSetupChecklistEvaluator.MandatorySteps);
        Assert.DoesNotContain("till", PlatformTenantSetupChecklistEvaluator.MandatorySteps);
        Assert.Equal(5, PlatformTenantSetupChecklistEvaluator.MandatorySteps.Count);
    }
}
