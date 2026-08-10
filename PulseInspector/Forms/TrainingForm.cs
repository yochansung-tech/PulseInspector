using System.Windows.Forms;
using PulseInspector.Services;

namespace PulseInspector.Forms;

public sealed class TrainingForm : Form
{
    private readonly TextBox _confidence = new() { Text = "0.999", Dock = DockStyle.Top };
    public TrainingForm()
    {
        Text = "Training"; Width = 420; Height = 180; StartPosition = FormStartPosition.CenterParent;
        var label = new Label { Text = "Confidence", Dock = DockStyle.Top, Height = 30 };
        var button = new Button { Text = "Calculate threshold", Dock = DockStyle.Top, Height = 36 };
        button.Click += (_, _) => { var value = StatisticsService.ChiSquare99_9(6); MessageBox.Show(this, $"Chi-square threshold (df=6, 99.9%) = {value:F6}"); };
        Controls.Add(button); Controls.Add(_confidence); Controls.Add(label);
    }
}
