using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Shared.Media.Options;

public sealed class AzureBlobStorageOptionsValidator : IValidateOptions<AzureBlobStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureBlobStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("AzureBlobStorage:ConnectionString is required.");
        }

        if (!options.ConnectionString.Contains("AccountKey=") &&
            !options.ConnectionString.Contains("SharedAccessSignature=") &&
            !options.ConnectionString.Contains("UseDevelopmentStorage=true"))
        {
            return ValidateOptionsResult.Fail("AzureBlobStorage:ConnectionString is not configured or malformed. Configure it using .NET User Secrets or an environment variable.");
        }

        return ValidateOptionsResult.Success;
    }
}
