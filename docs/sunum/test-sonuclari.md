# Bolum 22 — Test Sonuclari

Bu tablo, SDLC raporu Bolum 22'deki 10 kabul testinin otomasyon durumunu ozetler.
Tum testler Playwright .NET (Microsoft.Playwright 1.59) ile gercek Chromium browser'da kosturuldu.

| TC | Senaryo | Otomasyon | Sonuc | Kanit |
|---|---|---|---|---|
| TC-01 | Giris — Gecerli kimlik | Playwright | OK | [Screenshot](../test-results/s5b/TC-01/final.png) |
| TC-02 | Giris — Hatali parola (generic mesaj) | Playwright | OK | [Screenshot](../test-results/s5b/TC-02/final.png) |
| TC-03 | Kayit — Yeni kullanici | Playwright | OK | [Screenshot](../test-results/s5b/TC-03/final.png) |
| TC-04 | Abonelik Ekleme — Tum alanlar | Playwright | OK | [Screenshot](../test-results/s5b/TC-04/final.png) |
| TC-05 | Abonelik Ekleme — Bos alan validation | Playwright | OK | [Screenshot](../test-results/s5b/TC-05/final.png) |
| TC-06 | Liste — Arama + kategori dropdown filtreleme | Playwright | OK | [Screenshot](../test-results/s5b/TC-06/final.png) |
| TC-07 | Silme — Onay diyalogu | Playwright | OK | [Screenshot](../test-results/s5b/TC-07/final.png) |
| TC-08 | Dashboard — KPI hesaplama (tr-TR format) | Playwright | OK | [Screenshot](../test-results/s5b/TC-08/final.png) |
| TC-09 | Analiz — Kullanilmayan abonelik tespiti | Playwright | OK | [Screenshot](../test-results/s5b/TC-09/final.png) |
| TC-10 | Cikis (Logout) + token invalidation | Playwright | OK | [Screenshot](../test-results/s5b/TC-10/final.png) |

**Toplam: 10/10 basarili**

## Test Sayilari (Tum Suite)

```
dotnet test
Passed!  - Failed: 0, Skipped: 0, Total: 165+
```

| Kategori | Sayi |
|---|---|
| Backend unit + integration (S0..S3) | 145 |
| `/api/subscriptions/upcoming` (S6) | 2 |
| `SecurityHeadersMiddleware` (S6) | 6 |
| Playwright E2E — Bolum 22 TC-01..TC-10 (S5b) | 10 |
| HomePage smoke (S0/S5b) | 1 |
| **Toplam** | **164+** |

## Calistirma Notu

Playwright testleri Api + Client process'lerinin manuel veya CI'da spawn'ina ihtiyac duyar
(detay: [`README.md` → Testler bolumu](../../README.md#testler)). CI'da GitHub Actions
windows-latest runner'da otomatize edilmistir.
