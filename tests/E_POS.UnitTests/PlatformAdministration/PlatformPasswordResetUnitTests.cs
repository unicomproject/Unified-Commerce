using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformPasswordPolicyValidatorTests
{
    private readonly PlatformPasswordPolicyValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Empty_Fails(string? password)
    {
        var error = _validator.Validate(password);
        Assert.NotNull(error);
        Assert.Equal("platform_password_reset.password_policy", error!.Code);
    }

    [Fact]
    public void Validate_TooShort_Fails()
    {
        var error = _validator.Validate("Ab1");
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_MissingComplexity_Fails()
    {
        var error = _validator.Validate("abcdefgh");
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_StrongPassword_Passes()
    {
        var error = _validator.Validate("NewPass123");
        Assert.Null(error);
    }
}
