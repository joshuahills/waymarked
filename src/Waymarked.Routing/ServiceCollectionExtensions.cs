using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Waymarked.Routing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraphHopperClient(
        this IServiceCollection services,
        string baseAddress)
    {
        services.AddHttpClient<GraphHopperClient>(client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
