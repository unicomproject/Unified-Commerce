using System.Globalization;
using System.Text;
using System.Text.Json;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public static class PlatformTenantBootstrapProductImportParser
{
    public const string TemplateVersion = "OVZ-ST-PRODUCT-IMPORT-v1";
    public const int MaxFileSizeBytes = 5 * 1024 * 1024;
    public const int MaxRowCount = 2000;
  private static readonly string[] RequiredHeaders =
    [
        "product_name",
        "sku",
        "selling_price",
        "category_code",
        "brand_code",
        "barcode",
        "track_inventory",
        "outlet_code",
        "opening_stock",
        "status"
    ];

    public static string BuildTemplateCsv()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# oneverz_bootstrap_product_import_version=1");
        builder.AppendLine(string.Join(',', RequiredHeaders));
        return builder.ToString();
    }

    public static PlatformTenantBootstrapProductImportParseResult Parse(Stream csvStream, string fileName)
    {
        if (csvStream.Length > MaxFileSizeBytes)
        {
            return PlatformTenantBootstrapProductImportParseResult.Failed(
                "import.file_too_large",
                "CSV file exceeds the 5 MB bootstrap import limit.");
        }

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            lines.Add(line);
            if (lines.Count > MaxRowCount + 1)
            {
                return PlatformTenantBootstrapProductImportParseResult.Failed(
                    "import.too_many_rows",
                    "CSV exceeds the 2,000 row bootstrap import limit.");
            }
        }

        if (lines.Count == 0)
        {
            return PlatformTenantBootstrapProductImportParseResult.Failed(
                "import.empty_file",
                "CSV file does not contain any data rows.");
        }

        var headerFields = ParseCsvLine(lines[0]);
        if (!HeaderMatches(headerFields))
        {
            return PlatformTenantBootstrapProductImportParseResult.Failed(
                "import.invalid_template",
                $"CSV header does not match {TemplateVersion}.");
        }

        var rows = new List<PlatformTenantBootstrapProductImportParsedRow>();
        for (var index = 1; index < lines.Count; index++)
        {
            var fields = ParseCsvLine(lines[index]);
            if (fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new PlatformTenantBootstrapProductImportParsedRow(
                index + 1,
                MapRow(headerFields, fields),
                lines[index]));
        }

        return PlatformTenantBootstrapProductImportParseResult.Success(rows);
    }

    private static bool HeaderMatches(IReadOnlyList<string> headerFields) =>
        RequiredHeaders.SequenceEqual(headerFields.Select(field => field.Trim()), StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string?> MapRow(IReadOnlyList<string> headers, IReadOnlyList<string> fields)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Count; index++)
        {
            map[headers[index].Trim()] = index < fields.Count ? fields[index]?.Trim() : null;
        }

        return map;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (character == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        fields.Add(current.ToString());
        return fields;
    }
}

public sealed record PlatformTenantBootstrapProductImportParsedRow(
    int RowNumber,
    IReadOnlyDictionary<string, string?> Values,
    string RawLine);

public sealed class PlatformTenantBootstrapProductImportParseResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<PlatformTenantBootstrapProductImportParsedRow> Rows { get; init; } = [];

    public static PlatformTenantBootstrapProductImportParseResult Success(
        IReadOnlyList<PlatformTenantBootstrapProductImportParsedRow> rows) =>
        new() { IsSuccess = true, Rows = rows };

    public static PlatformTenantBootstrapProductImportParseResult Failed(string errorCode, string errorMessage) =>
        new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}

internal sealed record PlatformTenantBootstrapProductImportValidatedRow(
    int RowNumber,
    string RawLine,
    string RawRowJson,
    bool IsValid,
    string? ErrorCode,
    string? ErrorDetail,
    PlatformTenantBootstrapProductCreateRequest? ProductRequest);

internal static class PlatformTenantBootstrapProductImportValidator
{
    public static async Task<IReadOnlyList<PlatformTenantBootstrapProductImportValidatedRow>> ValidateRowsAsync(
        Guid tenantId,
        IReadOnlyList<PlatformTenantBootstrapProductImportParsedRow> rows,
        IPlatformTenantBootstrapRepository repository,
        IProductRepository productRepository,
        CancellationToken cancellationToken)
    {
        var skuInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var barcodeInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var validatedRows = new List<PlatformTenantBootstrapProductImportValidatedRow>(rows.Count);

        foreach (var row in rows)
        {
            var values = row.Values;
            var productName = values.GetValueOrDefault("product_name");
            var sku = values.GetValueOrDefault("sku");
            var sellingPriceRaw = values.GetValueOrDefault("selling_price");
            var categoryCode = values.GetValueOrDefault("category_code");
            var brandCode = values.GetValueOrDefault("brand_code");
            var barcode = values.GetValueOrDefault("barcode");
            var trackInventoryRaw = values.GetValueOrDefault("track_inventory");
            var outletCode = values.GetValueOrDefault("outlet_code");
            var openingStockRaw = values.GetValueOrDefault("opening_stock");
            var status = values.GetValueOrDefault("status");

            var errors = new List<(string Code, string Detail)>();

            if (string.IsNullOrWhiteSpace(productName) || productName.Trim().Length < 2 || productName.Trim().Length > 200)
            {
                errors.Add(("import.invalid_product_name", "Product name must be between 2 and 200 characters."));
            }

            if (string.IsNullOrWhiteSpace(sku) || sku.Trim().Length > 80)
            {
                errors.Add(("import.invalid_sku", "SKU is required and must be 80 characters or fewer."));
            }
            else if (!skuInFile.Add(sku.Trim()))
            {
                errors.Add(("import.duplicate_sku_in_file", $"Duplicate SKU '{sku.Trim()}' in file."));
            }
            else if (await productRepository.SkuExistsAsync(tenantId, sku.Trim(), null, cancellationToken))
            {
                errors.Add(("import.duplicate_sku_exists", $"SKU '{sku.Trim()}' already exists for this tenant."));
            }

            if (!decimal.TryParse(sellingPriceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var sellingPrice) ||
                sellingPrice < 0)
            {
                errors.Add(("import.invalid_selling_price", "Selling price must be a number greater than or equal to 0."));
            }

            Guid? categoryId = null;
            if (!string.IsNullOrWhiteSpace(categoryCode))
            {
                categoryId = await repository.ResolveCategoryIdByCodeAsync(tenantId, categoryCode, cancellationToken);
                if (!categoryId.HasValue)
                {
                    errors.Add(("import.unknown_category", $"Unknown category code '{categoryCode}'."));
                }
            }

            if (!string.IsNullOrWhiteSpace(brandCode))
            {
                var brandId = await repository.ResolveBrandIdByCodeAsync(tenantId, brandCode, cancellationToken);
                if (!brandId.HasValue)
                {
                    errors.Add(("import.unknown_brand", $"Unknown brand code '{brandCode}'."));
                }
            }

            if (!string.IsNullOrWhiteSpace(barcode))
            {
                if (!barcodeInFile.Add(barcode.Trim()))
                {
                    errors.Add(("import.duplicate_barcode_in_file", $"Duplicate barcode '{barcode.Trim()}' in file."));
                }
                else if (await productRepository.BarcodeExistsAsync(tenantId, barcode.Trim(), null, cancellationToken))
                {
                    errors.Add(("import.duplicate_barcode_exists", $"Barcode '{barcode.Trim()}' already exists for this tenant."));
                }
            }

            var trackInventory = string.IsNullOrWhiteSpace(trackInventoryRaw) ||
                                 bool.TryParse(trackInventoryRaw, out var parsedTrack) && parsedTrack;

            decimal openingStock = 0;
            if (!string.IsNullOrWhiteSpace(openingStockRaw))
            {
                if (!decimal.TryParse(openingStockRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out openingStock) ||
                    openingStock < 0)
                {
                    errors.Add(("import.invalid_opening_stock", "Opening stock must be a number greater than or equal to 0."));
                }
            }

            Guid? outletId = null;
            if (openingStock > 0)
            {
                if (string.IsNullOrWhiteSpace(outletCode))
                {
                    errors.Add(("import.outlet_required", "Outlet code is required when opening stock is greater than 0."));
                }
                else
                {
                    outletId = await repository.ResolveOutletIdByCodeAsync(tenantId, outletCode, cancellationToken);
                    if (!outletId.HasValue)
                    {
                        errors.Add(("import.unknown_outlet", $"Unknown outlet code '{outletCode}'."));
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(outletCode))
            {
                outletId = await repository.ResolveOutletIdByCodeAsync(tenantId, outletCode, cancellationToken);
                if (!outletId.HasValue)
                {
                    errors.Add(("import.unknown_outlet", $"Unknown outlet code '{outletCode}'."));
                }
            }

            var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "ACTIVE" : status.Trim().ToUpperInvariant();
            if (normalizedStatus is not ("ACTIVE" or "DRAFT"))
            {
                errors.Add(("import.invalid_status", "Status must be ACTIVE or DRAFT."));
            }

            PlatformTenantBootstrapProductCreateRequest? request = null;
            if (errors.Count == 0)
            {
                request = new PlatformTenantBootstrapProductCreateRequest
                {
                    ProductName = productName!.Trim(),
                    Sku = sku!.Trim(),
                    SellingPrice = sellingPrice,
                    CategoryId = categoryId,
                    Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim(),
                    TrackInventory = trackInventory,
                    OpeningStockQuantity = openingStock > 0 ? openingStock : null,
                    OutletId = outletId,
                    Status = normalizedStatus
                };
            }

            validatedRows.Add(new PlatformTenantBootstrapProductImportValidatedRow(
                row.RowNumber,
                row.RawLine,
                JsonSerializer.Serialize(values),
                errors.Count == 0,
                errors.FirstOrDefault().Code,
                errors.FirstOrDefault().Detail,
                request));
        }

        return validatedRows;
    }
}
