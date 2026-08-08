namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public static class ParkSaleReference
{
    public const int MaximumSequence = 99_999;

    public static string Prefix(DateTimeOffset heldAt) =>
        $"PS-{heldAt.UtcDateTime.Year:D4}-";

    public static string LockResource(Guid tenantId, DateTimeOffset heldAt) =>
        $"pos-park-reference:{tenantId:N}:{heldAt.UtcDateTime.Year:D4}";

    public static string Format(DateTimeOffset heldAt, int sequence)
    {
        if (sequence is < 1 or > MaximumSequence)
            throw new ArgumentOutOfRangeException(nameof(sequence));

        return $"{Prefix(heldAt)}{sequence:D5}";
    }

    public static bool TryReadSequence(
        string reference,
        DateTimeOffset heldAt,
        out int sequence)
    {
        sequence = 0;
        var prefix = Prefix(heldAt);
        return reference.StartsWith(prefix, StringComparison.Ordinal) &&
               int.TryParse(reference[prefix.Length..], out sequence) &&
               sequence is >= 1 and <= MaximumSequence;
    }
}
