namespace Waymarked.E2E.Tests;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Projects;
using Xunit;

/// <summary>
/// xunit fixture that boots the full Aspire AppHost (GraphHopper + waymarked-api + waymarked-web)
/// and exposes the web frontend base URL for Playwright tests.
/// </summary>
public class AspireFixture : IAsyncLifetime
{
    public DistributedApplication? App { get; private set; }
    public string WebBaseUrl { get; private set; } = "";

    /// <summary>
    /// The base URL of the smtp4dev web UI, used by auth tests to retrieve
    /// password-reset emails. Null when smtp4dev is not available (e.g., publish mode).
    /// </summary>
    public string? SmtpWebUiBaseUrl { get; private set; }

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

        // smtp4dev is only present in non-publish (local / CI test) mode.
        // If it is unavailable we degrade gracefully — tests that need it return early.
        using var smtpCts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        try
        {
            await notificationService.WaitForResourceAsync(
                "smtp4dev",
                KnownResourceStates.Running,
                smtpCts.Token);

            SmtpWebUiBaseUrl = App.GetEndpoint("smtp4dev", "webui").ToString().TrimEnd('/');
        }
        catch
        {
            SmtpWebUiBaseUrl = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (App is not null)
            await App.DisposeAsync();
    }
}
