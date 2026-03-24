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

## GitHub’a paylaşma

Bu klasörde Git deposu hazır (`main` dalında ilk commit mevcut). Henüz **uzak depo bağlı değilse**:

1. [GitHub](https://github.com/new) üzerinde **boş** bir depo oluşturun (README / .gitignore eklemeyin).
2. Yerelde (kendi kullanıcı ve depo adınızla):

```bash
cd path/to/FullBenchmark
git remote add origin https://github.com/KULLANICI_ADINIZ/DEPO_ADI.git
git push -u origin main
```

**GitHub CLI** kullanıyorsanız: `gh auth login` sonrası proje kökünde `gh repo create FullBenchmark --public --source=. --remote=origin --push` de tek adımda oluşturup gönderebilirsiniz.

## Lisans

[MIT](LICENSE)
