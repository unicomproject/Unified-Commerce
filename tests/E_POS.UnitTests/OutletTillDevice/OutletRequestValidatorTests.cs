using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;
using Xunit;

namespace E_POS.UnitTests.OutletTillDevice;

/// <summary>
/// Unit tests for <see cref="OutletRequestValidator"/> covering:
/// - ContactEmail field validation (Step 2: Location &amp; Contact)
/// - ImageOperation matrix (KEEP / REPLACE / REMOVE)
/// </summary>
public sealed class OutletRequestValidatorTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static OutletRequestValidator CreateValidator() => new();

    private static OutletAddressRequest ValidAddress() => new(
        "123 Main Street",
        null,
        "Colombo",
        null,
        "00100",
        "LK",
        "Jane Doe",
        "+94771234567",
        null);   // ContactEmail – will be overridden per test

    private static OutletCreateRequest ValidCreateRequest(
        OutletAddressRequest? address = null,
        Guid? imageMediaAssetId = null) => new(
            "Test Outlet",
            "ACTIVE",
            "STORE",
            "Asia/Colombo",
            false,
            null,
            null,
            address ?? ValidAddress(),
            null,
            false,
            null,
            null,
            null,
            imageMediaAssetId);

    private static OutletUpdateRequest ValidUpdateRequest(
        OutletAddressRequest? address = null,
        OutletImageOperation? imageOperation = null,
        Guid? imageMediaAssetId = null) => new(
            "Test Outlet",
            "ACTIVE",
            "STORE",
            "Asia/Colombo",
            false,
            null,
            null,
            address ?? ValidAddress(),
            null,
            false,
            null,
            null,
            null,
            imageOperation,
            imageMediaAssetId);

    // -----------------------------------------------------------------------
    // ContactEmail – ValidateCreate
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateCreate_WithNullContactEmail_ReturnsNull()
    {
        var validator = CreateValidator();
        var request = ValidCreateRequest(ValidAddress() with { ContactEmail = null });

        var error = validator.ValidateCreate(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateCreate_WithValidContactEmail_ReturnsNull()
    {
        var validator = CreateValidator();
        var request = ValidCreateRequest(ValidAddress() with { ContactEmail = "contact@example.com" });

        var error = validator.ValidateCreate(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateCreate_WithContactEmailMissingAtSign_ReturnsValidationFailure()
    {
        var validator = CreateValidator();
        var request = ValidCreateRequest(ValidAddress() with { ContactEmail = "invalid-email" });

        var error = validator.ValidateCreate(request);

        Assert.NotNull(error);
        Assert.Equal("outlet.validation_failed", error.Code);
        Assert.Contains(error.FieldErrors ?? [], f => f.Field == "address.contactEmail");
    }

    [Fact]
    public void ValidateCreate_WithContactEmailExceeding255Chars_ReturnsValidationFailure()
    {
        var validator = CreateValidator();
        var longEmail = new string('a', 250) + "@x.com"; // 256 chars
        var request = ValidCreateRequest(ValidAddress() with { ContactEmail = longEmail });

        var error = validator.ValidateCreate(request);

        Assert.NotNull(error);
        Assert.Equal("outlet.validation_failed", error.Code);
        Assert.Contains(error.FieldErrors ?? [], f => f.Field == "address.contactEmail");
    }

    // -----------------------------------------------------------------------
    // ContactEmail – ValidateUpdate
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateUpdate_WithValidContactEmail_ReturnsNull()
    {
        var validator = CreateValidator();
        var request = ValidUpdateRequest(ValidAddress() with { ContactEmail = "store@brand.com" });

        var error = validator.ValidateUpdate(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateUpdate_WithInvalidContactEmail_ReturnsValidationFailure()
    {
        var validator = CreateValidator();
        var request = ValidUpdateRequest(ValidAddress() with { ContactEmail = "notanemail" });

        var error = validator.ValidateUpdate(request);

        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors ?? [], f => f.Field == "address.contactEmail");
    }

    // -----------------------------------------------------------------------
    // ImageOperation – REPLACE requires ImageMediaAssetId
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateUpdate_ReplaceWithAssetId_ReturnsNull()
    {
        var validator = CreateValidator();
        var request = ValidUpdateRequest(
            imageOperation: OutletImageOperation.REPLACE,
            imageMediaAssetId: Guid.NewGuid());

        var error = validator.ValidateUpdate(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateUpdate_ReplaceWithoutAssetId_ReturnsValidationFailure()
    {
        var validator = CreateValidator();
        var request = ValidUpdateRequest(
            imageOperation: OutletImageOperation.REPLACE,
            imageMediaAssetId: null);

        var error = validator.ValidateUpdate(request);

        Assert.NotNull(error);
        Assert.Equal("outlet.validation_failed", error.Code);
        Assert.Contains(error.FieldErrors ?? [], f => f.Field == "imageMediaAssetId");
    }

    // -----------------------------------------------------------------------
    // ImageOperation – REMOVE must not have ImageMediaAssetId
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateUpdate_RemoveWithoutAssetId_ReturnsNull()
    {
        var validator = CreateValidator();
        var request = ValidUpdateRequest(
            imageOperation: OutletImageOperation.REMOVE,
            imageMediaAssetId: null);

        var error = validator.ValidateUpdate(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateUpdate_RemoveWithAssetId_ReturnsValidationFailure()
    {
        var validator = CreateValidator();
        var request = ValidUpdateRequest(
            imageOperation: OutletImageOperation.REMOVE,
            imageMediaAssetId: Guid.NewGuid());

        var error = validator.ValidateUpdate(request);

        Assert.NotNull(error);
        Assert.Equal("outlet.validation_failed", error.Code);
        Assert.Contains(error.FieldErrors ?? [], f => f.Field == "imageMediaAssetId");
    }

    // -----------------------------------------------------------------------
    // ImageOperation – KEEP ignores ImageMediaAssetId (no errors either way)
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateUpdate_KeepWithAssetId_ReturnsNull()
    {
        var validator = CreateValidator();
        var request = ValidUpdateRequest(
            imageOperation: OutletImageOperation.KEEP,
            imageMediaAssetId: Guid.NewGuid()); // ignored for KEEP

        var error = validator.ValidateUpdate(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateUpdate_NullOperationDefaultsToKeep_ReturnsNull()
    {
        var validator = CreateValidator();
        var request = ValidUpdateRequest(
            imageOperation: null,
            imageMediaAssetId: null);

        var error = validator.ValidateUpdate(request);

        Assert.Null(error);
    }

    // -----------------------------------------------------------------------
    // ImageOperation – ValidateCreate derives operation from presence of ID
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateCreate_WithImageMediaAssetId_TreatedAsReplace_ReturnsNull()
    {
        var validator = CreateValidator();
        var request = ValidCreateRequest(imageMediaAssetId: Guid.NewGuid());

        var error = validator.ValidateCreate(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateCreate_WithoutImageMediaAssetId_TreatedAsKeep_ReturnsNull()
    {
        var validator = CreateValidator();
        var request = ValidCreateRequest(imageMediaAssetId: null);

        var error = validator.ValidateCreate(request);

        Assert.Null(error);
    }

    // -----------------------------------------------------------------------
    // Combined: invalid email + bad image operation → multiple field errors
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateUpdate_InvalidEmailAndBadImageOp_ReturnsBothFieldErrors()
    {
        var validator = CreateValidator();
        var request = ValidUpdateRequest(
            address: ValidAddress() with { ContactEmail = "bademail" },
            imageOperation: OutletImageOperation.REPLACE, // missing asset id
            imageMediaAssetId: null);

        var error = validator.ValidateUpdate(request);

        Assert.NotNull(error);
        Assert.Equal("outlet.validation_failed", error.Code);
        var fields = (error.FieldErrors ?? []).Select(f => f.Field).ToList();
        Assert.Contains("address.contactEmail", fields);
        Assert.Contains("imageMediaAssetId", fields);
    }
}
