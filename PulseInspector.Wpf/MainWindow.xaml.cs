using System.Windows;
using Microsoft.Win32;
using PulseInspector.Controls;
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
        var dialog = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Multiselect = true,
            Title = "Select waveforms belonging to one group"
        };
        if (dialog.ShowDialog(this) != true) return;
        ViewModel?.AddGroup(dialog.FileNames);
    }

    private void Defective_Checked(object sender, RoutedEventArgs e) => ViewModel?.SetSelectedGroupDefective(true);

    private void Defective_Unchecked(object sender, RoutedEventArgs e) => ViewModel?.SetSelectedGroupDefective(false);

    private void OnWaveformChanged(object? sender, EventArgs e)
    {
        var samples = ViewModel?.Waveform;
        if (samples is null || samples.Count == 0) return;
        _waveformControl.SetData(samples.ToArray());
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (ViewModel is not null)
            ViewModel.WaveformChanged -= OnWaveformChanged;
        WaveformHost.Child = null;
        _waveformControl.Dispose();
    }
}
