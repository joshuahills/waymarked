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

        // Wait for the web frontend to reach Running state (up to 2 minutes)
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var notificationService = App.Services
            .GetRequiredService<ResourceNotificationService>();

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
