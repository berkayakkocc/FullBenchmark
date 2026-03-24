using FullBenchmark.Benchmarks.Extensions;
using FullBenchmark.Infrastructure.Extensions;
using FullBenchmark.Scoring.Extensions;
using FullBenchmark.Telemetry.Windows.Extensions;
using FullBenchmark.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Windows;
// Aliases to avoid conflict with FullBenchmark.Application namespace
using WpfApp       = System.Windows.Application;
using AppExtensions = FullBenchmark.Application.Extensions.ServiceCollectionExtensions;
using AppServices   = FullBenchmark.Application.Services;

namespace FullBenchmark.UI;

public partial class App : WpfApp
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Serilog ────────────────────────────────────────────────────────
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir  = Path.Combine(appData, "FullBenchmark", "Logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft",                    LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .WriteTo.File(
                Path.Combine(logDir, "fullbenchmark-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        // ── Host ───────────────────────────────────────────────────────────
        var dbPath = Path.Combine(appData, "FullBenchmark", "benchmark.db");

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, svc) =>
            {
                svc.AddInfrastructure(dbPath);
                svc.AddWindowsTelemetry();
                svc.AddBenchmarkModules();
                svc.AddScoring();
                AppExtensions.AddApplicationServices(svc);

                // ViewModels
                svc.AddSingleton<MainWindowViewModel>();
                svc.AddSingleton<DashboardViewModel>();
                svc.AddSingleton<LiveMonitorViewModel>();
                svc.AddTransient<RunBenchmarkViewModel>();
                svc.AddSingleton<HistoryViewModel>();
                svc.AddSingleton<CompareViewModel>();
                svc.AddSingleton<SettingsViewModel>();

                // Main window
                svc.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // ── Bootstrap ──────────────────────────────────────────────────────
        try
        {
            await _host.Services.InitialiseDatabaseAsync();
            _host.Services.InitialiseTelemetry();

            var sysInfo = _host.Services.GetRequiredService<AppServices.SystemInfoService>();
            await sysInfo.InitialiseAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Bootstrap failed.");
            MessageBox.Show(
                $"Startup failed:\n{ex.Message}\n\nThe application will continue with limited functionality.",
                "FullBenchmark", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // ── Show window ────────────────────────────────────────────────────
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.Services.GetService<AppServices.TelemetryOrchestrator>()?.Stop();

            await _host.StopAsync(TimeSpan.FromSeconds(3));
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
