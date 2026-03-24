using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullBenchmark.Application.Services;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Repositories;

namespace FullBenchmark.UI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject, INavigatedTo
{
    private readonly IBenchmarkRepository _repo;
    private readonly SystemInfoService    _systemInfo;

    public SettingsViewModel(IBenchmarkRepository repo, SystemInfoService systemInfo)
    {
        _repo       = repo;
        _systemInfo = systemInfo;
    }

    // ── Benchmark settings ─────────────────────────────────────────────────
    [ObservableProperty] private bool   _cpuEnabled     = true;
    [ObservableProperty] private bool   _memoryEnabled  = true;
    [ObservableProperty] private bool   _diskEnabled    = true;
    [ObservableProperty] private bool   _gpuEnabled     = false;
    [ObservableProperty] private bool   _networkEnabled = false;
    [ObservableProperty] private int    _cpuRunSeconds  = 10;
    [ObservableProperty] private int    _memRunSeconds  = 10;
    [ObservableProperty] private int    _diskFileSizeMb = 512;
    [ObservableProperty] private string _diskTempPath   = string.Empty;
    [ObservableProperty] private bool   _diskCleanup    = true;
    [ObservableProperty] private int    _warmupSeconds  = 3;

    // ── Machine info (read-only display) ───────────────────────────────────
    [ObservableProperty] private string _machineId    = "—";
    [ObservableProperty] private string _machineName  = "—";
    [ObservableProperty] private string _manufacturer = "—";
    [ObservableProperty] private string _model        = "—";

    // ── State ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private string _saveStatus = string.Empty;

    public string AppVersion => "1.0.0";
    public string DotNetVersion => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    public void OnNavigatedTo() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        // Populate machine info
        if (_systemInfo.CurrentProfile is not null)
        {
            MachineName  = _systemInfo.CurrentProfile.MachineName;
            MachineId    = _systemInfo.CurrentProfile.MachineId;
            Manufacturer = _systemInfo.CurrentProfile.Manufacturer ?? "—";
            Model        = _systemInfo.CurrentProfile.Model ?? "—";
        }

        // Load saved config
        var config = await _repo.GetDefaultConfigAsync();
        if (config is not null) ApplyConfig(config);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving   = true;
        SaveStatus = string.Empty;
        try
        {
            var config = BuildConfig();
            await _repo.SaveConfigAsync(config);
            SaveStatus = "Settings saved.";
        }
        catch (Exception ex)
        {
            SaveStatus = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        CpuEnabled     = true;
        MemoryEnabled  = true;
        DiskEnabled    = true;
        GpuEnabled     = false;
        NetworkEnabled = false;
        CpuRunSeconds  = 10;
        MemRunSeconds  = 10;
        DiskFileSizeMb = 512;
        DiskCleanup    = true;
        WarmupSeconds  = 3;
        DiskTempPath   = string.Empty;
    }

    private void ApplyConfig(BenchmarkConfig c)
    {
        CpuEnabled     = c.CpuEnabled;
        MemoryEnabled  = c.MemoryEnabled;
        DiskEnabled    = c.DiskEnabled;
        GpuEnabled     = c.GpuEnabled;
        NetworkEnabled = c.NetworkEnabled;
        CpuRunSeconds  = c.CpuRunSeconds;
        MemRunSeconds  = c.MemoryRunSeconds;
        DiskFileSizeMb = c.DiskTestFileSizeMB;
        DiskTempPath   = c.DiskTestTempPath ?? string.Empty;
        DiskCleanup    = c.DiskCleanupAfterTest;
        WarmupSeconds  = c.WarmupSeconds;
    }

    private BenchmarkConfig BuildConfig() => new()
    {
        Id                   = Guid.NewGuid(),
        Name                 = "Default",
        IsDefault            = true,
        CpuEnabled           = CpuEnabled,
        MemoryEnabled        = MemoryEnabled,
        DiskEnabled          = DiskEnabled,
        GpuEnabled           = GpuEnabled,
        NetworkEnabled       = NetworkEnabled,
        CpuRunSeconds        = CpuRunSeconds,
        MemoryRunSeconds     = MemRunSeconds,
        DiskRunSeconds       = 15,
        DiskTestFileSizeMB   = DiskFileSizeMb,
        DiskTestTempPath     = string.IsNullOrWhiteSpace(DiskTempPath) ? null : DiskTempPath,
        DiskCleanupAfterTest = DiskCleanup,
        WarmupSeconds        = WarmupSeconds,
        ScoringSchemaVersion = 1,
        CreatedAt            = DateTimeOffset.UtcNow
    };
}
