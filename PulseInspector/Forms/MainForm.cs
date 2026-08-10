using System.Drawing;
using System.Windows.Forms;
using PulseInspector.Controls;
using PulseInspector.Services;

namespace PulseInspector.Forms;

public sealed class MainForm : Form
{
    private readonly WaveformControl _waveform = new();
    private readonly FeatureGrid _features = new();
    private readonly StatusIndicator _status = new();
    private readonly FeatureExtractor _extractor = new();

    public MainForm()
    {
        Text = "PulseInspector Release 1.0"; Width = 1200; Height = 760; StartPosition = FormStartPosition.CenterScreen;
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File"); var open = new ToolStripMenuItem("Open CSV..."); open.Click += (_, _) => LoadCsv();
        var settings = new ToolStripMenuItem("Settings"); settings.Click += (_, _) => new SettingsForm().ShowDialog(this);
        var training = new ToolStripMenuItem("Training"); training.Click += (_, _) => new TrainingForm().ShowDialog(this);
        file.DropDownItems.Add(open); menu.Items.Add(file); menu.Items.Add(training); menu.Items.Add(settings);
        var about = new ToolStripMenuItem("About"); about.Click += (_, _) => new AboutForm().ShowDialog(this); menu.Items.Add(about);
        Controls.Add(menu); MainMenuStrip = menu;

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 800 };
        split.Panel1.Controls.Add(_waveform); split.Panel2.Controls.Add(_features); Controls.Add(split);
        var status = new Panel { Dock = DockStyle.Bottom, Height = 34 }; status.Controls.Add(_status); Controls.Add(status);
    }

    private void LoadCsv()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var values = File.ReadAllLines(dialog.FileName).SelectMany(line => line.Split(',', ';', '\t')).Select(s => double.TryParse(s.Trim(), out var v) ? (double?)v : null).Where(v => v.HasValue).Select(v => v!.Value).ToArray();
        if (values.Length == 0) { MessageBox.Show(this, "No numeric samples found."); return; }
        _waveform.SetData(values); _features.SetFeatures(_extractor.Extract(values)); _status.SetState(true, $"Loaded {values.Length} samples");
    }
}
