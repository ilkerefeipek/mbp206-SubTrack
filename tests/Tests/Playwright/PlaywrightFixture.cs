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

    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5277";

    [Fact]
    public async Task Home_Page_Renders_SubTrack_Heading()
    {
        var context = await _fixture.Browser!.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        try
        {
            // Unauthenticated user landing on / will be redirected to /login by AuthorizeView.
            await page.GotoAsync($"{BaseUrl}/login");
            var heading = page.Locator("h1:has-text(\"SubTrack\")").First;
            await Assertions.Expect(heading).ToBeVisibleAsync(new() { Timeout = 10000 });
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
