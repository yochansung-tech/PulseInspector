using System.Windows;
using Microsoft.Win32;
using PulseInspector.Controls;
using PulseInspector.Wpf.Views;
using PulseInspector.Wpf.ViewModels;

namespace PulseInspector.Wpf;

public partial class MainWindow : Window
{
    private readonly WaveformControl _waveformControl;

    public MainWindow()
    {
        InitializeComponent();
        _waveformControl = new WaveformControl();
        WaveformHost.Child = _waveformControl;
        Loaded += OnLoaded;
        Closed += OnClosed;
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
        if (window.ShowDialog() == true)
        {
            try { ViewModel.ApplySettings(window.Policy, window.Confidence, window.SampleIntervalSeconds); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
    }

    private void OnWaveformChanged(object? sender, EventArgs e)
    {
        var samples = ViewModel?.Waveform;
        if (samples is { Count: > 0 }) _waveformControl.SetData(samples.ToArray());
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (ViewModel is not null) ViewModel.WaveformChanged -= OnWaveformChanged;
        WaveformHost.Child = null;
        _waveformControl.Dispose();
    }
}
