namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed class CustomerLoginRequest
{
    public string EmailOrPhone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public bool RememberMe { get; set; }
}
