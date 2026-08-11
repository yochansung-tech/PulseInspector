using PulseInspector.Models;
using System.Globalization;
using System.Windows.Forms;

namespace PulseInspector.Forms;

public sealed class SettingsForm : Form
{
    private readonly ComboBox _rule = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox _rate = new() { Dock = DockStyle.Fill };
    private readonly TextBox _confidence = new() { Dock = DockStyle.Fill };
    private readonly TextBox _sampleInterval = new() { Dock = DockStyle.Fill };
    private readonly GroupDecisionPolicy _policy;

    public GroupDecisionPolicy Policy => new(ParseRule(), ParseRate());
    public double Confidence => ParseDouble(_confidence.Text, "Confidence");
    public double SampleIntervalSeconds => ParseDouble(_sampleInterval.Text, "Sample interval");

    public SettingsForm(GroupDecisionPolicy? policy = null, double confidence = 0.999, double sampleIntervalSeconds = 2.56e-6 / 64.0)
    {
        _policy = policy ?? new GroupDecisionPolicy();
        Text = "Settings"; Width = 520; Height = 300; StartPosition = FormStartPosition.CenterParent;

        _rule.Items.AddRange(new object[] { "Any defective subgroup", "Defective subgroup rate", "Maximum Mahalanobis" });
        _rule.SelectedIndex = (int)_policy.Rule;
        _rate.Text = _policy.DefectiveSubgroupRateThreshold.ToString("G6", CultureInfo.InvariantCulture);
        _confidence.Text = confidence.ToString("G6", CultureInfo.InvariantCulture);
        _sampleInterval.Text = sampleIntervalSeconds.ToString("G10", CultureInfo.InvariantCulture);

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(12) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        grid.Controls.Add(new Label { Text = "Group decision rule", AutoSize = true }, 0, 0); grid.Controls.Add(_rule, 1, 0);
        grid.Controls.Add(new Label { Text = "Defect rate threshold", AutoSize = true }, 0, 1); grid.Controls.Add(_rate, 1, 1);
        grid.Controls.Add(new Label { Text = "Training confidence", AutoSize = true }, 0, 2); grid.Controls.Add(_confidence, 1, 2);
        grid.Controls.Add(new Label { Text = "Sample interval (s)", AutoSize = true }, 0, 3); grid.Controls.Add(_sampleInterval, 1, 3);
        grid.Controls.Add(new Label { Text = "Defect rate is used only by the selected rate rule.", AutoSize = true }, 0, 4); grid.SetColumnSpan(grid.GetControlFromPosition(0, 4), 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel); grid.Controls.Add(buttons, 0, 5); grid.SetColumnSpan(buttons, 2);
        Controls.Add(grid); AcceptButton = ok; CancelButton = cancel;
    }

    private GroupDecisionRule ParseRule() => (GroupDecisionRule)_rule.SelectedIndex;
    private double ParseRate() => ParseDouble(_rate.Text, "Defect rate threshold");

    private static double ParseDouble(string text, string name)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
            throw new InvalidOperationException($"{name} must be a valid finite number.");
        return value;
    }
}
