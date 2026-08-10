using System.Drawing;
using System.Windows.Forms;
using PulseInspector.Controls;
using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Forms;

public sealed class MainForm : Form
{
    private readonly WaveformControl _waveform = new();
    private readonly FeatureGrid _features = new();
    private readonly StatusIndicator _status = new();
    private readonly FeatureExtractor _extractor = new();
    private readonly GroupInspectionService _groupService = new();
    private readonly List<GroupData> _groups = new();
    private readonly ListBox _groupList = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _defective = new() { Text = "Selected group is defective", Dock = DockStyle.Top, Height = 28 };
    private InspectionModel? _model;
    private bool _updatingDefective;

    public MainForm()
    {
        Text = "PulseInspector Release 1.0";
        Width = 1400;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;

        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File");
        var addGroup = new ToolStripMenuItem("Add Group from CSV...");
        addGroup.Click += (_, _) => AddGroupFromCsv();
        var clear = new ToolStripMenuItem("Clear Groups");
        clear.Click += (_, _) => ClearGroups();
        file.DropDownItems.Add(addGroup);
        file.DropDownItems.Add(clear);
        menu.Items.Add(file);

        var training = new ToolStripMenuItem("Training");
        var train = new ToolStripMenuItem("Train Normal Groups");
        train.Click += (_, _) => TrainModel();
        training.DropDownItems.Add(train);
        var inspect = new ToolStripMenuItem("Inspect Selected Group");
        inspect.Click += (_, _) => InspectSelectedGroup();
        training.DropDownItems.Add(inspect);
        menu.Items.Add(training);

        var settings = new ToolStripMenuItem("Settings");
        settings.Click += (_, _) => new SettingsForm().ShowDialog(this);
        menu.Items.Add(settings);

        var about = new ToolStripMenuItem("About");
        about.Click += (_, _) => new AboutForm().ShowDialog(this);
        menu.Items.Add(about);

        Controls.Add(menu);
        MainMenuStrip = menu;

        _groupList.SelectedIndexChanged += (_, _) => ShowSelectedGroup();
        _defective.CheckedChanged += (_, _) => UpdateSelectedGroupLabel();

        var groupPanel = new Panel { Dock = DockStyle.Left, Width = 300, Padding = new Padding(8) };
        groupPanel.Controls.Add(_groupList);
        groupPanel.Controls.Add(_defective);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 850
        };
        split.Panel1.Controls.Add(_waveform);
        split.Panel2.Controls.Add(_features);

        Controls.Add(split);
        Controls.Add(groupPanel);

        var status = new Panel { Dock = DockStyle.Bottom, Height = 34 };
        status.Controls.Add(_status);
        Controls.Add(status);
    }

    private void AddGroupFromCsv()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Multiselect = true,
            Title = "Select waveforms belonging to one group"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var group = new GroupData();
        try
        {
            foreach (var file in dialog.FileNames)
            {
                var values = ReadCsv(file);
                if (values.Length == 0)
                    throw new InvalidOperationException($"No numeric samples found in '{Path.GetFileName(file)}'.");

                group.AddWaveform(values, _extractor.Extract(values));
            }

            _groups.Add(group);
            _groupList.Items.Add(CreateGroupLabel(group));
            _groupList.SelectedIndex = _groups.Count - 1;
            _model = null;
            _status.SetState(true, $"Added Group {ShortId(group)}: {group.SampleCount} waveform(s)");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Group load error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void TrainModel()
    {
        var normalGroups = _groups.Where(g => !g.IsDefective).ToArray();
        var required = FeatureVector.StatisticalFeatureNames.Count + 1;

        if (normalGroups.Length < required)
        {
            MessageBox.Show(this,
                $"At least {required} normal groups are required for the six-feature covariance model.\n" +
                $"Current normal groups: {normalGroups.Length}",
                "Training data insufficient",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _model = _groupService.Train(normalGroups);
            _status.SetState(true,
                $"Model trained: {normalGroups.Length} normal groups, threshold={_model.Threshold:F6}");
        }
        catch (Exception ex)
        {
            _model = null;
            MessageBox.Show(this, ex.Message, "Training error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.SetState(false, "Training failed");
        }
    }

    private void InspectSelectedGroup()
    {
        var index = _groupList.SelectedIndex;
        if (index < 0 || index >= _groups.Count)
        {
            MessageBox.Show(this, "Select a group first.");
            return;
        }

        var selected = _groups[index];
        var trainingGroups = _groups
            .Where(g => !g.IsDefective && !ReferenceEquals(g, selected))
            .ToArray();
        var required = FeatureVector.StatisticalFeatureNames.Count + 1;

        if (trainingGroups.Length < required)
        {
            MessageBox.Show(this,
                $"Inspection requires at least {required} normal training groups excluding the selected group.",
                "Training data insufficient",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _model = _groupService.Train(trainingGroups);
            var result = _groupService.Inspect(selected, _model);
            _features.SetFeatures(result.Features);
            _status.SetState(!result.IsDefect,
                $"Group {ShortId(selected)}: {(result.IsDefect ? "DEFECT" : "NORMAL")} | " +
                $"MD={result.MahalanobisDistance:F6}, Threshold={result.Threshold:F6}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Inspection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.SetState(false, "Inspection failed");
        }
    }

    private void ShowSelectedGroup()
    {
        var index = _groupList.SelectedIndex;
        if (index < 0 || index >= _groups.Count) return;

        var group = _groups[index];
        var waveform = group.MeanWaveform();
        if (waveform is not null) _waveform.SetData(waveform);

        var features = group.MeanFeatures();
        if (features is not null) _features.SetFeatures(features);

        _updatingDefective = true;
        _defective.Checked = group.IsDefective;
        _updatingDefective = false;
        _status.SetState(true, $"Selected Group {ShortId(group)}: {group.SampleCount} waveform(s)");
    }

    private void UpdateSelectedGroupLabel()
    {
        if (_updatingDefective) return;

        var index = _groupList.SelectedIndex;
        if (index < 0 || index >= _groups.Count) return;

        _groups[index].IsDefective = _defective.Checked;
        _groupList.Items[index] = CreateGroupLabel(_groups[index]);
        _groupList.SelectedIndex = index;
        _model = null;
    }

    private void ClearGroups()
    {
        _groups.Clear();
        _groupList.Items.Clear();
        _model = null;
        _waveform.SetData(Array.Empty<double>());
        _status.SetState(false, "Groups cleared");
    }

    private static double[] ReadCsv(string file)
    {
        return File.ReadAllLines(file)
            .SelectMany(line => line.Split(',', ';', '\t'))
            .Select(s => double.TryParse(s.Trim(), out var value) ? (double?)value : null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToArray();
    }

    private static string ShortId(GroupData group) => group.Id[..8];

    private static string CreateGroupLabel(GroupData group) =>
        $"{ShortId(group)} | {group.SampleCount} waveform(s) | {(group.IsDefective ? "DEFECT" : "NORMAL")}";
}
