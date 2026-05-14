# SubTrack — Akilli Abonelik Yonetim Platformu

SubTrack, kullanicilarin streaming, muzik, verimlilik, oyun ve spor
aboneliklerini tek bir panelden takip etmelerini saglayan tam yigin web
platformudur. Otomatik yenileme uyarilari, kullanim takibi ve kullanilmayan
abonelik tespiti ile kullanicilarin aylik harcamalarini gorunur kilar.

Bu proje MBP 206 — Sistem Analizi ve Tasarimi dersi (Izmir Ekonomi
Universitesi) donem projesi olarak gelistirilmektedir. SDLC raporu
spec'i `docs/SDLC_Planlama_SubTrack.docx` dosyasinda saklanir; tum sprint'ler
spec'in Bolum 14/15/16/17/19/20/22'sine birebir uyumludur. Sadece Bolum 18
(Teknoloji Stack) operator tercihi ile .NET 9 ailesine pivot edilmistir.

Calisma birimi tek atomic abonelik kaydidir: bir kullanici, bir kategoriye
bagli, bir parasal degerle, bir yenileme dongusu ile takip edilir.
Hesaplama, bildirim ve raporlama hizmetleri bu temel uzerine kurulur.

## Teknoloji Yigini

| Katman | Teknoloji |
|---|---|
| Backend | ASP.NET Core 9 Web API (C#, controller-based) |
| Frontend | Blazor WebAssembly 9 (C#) |
| ORM | Entity Framework Core 9 |
| DB | Microsoft SQL Server (LocalDB development) |
| Auth | JWT (Microsoft.AspNetCore.Authentication.JwtBearer) + BCrypt.Net-Next |
| Validation | FluentValidation |
| Test | xUnit + FluentAssertions + Bogus + Microsoft.AspNetCore.Mvc.Testing |
| E2E | Microsoft.Playwright (S5'te aktif) |
| Styling | Tailwind CSS 3.4 (standalone CLI binary, Node bagimsiz) |
| Logging | Serilog |
| Lint | dotnet format + .editorconfig + Husky.Net (pre-commit) |
| CI | GitHub Actions (windows-latest, .NET 9) |

## Onkosullar

- **Windows 10/11** (SQL Server LocalDB Windows-only)
- **.NET 9 SDK** ([indir](https://dotnet.microsoft.com/download/dotnet/9.0))
- **MSSQL LocalDB** (Visual Studio Installer ile veya SQL Server Express tek
  basina kurulumu)
- **Git** + **PowerShell 5.1+**
- **GitHub CLI** (`gh`) — PR olusturmak icin opsiyonel, yoksa tarayicidan elle acilabilir

## Kurulum

```powershell
# 1. Klonla
git clone https://github.com/ilkerefeipek/mbp206-SubTrack.git
cd mbp206-SubTrack

# 2. Tailwind standalone CLI'i indir (tools/ klasoru olusur, gitignored)
.\scripts\install-tailwind.ps1

# 3. Restore + build + DB migration + seed (tek seferde)
dotnet restore
dotnet build
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

User-secrets ile JWT key tanimi (tek seferlik):
```powershell
cd src/Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "$(([guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')))"
cd ../..
```

## Calistirma

Iki ayri terminalde:
```powershell
# Terminal 1 — API
dotnet run --project src/Api
# Listens at http://localhost:5000

# Terminal 2 — Blazor Client
dotnet run --project src/Client
# Listens at http://localhost:5173
```

Saglik kontrolu:
```powershell
curl http://localhost:5000/api/health
# { "status": "OK", "version": "0.1.0", "environment": "Development", "dbConnected": true }
```

## Test

```powershell
dotnet test
```

## Sprint Plani

| Sprint | Kapsam | Durum |
|---|---|---|
| S0 | Bootstrap (5 proje, EF Core, ilk migration, seed, health endpoint) | gelistiriliyor |
| S1 | Database refinement (repository pattern, index'ler, query optimization) | bekliyor |
| S2 | Auth + User + Category modulu (16 endpoint) | bekliyor |
| S3 | Subscription modulu (CRUD + calculateNextPayment + markAsUsed) | bekliyor |
| S4 | Payment + UsageLog + Notification + Analytics | bekliyor |
| S5 | UI tamamlanmasi + Playwright 10 test senaryosu (TC-01..TC-10) | bekliyor |
| S6 | A11y + performans + Azure App Service deploy | bekliyor |

## Branch Stratejisi

Spec Bolum 19 — sprint bazli:
- `main` (release)
- `develop` (integration)
- `feature/sN-<scope>` (her sprint icin)

Conventional Commits zorunlu: `feat:`, `fix:`, `docs:`, `style:`, `refactor:`, `test:`, `chore:`.

## Lisans

MIT — `LICENSE` dosyasina bakin.
