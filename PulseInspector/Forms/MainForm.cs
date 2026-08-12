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
    private readonly FeatureDeviationGrid _deviations = new();
    private readonly StatusIndicator _status = new();
    private readonly FeatureExtractor _extractor = new();
    private readonly CsvWaveformLoader _csvLoader = new();
    private readonly CsvRowWaveformLoader _csvRowLoader = new();
    private readonly GroupInspectionService _groupService = new();
    private readonly SubgroupInspectionService _subgroupService = new();
    private readonly FeatureDeviationService _deviationService = new();
    private readonly InspectionSelectionService _selectionService = new();
    private readonly GroupDecisionService _decisionService = new();
    private readonly TrainingValidationService _trainingValidationService = new();
    private readonly List<GroupData> _groups = new();
    private readonly List<SubgroupInspectionResult> _subgroupResults = new();
    private readonly ListBox _groupList = new() { Dock = DockStyle.Fill };
    private readonly ListView _subgroupList = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, HideSelection = false };
    private readonly CheckBox _defective = new() { Text = "Selected group is defective", Dock = DockStyle.Top, Height = 28 };
    private readonly Label _subgroupHeader = new() { Text = "Subgroups", Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleLeft };
    private InspectionModel? _model;
    private GroupDecisionPolicy _decisionPolicy = new();
    private double _confidence = 0.999;
    private double _sampleIntervalSeconds = 2.56e-6 / 64.0;
    private bool _updatingDefective;

    public MainForm()
    {
        Text = "PulseInspector Release 1.0"; Width = 1500; Height = 900; StartPosition = FormStartPosition.CenterScreen;
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File");
        var addGroup = new ToolStripMenuItem("Add Group from CSV..."); addGroup.Click += (_, _) => AddGroupFromCsv(); file.DropDownItems.Add(addGroup);
        var addRowGroup = new ToolStripMenuItem("Add Group from CSV Rows..."); addRowGroup.Click += (_, _) => AddGroupFromCsvRows(); file.DropDownItems.Add(addRowGroup);
        var clear = new ToolStripMenuItem("Clear Groups"); clear.Click += (_, _) => ClearGroups(); file.DropDownItems.Add(clear); menu.Items.Add(file);
        var training = new ToolStripMenuItem("Training");
        var train = new ToolStripMenuItem("Train Normal Groups"); train.Click += (_, _) => TrainModel(); training.DropDownItems.Add(train);
        var inspect = new ToolStripMenuItem("Inspect Selected Group"); inspect.Click += (_, _) => InspectSelectedGroup(); training.DropDownItems.Add(inspect); menu.Items.Add(training);
        var settings = new ToolStripMenuItem("Settings"); settings.Click += (_, _) => EditSettings(); menu.Items.Add(settings);
        var about = new ToolStripMenuItem("About"); about.Click += (_, _) => new AboutForm().ShowDialog(this); menu.Items.Add(about);
        Controls.Add(menu); MainMenuStrip = menu;
        _groupList.SelectedIndexChanged += (_, _) => ShowSelectedGroup(); _subgroupList.SelectedIndexChanged += (_, _) => ShowSelectedSubgroup(); _defective.CheckedChanged += (_, _) => UpdateSelectedGroupLabel();
        _subgroupList.Columns.Add("#", 50); _subgroupList.Columns.Add("Source", 180); _subgroupList.Columns.Add("Mahalanobis", 110); _subgroupList.Columns.Add("Threshold", 110); _subgroupList.Columns.Add("Result", 90);
        var groupPanel = new Panel { Dock = DockStyle.Left, Width = 300, Padding = new Padding(8) }; groupPanel.Controls.Add(_groupList); groupPanel.Controls.Add(_defective);
        var center = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) }; var subgroupPanel = new Panel { Dock = DockStyle.Bottom, Height = 220 }; subgroupPanel.Controls.Add(_subgroupList); subgroupPanel.Controls.Add(_subgroupHeader);
        var analysisSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 400 }; var waveformFeatureSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 850 };
        waveformFeatureSplit.Panel1.Controls.Add(_waveform); waveformFeatureSplit.Panel2.Controls.Add(_features); analysisSplit.Panel1.Controls.Add(waveformFeatureSplit); analysisSplit.Panel2.Controls.Add(_deviations); center.Controls.Add(analysisSplit); center.Controls.Add(subgroupPanel); Controls.Add(center); Controls.Add(groupPanel);
        var status = new Panel { Dock = DockStyle.Bottom, Height = 34 }; status.Controls.Add(_status); Controls.Add(status);
    }

    private void EditSettings()
    {
        try
        {
            using var form = new SettingsForm(_decisionPolicy, _confidence, _sampleIntervalSeconds);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            var policy = form.Policy; policy.Validate(); _decisionPolicy = policy; _confidence = form.Confidence; _sampleIntervalSeconds = form.SampleIntervalSeconds;
            _model = null; _subgroupResults.Clear(); _subgroupList.Items.Clear(); _deviations.SetResults(Array.Empty<FeatureDeviation>());
            _status.SetState(true, $"Settings applied: rule={_decisionPolicy.Rule}, confidence={_confidence:G6}, rate={_decisionPolicy.DefectiveSubgroupRateThreshold:P2}, dt={_sampleIntervalSeconds:E3}s");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Invalid settings", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void AddGroupFromCsv()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Multiselect = true, Title = "Select waveforms belonging to one group" }; if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var group = new GroupData();
        try { foreach (var file in dialog.FileNames) { var data = _csvLoader.Load(file, new CsvImportOptions { SampleIntervalSeconds = _sampleIntervalSeconds }); group.AddWaveform(data.Samples, _extractor.Extract(data.Samples, data.SampleIntervalSeconds), data.SourceName, data.SampleIntervalSeconds, data.HasExplicitTimeAxis); } AddGroupToUi(group); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Group load error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void AddGroupFromCsvRows()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Title = "Select a CSV where each row is one subgroup" }; if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { var rows = _csvRowLoader.LoadRows(dialog.FileName, new CsvImportOptions { SampleIntervalSeconds = _sampleIntervalSeconds }); var group = new GroupData(); foreach (var data in rows) group.AddWaveform(data.Samples, _extractor.Extract(data.Samples, data.SampleIntervalSeconds), data.SourceName, data.SampleIntervalSeconds, data.HasExplicitTimeAxis); AddGroupToUi(group); _status.SetState(true, $"Loaded {rows.Count} subgroup row(s) from {Path.GetFileName(dialog.FileName)} | dt={_sampleIntervalSeconds:E3}s"); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Row-based CSV load error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void AddGroupToUi(GroupData group)
    {
        _groups.Add(group); _groupList.Items.Add(CreateGroupLabel(group)); _groupList.SelectedIndex = _groups.Count - 1; _model = null; _deviations.SetResults(Array.Empty<FeatureDeviation>());
        _status.SetState(true, $"Added Group {ShortId(group)}: {group.RecordCount} waveform(s), N={group.WaveformSampleCount}, dt={group.Records[0].SampleIntervalSeconds:E3}s");
    }

    private FeatureVector[] GetTrainingFeatures(GroupData[] normalGroups)
    {
        return normalGroups.Select(g =>
        {
            var features = g.MeanFeatures();
            return features ?? throw new InvalidOperationException($"Group '{g.Id}' contains no valid features.");
        }).ToArray();
    }

    private bool ValidateTrainingGroups(GroupData[] normalGroups, out TrainingValidationResult report)
    {
        var vectors = GetTrainingFeatures(normalGroups);
        report = _trainingValidationService.Validate(vectors);
        if (report.IsValid) return true;

        var lines = report.Issues.Select(i => $"• {(string.IsNullOrWhiteSpace(i.FeatureName) ? "Training" : i.FeatureName)}: {i.Message}");
        MessageBox.Show(this, string.Join(Environment.NewLine, lines), "Training data validation failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _status.SetState(false, $"Training validation failed: {report.Issues.Count} issue(s)");
        return false;
    }

    private void ShowTrainingWarnings(TrainingValidationResult report)
    {
        var warnings = report.Issues.Where(i => i.Code.StartsWith("WARN_", StringComparison.Ordinal)).ToArray();
        if (warnings.Length == 0) return;
        var message = string.Join(Environment.NewLine, warnings.Select(i => $"• {(string.IsNullOrWhiteSpace(i.FeatureName) ? "Training" : i.FeatureName)}: {i.Message}"));
        MessageBox.Show(this, message, "Training data warnings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void TrainModel()
    {
        var normalGroups = _groups.Where(g => !g.IsDefective).ToArray();
        var required = FeatureVector.StatisticalFeatureNames.Count + 1;
        if (normalGroups.Length < required) { MessageBox.Show(this, $"At least {required} normal groups are required for the six-feature covariance model.\nCurrent normal groups: {normalGroups.Length}", "Training data insufficient", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try
        {
            if (!ValidateTrainingGroups(normalGroups, out var report)) return;
            ShowTrainingWarnings(report);
            _model = _groupService.Train(normalGroups, _confidence);
            _status.SetState(true, $"Model trained: {normalGroups.Length} normal groups, confidence={_confidence:G6}, threshold={_model.Threshold:F6}");
        }
        catch (Exception ex) { _model = null; MessageBox.Show(this, ex.Message, "Training error", MessageBoxButtons.OK, MessageBoxIcon.Error); _status.SetState(false, "Training failed"); }
    }

    private void InspectSelectedGroup()
    {
        var index = _groupList.SelectedIndex; if (index < 0 || index >= _groups.Count) { MessageBox.Show(this, "Select a group first."); return; }
        var selected = _groups[index]; var trainingGroups = _groups.Where(g => !g.IsDefective && !ReferenceEquals(g, selected)).ToArray(); var required = FeatureVector.StatisticalFeatureNames.Count + 1;
        if (trainingGroups.Length < required) { MessageBox.Show(this, $"Inspection requires at least {required} normal training groups excluding the selected group.", "Training data insufficient", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try
        {
            if (!ValidateTrainingGroups(trainingGroups, out var report)) return;
            ShowTrainingWarnings(report);
            _model = _groupService.Train(trainingGroups, _confidence); var meanResult = _groupService.Inspect(selected, _model); _features.SetFeatures(meanResult.Features); PopulateSubgroupResults(selected); _deviations.SetResults(_deviationService.Analyze(meanResult.Features, _model));
            var finalDefect = _decisionService.IsDefect(_subgroupResults, _decisionPolicy); var defectCount = _subgroupResults.Count(r => r.IsDefect); var rate = _subgroupResults.Count == 0 ? 0 : defectCount / (double)_subgroupResults.Count; var maxMd = _subgroupResults.Count == 0 ? 0 : _subgroupResults.Max(r => r.MahalanobisDistance);
            _status.SetState(!finalDefect, $"Group {ShortId(selected)}: {(finalDefect ? "DEFECT" : "NORMAL")} | Rule={_decisionPolicy.Rule} | Defective={defectCount}/{_subgroupResults.Count} ({rate:P2}) | Max MD={maxMd:F6} | Mean MD={meanResult.MahalanobisDistance:F6} | Threshold={meanResult.Threshold:F6}");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Inspection error", MessageBoxButtons.OK, MessageBoxIcon.Error); _status.SetState(false, "Inspection failed"); }
    }

    private void PopulateSubgroupResults(GroupData group)
    {
        _subgroupResults.Clear(); _subgroupList.Items.Clear(); if (_model is null) return; _subgroupResults.AddRange(_subgroupService.Inspect(group, _model));
        foreach (var row in _selectionService.CreateRows(_subgroupResults)) { var item = new ListViewItem(row.Index.ToString()); item.SubItems.Add(row.SourceName); item.SubItems.Add(row.MahalanobisDistance.ToString("F6")); item.SubItems.Add(row.Threshold.ToString("F6")); item.SubItems.Add(row.IsDefect ? "DEFECT" : "NORMAL"); item.Tag = row.Index - 1; _subgroupList.Items.Add(item); }
    }

    private void ShowSelectedGroup()
    {
        var index = _groupList.SelectedIndex; if (index < 0 || index >= _groups.Count) return; var group = _groups[index]; var waveform = group.MeanWaveform(); if (waveform is not null) _waveform.SetData(waveform); var features = group.MeanFeatures(); if (features is not null) _features.SetFeatures(features); _deviations.SetResults(Array.Empty<FeatureDeviation>()); _updatingDefective = true; _defective.Checked = group.IsDefective; _updatingDefective = false; _subgroupResults.Clear(); _subgroupList.Items.Clear(); _status.SetState(true, $"Selected Group {ShortId(group)}: {group.RecordCount} waveform(s)");
    }

    private void ShowSelectedSubgroup()
    {
        if (_groupList.SelectedIndex < 0 || _subgroupList.SelectedIndices.Count == 0 || _model is null) return;
        var group = _groups[_groupList.SelectedIndex]; var row = _subgroupList.SelectedItems[0]; if (row.Tag is not int recordIndex) return;
        var selected = _selectionService.Select(group, recordIndex, _subgroupResults, _model);
        _waveform.SetData(selected.Record.Samples); _features.SetFeatures(selected.Record.Features); _deviations.SetResults(selected.Deviations);
        var result = selected.Result; _status.SetState(result is null || !result.IsDefect, result is null ? $"Subgroup {recordIndex + 1}: {selected.Record.SourceName}" : $"Subgroup {result.Index}: {(result.IsDefect ? "DEFECT" : "NORMAL")} | MD={result.MahalanobisDistance:F6}, Threshold={result.Threshold:F6}");
    }

    private void UpdateSelectedGroupLabel()
    {
        if (_updatingDefective) return; var index = _groupList.SelectedIndex; if (index < 0 || index >= _groups.Count) return; _groups[index].IsDefective = _defective.Checked; _groupList.Items[index] = CreateGroupLabel(_groups[index]); _groupList.SelectedIndex = index; _model = null; _subgroupResults.Clear(); _subgroupList.Items.Clear(); _deviations.SetResults(Array.Empty<FeatureDeviation>());
    }

    private void ClearGroups()
    {
        _groups.Clear(); _groupList.Items.Clear(); _subgroupResults.Clear(); _subgroupList.Items.Clear(); _model = null; _waveform.SetData(Array.Empty<double>()); _deviations.SetResults(Array.Empty<FeatureDeviation>()); _status.SetState(false, "Groups cleared");
    }

    private static string ShortId(GroupData group) => group.Id[..8];
    private static string CreateGroupLabel(GroupData group) => $"{ShortId(group)} | {group.RecordCount} waveform(s) | {(group.IsDefective ? "DEFECT" : "NORMAL")}";
}
