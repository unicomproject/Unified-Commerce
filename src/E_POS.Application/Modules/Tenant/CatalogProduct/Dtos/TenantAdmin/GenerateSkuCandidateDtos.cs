namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed class GenerateSkuCandidateRequest
{
    /// <summary>Current supported purpose: NO_BARCODE_PRODUCT.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Selected assignable Product Category. Backend resolves CategoryCode.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>AUTO is the canonical no-barcode Product Setup mode.</summary>
    public string? Mode { get; set; }

    /// <summary>Optional existing no-barcode DRAFT when explicitly regenerating after Category change.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Required with ProductId; checked against products.row_version.</summary>
    public long? ExpectedRowVersion { get; set; }

    /// <summary>Legacy B5 field retained for wire compatibility; AUTO ignores it.</summary>
    public string? ProductName { get; set; }
}

public sealed record GenerateSkuCandidateResponse(
    string Candidate,
    bool Reserved);
