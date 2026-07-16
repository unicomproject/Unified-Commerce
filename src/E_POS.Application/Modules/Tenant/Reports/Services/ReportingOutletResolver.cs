namespace E_POS.Application.Modules.Tenant.Reports.Services;

public static class ReportingOutletResolver
{
    public static ReportingOutletResolution Resolve(ReportingOutletResolutionInput input)
    {
        var orderType = input.OrderType?.Trim().ToUpperInvariant();
        if (orderType == "POS_SALE")
        {
            if (input.TillOutletId.HasValue)
            {
                return ReportingOutletResolution.FromOutlet(
                    input.TillOutletId.Value,
                    input.TillOutletCode,
                    input.TillOutletName,
                    "pos_till_outlet");
            }

            if (input.AssignedOutletId.HasValue)
            {
                return ReportingOutletResolution.FromOutlet(
                    input.AssignedOutletId.Value,
                    input.AssignedOutletCode,
                    input.AssignedOutletName,
                    "pos_assigned_outlet");
            }
        }

        if (orderType == "CLICK_AND_COLLECT" || orderType == "ONLINE_PICKUP")
        {
            if (input.FulfillmentOutletId.HasValue)
            {
                return ReportingOutletResolution.FromOutlet(
                    input.FulfillmentOutletId.Value,
                    input.FulfillmentOutletCode,
                    input.FulfillmentOutletName,
                    "fulfillment_outlet");
            }
        }

        if (orderType == "RETURN")
        {
            if (input.ReturnProcessingOutletId.HasValue)
            {
                return ReportingOutletResolution.FromOutlet(
                    input.ReturnProcessingOutletId.Value,
                    input.ReturnProcessingOutletCode,
                    input.ReturnProcessingOutletName,
                    "return_processing_outlet");
            }
        }

        return ReportingOutletResolution.None;
    }
}

public sealed record ReportingOutletResolutionInput(
    string? OrderType,
    Guid? TillOutletId = null,
    string? TillOutletCode = null,
    string? TillOutletName = null,
    Guid? AssignedOutletId = null,
    string? AssignedOutletCode = null,
    string? AssignedOutletName = null,
    Guid? FulfillmentOutletId = null,
    string? FulfillmentOutletCode = null,
    string? FulfillmentOutletName = null,
    Guid? ReturnProcessingOutletId = null,
    string? ReturnProcessingOutletCode = null,
    string? ReturnProcessingOutletName = null);

public sealed record ReportingOutletResolution(
    Guid? OutletId,
    string? OutletCodeSnapshot,
    string? OutletNameSnapshot,
    string Reason)
{
    public static readonly ReportingOutletResolution None = new(null, null, null, "none");

    public static ReportingOutletResolution FromOutlet(Guid outletId, string? code, string? name, string reason) =>
        new(outletId, string.IsNullOrWhiteSpace(code) ? null : code.Trim(), string.IsNullOrWhiteSpace(name) ? null : name.Trim(), reason);
}
