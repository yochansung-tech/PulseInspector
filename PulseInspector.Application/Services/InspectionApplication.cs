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

    public InspectionApplication(
        FeatureExtractor? featureExtractor = null,
        CsvWaveformLoader? csvLoader = null,
        CsvRowWaveformLoader? csvRowLoader = null,
        GroupInspectionService? groupService = null,
        SubgroupInspectionService? subgroupService = null)
    {
        _featureExtractor = featureExtractor ?? new FeatureExtractor();
        _csvLoader = csvLoader ?? new CsvWaveformLoader();
        _csvRowLoader = csvRowLoader ?? new CsvRowWaveformLoader();
        _groupService = groupService ?? new GroupInspectionService();
        _subgroupService = subgroupService ?? new SubgroupInspectionService();
    }

    public FeatureVector ExtractFeatures(IReadOnlyList<double> samples, double sampleIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return _featureExtractor.Extract(samples, sampleIntervalSeconds);
    }

    public GroupData LoadGroup(IEnumerable<string> filePaths, double sampleIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        if (sampleIntervalSeconds <= 0 || !double.IsFinite(sampleIntervalSeconds))
            throw new ArgumentOutOfRangeException(nameof(sampleIntervalSeconds));

        var group = new GroupData();
        foreach (var filePath in filePaths)
        {
            var data = _csvLoader.Load(filePath, new CsvImportOptions
            {
                SampleIntervalSeconds = sampleIntervalSeconds
            });
            group.AddWaveform(
                data.Samples,
                _featureExtractor.Extract(data.Samples, data.SampleIntervalSeconds),
                data.SourceName,
                data.SampleIntervalSeconds,
                data.HasExplicitTimeAxis);
        }
        return group;
    }

    public IReadOnlyList<GroupData> LoadGroupsFromRows(IEnumerable<string> filePaths, double sampleIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        if (sampleIntervalSeconds <= 0 || !double.IsFinite(sampleIntervalSeconds))
            throw new ArgumentOutOfRangeException(nameof(sampleIntervalSeconds));

        var groups = new List<GroupData>();
        foreach (var filePath in filePaths)
        {
            var group = new GroupData();
            var rows = _csvRowLoader.LoadRows(filePath, new CsvImportOptions
            {
                SampleIntervalSeconds = sampleIntervalSeconds
            });
            foreach (var data in rows)
            {
                group.AddWaveform(
                    data.Samples,
                    _featureExtractor.Extract(data.Samples, data.SampleIntervalSeconds),
                    data.SourceName,
                    data.SampleIntervalSeconds,
                    data.HasExplicitTimeAxis);
            }
            groups.Add(group);
        }
        return groups;
    }

    public InspectionModel Train(IEnumerable<GroupData> normalGroups, double confidence)
    {
        ArgumentNullException.ThrowIfNull(normalGroups);
        return _groupService.Train(normalGroups, confidence);
    }

    public GroupInspectionResult Inspect(GroupData group, InspectionModel model, GroupDecisionPolicy decisionPolicy)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(decisionPolicy);
        return _groupService.Inspect(group, model, decisionPolicy);
    }

    public IReadOnlyList<SubgroupInspectionResult> InspectSubgroups(GroupData group, InspectionModel model)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(model);
        return _subgroupService.Inspect(group, model);
    }
}
