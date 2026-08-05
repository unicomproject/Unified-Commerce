namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed class CustomerGoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public bool RememberMe { get; set; }
    public bool AgreeTerms { get; set; }
    public bool SendOffers { get; set; }
}
