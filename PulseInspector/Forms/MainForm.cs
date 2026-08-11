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
    private readonly CsvWaveformLoader _csvLoader = new();
    private readonly CsvRowWaveformLoader _csvRowLoader = new();
    private readonly GroupInspectionService _groupService = new();
    private readonly SubgroupInspectionService _subgroupService = new();
    private readonly List<GroupData> _groups = new();
    private readonly List<SubgroupInspectionResult> _subgroupResults = new();
    private readonly ListBox _groupList = new() { Dock = DockStyle.Fill };
    private readonly ListView _subgroupList = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        GridLines = true,
        HideSelection = false
    };
    private readonly CheckBox _defective = new() { Text = "Selected group is defective", Dock = DockStyle.Top, Height = 28 };
    private readonly Label _subgroupHeader = new()
    {
        Text = "Subgroups",
        Dock = DockStyle.Top,
        Height = 28,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private InspectionModel? _model;
    private bool _updatingDefective;

    public MainForm()
    {
        Text = "PulseInspector Release 1.0";
        Width = 1500;
        Height = 850;
        StartPosition = FormStartPosition.CenterScreen;

        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File");
        var addGroup = new ToolStripMenuItem("Add Group from CSV...");
        addGroup.Click += (_, _) => AddGroupFromCsv();
        file.DropDownItems.Add(addGroup);

        var addRowGroup = new ToolStripMenuItem("Add Group from CSV Rows...");
        addRowGroup.Click += (_, _) => AddGroupFromCsvRows();
        file.DropDownItems.Add(addRowGroup);

        var clear = new ToolStripMenuItem("Clear Groups");
        clear.Click += (_, _) => ClearGroups();
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
        _subgroupList.SelectedIndexChanged += (_, _) => ShowSelectedSubgroup();
        _defective.CheckedChanged += (_, _) => UpdateSelectedGroupLabel();

        _subgroupList.Columns.Add("#", 50);
        _subgroupList.Columns.Add("Source", 180);
        _subgroupList.Columns.Add("Mahalanobis", 110);
        _subgroupList.Columns.Add("Threshold", 110);
        _subgroupList.Columns.Add("Result", 90);

        var groupPanel = new Panel { Dock = DockStyle.Left, Width = 300, Padding = new Padding(8) };
        groupPanel.Controls.Add(_groupList);
        groupPanel.Controls.Add(_defective);

        var center = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
        var subgroupPanel = new Panel { Dock = DockStyle.Bottom, Height = 220 };
        subgroupPanel.Controls.Add(_subgroupList);
        subgroupPanel.Controls.Add(_subgroupHeader);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 850
        };
        split.Panel1.Controls.Add(_waveform);
        split.Panel2.Controls.Add(_features);
        center.Controls.Add(split);
        center.Controls.Add(subgroupPanel);

        Controls.Add(center);
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
                var data = _csvLoader.Load(file);
                var features = _extractor.Extract(data.Samples, data.SampleIntervalSeconds);
                group.AddWaveform(data.Samples, features, data.SourceName,
                    data.SampleIntervalSeconds, data.HasExplicitTimeAxis);
            }
            AddGroupToUi(group);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Group load error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddGroupFromCsvRows()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Multiselect = false,
            Title = "Select a CSV where each row is one subgroup"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var rows = _csvRowLoader.LoadRows(dialog.FileName);
            var group = new GroupData();
            foreach (var data in rows)
            {
                var features = _extractor.Extract(data.Samples, data.SampleIntervalSeconds);
                group.AddWaveform(data.Samples, features, data.SourceName,
                    data.SampleIntervalSeconds, data.HasExplicitTimeAxis);
            }
            AddGroupToUi(group);
            _status.SetState(true, $"Loaded {rows.Count} subgroup row(s) from {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Row-based CSV load error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddGroupToUi(GroupData group)
    {
        _groups.Add(group);
        _groupList.Items.Add(CreateGroupLabel(group));
        _groupList.SelectedIndex = _groups.Count - 1;
        _model = null;
        _status.SetState(true,
            $"Added Group {ShortId(group)}: {group.SampleCount} waveform(s), " +
            $"N={group.Records[0].SampleCount}, dt={group.Records[0].SampleIntervalSeconds:E3}s");
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
                "Training data insufficient", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _model = _groupService.Train(normalGroups);
            _status.SetState(true, $"Model trained: {normalGroups.Length} normal groups, threshold={_model.Threshold:F6}");
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
        var trainingGroups = _groups.Where(g => !g.IsDefective && !ReferenceEquals(g, selected)).ToArray();
        var required = FeatureVector.StatisticalFeatureNames.Count + 1;
        if (trainingGroups.Length < required)
        {
            MessageBox.Show(this,
                $"Inspection requires at least {required} normal training groups excluding the selected group.",
                "Training data insufficient", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _model = _groupService.Train(trainingGroups);
            var result = _groupService.Inspect(selected, _model);
            _features.SetFeatures(result.Features);
            PopulateSubgroupResults(selected);
            _status.SetState(!result.IsDefect,
                $"Group {ShortId(selected)}: {(result.IsDefect ? "DEFECT" : "NORMAL")} | " +
                $"MD={result.MahalanobisDistance:F6}, Threshold={result.Threshold:F6} | " +
                $"Subgroups={_subgroupResults.Count(r => r.IsDefect)}/{_subgroupResults.Count} defective");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Inspection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.SetState(false, "Inspection failed");
        }
    }

    private void PopulateSubgroupResults(GroupData group)
    {
        _subgroupResults.Clear();
        _subgroupList.Items.Clear();
        if (_model is null) return;

        _subgroupResults.AddRange(_subgroupService.Inspect(group, _model));
        foreach (var result in _subgroupResults)
        {
            var item = new ListViewItem(result.Index.ToString());
            item.SubItems.Add(result.SourceName);
            item.SubItems.Add(result.MahalanobisDistance.ToString("F6"));
            item.SubItems.Add(result.Threshold.ToString("F6"));
            item.SubItems.Add(result.IsDefect ? "DEFECT" : "NORMAL");
            item.Tag = result.Index - 1;
            _subgroupList.Items.Add(item);
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
        _subgroupResults.Clear();
        _subgroupList.Items.Clear();
        _status.SetState(true, $"Selected Group {ShortId(group)}: {group.SampleCount} waveform(s)");
    }

    private void ShowSelectedSubgroup()
    {
        if (_groupList.SelectedIndex < 0 || _subgroupList.SelectedIndices.Count == 0) return;
        var group = _groups[_groupList.SelectedIndex];
        var row = _subgroupList.SelectedItems[0];
        if (row.Tag is not int recordIndex || recordIndex < 0 || recordIndex >= group.Records.Count) return;

        var record = group.Records[recordIndex];
        _waveform.SetData(record.Samples);
        _features.SetFeatures(record.Features);

        var result = _subgroupResults.FirstOrDefault(r => r.Index == recordIndex + 1);
        if (result is not null)
        {
            _status.SetState(!result.IsDefect,
                $"Subgroup {result.Index}: {(result.IsDefect ? "DEFECT" : "NORMAL")} | " +
                $"MD={result.MahalanobisDistance:F6}, Threshold={result.Threshold:F6}");
        }
        else
        {
            _status.SetState(true, $"Subgroup {recordIndex + 1}: {record.SourceName}");
        }
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
        _subgroupResults.Clear();
        _subgroupList.Items.Clear();
    }

    private void ClearGroups()
    {
        _groups.Clear();
        _groupList.Items.Clear();
        _subgroupResults.Clear();
        _subgroupList.Items.Clear();
        _model = null;
        _waveform.SetData(Array.Empty<double>());
        _status.SetState(false, "Groups cleared");
    }

    private static string ShortId(GroupData group) => group.Id[..8];
    private static string CreateGroupLabel(GroupData group) =>
        $"{ShortId(group)} | {group.SampleCount} waveform(s) | {(group.IsDefective ? "DEFECT" : "NORMAL")}";
}
