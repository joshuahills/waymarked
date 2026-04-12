using Microsoft.Extensions.DependencyInjection;

namespace Waymarked.Routing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraphHopperClient(this IServiceCollection services)
    {
        // "http://graphhopper" is resolved by Aspire service discovery using the resource name
        services.AddHttpClient<GraphHopperClient>(client =>
        {
            client.BaseAddress = new Uri("http://graphhopper");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
