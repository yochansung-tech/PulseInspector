using System.Windows.Forms;

namespace PulseInspector.Forms;

public sealed class SettingsForm : Form
{
    public SettingsForm()
    {
        Text = "Settings"; Width = 420; Height = 220; StartPosition = FormStartPosition.CenterParent;
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(12) };
        grid.Controls.Add(new Label { Text = "Sample interval (s)", AutoSize = true }, 0, 0);
        grid.Controls.Add(new TextBox { Text = (2.56e-6 / 64).ToString("G10"), Dock = DockStyle.Fill }, 1, 0);
        grid.Controls.Add(new Label { Text = "Confidence", AutoSize = true }, 0, 1);
        grid.Controls.Add(new TextBox { Text = "0.999", Dock = DockStyle.Fill }, 1, 1);
        grid.Controls.Add(new Label { Text = "Baseline / Noise", AutoSize = true }, 0, 2);
        grid.Controls.Add(new Label { Text = "Lower-amplitude portion of the waveform", AutoSize = true }, 1, 2);
        Controls.Add(grid);
    }
}
