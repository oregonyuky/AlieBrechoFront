using AlieBrecho.Application.Abstractions;
using AlieBrecho.Infrastructure.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AlieBrecho.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AlieBrechoApiOptions>(
            configuration.GetSection(AlieBrechoApiOptions.SectionName));

        services.AddTransient<AlieBrechoApiAuthorizationHandler>();
        services.AddHttpClient<AlieBrechoApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AlieBrechoApiOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(20);
        })
        .AddHttpMessageHandler<AlieBrechoApiAuthorizationHandler>();

        services.AddScoped<IProductCatalogGateway>(provider =>
            provider.GetRequiredService<AlieBrechoApiClient>());
        services.AddScoped<IOrderGateway>(provider =>
            provider.GetRequiredService<AlieBrechoApiClient>());
        services.AddScoped<IAuthenticationGateway>(provider =>
            provider.GetRequiredService<AlieBrechoApiClient>());
        services.AddScoped<ICustomerGateway>(provider =>
            provider.GetRequiredService<AlieBrechoApiClient>());

        return services;
    }
}
