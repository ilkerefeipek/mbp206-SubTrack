<div align="center">

# SubTrack — Akilli Abonelik Yonetim Platformu

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WASM-512BD4?logo=blazor&logoColor=white)](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
[![EF Core 9](https://img.shields.io/badge/EF%20Core-9.0-512BD4)](https://learn.microsoft.com/en-us/ef/core/)
[![Tests](https://img.shields.io/badge/tests-165%20pass-brightgreen)](#testler)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

**Dijital aboneliklerini tek bir panelde topla, harcamani analiz et, kullanmadiklarini tespit et.**

[Hakkinda](#hakkinda) · [Kurulum](#kurulum) · [Mimari](#mimari) · [Testler](#testler) · [SDLC](#sdlc-dokumantasyonu)

</div>

---

## Hakkinda

SubTrack, kullanicilarin Netflix, Spotify, Adobe Creative Cloud gibi dijital aboneliklerini tek bir noktada yonetmesini saglayan web platformudur. **MBP 206 Sistem Analizi ve Tasarimi dersi (Izmir Ekonomi Universitesi) kapsaminda Grup 2.2 tarafindan gelistirilmistir.**

Calisma birimi tek atomic abonelik kaydidir: bir kullanici, bir kategoriye bagli, bir parasal deger ile bir yenileme donguya sahip kayit. Hesaplama (BillingMath), bildirim ve raporlama hizmetleri bu temel uzerine kurulur.

### Temel Ozellikler

- JWT tabanli kimlik dogrulama + BCrypt parola hash (workfactor 10)
- Dashboard: 4 KPI karti (aktif/aylik toplam/yaklasan/kullanilmiyor) + kategori dagilimi + 6 aylik odeme trendi
- Abonelik CRUD: ekle, duzenle, sil, "kullanildi" isaretle
- Filtreleme + arama (300ms debounce + server-side kategori dropdown)
- Analitik: 12 aylik trend, kategori bazli pasta grafik, akilli oneriler
- Kullanilmayan abonelik tespiti (kullanici tercihli esik gunu)
- Mobile responsive (Tailwind breakpoint'leri)
- Turkce arayuz + tr-TR culture (currency, date)
- OWASP security headers (CSP, X-Frame-Options, HSTS, Referrer-Policy, Permissions-Policy)

---

## Kurulum

### Onkosullar

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (Visual Studio kurulumu ile gelir)
- [Git](https://git-scm.com/)
- Windows 10/11 (LocalDB Windows-only)

### Adimlar

1. **Repository klonla:**
   ```powershell
   git clone https://github.com/ilkerefeipek/mbp206-SubTrack.git
   cd mbp206-SubTrack
   ```

2. **Bagimliliklari yukle:**
   ```powershell
   dotnet restore
   ```

3. **User secrets ayarla** (ilk kurulum):
   ```powershell
   cd src/Api
   $key = [System.Guid]::NewGuid().ToString('N') + [System.Guid]::NewGuid().ToString('N')
   dotnet user-secrets set "Jwt:Key" $key
   cd ../..
   ```

4. **Veritabanini olustur ve seed et:**
   ```powershell
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   ```
   API ilk calistirildiginda `DataSeeder` 5 kategori + demo kullanici + 7 abonelik ekler.

5. **Calistir (2 terminal):**

   **Terminal A — Backend API:**
   ```powershell
   dotnet run --project src/Api
   ```
   API: http://localhost:5000 — Swagger: http://localhost:5000/swagger

   **Terminal B — Blazor Client:**
   ```powershell
   dotnet run --project src/Client
   ```
   UI: http://localhost:5277

### Demo Hesap

- **E-posta:** `demo@subtrack.app`
- **Parola:** `Test1234!`

---

## Mimari

```
+----------------------------------------------------------------+
|  SubTrack.Client (Blazor WASM)                                 |
|  - Pages (5 ekran: Login, Dashboard, Subscriptions, Add, Analytics, Notifications)
|  - Components (Ui primitives + Charts + Layout)                |
|  - Services/Api (6 HttpClient sarmalayicisi)                   |
|  - Services/Auth (JWT state provider + Bearer handler)         |
+--------------------+---------------------------------------------+
                     | HTTP + JWT Bearer
                     v
+----------------------------------------------------------------+
|  SubTrack.Api (ASP.NET Core 9 Web API)                         |
|  - Controllers (17 endpoint)                                   |
|  - Services (Auth, Subscription, Payment, Analytics, ...)      |
|  - Middleware (Exception, RateLimit, SecurityHeaders)          |
|  - Validators (FluentValidation)                               |
+--------------------+---------------------------------------------+
                     |
                     v
+----------------------------------------------------------------+
|  SubTrack.Infrastructure (EF Core 9)                           |
|  - AppDbContext + 6 EntityConfiguration                        |
|  - Repositories (Generic + 6 spesifik) + UnitOfWork            |
|  - DataSeeder                                                  |
+--------------------+---------------------------------------------+
                     |
                     v
              +---------------+
              | MSSQL LocalDB |
              |  SubTrack DB  |
              +---------------+

SubTrack.Domain - Entity + Enum + BaseEntity + Exceptions (bagimsiz)
SubTrack.Tests  - xUnit + Playwright .NET (tek proje)
```

### Teknoloji Stack

| Katman | Teknoloji |
|---|---|
| Frontend | Blazor WASM (.NET 9), Tailwind CSS 3.4 (standalone CLI), Blazor-ApexCharts |
| Backend | ASP.NET Core 9 (controller-based), FluentValidation 11.3.1, Serilog |
| ORM | EF Core 9 (SqlServer provider) |
| Auth | JWT HS256 + BCrypt.Net-Next |
| Security | OWASP A05 headers, rate limiter (login 5/15min, register 3/hr), owner-check 404 |
| Test | xUnit + FluentAssertions + Bogus + Microsoft.AspNetCore.Mvc.Testing + Microsoft.Playwright |
| CI | GitHub Actions (Windows runner, LocalDB) |
| Tooling | dotnet format + Husky.Net pre-commit, Conventional Commits |

---

## Testler

```powershell
dotnet test
```

**Toplam: 165+ test, 0 skip, 0 fail**

| Kategori | Sayi |
|---|---|
| Backend unit + integration (S0-S3) | 145 |
| Security headers middleware (S6) | 6 |
| /api/subscriptions/upcoming (S6) | 2 |
| Playwright E2E — Bolum 22 TC-01..TC-10 | 10 |
| Smoke (SubTrack heading) | 1 |

### Bolum 22 Acceptance Senaryolari

| TC | Senaryo | Durum |
|----|---|---|
| TC-01 | Giris — Gecerli kimlik | OK |
| TC-02 | Giris — Hatali parola (generic mesaj) | OK |
| TC-03 | Kayit — Yeni kullanici | OK |
| TC-04 | Abonelik Ekleme — Tum alanlar | OK |
| TC-05 | Abonelik Ekleme — Bos alan (validation) | OK |
| TC-06 | Liste — Arama + kategori dropdown filtreleme | OK |
| TC-07 | Silme — Onay diyalogu | OK |
| TC-08 | Dashboard — KPI hesaplama (tr-TR format + non-zero) | OK |
| TC-09 | Analiz — Kullanilmayan abonelik tespiti | OK |
| TC-10 | Cikis (Logout) + token invalidation | OK |

### Playwright Calistirma (Manuel)

```powershell
# Terminal A: API
dotnet run --project src/Api

# Terminal B: Client
dotnet run --project src/Client

# Terminal C: Tests
$env:E2E_BASE_URL = "http://localhost:5277"
dotnet test --filter "FullyQualifiedName~E2E"
```

CI'da GitHub Actions otomatik koshturur (Windows runner).

---

## Ekran Goruntuleri

Sunum klasorundeki ekran goruntuleri: [`docs/sunum/`](docs/sunum/)
E2E test kanitlari: [`docs/test-results/s5b/`](docs/test-results/s5b/)

---

## Proje Yapisi

```
mbp206-SubTrack/
+- src/
|  +- Api/              ASP.NET Core Web API
|  +- Domain/           Entity + Enum + Exceptions
|  +- Infrastructure/   EF Core + Repositories + UoW
|  +- Client/           Blazor WASM
+- tests/
|  +- Tests/            xUnit + Playwright
+- docs/
|  +- SDLC_Planlama_SubTrack.docx
|  +- sunum/            Ekran goruntuleri + demo script
|  +- test-results/     E2E test kanitlari
+- .github/
|  +- workflows/        CI/CD pipeline
+- README.md
```

---

## SDLC Dokumantasyonu

Bu proje, [`docs/SDLC_Planlama_SubTrack.docx`](docs/SDLC_Planlama_SubTrack.docx) raporundaki yazilim gelistirme yasam dongusu planina gore 7 sprint'te gelistirilmistir:

- **S0** Bootstrap — repo iskeleti, tooling
- **S1** Repository Pattern + UnitOfWork
- **S2** Authentication + JWT + FluentValidation
- **S3** Domain Services + 13 endpoint + owner-check
- **S4** Frontend Foundation — Blazor + layout + UI primitives
- **S5/S5b** 5 ekran + Playwright E2E (Bolum 22 TC-01..TC-10 aktif)
- **S6** Polish + Deploy — TC-06/08 fix, upcoming endpoint, security headers, CI, README, v1.0

### Spec Uyumu

| Bolum | Konu | Durum |
|---|---|---|
| 14 | N-Tier Mimari | Birebir |
| 15 | Veritabani Tasarimi | Birebir |
| 16 | Wireframe / UI | Birebir |
| 17 | UML Sinif Diyagrami | Birebir |
| 18 | Teknoloji Stack | **Pivot:** React/Node yerine .NET (gerekce: ekibin gucu) |
| 19 | Git Stratejisi | Sprint-bazli dallar |
| 20 | Veritabani Olusturma | EF Core migration esdeger |
| 21 | Ekran Detaylari | Birebir |
| 22 | 10 Test Senaryosu | Playwright otomatize |

---

## Lisans

MIT — bkz. [LICENSE](LICENSE)
