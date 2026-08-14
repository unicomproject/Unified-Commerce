using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public class BundleComponentCandidateDto
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? VariantLabel { get; set; }
    public string? ImageUrl { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductStructure { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TrackingType { get; set; } = string.Empty;
    public Guid UomId { get; set; }
    public string UomCode { get; set; } = string.Empty;
    public string UomName { get; set; } = string.Empty;
    public decimal AvailableStock { get; set; }
    public Guid? OutletId { get; set; }
}

public class BundleComponentVariantDto
{
    public Guid ProductVariantId { get; set; }
    public string VariantLabel { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TrackingType { get; set; } = string.Empty;
    public Guid UomId { get; set; }
    public string UomCode { get; set; } = string.Empty;
    public string UomName { get; set; } = string.Empty;
    public decimal AvailableStock { get; set; }
}
