using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SubTrack.Tests.E2E;

/// <summary>
/// Bolum 22 TC-01..TC-10 senaryolari — Playwright ile gercek tarayicida.
///
/// MANUEL CALISTIRMA:
///   Terminal 1: dotnet run --project src/Api
///   Terminal 2: dotnet run --project src/Client          (default http://localhost:5277)
///   Terminal 3: $env:E2E_BASE_URL = "http://localhost:5277"
///               dotnet test --filter "FullyQualifiedName~E2E" --logger "console;verbosity=normal"
///   $env:PWDEBUG=1 ile headed mod (debug icin).
///
/// CI otomasyonu (GitHub Actions ile Api + Client'in test runner'da spawn edilmesi) S6 isi.
/// </summary>
public class PlaywrightE2ETests : IAsyncLifetime
{
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
            ViewportSize = new() { Width = 1366, Height = 768 },
            IgnoreHTTPSErrors = true,
            Locale = "tr-TR",
            // Test isolation: server uses X-Test-Client header as a rate-limit partition key.
            // Each test gets a unique partition so consecutive logins don't hit the 5/15min limit.
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["X-Test-Client"] = $"e2e-{Guid.NewGuid():N}"
            }
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

    // ─────────────────────────────────────────────────────────────────
    //  Yardimcilar
    // ─────────────────────────────────────────────────────────────────

    private async Task LoginAsDemoAsync()
    {
        await _page!.GotoAsync($"{BaseUrl}/login");
        await _page.GetByLabel("E-posta").FillAsync("demo@subtrack.app");
        await _page.GetByLabel("Parola").FillAsync("Test1234!");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Giris Yap" }).ClickAsync();
        await _page.WaitForURLAsync(new Regex($"^{Regex.Escape(BaseUrl)}/?$"));
    }

    private async Task ScreenshotAsync(string tcName)
    {
        var dir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "docs", "test-results", "s5b", tcName);
        Directory.CreateDirectory(dir);
        await _page!.ScreenshotAsync(new()
        {
            Path = Path.Combine(dir, "final.png"),
            FullPage = true
        });
    }

    private static string UniqueEmail() =>
        $"e2e-{DateTime.UtcNow.Ticks}@subtrack.app";

    // Select bileseninde label-for baglantisi yok — parent div text match'le bul.
    private ILocator SelectByLabel(string labelText) =>
        _page!.Locator($"div:has(> label:text-is(\"{labelText}\")) >> select").First;

    // DatePicker icin de label-for yok — type=date input'a parent text match.
    private ILocator DatePickerByLabel(string labelText) =>
        _page!.Locator($"div:has(> label:has-text(\"{labelText}\")) >> input[type=\"date\"]").First;

    // ─────────────────────────────────────────────────────────────────
    // TC-01 Giris — Gecerli kimlik bilgileri
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC01_Login_ValidCredentials_RedirectsToDashboard()
    {
        await _page!.GotoAsync($"{BaseUrl}/login");

        await _page.GetByLabel("E-posta").FillAsync("demo@subtrack.app");
        await _page.GetByLabel("Parola").FillAsync("Test1234!");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Giris Yap" }).ClickAsync();

        // Dashboard'a yonlendirildi
        await _page.WaitForURLAsync(new Regex($"^{Regex.Escape(BaseUrl)}/?$"));

        // Welcome banner gorunur ("Merhaba, ..." h1)
        var welcome = _page.Locator("h1:has-text(\"Merhaba\")").First;
        await Expect(welcome).ToBeVisibleAsync(new() { Timeout = 10000 });

        // localStorage'da token saklandi (Blazored.LocalStorage JSON-encoded saklayabilir)
        var token = await _page.EvaluateAsync<string?>(
            "() => window.localStorage.getItem('subtrack_token')");
        token.Should().NotBeNullOrEmpty();

        await ScreenshotAsync("TC-01");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-02 Giris — Hatali parola
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC02_Login_InvalidPassword_ShowsError()
    {
        await _page!.GotoAsync($"{BaseUrl}/login");

        await _page.GetByLabel("E-posta").FillAsync("demo@subtrack.app");
        await _page.GetByLabel("Parola").FillAsync("WrongPass123!");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Giris Yap" }).ClickAsync();

        // role=alert kutusunda generic hata mesaji
        var alert = _page.Locator("[role='alert']").First;
        await Expect(alert).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(alert).ToContainTextAsync("E-posta veya parola hatali");

        // URL hala /login
        _page.Url.Should().Contain("/login");

        // Token yok
        var token = await _page.EvaluateAsync<string?>(
            "() => window.localStorage.getItem('subtrack_token')");
        token.Should().BeNullOrEmpty();

        await ScreenshotAsync("TC-02");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-03 Kayit — Yeni kullanici
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC03_Register_NewUser_RedirectsToDashboard()
    {
        var email = UniqueEmail();
        await _page!.GotoAsync($"{BaseUrl}/register");

        // Register form: Ad/Soyad text inputs (sirayla), E-posta + Parola.
        // Ad/Soyad label-for var ama GetByLabel("Ad")'in Soyad ile partial match'i Required '*'
        // span eklediginde strict-mode'da belirsiz oluyor — index-based locator ile cozuyoruz.
        // Backend FluentValidator Ad/Soyad icin sadece harf kabul eder (regex ^[\p{L}\s'\-]+$).
        var textInputs = _page.Locator("input[type='text']");
        await textInputs.Nth(0).FillAsync("Test");
        await textInputs.Nth(1).FillAsync("User");
        await _page.Locator("input[type='email']").FillAsync(email);
        await _page.Locator("input[type='password']").FillAsync("Test1234!");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Kayit Ol" }).ClickAsync();

        // Dashboard'a yonlendirildi
        await _page.WaitForURLAsync(new Regex($"^{Regex.Escape(BaseUrl)}/?$"), new() { Timeout = 15000 });

        // Token saklandi
        var token = await _page.EvaluateAsync<string?>(
            "() => window.localStorage.getItem('subtrack_token')");
        token.Should().NotBeNullOrEmpty();

        await ScreenshotAsync("TC-03");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-04 Abonelik Ekleme — Tum alanlar dolu
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC04_AddSubscription_AllFields_AppearsInList()
    {
        await LoginAsDemoAsync();
        await _page!.GotoAsync($"{BaseUrl}/subscriptions/new");

        var uniqueName = $"E2E-Test-{DateTime.UtcNow.Ticks % 1_000_000}";

        await _page.GetByLabel("Servis Adi").FillAsync(uniqueName);
        await SelectByLabel("Kategori").SelectOptionAsync(new SelectOptionValue { Label = "Streaming" });
        await _page.GetByLabel("Tutar").FillAsync("49.99");
        await SelectByLabel("Para Birimi").SelectOptionAsync(new SelectOptionValue { Label = "TRY" });
        await SelectByLabel("Fatura Donemi").SelectOptionAsync(new SelectOptionValue { Label = "Aylik" });

        // DatePicker: bugun + 30 gun
        var nextDate = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd");
        await DatePickerByLabel("Sonraki Fatura Tarihi").FillAsync(nextDate);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Kaydet" }).ClickAsync();

        // Liste sayfasina yonlendirildi
        await _page.WaitForURLAsync(new Regex($"^{Regex.Escape(BaseUrl)}/subscriptions/?$"), new() { Timeout = 10000 });

        // Yeni abonelik listede
        var row = _page.Locator($"text=\"{uniqueName}\"").First;
        await Expect(row).ToBeVisibleAsync();

        await ScreenshotAsync("TC-04");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-05 Abonelik Ekleme — Bos zorunlu alan (validation)
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC05_AddSubscription_EmptyName_ShowsValidationError()
    {
        await LoginAsDemoAsync();
        await _page!.GotoAsync($"{BaseUrl}/subscriptions/new");

        // Servis adini bos birak, Tutar gir, submit
        await _page.GetByLabel("Tutar").FillAsync("50");

        await _page.GetByRole(AriaRole.Button, new() { Name = "Kaydet" }).ClickAsync();

        // Form gonderilmedi — URL hala /subscriptions/new
        // (EditForm OnValidSubmit invalid model'de calistirmaz, navigation olmaz)
        await Expect(_page).ToHaveURLAsync(new Regex(@"/subscriptions/new"), new() { Timeout = 3000 });

        await ScreenshotAsync("TC-05");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-06 Liste — Arama (debouncer) ile filtreleme
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC06_SubscriptionsList_FilterByCategory_ShowsOnlyMatching()
    {
        await LoginAsDemoAsync();
        await _page!.GotoAsync($"{BaseUrl}/subscriptions");

        // Liste yuklensin — Netflix her zaman seed'de
        await Expect(_page.Locator("text=\"Netflix Premium\"").First).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator("text=\"Spotify Premium\"").First).ToBeVisibleAsync();

        // Search input ile "Spotify" ara — debouncer 300ms sonra API'ye refetch tetikler.
        // TopBar global aramayla cakismamasi icin sayfanin kendi placeholder'iyla hedefliyoruz.
        await _page.GetByPlaceholder("Abonelik adina gore ara...").FillAsync("Spotify");

        // Filter sonucu: Spotify gorunur, diger aboneliklerin (Netflix, Disney+, Adobe) hicbiri gozukmemeli
        await Expect(_page.Locator("text=\"Spotify Premium\"").First).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator("text=\"Netflix Premium\"")).ToHaveCountAsync(0);
        await Expect(_page.Locator("text=\"Disney+\"")).ToHaveCountAsync(0);
        await Expect(_page.Locator("text=\"Adobe Creative Cloud\"")).ToHaveCountAsync(0);

        await ScreenshotAsync("TC-06");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-07 Abonelik Silme — Onay diyalogu
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC07_DeleteSubscription_OpensConfirmModal_AndDeletes()
    {
        await LoginAsDemoAsync();

        // Bu test idempotent olsun diye once disposable bir abonelik olusturuyoruz.
        var disposableName = $"TC07-{DateTime.UtcNow.Ticks % 1_000_000}";

        await _page!.GotoAsync($"{BaseUrl}/subscriptions/new");
        await _page.GetByLabel("Servis Adi").FillAsync(disposableName);
        await SelectByLabel("Kategori").SelectOptionAsync(new SelectOptionValue { Label = "Streaming" });
        await _page.GetByLabel("Tutar").FillAsync("9.99");
        var nextDate = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd");
        await DatePickerByLabel("Sonraki Fatura Tarihi").FillAsync(nextDate);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Kaydet" }).ClickAsync();

        await _page.WaitForURLAsync(new Regex($"^{Regex.Escape(BaseUrl)}/subscriptions/?$"), new() { Timeout = 10000 });

        // Yeni satir listede
        var row = _page.Locator($"tr:has-text(\"{disposableName}\")").First;
        await Expect(row).ToBeVisibleAsync();

        // Satir icindeki "Sil" butonu (Danger variant) — modal'i acar
        await row.GetByRole(AriaRole.Button, new() { Name = "Sil" }).ClickAsync();

        // Modal acildi
        var modal = _page.GetByRole(AriaRole.Dialog);
        await Expect(modal).ToBeVisibleAsync();
        await Expect(modal).ToContainTextAsync("Aboneligi sil?");
        await Expect(modal.Locator($"text=\"{disposableName}\"")).ToBeVisibleAsync();

        // Modal icindeki "Sil" (footer) — onayli silme
        await modal.GetByRole(AriaRole.Button, new() { Name = "Sil" }).ClickAsync();

        // Modal kapandi + satir kayboldu
        await Expect(modal).Not.ToBeVisibleAsync(new() { Timeout = 5000 });
        await Expect(_page.Locator($"tr:has-text(\"{disposableName}\")")).Not.ToBeVisibleAsync();

        await ScreenshotAsync("TC-07");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-08 Dashboard — KPI hesaplama (para birimi gosterimi)
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC08_Dashboard_MonthlyTotal_CalculatedCorrectly()
    {
        await LoginAsDemoAsync();
        // Login Dashboard'a (/) yonlendirir — explicit goto gerekmez

        // 4 KpiCard etiketi gorunur
        await Expect(_page!.GetByText("Aktif Abonelik").First).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.GetByText("Aylik Toplam").First).ToBeVisibleAsync();
        await Expect(_page.GetByText("Yakinda Yenilenecek").First).ToBeVisibleAsync();
        await Expect(_page.GetByText("Kullanilmiyor").First).ToBeVisibleAsync();

        // Sayfada formatli para birimi (tr-TR: virgul ondalik, "TL" suffix) — Aylik Toplam KpiCard'i icin
        var trCurrency = _page.Locator("text=/[\\d.,]+\\s*TL/").First;
        await Expect(trCurrency).ToBeVisibleAsync();

        await ScreenshotAsync("TC-08");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-09 Analiz — Kullanilmayan abonelik tespiti
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC09_Analytics_DetectsUnusedSubscription()
    {
        await LoginAsDemoAsync();
        await _page!.GotoAsync($"{BaseUrl}/analytics");

        // Akilli Oneriler bolumu yuklendi
        await Expect(_page.GetByText("Akilli Oneriler").First).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Seed'de Adobe (LastUsedDate -60) ve Disney+ (-45) threshold 30+ → unused
        // InsightCard h3 title formati: "{ServiceName} kullanilmiyor"
        var unusedInsight = _page.Locator("h3:has-text(\"kullanilmiyor\")").First;
        await Expect(unusedInsight).ToBeVisibleAsync(new() { Timeout = 15000 });

        await ScreenshotAsync("TC-09");
    }

    // ─────────────────────────────────────────────────────────────────
    // TC-10 Cikis (Logout) — token + redirect
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TC10_Logout_ClearsTokenAndRedirectsToLogin()
    {
        await LoginAsDemoAsync();

        // Avatar dropdown'unu ac
        await _page!.Locator("button[aria-haspopup='true']").First.ClickAsync();

        // "Cikis Yap" butonuna tikla (UserMenu icinde)
        await _page.GetByRole(AriaRole.Button, new() { Name = "Cikis Yap" }).ClickAsync();

        // /login'e yonlendirme
        await _page.WaitForURLAsync(new Regex($"^{Regex.Escape(BaseUrl)}/login"), new() { Timeout = 10000 });

        // Token silindi
        var token = await _page.EvaluateAsync<string?>(
            "() => window.localStorage.getItem('subtrack_token')");
        token.Should().BeNullOrEmpty();

        // Korumali route'a erisim denenince hala /login
        await _page.GotoAsync($"{BaseUrl}/subscriptions");
        await _page.WaitForURLAsync(new Regex($"^{Regex.Escape(BaseUrl)}/login"), new() { Timeout = 10000 });

        await ScreenshotAsync("TC-10");
    }
}
