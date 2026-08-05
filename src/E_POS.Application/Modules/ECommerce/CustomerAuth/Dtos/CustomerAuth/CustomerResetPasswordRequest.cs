namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed class CustomerResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
