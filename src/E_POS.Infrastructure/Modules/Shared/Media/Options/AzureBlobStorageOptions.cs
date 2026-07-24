namespace E_POS.Infrastructure.Modules.Shared.Media.Options;

public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "tenant-media";
    public bool CreateContainerIfNotExists { get; set; } = true;
    public string? PublicBaseUrl { get; set; }
}
