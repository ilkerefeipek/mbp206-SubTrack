# SubTrack — Canli Demo Script (~5 dakika)

## Hazirlik

- **3 terminal** acik:
  - Terminal A: `dotnet run --project src/Api` (port 5000)
  - Terminal B: `dotnet run --project src/Client` (port 5277)
  - Terminal C: serbest (gerekirse `dotnet test` icin)
- **Tarayici:** Chrome incognito (cache temiz, bos localStorage)
- **Demo hesap:** `demo@subtrack.app` / `Test1234!`
- **Pencere:** 1280x800 onerilen

---

## Akis

### 1. Giris ve Korumali Route (~30 sn)

- `http://localhost:5277/` → otomatik `/login`'e yonlendirildi (AuthorizeView).
- Demo hesabiyla giris yap.
- Dashboard'a yonlendigi gozukur. F12 → Application → Local Storage → `subtrack_token` var.

### 2. Dashboard (~60 sn)

- 4 KPI kartini goster:
  - **Aktif Abonelik** (count)
  - **Aylik Toplam** — BillingMath (hafta/aylik/yillik normalize)
  - **Yakinda Yenilenecek** (server-side `/api/subscriptions/upcoming?daysAhead=30`)
  - **Kullanilmiyor** (kullanici ThresholdDays > LastUsedDate gap)
- Pie chart: kategori dagilimi (Streaming/Muzik/Verimlilik renkleriyle)
- Line chart: son 6 ay odeme trendi (Payment tablosu)
- Yaklasan Odemeler listesi: tarih + tutar

### 3. Abonelik Ekleme (~60 sn)

- "Yeni Abonelik" butonuna tikla.
- Form alanlarini doldur: orn. `Apple Music`, Kategori `Muzik`, 19.99 TRY Aylik, tarih +30 gun.
- "Kaydet" → toast success + `/subscriptions`'a yonlendirme.
- Listede yeni kayit gozukur.

### 4. Filtreleme ve Silme (~60 sn)

- Search input: "Spotify" yaz → 300ms debounce sonra liste sadece Spotify gosterir.
- Search'i temizle, kategori dropdown'undan "Streaming" sec → sadece Netflix/Disney+/YouTube.
- Bir abonelik satirinda "Sil" → onay modal'i acilir → modal icindeki "Sil" → toast + liste guncellenir.

### 5. Analiz (~60 sn)

- `/analytics`'e git.
- Month range selector: 3/6/12/24 ay.
- 3 KPI ozet karti (Aylik Toplam / Aktif Abonelik / Kullanilmiyor).
- Line chart: trend.
- Donut + legend tablosu: kategori bazli yuzde dagilim.
- **Akilli Oneriler:** "Adobe Creative Cloud kullanilmiyor" insight'i (LastUsedDate -60 gun, threshold 30).

### 6. Cikis (~30 sn)

- Sag ust avatar → User menu → "Cikis Yap"
- `/login`'e yonlendirme + localStorage token silindi (F12'de goster)
- Manuel olarak `http://localhost:5277/subscriptions` URL'ine git → korumali route, otomatik `/login`'e geri.

---

## Backup

Demo sirasinda hata olursa **`docs/test-results/s5b/TC-XX/final.png`** ekran goruntulerini ac — 10 senaryonun otomatik Playwright kanitlari.

## Sunum Tasiyici Notlar

- **Pivot:** Spec Bolum 18 React/Node istiyor, biz .NET 9 + Blazor WASM gittik (CLAUDE.md Bolum 15'te kayit).
- **Test sayisi:** 165+ test, hepsi yesil. 10 Playwright E2E + 6 security headers + 2 upcoming endpoint S6'da eklendi.
- **Spec Bolum 14/15/16/17/19/20/22:** birebir uygulanmis.
