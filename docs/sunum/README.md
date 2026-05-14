# Sunum Klasoru

Bu klasor SubTrack final sunumu icin hazirlanmistir.

## Ekran Goruntuleri (Manuel Alinacak)

Asagidaki ekranlar kullanici tarafindan manuel olarak alinmali ve bu klasore yerlestirilmelidir
(Claude Code env'inde browser yok, otomatik alinamadi).

Onerilen viewport: **1280x800** (Chrome DevTools "Toggle device toolbar" -> Responsive).

| Dosya | Sayfa | Aciklama |
|---|---|---|
| `login.png` | http://localhost:5277/login | Brand panel + form |
| `dashboard.png` | http://localhost:5277/ (login sonrasi) | 4 KPI karti + 2 grafik + yaklasan odemeler |
| `subscriptions.png` | http://localhost:5277/subscriptions | Liste, search input, status chip'ler, kategori dropdown |
| `add-sub.png` | http://localhost:5277/subscriptions/new | Form alanlari hepsi gozukur |
| `analytics.png` | http://localhost:5277/analytics | LineChart trend + PieChart breakdown + Akilli Oneriler |
| `mobile-dashboard.png` | Chrome DevTools "iPhone 12 Pro" | Mobile responsive sidebar/top-bar |

**Onerilen arac:** Windows Snipping Tool (Win+Shift+S) veya Chrome DevTools "Capture screenshot".

## Diger Dosyalar

- `demo-script.md` — 5 dakikalik canli demo akisi
- `test-sonuclari.md` — Bolum 22 TC sonuclari + screenshot link'leri

## E2E Test Kanitlari

`docs/test-results/s5b/TC-XX/final.png` — Playwright otomasyonu ekran goruntuleri (gitignored, lokal).
