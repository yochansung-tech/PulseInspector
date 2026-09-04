using PulseInspector.Application.Contracts;
using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Application.Services;

public sealed class InspectionApplication : IInspectionApplication
{
    private readonly FeatureExtractor _featureExtractor;
    private readonly CsvWaveformLoader _csvLoader;
    private readonly CsvRowWaveformLoader _csvRowLoader;
    private readonly GroupInspectionService _groupService;
    private readonly SubgroupInspectionService _subgroupService;
    private readonly FeatureDeviationService _deviationService;
    private readonly TrainingValidationService _trainingValidationService;
    private readonly InspectionCsvExporter _inspectionCsvExporter;

    public InspectionApplication(
        FeatureExtractor? featureExtractor = null,
        CsvWaveformLoader? csvLoader = null,
        CsvRowWaveformLoader? csvRowLoader = null,
        GroupInspectionService? groupService = null,
        SubgroupInspectionService? subgroupService = null,
        FeatureDeviationService? deviationService = null,
        TrainingValidationService? trainingValidationService = null,
        InspectionCsvExporter? inspectionCsvExporter = null)
    {
        _featureExtractor = featureExtractor ?? new FeatureExtractor();
        _csvLoader = csvLoader ?? new CsvWaveformLoader();
        _csvRowLoader = csvRowLoader ?? new CsvRowWaveformLoader();
        _groupService = groupService ?? new GroupInspectionService();
        _subgroupService = subgroupService ?? new SubgroupInspectionService();
        _deviationService = deviationService ?? new FeatureDeviationService();
        _trainingValidationService = trainingValidationService ?? new TrainingValidationService();
        _inspectionCsvExporter = inspectionCsvExporter ?? new InspectionCsvExporter();
    }

    public FeatureVector ExtractFeatures(IReadOnlyList<double> samples, double sampleIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return _featureExtractor.Extract(samples, sampleIntervalSeconds);
    }

    public GroupData LoadGroup(IEnumerable<string> filePaths, double sampleIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ValidateInterval(sampleIntervalSeconds);
        var group = new GroupData();
        foreach (var filePath in filePaths)
        {
            var data = _csvLoader.Load(filePath, new CsvImportOptions { SampleIntervalSeconds = sampleIntervalSeconds });
            group.AddWaveform(data.Samples, _featureExtractor.Extract(data.Samples, data.SampleIntervalSeconds), data.SourceName, data.SampleIntervalSeconds, data.HasExplicitTimeAxis);
        }
        return group;
    }

    public IReadOnlyList<GroupData> LoadGroupsFromRows(IEnumerable<string> filePaths, double sampleIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ValidateInterval(sampleIntervalSeconds);
        var groups = new List<GroupData>();
        foreach (var filePath in filePaths)
        {
            var group = new GroupData();
            foreach (var data in _csvRowLoader.LoadRows(filePath, new CsvImportOptions { SampleIntervalSeconds = sampleIntervalSeconds }))
                group.AddWaveform(data.Samples, _featureExtractor.Extract(data.Samples, data.SampleIntervalSeconds), data.SourceName, data.SampleIntervalSeconds, data.HasExplicitTimeAxis);
            groups.Add(group);
        }
        return groups;
    }

    public TrainingValidationResult ValidateTraining(IEnumerable<GroupData> normalGroups)
    {
        ArgumentNullException.ThrowIfNull(normalGroups);
        var vectors = normalGroups.Select(g => g.MeanFeatures() ?? throw new InvalidOperationException($"Group '{g.Id}' contains no valid features."));
        return _trainingValidationService.Validate(vectors);
    }

    public InspectionModel Train(IEnumerable<GroupData> normalGroups, double confidence) => _groupService.Train(normalGroups, confidence);
    public GroupInspectionResult Inspect(GroupData group, InspectionModel model, GroupDecisionPolicy decisionPolicy) => _groupService.Inspect(group, model, decisionPolicy);
    public IReadOnlyList<SubgroupInspectionResult> InspectSubgroups(GroupData group, InspectionModel model) => _subgroupService.Inspect(group, model);
    public IReadOnlyList<FeatureDeviation> AnalyzeDeviations(FeatureVector vector, InspectionModel model) => _deviationService.Analyze(vector, model);
    public void ExportInspectionResult(string filePath, GroupInspectionResult result, IReadOnlyList<SubgroupInspectionResult>? subgroupResults = null) => _inspectionCsvExporter.ExportGroupResult(filePath, result, subgroupResults);

    private static void ValidateInterval(double value)
    {
        if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
    }
}
