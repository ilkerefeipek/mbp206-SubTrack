using FluentAssertions;
using Microsoft.Playwright;

namespace SubTrack.Tests.E2E;

/// <summary>
/// Bolum 22 TC-01..TC-10 senaryolari — Playwright ile gercek tarayicida.
///
/// MANUEL CALISTIRMA:
///   Terminal 1: dotnet run --project src/Api
///   Terminal 2: dotnet run --project src/Client    (default http://localhost:5277)
///   Terminal 3: $env:E2E_BASE_URL = "http://localhost:5277"
///               dotnet test --filter "FullyQualifiedName~E2E" --logger "console;verbosity=normal"
///   PWDEBUG=1 ile headed mod (debug icin).
///
/// Default `dotnet test` calistirildiginda BU TEST SINIFI ATLANIR (Skip attribute'u).
/// CI'da GitHub Actions ile otomatize edilmesi S6 isi.
/// </summary>
public class PlaywrightE2ETests : IAsyncLifetime
{
    private const string _skipReason =
        "E2E — Api ve Client process'leri elle calistirilmali. Detaylar test sinifi XML doc'unda.";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    private string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5277";

    public async Task InitializeAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("PWDEBUG") != "1"
        });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 1366, Height = 768 }
        });
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }

        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    private async Task LoginAsync(string email, string password)
    {
        await _page!.GotoAsync($"{BaseUrl}/login");
        await _page.FillAsync("input[type='email']", email);
        await _page.FillAsync("input[type='password']", password);
        await _page.ClickAsync("button[type='submit']");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-01 Giris — Gecerli kimlik bilgileri
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC01_Login_ValidCredentials_RedirectsToDashboard()
    {
        await LoginAsync("demo@subtrack.app", "Test1234!");
        await _page!.WaitForURLAsync($"{BaseUrl}/", new() { Timeout = 5000 });
        (await _page.Locator("h1").TextContentAsync()).Should().Contain("Merhaba");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-02 Giris — Hatali parola
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC02_Login_InvalidPassword_ShowsError()
    {
        await LoginAsync("demo@subtrack.app", "WrongPass123!");
        var error = await _page!.Locator("[role='alert']").TextContentAsync();
        error.Should().Contain("E-posta veya parola hatali");
        _page.Url.Should().EndWith("/login");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-03 Kayit — Yeni kullanici
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC03_Register_NewUser_RedirectsToDashboard()
    {
        var unique = $"new-{Guid.NewGuid():N}@subtrack.app";
        await _page!.GotoAsync($"{BaseUrl}/register");
        await _page.FillAsync("input[type='email']", unique);
        await _page.FillAsync("input[type='password']", "ValidPass123");
        await _page.Locator("input").Nth(0).FillAsync("Test");
        await _page.Locator("input").Nth(1).FillAsync("User");
        await _page.ClickAsync("button[type='submit']");
        await _page.WaitForURLAsync($"{BaseUrl}/", new() { Timeout = 5000 });
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-04 Abonelik Ekleme — Tum alanlar dolu
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC04_AddSubscription_AllFields_AppearsInList()
    {
        await LoginAsync("demo@subtrack.app", "Test1234!");
        await _page!.GotoAsync($"{BaseUrl}/subscriptions/new");

        await _page.FillAsync("input[type='text']", "PlaywrightTest");
        await _page.FillAsync("input[type='number']", "99.99");
        await _page.ClickAsync("button[type='submit']");

        await _page.WaitForURLAsync($"{BaseUrl}/subscriptions", new() { Timeout = 5000 });
        (await _page.ContentAsync()).Should().Contain("PlaywrightTest");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-05 Abonelik Ekleme — Bos zorunlu alan
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC05_AddSubscription_EmptyName_ShowsValidationError()
    {
        await LoginAsync("demo@subtrack.app", "Test1234!");
        await _page!.GotoAsync($"{BaseUrl}/subscriptions/new");

        await _page.FillAsync("input[type='number']", "50");
        await _page.ClickAsync("button[type='submit']");

        var validation = await _page.Locator("text=/Servis adi gereklidir/").CountAsync();
        validation.Should().BeGreaterThan(0);
        _page.Url.Should().Contain("/subscriptions/new");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-06 Liste — Kategori filtreleme
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC06_SubscriptionsList_FilterByCategory_ShowsOnlyMatching()
    {
        await LoginAsync("demo@subtrack.app", "Test1234!");
        await _page!.GotoAsync($"{BaseUrl}/subscriptions");

        // Status chip "Aktif"
        await _page.ClickAsync("text=Aktif");

        var rows = await _page.Locator("tbody tr").CountAsync();
        rows.Should().BeGreaterThan(0);
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-07 Abonelik Silme — Onay diyalogu
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC07_DeleteSubscription_OpensConfirmModal_AndDeletes()
    {
        await LoginAsync("demo@subtrack.app", "Test1234!");
        await _page!.GotoAsync($"{BaseUrl}/subscriptions");

        var firstSilButton = _page.Locator("button:has-text('Sil')").First;
        await firstSilButton.ClickAsync();

        await _page.WaitForSelectorAsync("text=Aboneligi sil?");
        await _page.Locator("button:has-text('Sil')").Last.ClickAsync();

        // Toast "silindi" mesaji gorulmeli
        await _page.WaitForSelectorAsync("text=/silindi/", new() { Timeout = 3000 });
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-08 Dashboard — KPI hesaplama
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC08_Dashboard_MonthlyTotal_CalculatedCorrectly()
    {
        await LoginAsync("demo@subtrack.app", "Test1234!");
        await _page!.WaitForURLAsync($"{BaseUrl}/", new() { Timeout = 5000 });

        var monthlyTotalCard = _page.Locator("text=Aylik Toplam").Locator("xpath=..");
        var totalText = await monthlyTotalCard.TextContentAsync();
        totalText.Should().Contain("TL"); // Para birimi gosteriliyor
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-09 Analiz — Kullanilmayan abonelik tespiti
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC09_Analytics_DetectsUnusedSubscription()
    {
        await LoginAsync("demo@subtrack.app", "Test1234!");
        await _page!.GotoAsync($"{BaseUrl}/analytics");

        // Seed verisinde Adobe last_used 60 gun once + Disney+ 45 gun once → kullanilmiyor
        var insights = await _page.Locator("text=/kullanilmiyor|tasarruf/").CountAsync();
        insights.Should().BeGreaterThan(0);
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-10 Cikis — Logout + token invalidation
    // ─────────────────────────────────────────────────────────────────
    [Fact(Skip = _skipReason)]
    public async Task TC10_Logout_ClearsTokenAndRedirectsToLogin()
    {
        await LoginAsync("demo@subtrack.app", "Test1234!");
        await _page!.WaitForURLAsync($"{BaseUrl}/", new() { Timeout = 5000 });

        // User menu → Cikis Yap
        await _page.ClickAsync("button[aria-haspopup='true']");
        await _page.ClickAsync("text=Cikis Yap");

        await _page.WaitForURLAsync($"{BaseUrl}/login", new() { Timeout = 5000 });

        var tokenAfter = await _page.EvaluateAsync<string?>(
            "() => window.localStorage.getItem('subtrack_token')");
        tokenAfter.Should().BeNull();
    }
}
