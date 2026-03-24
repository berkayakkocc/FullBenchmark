using CommunityToolkit.Mvvm.ComponentModel;

namespace FullBenchmark.UI.Models;

/// <summary>Single entry in the navigation rail.</summary>
public sealed class NavigationItem : ObservableObject
{
    public string           Label     { get; init; } = string.Empty;
    public string           Icon      { get; init; } = string.Empty;
    public ObservableObject ViewModel { get; init; } = null!;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
