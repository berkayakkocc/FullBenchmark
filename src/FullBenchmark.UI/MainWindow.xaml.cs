using FullBenchmark.UI.ViewModels;
using System.Windows;

namespace FullBenchmark.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
