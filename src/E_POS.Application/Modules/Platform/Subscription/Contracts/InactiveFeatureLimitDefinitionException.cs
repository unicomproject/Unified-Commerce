namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public sealed class InactiveFeatureLimitDefinitionException : Exception
{
    public InactiveFeatureLimitDefinitionException(Guid featureLimitDefinitionId)
        : base($"Feature limit definition '{featureLimitDefinitionId}' was not found or is inactive.")
    {
        FeatureLimitDefinitionId = featureLimitDefinitionId;
    }

    public Guid FeatureLimitDefinitionId { get; }
}
