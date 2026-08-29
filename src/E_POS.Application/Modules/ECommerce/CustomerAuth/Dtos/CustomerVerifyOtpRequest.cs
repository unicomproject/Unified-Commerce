namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public class CustomerVerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
