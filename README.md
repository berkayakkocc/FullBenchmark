# FullBenchmark

Windows için **.NET 9** ve **WPF** tabanlı bir sistem performans ölçüm ve karşılaştırma masaüstü uygulaması: CPU, bellek ve disk benchmark’ları; canlı telemetri; SQLite ile geçmiş; referans cihazlarla karşılaştırma.

## Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Windows 10/11 x64 (WMI, WPF)

## Derleme ve çalıştırma

```bash
cd path/to/FullBenchmark
dotnet build FullBenchmark.sln -c Debug
dotnet run --project src/FullBenchmark.UI/FullBenchmark.UI.csproj
```

Veritabanı ve loglar: `%LOCALAPPDATA%\FullBenchmark\`

## Dokümantasyon

Ayrıntılı mimari, modül durumu ve test senaryoları için [PROJECT_STATUS.md](PROJECT_STATUS.md) dosyasına bakın.

## Lisans

[MIT](LICENSE)
