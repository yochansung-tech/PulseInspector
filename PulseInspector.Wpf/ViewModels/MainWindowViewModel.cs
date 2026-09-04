using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PulseInspector.Application.Contracts;
using PulseInspector.Models;

namespace PulseInspector.Wpf.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IInspectionApplication _application;
    private readonly List<GroupData> _groupModels = new();
    private InspectionModel? _model;
    private GroupViewModel? _selectedGroup;
    private SubgroupResultViewModel? _selectedSubgroup;
    private string _statusText = "WPF migration foundation ready";
    private double _confidence = 0.999;
    private double _sampleIntervalSeconds = 2.56e-6 / 64.0;
    private IReadOnlyList<double> _waveform = Array.Empty<double>();

    public MainWindowViewModel(IInspectionApplication application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        AddGroupCommand = new RelayCommand(AddGroup, () => CanAddGroup);
        ClearGroupsCommand = new RelayCommand(ClearGroups, () => Groups.Count > 0);
        TrainCommand = new RelayCommand(Train, () => NormalGroupCount >= FeatureVector.StatisticalFeatureCount + 1);
        InspectCommand = new RelayCommand(Inspect, () => SelectedGroup is not null && NormalGroupCountExcludingSelection >= FeatureVector.StatisticalFeatureCount + 1);
    }

    public ObservableCollection<GroupViewModel> Groups { get; } = new();
    public ObservableCollection<FeatureValueViewModel> Features { get; } = new();
    public ObservableCollection<SubgroupResultViewModel> Subgroups { get; } = new();

    public RelayCommand AddGroupCommand { get; }
    public RelayCommand ClearGroupsCommand { get; }
    public RelayCommand TrainCommand { get; }
    public RelayCommand InspectCommand { get; }

    public GroupViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (ReferenceEquals(_selectedGroup, value)) return;
            _selectedGroup = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedGroupIsDefective));
            ShowSelectedGroup();
            RaiseCommandStates();
        }
    }

    public bool SelectedGroupIsDefective
    {
        get => SelectedGroup?.IsDefective ?? false;
        set => SetSelectedGroupDefective(value);
    }

    public SubgroupResultViewModel? SelectedSubgroup
    {
        get => _selectedSubgroup;
        set
        {
            if (ReferenceEquals(_selectedSubgroup, value)) return;
            _selectedSubgroup = value;
            OnPropertyChanged();
            ShowSelectedSubgroup();
        }
    }

    public IReadOnlyList<double> Waveform
    {
        get => _waveform;
        private set
        {
            _waveform = value;
            OnPropertyChanged();
            WaveformChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value) return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public bool CanAddGroup => true;
    public int NormalGroupCount => _groupModels.Count(g => !g.IsDefective);
    private int NormalGroupCountExcludingSelection => _groupModels.Count(g => !g.IsDefective && !ReferenceEquals(g, SelectedGroup?.Model));

    public double Confidence
    {
        get => _confidence;
        set { if (Math.Abs(_confidence - value) < double.Epsilon) return; _confidence = value; OnPropertyChanged(); }
    }

    public double SampleIntervalSeconds
    {
        get => _sampleIntervalSeconds;
        set { if (Math.Abs(_sampleIntervalSeconds - value) < double.Epsilon) return; _sampleIntervalSeconds = value; OnPropertyChanged(); }
    }

    public event EventHandler? WaveformChanged;

    public void AddGroup(IEnumerable<string> filePaths)
    {
        try
        {
            var group = _application.LoadGroup(filePaths, SampleIntervalSeconds);
            _groupModels.Add(group);
            var vm = new GroupViewModel(group);
            Groups.Add(vm);
            SelectedGroup = vm;
            SetStatus($"Added Group {group.Id[..Math.Min(8, group.Id.Length)]}: {group.RecordCount} waveform(s)");
            RaiseCommandStates();
        }
        catch (Exception ex) { SetStatus($"Group load failed: {ex.Message}"); }
    }

    private void AddGroup() => SetStatus("Use the file picker to add a group.");

    private void ClearGroups()
    {
        _groupModels.Clear();
        Groups.Clear();
        Features.Clear();
        Subgroups.Clear();
        _model = null;
        _selectedGroup = null;
        _selectedSubgroup = null;
        Waveform = Array.Empty<double>();
        OnPropertyChanged(nameof(SelectedGroup));
        OnPropertyChanged(nameof(SelectedSubgroup));
        OnPropertyChanged(nameof(SelectedGroupIsDefective));
        SetStatus("Groups cleared");
        RaiseCommandStates();
    }

    private void ShowSelectedGroup()
    {
        Features.Clear();
        Subgroups.Clear();
        _selectedSubgroup = null;
        OnPropertyChanged(nameof(SelectedSubgroup));
        _model = null;
        if (SelectedGroup is null)
        {
            Waveform = Array.Empty<double>();
            return;
        }

        var group = SelectedGroup.Model;
        var waveform = group.MeanWaveform();
        if (waveform is not null) Waveform = waveform;
        var features = group.MeanFeatures();
        if (features is not null) SetFeatures(features);
        SetStatus($"Selected Group {group.Id[..Math.Min(8, group.Id.Length)]}: {group.RecordCount} waveform(s)");
    }

    private void Train()
    {
        try
        {
            var normalGroups = _groupModels.Where(g => !g.IsDefective).ToArray();
            _model = _application.Train(normalGroups, Confidence);
            SetStatus($"Model trained: {normalGroups.Length} normal groups, threshold={_model.Threshold:F6}");
        }
        catch (Exception ex) { _model = null; SetStatus($"Training failed: {ex.Message}"); }
    }

    private void Inspect()
    {
        if (SelectedGroup is null) return;
        try
        {
            var trainingGroups = _groupModels.Where(g => !g.IsDefective && !ReferenceEquals(g, SelectedGroup.Model)).ToArray();
            _model = _application.Train(trainingGroups, Confidence);
            var result = _application.Inspect(SelectedGroup.Model, _model, new GroupDecisionPolicy());
            SetFeatures(result.Features);
            PopulateSubgroups(SelectedGroup.Model);
            SetStatus($"Group {result.GroupId[..Math.Min(8, result.GroupId.Length)]}: {(result.IsDefect ? "DEFECT" : "NORMAL")} | MD={result.MahalanobisDistance:F6} | Threshold={result.Threshold:F6}");
        }
        catch (Exception ex) { SetStatus($"Inspection failed: {ex.Message}"); }
    }

    private void PopulateSubgroups(GroupData group)
    {
        Subgroups.Clear();
        _selectedSubgroup = null;
        OnPropertyChanged(nameof(SelectedSubgroup));
        if (_model is null) return;
        foreach (var row in _application.InspectSubgroups(group, _model))
            Subgroups.Add(new SubgroupResultViewModel(row.Index, row.SourceName, row.MahalanobisDistance, row.Threshold, row.IsDefect));
    }

    private void ShowSelectedSubgroup()
    {
        if (SelectedGroup is null || SelectedSubgroup is null) return;
        var recordIndex = SelectedSubgroup.Index - 1;
        if (recordIndex < 0 || recordIndex >= SelectedGroup.Model.Records.Count) return;
        var record = SelectedGroup.Model.Records[recordIndex];
        Waveform = record.Samples;
        SetFeatures(record.Features);
        SetStatus($"Selected subgroup #{SelectedSubgroup.Index}: {SelectedSubgroup.SourceName} | {(SelectedSubgroup.IsDefect ? "DEFECT" : "NORMAL")}");
    }

    public void SetSelectedGroupDefective(bool value)
    {
        if (SelectedGroup is null) return;
        SelectedGroup.IsDefective = value;
        OnPropertyChanged(nameof(SelectedGroupIsDefective));
        OnPropertyChanged(nameof(NormalGroupCount));
        RaiseCommandStates();
    }

    private void SetFeatures(FeatureVector vector)
    {
        Features.Clear();
        foreach (var name in FeatureVector.FeatureNames)
            Features.Add(new FeatureValueViewModel(name, vector[name]));
    }

    public void SetStatus(string message) => StatusText = message ?? string.Empty;

    private void RaiseCommandStates()
    {
        ClearGroupsCommand.RaiseCanExecuteChanged();
        TrainCommand.RaiseCanExecuteChanged();
        InspectCommand.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
