using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Projects;
using Xunit;

namespace Waymarked.E2E.Tests;

/// <summary>
/// xunit fixture that boots the full Aspire AppHost (GraphHopper + waymarked-api + waymarked-web)
/// and exposes the web frontend base URL for Playwright tests.
/// </summary>
public class AspireFixture : IAsyncLifetime
{
    public DistributedApplication? App { get; private set; }
    public string WebBaseUrl { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Waymarked_AppHost>();

        App = await appHost.BuildAsync();
        await App.StartAsync();

        // Wait up to 5 minutes for all resources to be ready (IoW graph loads fast,
        // but cold starts / image pulls in CI can be slow)
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var notificationService = App.Services
            .GetRequiredService<ResourceNotificationService>();

        // Wait for GraphHopper container to reach Running state first.
        // Even with a pre-built graph, the JVM + graph load takes a few seconds
        // after the container starts — the api's WaitFor(graphhopper) in the AppHost
        // ensures the HTTP endpoint is actually healthy before the api starts.
        await notificationService.WaitForResourceAsync(
            "graphhopper",
            KnownResourceStates.Running,
            cts.Token);

        // Wait for the web frontend to reach Running state (implies api + graphhopper healthy)
        await notificationService.WaitForResourceAsync(
            "waymarked-web",
            KnownResourceStates.Running,
            cts.Token);

        WebBaseUrl = App.GetEndpoint("waymarked-web", "http").ToString().TrimEnd('/');
    }

    public async Task DisposeAsync()
    {
        if (App is not null)
            await App.DisposeAsync();
    }
}
