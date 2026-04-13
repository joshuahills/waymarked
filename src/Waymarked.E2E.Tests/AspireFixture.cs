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

        // Hard timeout covers BuildAsync + StartAsync — without this, a hung
        // container image build or failed docker pull waits forever.
        using var startCts = new CancellationTokenSource(TimeSpan.FromMinutes(8));

        App = await appHost.BuildAsync(startCts.Token);
        await App.StartAsync(startCts.Token);

        // Wait up to the remaining time for resources to reach Running state.
        var notificationService = App.Services
            .GetRequiredService<ResourceNotificationService>();

        await notificationService.WaitForResourceAsync(
            "graphhopper",
            KnownResourceStates.Running,
            startCts.Token);

        await notificationService.WaitForResourceAsync(
            "waymarked-web",
            KnownResourceStates.Running,
            startCts.Token);

        WebBaseUrl = App.GetEndpoint("waymarked-web", "http").ToString().TrimEnd('/');
    }

    public async Task DisposeAsync()
    {
        if (App is not null)
            await App.DisposeAsync();
    }
}
