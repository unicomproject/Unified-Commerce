using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Xunit;

namespace E_POS.UnitTests.SubscriptionBilling;

public sealed class SubscriptionInvoiceLifecycleTests
{
    private static readonly DateTimeOffset Created = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_DraftInvoice_MovesToPendingAndSetsIssueTime()
    {
        var invoice = Draft();
        var issued = Created.AddDays(1);
        invoice.Issue(issued);
        Assert.Equal(TenantSubscriptionBillingConstants.InvoiceStatusPending, invoice.InvoiceStatus);
        Assert.Equal(issued, invoice.IssuedAt);
        Assert.True(invoice.CanMarkPaid);
    }

    [Fact]
    public void Issue_NonDraftInvoice_IsRejected()
    {
        var invoice = Draft();
        invoice.Issue(Created.AddDays(1));
        Assert.Throws<InvalidOperationException>(() => invoice.Issue(Created.AddDays(2)));
    }

    [Fact]
    public void MarkPaid_PendingInvoice_SettlesBalance()
    {
        var invoice = Draft();
        invoice.Issue(Created.AddDays(1));
        invoice.MarkPaid(Created.AddDays(2), Created.AddDays(2));
        Assert.Equal(TenantSubscriptionBillingConstants.InvoiceStatusPaid, invoice.InvoiceStatus);
        Assert.Equal(125m, invoice.PaidAmount);
        Assert.Equal(0m, invoice.BalanceDue);
        Assert.False(invoice.CanMarkPaid);
    }

    [Fact]
    public void MarkPaid_DraftInvoice_IsRejected()
    {
        var invoice = Draft();
        Assert.Throws<InvalidOperationException>(() => invoice.MarkPaid(Created, Created));
    }

    private static SubscriptionInvoice Draft() => SubscriptionInvoice.CreateDraft(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "INV-TEST-1", 125m, "MONTHLY", Created.AddDays(14), "LKR", Created, Created.AddMonths(1), Created);
}
