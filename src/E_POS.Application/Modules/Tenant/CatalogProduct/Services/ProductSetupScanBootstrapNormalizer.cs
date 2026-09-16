using System.Text.Json;
using System.Text.Json.Serialization;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

/// <summary>
/// Validates and normalizes scanner-first <c>scanBootstrap</c> create payloads (B8).
/// </summary>
public static class ProductSetupScanBootstrapNormalizer
{
    public const string AcquisitionScan = "SCAN";
    public const string AcquisitionManual = "MANUAL";
    public const string AcquisitionNoBarcode = "NO_BARCODE";

    public const string ActionUseThisProduct = "USE_THIS_PRODUCT";
    public const string ActionCreateManually = "CREATE_MANUALLY";
    public const string ActionContinueWithBarcode = "CONTINUE_WITH_BARCODE";
    public const string ActionContinueToBasicDetails = "CONTINUE_TO_BASIC_DETAILS";

    public const string ExternalNotStarted = "NOT_STARTED";
    public const string ExternalFound = "FOUND";
    public const string ExternalNoMatch = "NO_MATCH";
    public const string ExternalTemporaryFailure = "TEMPORARY_FAILURE";

    public const string ReasonOwnMade = "OWN_MADE";
    public const string ReasonServiceFee = "SERVICE_FEE";
    public const string ReasonUnlabelled = "UNLABELLED";

    private static readonly JsonSerializerOptions PrefillJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static ApplicationError? TryNormalize(
        ProductSetupScanBootstrapRequest? request,
        out ProductSetupScanBootstrapPersistence? persistence)
    {
        persistence = null;
        if (request is null)
        {
            return new ApplicationError(
                "product.validation_failed",
                "scanBootstrap is required for scanner-first draft create.",
                [new ApplicationFieldError("scanBootstrap", "scanBootstrap is required.")]);
        }

        var mode = NormalizeCode(request.AcquisitionMode);
        if (mode is not (AcquisitionScan or AcquisitionManual or AcquisitionNoBarcode))
        {
            return FieldError(
                "acquisitionMode",
                "acquisitionMode must be SCAN, MANUAL, or NO_BARCODE.");
        }

        if (string.Equals(mode, "LEGACY", StringComparison.Ordinal))
        {
            return FieldError("acquisitionMode", "LEGACY is not allowed for fresh scanner-first create.");
        }

        var action = NormalizeCode(request.CreationAction);
        if (action is not (
            ActionUseThisProduct or
            ActionCreateManually or
            ActionContinueWithBarcode or
            ActionContinueToBasicDetails))
        {
            return FieldError(
                "creationAction",
                "creationAction must be USE_THIS_PRODUCT, CREATE_MANUALLY, CONTINUE_WITH_BARCODE, or CONTINUE_TO_BASIC_DETAILS.");
        }

        var externalStatus = NormalizeCode(request.ExternalLookupStatus) ?? ExternalNotStarted;
        if (externalStatus is not (
            ExternalNotStarted or
            ExternalFound or
            ExternalNoMatch or
            ExternalTemporaryFailure))
        {
            return FieldError(
                "externalLookupStatus",
                "externalLookupStatus must be NOT_STARTED, FOUND, NO_MATCH, or TEMPORARY_FAILURE.");
        }

        string? candidate = null;
        string? identifierStandard = null;
        string? symbologyHint = null;
        string? noBarcodeReason = null;
        string? prefillJson = null;
        var applyPrefill = false;

        if (mode is AcquisitionScan or AcquisitionManual)
        {
            if (!string.IsNullOrWhiteSpace(request.NoBarcodeReason))
            {
                return FieldError("noBarcodeReason", "noBarcodeReason is not allowed when a barcode candidate is present.");
            }

            if (string.IsNullOrWhiteSpace(request.CandidateIdentifier))
            {
                return FieldError("candidateIdentifier", "candidateIdentifier is required for SCAN/MANUAL.");
            }

            if (action == ActionContinueToBasicDetails)
            {
                return FieldError("creationAction", "CONTINUE_TO_BASIC_DETAILS requires acquisitionMode NO_BARCODE.");
            }

            var classification = ProductBarcodeFormatValidator.Classify(
                request.CandidateIdentifier,
                request.SymbologyHint);

            if (!classification.IsValid)
            {
                // Temporarily bypassing for CHECKSUM_FAILED only if user wants to continue
                if (classification.FailureReason != ProductBarcodeFormatValidator.ChecksumFailedFailureReason || 
                    (action != ActionContinueWithBarcode && action != ActionCreateManually))
                {
                    return new ApplicationError(
                        "product.validation_failed",
                        classification.FailureReason ?? "candidateIdentifier is invalid.",
                        [new ApplicationFieldError("candidateIdentifier", classification.FailureReason ?? "Invalid identifier.")]);
                }
            }

            candidate = classification.NormalizedIdentifier;
            identifierStandard = classification.IdentifierStandard;
            symbologyHint = classification.BarcodeType;

            if (action == ActionUseThisProduct)
            {
                if (externalStatus != ExternalFound)
                {
                    return FieldError("creationAction", "USE_THIS_PRODUCT requires externalLookupStatus FOUND.");
                }

                if (request.NormalizedPrefill is null ||
                    string.IsNullOrWhiteSpace(request.NormalizedPrefill.ProductName))
                {
                    return FieldError("normalizedPrefill", "USE_THIS_PRODUCT requires normalizedPrefill.productName.");
                }

                prefillJson = SerializePrefill(SanitizePrefill(request.NormalizedPrefill, candidate, identifierStandard));
                applyPrefill = true;
            }
            else if (action == ActionCreateManually)
            {
                // Retain candidate + acquisition history; discard external product suggestions.
                prefillJson = null;
                applyPrefill = false;
            }
            else if (action == ActionContinueWithBarcode)
            {
                prefillJson = null;
                applyPrefill = false;
            }
            else
            {
                return FieldError("creationAction", $"creationAction {action} is incompatible with {mode}.");
            }
        }
        else
        {
            // NO_BARCODE
            if (!string.IsNullOrWhiteSpace(request.CandidateIdentifier) ||
                !string.IsNullOrWhiteSpace(request.IdentifierStandard) ||
                !string.IsNullOrWhiteSpace(request.SymbologyHint))
            {
                return FieldError(
                    "candidateIdentifier",
                    "NO_BARCODE must not include candidateIdentifier, identifierStandard, or symbologyHint.");
            }

            if (action != ActionContinueToBasicDetails)
            {
                return FieldError("creationAction", "NO_BARCODE requires CONTINUE_TO_BASIC_DETAILS.");
            }

            noBarcodeReason = NormalizeCode(request.NoBarcodeReason);
            if (noBarcodeReason is not (ReasonOwnMade or ReasonServiceFee or ReasonUnlabelled))
            {
                return FieldError(
                    "noBarcodeReason",
                    "noBarcodeReason must be OWN_MADE, SERVICE_FEE, or UNLABELLED.");
            }

            externalStatus = ExternalNotStarted;
            applyPrefill = false;
        }

        var sourceReference = string.IsNullOrWhiteSpace(request.ExternalSourceReference)
            ? null
            : request.ExternalSourceReference.Trim();
        if (sourceReference is { Length: > 100 })
        {
            return FieldError("externalSourceReference", "externalSourceReference must be at most 100 characters.");
        }

        var skuCandidate = string.IsNullOrWhiteSpace(request.GeneratedSkuCandidate)
            ? null
            : request.GeneratedSkuCandidate.Trim();
        if (skuCandidate is { Length: > 100 })
        {
            return FieldError("generatedSkuCandidate", "generatedSkuCandidate must be at most 100 characters.");
        }

        if (mode != AcquisitionNoBarcode)
        {
            skuCandidate = null;
        }

        persistence = new ProductSetupScanBootstrapPersistence(
            AcquisitionMode: mode,
            CreationAction: action,
            CandidateIdentifier: candidate,
            IdentifierStandard: identifierStandard,
            SymbologyHint: symbologyHint,
            NoBarcodeReason: noBarcodeReason,
            ExternalLookupStatus: externalStatus,
            ExternalSourceReference: sourceReference,
            NormalizedPrefillJson: prefillJson,
            GeneratedSkuCandidate: skuCandidate,
            ApplyPrefillToDraft: applyPrefill);

        return null;
    }

    public static void ApplyPrefillToDraftRequest(
        SaveProductDraftRequest request,
        ExternalProductSuggestion? suggestion)
    {
        if (suggestion is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ProductName) && !string.IsNullOrWhiteSpace(suggestion.ProductName))
        {
            request.ProductName = suggestion.ProductName.Trim();
        }

        if (string.IsNullOrWhiteSpace(request.ShortName) && !string.IsNullOrWhiteSpace(suggestion.ShortName))
        {
            request.ShortName = suggestion.ShortName.Trim();
        }

        if (string.IsNullOrWhiteSpace(request.ShortDescription) && !string.IsNullOrWhiteSpace(suggestion.ShortDescription))
        {
            request.ShortDescription = suggestion.ShortDescription.Trim();
        }

        if (string.IsNullOrWhiteSpace(request.LongDescription) && !string.IsNullOrWhiteSpace(suggestion.LongDescription))
        {
            request.LongDescription = suggestion.LongDescription.Trim();
        }
    }

    private static ExternalProductSuggestion SanitizePrefill(
        ExternalProductSuggestion raw,
        string? primaryGtinFallback,
        string? identifierStandardFallback) =>
        new(
            ProductName: Truncate(Clean(raw.ProductName), ProductConstants.ProductNameMaxLength),
            ShortName: Truncate(Clean(raw.ShortName), 100),
            BrandText: Truncate(Clean(raw.BrandText), 100),
            CategoryText: Truncate(Clean(raw.CategoryText), 100),
            UnitText: Truncate(Clean(raw.UnitText), 40),
            CountryCode: Truncate(Clean(raw.CountryCode), 8),
            ShortDescription: Truncate(Clean(raw.ShortDescription), ProductConstants.ShortDescriptionMaxLength),
            LongDescription: Truncate(Clean(raw.LongDescription), ProductConstants.LongDescriptionMaxLength),
            ImageCandidate: Truncate(Clean(raw.ImageCandidate), 500),
            PrimaryGtin: Truncate(Clean(raw.PrimaryGtin) ?? primaryGtinFallback, 100),
            IdentifierStandard: Truncate(Clean(raw.IdentifierStandard) ?? identifierStandardFallback, 40));

    private static string SerializePrefill(ExternalProductSuggestion suggestion) =>
        JsonSerializer.Serialize(suggestion, PrefillJsonOptions);

    private static ApplicationError FieldError(string field, string message) =>
        new(
            "product.validation_failed",
            "Product validation failed.",
            [new ApplicationFieldError(field, message)]);

    private static string? NormalizeCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
