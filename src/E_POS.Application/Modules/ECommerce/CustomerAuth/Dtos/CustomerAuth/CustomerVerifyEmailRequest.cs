namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed class CustomerVerifyEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
