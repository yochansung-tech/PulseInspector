using System.ComponentModel;
using System.Runtime.CompilerServices;
using PulseInspector.Application.Contracts;

namespace PulseInspector.Wpf.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IInspectionApplication _application;
    private string _statusText = "WPF migration foundation ready";

    public MainWindowViewModel(IInspectionApplication application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
                return;

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public void SetStatus(string message)
    {
        StatusText = message ?? string.Empty;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
