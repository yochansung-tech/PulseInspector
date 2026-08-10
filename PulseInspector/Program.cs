using System.Windows.Forms;
using PulseInspector.Forms;

namespace PulseInspector;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
