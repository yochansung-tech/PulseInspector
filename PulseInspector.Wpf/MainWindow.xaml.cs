using System.Windows;
using Microsoft.Win32;
using PulseInspector.Wpf.ViewModels;
using PulseInspector.Wpf.Views;

namespace PulseInspector.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        ViewModel.WaveformChanged += OnWaveformChanged;
        OnWaveformChanged(ViewModel, EventArgs.Empty);
    }

    private void AddGroup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Multiselect = true, Title = "Select waveforms belonging to one group" };
        if (dialog.ShowDialog(this) == true) ViewModel?.AddGroup(dialog.FileNames);
    }

    private void AddRows_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Multiselect = true, Title = "Select CSV files (one row = one subgroup)" };
        if (dialog.ShowDialog(this) == true) ViewModel?.AddGroupsFromRows(dialog.FileNames);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        var window = new SettingsWindow(ViewModel.DecisionPolicy, ViewModel.Confidence, ViewModel.SampleIntervalSeconds) { Owner = this };
        if (window.ShowDialog() == true) ViewModel.ApplySettings(window.Policy, window.Confidence, window.SampleIntervalSeconds);
    }

    private void OnWaveformChanged(object? sender, EventArgs e) => WaveformView.SetData(ViewModel?.Waveform);

    protected override void OnClosed(EventArgs e)
    {
        if (ViewModel is not null) ViewModel.WaveformChanged -= OnWaveformChanged;
        base.OnClosed(e);
    }
}
