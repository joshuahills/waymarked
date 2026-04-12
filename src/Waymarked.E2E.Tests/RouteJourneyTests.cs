using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Waymarked.E2E.Tests;

/// <summary>
/// End-to-end Playwright tests driven against the full Aspire stack
/// (GraphHopper + waymarked-api + waymarked-web).
///
/// The <see cref="AspireFixture"/> boots the AppHost once per test-class run;
/// each test gets a fresh browser page.
/// </summary>
public class RouteJourneyTests : IClassFixture<AspireFixture>
{
    private readonly AspireFixture _fixture;

    public RouteJourneyTests(AspireFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Launches a headless Chromium browser and navigates to the app's base URL.
    /// Callers are responsible for disposing playwright and browser via await using / try-finally.
    /// </summary>
    private async Task<(IPlaywright playwright, IBrowser browser, IPage page)> OpenAppAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_fixture.WebBaseUrl);
        return (playwright, browser, page);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 1 — Home page loads with map
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HomePage_LoadsWithMap()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            var title = await page.TitleAsync();
            title.Should().ContainEquivalentOf("waymarked", AtLeast.Once());

            // The Leaflet map container should be visible
            var mapDiv = page.Locator("#map");
            await mapDiv.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
            (await mapDiv.IsVisibleAsync()).Should().BeTrue("the map div should be visible on load");

            // Plan Route button must be present
            var planButton = page.Locator("button", new PageLocatorOptions { HasTextString = "Plan Route" });
            (await planButton.CountAsync()).Should().BeGreaterThan(0, "a Plan Route button should exist");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 2 — Round-trip: search, plan, see stats
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RoundTrip_SearchAndPlan_ShowsRoute()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            // Type into the start search box and wait for autocomplete
            await page.FillAsync("#startSearch", "Keswick, Cumbria");
            var firstResult = page.Locator(".autocomplete-item").First;
            await firstResult.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
            await firstResult.ClickAsync();

            // Confirm lat/lon hidden inputs are populated after selection
            var startLat = await page.InputValueAsync("#startLat");
            var startLon = await page.InputValueAsync("#startLon");
            startLat.Should().NotBeNullOrEmpty("startLat should be set after picking a result");
            startLon.Should().NotBeNullOrEmpty("startLon should be set after picking a result");

            // Leave end point blank → round trip. Set distance to 10 km.
            await page.FillAsync("#distance", "10");
            var unitSelect = page.Locator("#distanceUnit");
            if (await unitSelect.CountAsync() > 0)
                await unitSelect.SelectOptionAsync("kilometres");

            // Plan the route
            await page.ClickAsync("button:has-text('Plan Route')");

            // Wait for the stats panel to become visible (up to 30 s — real GH call)
            await page.WaitForSelectorAsync(".stats.visible",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            var distText = await page.InnerTextAsync("#statDistance");
            distText.Should().Contain("km", "distance stat should include the unit");

            var timeText = await page.InnerTextAsync("#statTime");
            timeText.Should().NotBeNullOrEmpty("time stat should not be empty");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 3 — A→B route: search both ends, plan, see stats
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AbRoute_WithStartAndEnd_ShowsRoute()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            // Pick start
            await page.FillAsync("#startSearch", "Keswick");
            var firstStart = page.Locator(".autocomplete-item").First;
            await firstStart.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
            await firstStart.ClickAsync();

            // Pick end
            await page.FillAsync("#endSearch", "Derwentwater");
            var firstEnd = page.Locator(".autocomplete-item").First;
            await firstEnd.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
            await firstEnd.ClickAsync();

            // Plan
            await page.ClickAsync("button:has-text('Plan Route')");

            // Stats should appear
            await page.WaitForSelectorAsync(".stats.visible",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            var distText = await page.InnerTextAsync("#statDistance");
            distText.Should().NotBeNullOrEmpty("distance stat should be shown for A→B route");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 4 — Clicking "Set Start" map-click mode, then clicking the map
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapClick_SetsStartPoint()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            // Activate the "Set Start" click mode
            var setStartButton = page.Locator("button", new PageLocatorOptions
            {
                HasTextString = "Set Start"
            });
            await setStartButton.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
            await setStartButton.First.ClickAsync();

            // Click the centre of the map div
            var mapDiv = page.Locator("#map");
            var box = await mapDiv.BoundingBoxAsync();
            box.Should().NotBeNull("the map div must be on screen");
            await page.Mouse.ClickAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);

            // Give the reverse-geocode request time to complete
            await Task.Delay(TimeSpan.FromSeconds(3));

            // startLat hidden input should now have a value
            var startLat = await page.InputValueAsync("#startLat");
            startLat.Should().NotBeNullOrEmpty("clicking the map in Set-Start mode should set startLat");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 5 — Out-of-bounds coords trigger error message
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task OutOfBoundsCoords_ShowsError()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            // Inject Paris coordinates directly into the hidden inputs
            await page.EvalOnSelectorAsync("#startLat", "el => el.value = '48.8566'");
            await page.EvalOnSelectorAsync("#startLon", "el => el.value = '2.3522'");

            // Click Plan Route — the API should return 400 (outside GB)
            await page.ClickAsync("button:has-text('Plan Route')");

            // Wait for the error div to become visible
            await page.WaitForSelectorAsync(".error.visible",
                new PageWaitForSelectorOptions { Timeout = 15_000 });

            var errorText = await page.InnerTextAsync(".error");
            errorText.Should().MatchRegex("outside Great Britain|outside GB|bounds",
                "the error message should mention the UK boundary problem");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }
}
