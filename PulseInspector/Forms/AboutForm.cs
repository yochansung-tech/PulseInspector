using System.Windows.Forms;

namespace PulseInspector.Forms;

public sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About PulseInspector"; Width = 500; Height = 260; StartPosition = FormStartPosition.CenterParent;
        Controls.Add(new Label { Dock = DockStyle.Fill, Padding = new Padding(20), Text = "PulseInspector Release 1.0\n\n.NET 8 WinForms\nDesigner-free pure C#\n\nPulse waveform inspection and statistical anomaly detection.", AutoSize = false });
    }
}
