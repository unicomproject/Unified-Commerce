namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public sealed class MissingCanonicalCapacityLimitDefinitionException : Exception
{
    public MissingCanonicalCapacityLimitDefinitionException(string limitKey, Guid expectedDefinitionId)
        : base($"Canonical capacity limit definition '{limitKey}' (id '{expectedDefinitionId}') was not found or is not active.")
    {
        LimitKey = limitKey;
        ExpectedDefinitionId = expectedDefinitionId;
    }

    public string LimitKey { get; }

    public Guid ExpectedDefinitionId { get; }
}
