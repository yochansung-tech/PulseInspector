using System.ComponentModel;
using System.Windows;
using PulseInspector.Controls;

namespace PulseInspector.Wpf;

public partial class MainWindow : Window
{
    private readonly WaveformControl _waveformControl;

    public MainWindow()
    {
        InitializeComponent();

        _waveformControl = new WaveformControl();
        WaveformHost.Child = _waveformControl;
        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        WaveformHost.Child = null;
        _waveformControl.Dispose();
    }
}
