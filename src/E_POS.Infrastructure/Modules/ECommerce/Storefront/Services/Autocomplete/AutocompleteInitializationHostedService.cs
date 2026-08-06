using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Services.Autocomplete;

public class AutocompleteInitializationHostedService : IHostedService
{
    private readonly IStorefrontAutocompleteService _autocompleteService;
    private readonly ILogger<AutocompleteInitializationHostedService> _logger;

    public AutocompleteInitializationHostedService(IStorefrontAutocompleteService autocompleteService, ILogger<AutocompleteInitializationHostedService> logger)
    {
        _autocompleteService = autocompleteService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Autocomplete Initialization...");
        await _autocompleteService.LoadAllTenantsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
