# FullBenchmark — Proje Durum Raporu

Tarih: 2026-03-25

---

## 1. Proje gerçekten tamamen bitti mi?

**Hayır. Ama ciddi bir MVP — iskelet değil.**

Toplam ~4.200 satır gerçek iş mantığı var. 5 benchmark modülünden 3'ü tam çalışır (CPU, Memory, Disk). GPU ve Network kasıtlı stub. Telemetri, scoring, veri katmanı, UI — hepsi gerçek implementasyon.

---

## 2. Çalışan modüller

| Bileşen | Durum | Detay |
|---------|-------|-------|
| **CPU Benchmark** | ✅ Tam çalışır | 5 workload: LCG integer ST/MT, FP sqrt ST/MT, SHA-256 |
| **Memory Benchmark** | ✅ Tam çalışır | 4 workload: allocation stress, copy throughput, latency, bandwidth |
| **Disk Benchmark** | ✅ Tam çalışır | 4 workload: seq read/write, 4K random read/write |
| **Scoring Engine V1** | ✅ Tam çalışır | 0-1000 normalize, ref = i7-12700/R7 5800X = 500 pt |
| **WMI Telemetri** | ✅ Tam çalışır | CPU, RAM, Disk, GPU, Network, güç durumu |
| **Performance Counters** | ✅ Tam çalışır | Per-core CPU, disk I/O, network |
| **LibreHardwareMonitor** | ✅ Çalışır* | CPU/GPU sıcaklık; *admin olmadan null döner |
| **EF Core + SQLite** | ✅ Tam çalışır | Migration mevcut, 13 tablo, seed data |
| **Karşılaştırma veri seti** | ✅ Tam çalışır | 23 referans cihaz, 7 kategori |
| **Dashboard VM** | ✅ Tam çalışır | Score kartları, sistem bilgisi, yakın cihazlar |
| **Run Benchmark VM** | ✅ Tam çalışır | Config, progress, sonuç gösterimi |
| **Live Monitor VM** | ✅ Tam çalışır | 5 canlı grafik, gerçek zamanlı metrikler |
| **History VM** | ✅ Tam çalışır | Geçmiş listesi, trend grafikleri |
| **Compare VM** | ✅ Tam çalışır | Filtreleme, percentile, cihaz karşılaştırması |
| **Settings VM** | ✅ Tam çalışır | Persist edilen benchmark konfigürasyonu |

---

## 3. Eksik / Placeholder / Mock

| Bileşen | Durum | Ne Eksik |
|---------|-------|----------|
| **GPU Benchmark** | ⚠️ Stub | `BenchmarkStatus.Skipped` döner. GPU compute algoritması yok. |
| **Network Benchmark** | ⚠️ Stub | `BenchmarkStatus.Skipped` döner. Loopback/endpoint stratejisi bekleniyor. |
| **GPU skoru** | ⚠️ Etkisiz | GPU stub olduğu için category weight redistribüsyonu yapılıyor (%10 GPU diğerlerine dağıtılıyor). |
| **LibreHardwareMonitor admin** | ⚠️ Koşullu | Admin yetkisi olmadan CPU/GPU sıcaklıkları null gelir, uygulama çökmez ama göstermez. |

Bunların dışında mock, hardcoded sahte veri, ya da "TODO: implement" yok.

---

## 4. Gerçek MVP mi, iskelet mi?

**Gerçek çalışan MVP.**

- Benchmark algoritmalar gerçek (LCG, SHA-256, Buffer.BlockCopy, FileStream WriteThrough)
- Telemetri gerçek Windows API'leri kullanıyor (WMI, PerformanceCounter, P/Invoke)
- SQLite veritabanı gerçek schema + migration + seed data
- Scoring referans değerleri araştırılmış (i7-12700 benchmarkları baz alınmış)
- UI 1.061 satır gerçek XAML, veri bağlama çalışır

Çalıştırırsanız: CPU/Memory/Disk benchmark tamamlanır, sonuçlar puanlanır, SQLite'a yazılır, Dashboard'da görünür, 23 referans cihazla karşılaştırılırsınız.

---

---

## Nasıl Çalıştırılır

### Startup Project

```
src/FullBenchmark.UI/FullBenchmark.UI.csproj
```

### Gerekli SDK ve Bağımlılıklar

| Gereksinim | Versiyon | Kontrol |
|------------|----------|---------|
| .NET SDK | 9.0+ | `dotnet --version` |
| Windows | 10/11 x64 | Zorunlu (WMI, WPF) |
| Visual Studio | 2022 17.8+ veya Rider | İsteğe bağlı |

NuGet paketleri otomatik restore edilir — ek kurulum gerekmez.

### Terminal Komutları

```bash
# Klonla / dizine gir
cd c:\cursor\project\FullBenchmark

# Restore + Build
dotnet build FullBenchmark.sln -c Debug

# Çalıştır (WPF = Windows zorunlu)
dotnet run --project src/FullBenchmark.UI/FullBenchmark.UI.csproj

# Release build
dotnet publish src/FullBenchmark.UI/FullBenchmark.UI.csproj -c Release -r win-x64 --self-contained
```

### Veri Konumu

Uygulama verileri otomatik oluşturulur:
```
%LOCALAPPDATA%\FullBenchmark\benchmark.db   ← SQLite veritabanı
%LOCALAPPDATA%\FullBenchmark\Logs\          ← Serilog log dosyaları
```

---

## İlk Test Senaryosu

1. Uygulamayı başlat → Dashboard açılır (skorlar boş, sistem bilgisi dolu)
2. Sol menüden **Run Benchmark** seç
3. CPU / Memory / Disk toggle'ları açık bırak, **Start Benchmark** tıkla
4. ~2-3 dakika bekle (progress bar dolar, workload isimleri görünür)
5. Tamamlandığında skor kartları dolar (0-1000 arası)
6. Dashboard'a dön → Overall, CPU, Memory, Disk skorları görünür
7. **Compare** sayfasına git → kendi cihazını 23 referans cihazla karşılaştır
8. **History** sayfasına git → trend grafiği görünür (şimdilik tek nokta)
9. **Live Monitor** → Start tıkla → CPU/RAM/Disk/Network grafikleri canlı akar

---

## Bilinen Hatalar ve Sınırlamalar

### Kesin Çalışmayan
| # | Sorun | Etki |
|---|-------|------|
| 1 | **GPU Benchmark** stub | GPU skoru hesaplanmaz, Overall skor 3 kategoriden oluşur |
| 2 | **Network Benchmark** stub | Network skoru yok |

### Koşullu / Ortama Bağlı
| # | Sorun | Etki | Çözüm |
|---|-------|------|-------|
| 3 | LibreHardwareMonitor admin gerektiriyor | Sıcaklık verileri Live Monitor'da boş gelir | Admin olarak çalıştır |
| 4 | Disk benchmark büyük temp dosyası yazar | 512MB temp dosya `%TEMP%` altına yazılır, sonra silinir | Yeterli disk alanı olmalı |
| 5 | İlk açılışta EF migration süresi | ~1-2sn SQLite schema oluşturma gecikmesi | Sadece ilk çalıştırmada |
| 6 | Seed data idempotent | Her restart'ta tekrar seed etmez | Tasarım gereği |

### Beklenen Ama Henüz Test Edilmemiş
| # | Alan | Risk |
|---|------|------|
| 7 | WMI sorguları bazı ortamlarda yavaş olabilir | İlk açılış 3-5sn sürebilir |
| 8 | PerformanceCounter init warm-up | İlk telemetri sample'ı 0 değerle gelebilir |
| 9 | Çok düşük puanlı sistemlerde NaN | Scoring clamp [0,1000] koruyor, risk düşük |

---

## Mimari Özet (Referans)

```
FullBenchmark.Contracts          ← Interface'ler, Entity'ler
FullBenchmark.Core               ← Shared utilities (ring buffer, vb.)
FullBenchmark.Benchmarks         ← CPU/Memory/Disk (tam), GPU/Network (stub)
FullBenchmark.Scoring            ← ScoringEngineV1 (tam)
FullBenchmark.Telemetry.Windows  ← WMI + PerfCounter + LibreHW (tam)
FullBenchmark.Infrastructure     ← EF Core + SQLite + Repositories + Seed (tam)
FullBenchmark.Application        ← Orchestrators + Services (tam)
FullBenchmark.UI                 ← WPF MVVM, 6 view, dark theme (tam)
```
