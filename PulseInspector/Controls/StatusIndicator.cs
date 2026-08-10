using System.Drawing;
using System.Windows.Forms;

namespace PulseInspector.Controls;

public sealed class StatusIndicator : Label
{
    public StatusIndicator() { AutoSize = false; Dock = DockStyle.Fill; TextAlign = ContentAlignment.MiddleLeft; Padding = new Padding(8, 0, 0, 0); SetState(false, "Ready"); }
    public void SetState(bool ok, string message) { Text = message; BackColor = ok ? Color.LightGreen : Color.LightGray; ForeColor = Color.Black; }
}
