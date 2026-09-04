using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PulseInspector.Application.Contracts;
using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Wpf.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IInspectionApplication _application;
    private readonly List<GroupData> _groupModels = new();
    private InspectionModel? _model;
    private GroupInspectionResult? _lastInspectionResult;
    private IReadOnlyList<SubgroupInspectionResult> _lastSubgroupResults = Array.Empty<SubgroupInspectionResult>();
    private GroupViewModel? _selectedGroup;
    private SubgroupResultViewModel? _selectedSubgroup;
    private string _statusText = "Ready";
    private double _confidence = 0.999;
    private double _sampleIntervalSeconds = 2.56e-6 / 64.0;
    private IReadOnlyList<double> _waveform = Array.Empty<double>();
    private IReadOnlyList<double> _mahalanobisValues = Array.Empty<double>();
    private IReadOnlyList<ChartPointViewModel> _mahalanobisPoints = Array.Empty<ChartPointViewModel>();
    private GroupDecisionPolicy _decisionPolicy = new();

    public MainWindowViewModel(IInspectionApplication application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        AddGroupCommand = new RelayCommand(AddGroupPlaceholder);
        AddRowsCommand = new RelayCommand(AddRowsPlaceholder);
        ClearGroupsCommand = new RelayCommand(ClearGroups, () => Groups.Count > 0);
        TrainCommand = new RelayCommand(TrainModel, () => NormalGroupCount >= RequiredTrainingGroups);
        InspectCommand = new RelayCommand(Inspect, () => SelectedGroup is not null && NormalGroupCountExcludingSelection >= RequiredTrainingGroups);
        ExportCommand = new RelayCommand(() => ExportRequested?.Invoke(this, EventArgs.Empty), () => _lastInspectionResult is not null);
    }

    private static int RequiredTrainingGroups => FeatureVector.StatisticalFeatureCount + 1;
    public ObservableCollection<GroupViewModel> Groups { get; } = new();
    public ObservableCollection<FeatureValueViewModel> Features { get; } = new();
    public ObservableCollection<FeatureDeviationViewModel> Deviations { get; } = new();
    public ObservableCollection<SubgroupResultViewModel> Subgroups { get; } = new();
    public RelayCommand AddGroupCommand { get; }
    public RelayCommand AddRowsCommand { get; }
    public RelayCommand ClearGroupsCommand { get; }
    public RelayCommand TrainCommand { get; }
    public RelayCommand InspectCommand { get; }
    public RelayCommand ExportCommand { get; }

    public GroupViewModel? SelectedGroup { get => _selectedGroup; set { if (ReferenceEquals(_selectedGroup, value)) return; _selectedGroup = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedGroupIsDefective)); ShowSelectedGroup(); RaiseCommandStates(); } }
    public SubgroupResultViewModel? SelectedSubgroup { get => _selectedSubgroup; set { if (ReferenceEquals(_selectedSubgroup, value)) return; _selectedSubgroup = value; OnPropertyChanged(); ShowSelectedSubgroup(); } }
    public bool SelectedGroupIsDefective { get => SelectedGroup?.IsDefective ?? false; set => SetSelectedGroupDefective(value); }
    public IReadOnlyList<double> Waveform { get => _waveform; private set { _waveform = value; OnPropertyChanged(); WaveformChanged?.Invoke(this, EventArgs.Empty); } }
    public IReadOnlyList<double> MahalanobisValues { get => _mahalanobisValues; private set { _mahalanobisValues = value; OnPropertyChanged(); } }
    public IReadOnlyList<ChartPointViewModel> MahalanobisPoints { get => _mahalanobisPoints; private set { _mahalanobisPoints = value; OnPropertyChanged(); } }
    public string StatusText { get => _statusText; private set { if (_statusText == value) return; _statusText = value; OnPropertyChanged(); } }
    public int NormalGroupCount => _groupModels.Count(g => !g.IsDefective);
    public int RequiredTrainingGroupCount => RequiredTrainingGroups;
    public double Confidence { get => _confidence; set { if (_confidence == value) return; _confidence = value; OnPropertyChanged(); } }
    public double SampleIntervalSeconds { get => _sampleIntervalSeconds; set { if (_sampleIntervalSeconds == value) return; _sampleIntervalSeconds = value; OnPropertyChanged(); } }
    public GroupDecisionPolicy DecisionPolicy => _decisionPolicy;
    private int NormalGroupCountExcludingSelection => _groupModels.Count(g => !g.IsDefective && !ReferenceEquals(g, SelectedGroup?.Model));
    public event EventHandler? WaveformChanged;
    public event EventHandler? OpenFilesRequested;
    public event EventHandler? OpenRowsRequested;
    public event EventHandler? ExportRequested;

    public void AddGroup(IEnumerable<string> filePaths)
    {
        try { AddGroupModel(_application.LoadGroup(filePaths, SampleIntervalSeconds)); SetStatus($"Added Group {ShortId(SelectedGroup!.Model)}: {SelectedGroup.RecordCount} waveform(s)"); }
        catch (Exception ex) { SetStatus($"Group load failed: {ex.Message}"); }
    }

    public void AddGroupsFromRows(IEnumerable<string> filePaths)
    {
        try { foreach (var group in _application.LoadGroupsFromRows(filePaths, SampleIntervalSeconds)) AddGroupModel(group); SetStatus($"Loaded groups from row-based CSV: {Groups.Count} group(s)"); }
        catch (Exception ex) { SetStatus($"Row CSV load failed: {ex.Message}"); }
    }

    public TrainingValidationResult ValidateTraining()
    {
        var normal = _groupModels.Where(g => !g.IsDefective).ToArray();
        return _application.ValidateTraining(normal);
    }

    public void ExportInspection(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (_lastInspectionResult is null) throw new InvalidOperationException("No inspection result is available to export.");
        _application.ExportInspectionResult(filePath, _lastInspectionResult, _lastSubgroupResults);
        SetStatus($"Inspection result exported: {filePath}");
    }

    public void ApplySettings(GroupDecisionPolicy policy, double confidence, double sampleIntervalSeconds)
    {
        policy.Validate();
        if (!double.IsFinite(confidence) || confidence <= 0 || confidence >= 1) throw new ArgumentOutOfRangeException(nameof(confidence));
        if (!double.IsFinite(sampleIntervalSeconds) || sampleIntervalSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(sampleIntervalSeconds));
        _decisionPolicy = policy; Confidence = confidence; SampleIntervalSeconds = sampleIntervalSeconds; _model = null; ClearInspectionResults();
        SetStatus($"Settings applied: rule={policy.Rule}, confidence={confidence:G6}, dt={sampleIntervalSeconds:E3}s"); RaiseCommandStates();
    }

    private void AddGroupModel(GroupData group) { _groupModels.Add(group); var vm = new GroupViewModel(group); Groups.Add(vm); OnPropertyChanged(nameof(NormalGroupCount)); SelectedGroup = vm; RaiseCommandStates(); }
    private void AddGroupPlaceholder() => OpenFilesRequested?.Invoke(this, EventArgs.Empty);
    private void AddRowsPlaceholder() => OpenRowsRequested?.Invoke(this, EventArgs.Empty);

    private void ClearGroups()
    {
        _groupModels.Clear(); Groups.Clear(); Features.Clear(); ClearInspectionResults(); _selectedGroup = null; _selectedSubgroup = null; Waveform = Array.Empty<double>();
        OnPropertyChanged(nameof(SelectedGroup)); OnPropertyChanged(nameof(SelectedSubgroup)); OnPropertyChanged(nameof(SelectedGroupIsDefective)); OnPropertyChanged(nameof(NormalGroupCount)); SetStatus("Groups cleared"); RaiseCommandStates();
    }

    private void ShowSelectedGroup()
    {
        Features.Clear(); ClearInspectionResults(); _selectedSubgroup = null; OnPropertyChanged(nameof(SelectedSubgroup)); _model = null;
        if (SelectedGroup is null) { Waveform = Array.Empty<double>(); return; }
        var group = SelectedGroup.Model; Waveform = group.MeanWaveform() ?? Array.Empty<double>(); var features = group.MeanFeatures(); if (features is not null) SetFeatures(features); SetStatus($"Selected Group {ShortId(group)}: {group.RecordCount} waveform(s)");
    }

    private void ShowSelectedSubgroup()
    {
        if (SelectedGroup is null || SelectedSubgroup is null) return;
        var recordIndex = SelectedSubgroup.Index - 1; if (recordIndex < 0 || recordIndex >= SelectedGroup.Model.Records.Count) return;
        var record = SelectedGroup.Model.Records[recordIndex]; Waveform = record.Samples; SetFeatures(record.Features); SetStatus($"Selected subgroup #{SelectedSubgroup.Index}: {SelectedSubgroup.SourceName}");
    }

    public void TrainModel()
    {
        try { var normal = _groupModels.Where(g => !g.IsDefective).ToArray(); var validation = _application.ValidateTraining(normal); if (!validation.IsValid) { SetStatus($"Training validation failed: {validation.Issues.First(i => i.Code.StartsWith("ERROR_", StringComparison.Ordinal)).Message}"); return; } _model = _application.Train(normal, Confidence); SetStatus($"Model trained: {normal.Length} normal groups, threshold={_model.Threshold:F6}"); }
        catch (Exception ex) { _model = null; SetStatus($"Training failed: {ex.Message}"); }
    }

    private void Inspect()
    {
        if (SelectedGroup is null) return;
        try
        {
            var training = _groupModels.Where(g => !g.IsDefective && !ReferenceEquals(g, SelectedGroup.Model)).ToArray();
            var validation = _application.ValidateTraining(training);
            if (!validation.IsValid) { SetStatus($"Inspection training validation failed: {validation.Issues.First(i => i.Code.StartsWith("ERROR_", StringComparison.Ordinal)).Message}"); return; }
            _model = _application.Train(training, Confidence);
            var result = _application.Inspect(SelectedGroup.Model, _model, _decisionPolicy);
            _lastInspectionResult = result;
            SetFeatures(result.Features);
            Deviations.Clear();
            foreach (var d in _application.AnalyzeDeviations(result.Features, _model)) Deviations.Add(new FeatureDeviationViewModel(d.FeatureName, d.Value, d.Mean, d.StandardDeviation, d.ZScore, d.AbsoluteZScore, d.MahalanobisContribution));
            var subgroupResults = _application.InspectSubgroups(SelectedGroup.Model, _model);
            _lastSubgroupResults = subgroupResults;
            Subgroups.Clear();
            foreach (var row in subgroupResults) Subgroups.Add(new SubgroupResultViewModel(row.Index, row.SourceName, row.MahalanobisDistance, row.Threshold, row.IsDefect));
            MahalanobisValues = Subgroups.Select(s => s.MahalanobisDistance).ToArray();
            MahalanobisPoints = Subgroups.Select(s => new ChartPointViewModel(s.Index, s.MahalanobisDistance)).ToArray();
            SetStatus($"Group {ShortId(SelectedGroup.Model)}: {(result.IsDefect ? "DEFECT" : "NORMAL")} | MD={result.MahalanobisDistance:F6} | Threshold={result.Threshold:F6}");
            RaiseCommandStates();
        }
        catch (Exception ex) { _model = null; _lastInspectionResult = null; _lastSubgroupResults = Array.Empty<SubgroupInspectionResult>(); RaiseCommandStates(); SetStatus($"Inspection failed: {ex.Message}"); }
    }

    public void SetSelectedGroupDefective(bool value) { if (SelectedGroup is null) return; SelectedGroup.IsDefective = value; OnPropertyChanged(nameof(SelectedGroupIsDefective)); OnPropertyChanged(nameof(NormalGroupCount)); RaiseCommandStates(); }
    private void SetFeatures(FeatureVector vector) { Features.Clear(); foreach (var name in FeatureVector.FeatureNames) Features.Add(new FeatureValueViewModel(name, vector[name])); }
    private void ClearInspectionResults() { _lastInspectionResult = null; _lastSubgroupResults = Array.Empty<SubgroupInspectionResult>(); Deviations.Clear(); Subgroups.Clear(); MahalanobisValues = Array.Empty<double>(); MahalanobisPoints = Array.Empty<ChartPointViewModel>(); RaiseCommandStates(); }
    public void SetStatus(string message) => StatusText = message ?? string.Empty;
    private static string ShortId(GroupData group) => group.Id[..Math.Min(8, group.Id.Length)];
    private void RaiseCommandStates() { ClearGroupsCommand.RaiseCanExecuteChanged(); TrainCommand.RaiseCanExecuteChanged(); InspectCommand.RaiseCanExecuteChanged(); ExportCommand.RaiseCanExecuteChanged(); }
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public event PropertyChangedEventHandler? PropertyChanged;
}
