using Microsoft.Extensions.DependencyInjection;

namespace E_POS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}