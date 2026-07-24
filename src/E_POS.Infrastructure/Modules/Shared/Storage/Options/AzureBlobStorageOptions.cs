namespace E_POS.Infrastructure.Modules.Shared.Storage.Options;

public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public bool CreateContainerIfNotExists { get; set; }
    public string PublicBaseUrl { get; set; } = string.Empty;
}
