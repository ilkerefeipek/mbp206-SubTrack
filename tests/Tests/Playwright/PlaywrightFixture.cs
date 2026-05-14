using Microsoft.Playwright;

namespace SubTrack.Tests.Playwright;

/// <summary>
/// Lazy Chromium browser fixture for Blazor WASM end-to-end tests.
/// Activated in Sprint S5 when the UI surface is complete; until then,
/// tests using this fixture are marked [Fact(Skip = ...)].
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright? Playwright { get; private set; }
    public IBrowser? Browser { get; private set; }

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        Playwright?.Dispose();
    }
}

[CollectionDefinition("playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>;

[Collection("playwright")]
public class HomePageSmokeTests
{
    private readonly PlaywrightFixture _fixture;

    public HomePageSmokeTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact(Skip = "Activated in Sprint S5 (E2E) when Blazor UI is complete")]
    public async Task Home_Page_Renders_SubTrack_Heading()
    {
        var page = await _fixture.Browser!.NewPageAsync();
        await page.GotoAsync("http://localhost:5173");
        var heading = await page.Locator("h1").TextContentAsync();
        Assert.Contains("SubTrack", heading);
    }
}
