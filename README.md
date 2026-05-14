<div align="center">

# SubTrack — Akıllı Abonelik Yönetim Platformu

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WASM-512BD4?logo=blazor&logoColor=white)](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
[![EF Core 9](https://img.shields.io/badge/EF%20Core-9.0-512BD4)](https://learn.microsoft.com/en-us/ef/core/)
[![Tests](https://img.shields.io/badge/tests-165%20pass-brightgreen)](#testler)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

**Dijital aboneliklerini tek bir panelde topla, harcamanı analiz et, kullanmadıklarını tespit et.**

[Hakkında](#hakkında) · [Kurulum](#kurulum) · [Mimari](#mimari) · [Testler](#testler) · [SDLC](#sdlc-dokümantasyonu)

</div>

---

## Hakkında

SubTrack, kullanıcıların Netflix, Spotify, Adobe Creative Cloud gibi dijital aboneliklerini tek bir noktada yönetmesini sağlayan web platformudur. **MBP 206 Sistem Analizi ve Tasarımı dersi (İzmir Ekonomi Üniversitesi) kapsamında Grup 2.2 tarafından geliştirilmiştir.**

Çalışma birimi tek atomic abonelik kaydıdır: bir kullanıcı, bir kategoriye bağlı, bir parasal değer ile bir yenileme döngüsüne sahip kayıt. Hesaplama (BillingMath), bildirim ve raporlama hizmetleri bu temel üzerine kurulur.

### Temel Özellikler

- JWT tabanlı kimlik doğrulama + BCrypt parola hash (workfactor 10)
- Dashboard: 4 KPI kartı (aktif/aylık toplam/yaklaşan/kullanılmıyor) + kategori dağılımı + 6 aylık ödeme trendi
- Abonelik CRUD: ekle, düzenle, sil, "kullanıldı" işaretle
- Filtreleme + arama (300ms debounce + server-side kategori dropdown)
- Analitik: 12 aylık trend, kategori bazlı pasta grafik, akıllı öneriler
- Kullanılmayan abonelik tespiti (kullanıcı tercihli eşik günü)
- Mobile responsive (Tailwind breakpoint'leri)
- Türkçe arayüz + tr-TR culture (currency, date)
- OWASP security headers (CSP, X-Frame-Options, HSTS, Referrer-Policy, Permissions-Policy)

---

## Kurulum

### Önkoşullar

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (Visual Studio kurulumu ile gelir)
- [Git](https://git-scm.com/)
- Windows 10/11 (LocalDB Windows-only)

### Adımlar

1. **Repository klonla:**
   ```powershell
   git clone https://github.com/ilkerefeipek/mbp206-SubTrack.git
   cd mbp206-SubTrack
   ```

2. **Bağımlılıkları yükle:**
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

4. **Veritabanını oluştur ve seed et:**
   ```powershell
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   ```
   API ilk çalıştırıldığında `DataSeeder` 5 kategori + demo kullanıcı + 7 abonelik ekler.

5. **Çalıştır (2 terminal):**

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

| Kategori | Sayı |
|---|---|
| Backend unit + integration (S0-S3) | 145 |
| Security headers middleware (S6) | 6 |
| /api/subscriptions/upcoming (S6) | 2 |
| Playwright E2E — Bölüm 22 TC-01..TC-10 | 10 |
| Smoke (SubTrack heading) | 1 |

### Bölüm 22 Acceptance Senaryoları

| TC | Senaryo | Durum |
|----|---|---|
| TC-01 | Giriş — Geçerli kimlik | OK |
| TC-02 | Giriş — Hatalı parola (generic mesaj) | OK |
| TC-03 | Kayıt — Yeni kullanıcı | OK |
| TC-04 | Abonelik Ekleme — Tüm alanlar | OK |
| TC-05 | Abonelik Ekleme — Boş alan (validation) | OK |
| TC-06 | Liste — Arama + kategori dropdown filtreleme | OK |
| TC-07 | Silme — Onay diyaloğu | OK |
| TC-08 | Dashboard — KPI hesaplama (tr-TR format + non-zero) | OK |
| TC-09 | Analiz — Kullanılmayan abonelik tespiti | OK |
| TC-10 | Çıkış (Logout) + token invalidation | OK |

### Playwright Çalıştırma (Manuel)

```powershell
# Terminal A: API
dotnet run --project src/Api

# Terminal B: Client
dotnet run --project src/Client

# Terminal C: Tests
$env:E2E_BASE_URL = "http://localhost:5277"
dotnet test --filter "FullyQualifiedName~E2E"
```

CI'da GitHub Actions otomatik koşturur (Windows runner).

---

## Ekran Görüntüleri

Sunum klasöründeki ekran görüntüleri: [`docs/sunum/`](docs/sunum/)
E2E test kanıtları: [`docs/test-results/s5b/`](docs/test-results/s5b/)

---

## Proje Yapısı

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
|  +- sunum/            Ekran görüntüleri + demo script
|  +- test-results/     E2E test kanıtları
+- .github/
|  +- workflows/        CI/CD pipeline
+- README.md
```

---

## SDLC Dokümantasyonu

Bu proje, [`docs/SDLC_Planlama_SubTrack.docx`](docs/SDLC_Planlama_SubTrack.docx) raporundaki yazılım geliştirme yaşam döngüsü planına göre 7 sprint'te geliştirilmiştir:

- **S0** Bootstrap — repo iskeleti, tooling
- **S1** Repository Pattern + UnitOfWork
- **S2** Authentication + JWT + FluentValidation
- **S3** Domain Services + 13 endpoint + owner-check
- **S4** Frontend Foundation — Blazor + layout + UI primitives
- **S5/S5b** 5 ekran + Playwright E2E (Bölüm 22 TC-01..TC-10 aktif)
- **S6** Polish + Deploy — TC-06/08 fix, upcoming endpoint, security headers, CI, README, v1.0

### Spec Uyumu

| Bölüm | Konu | Durum |
|---|---|---|
| 14 | N-Tier Mimari | Birebir |
| 15 | Veritabanı Tasarımı | Birebir |
| 16 | Wireframe / UI | Birebir |
| 17 | UML Sınıf Diyagramı | Birebir |
| 18 | Teknoloji Stack | **Pivot:** React/Node yerine .NET (gerekçe: ekibin gücü) |
| 19 | Git Stratejisi | Sprint-bazlı dallar |
| 20 | Veritabanı Oluşturma | EF Core migration eşdeğer |
| 21 | Ekran Detayları | Birebir |
| 22 | 10 Test Senaryosu | Playwright otomatize |

---

## Lisans

MIT — bkz. [LICENSE](LICENSE)
