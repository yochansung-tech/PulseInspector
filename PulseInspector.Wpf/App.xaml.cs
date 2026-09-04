using System.Windows;
using PulseInspector.Application.Contracts;
using PulseInspector.Application.Services;
using PulseInspector.Wpf.ViewModels;

namespace PulseInspector.Wpf;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IInspectionApplication application = new InspectionApplication();
        var window = new MainWindow
        {
            DataContext = new MainWindowViewModel(application)
        };

        window.Show();
    }
}
