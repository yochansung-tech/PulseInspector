using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class GroupInspectionService
{
    private readonly InspectionService _inspectionService;
    private readonly SubgroupInspectionService _subgroupInspectionService;
    private readonly GroupDecisionService _decisionService;

    public GroupInspectionService(
        InspectionService? inspectionService = null,
        SubgroupInspectionService? subgroupInspectionService = null,
        GroupDecisionService? decisionService = null)
    {
        _inspectionService = inspectionService ?? new InspectionService();
        _subgroupInspectionService = subgroupInspectionService ?? new SubgroupInspectionService(_inspectionService);
        _decisionService = decisionService ?? new GroupDecisionService();
    }

    /// <summary>
    /// Trains from every valid normal subgroup in every supplied training group.
    /// The previous implementation reduced each CSV group to one mean vector,
    /// which discarded the real multimodal waveform distribution. The new
    /// implementation preserves every subgroup and lets InspectionService learn
    /// the two known-normal waveform modes from the complete population.
    /// </summary>
    public InspectionModel Train(IEnumerable<GroupData> normalGroups, double confidence = 0.999)
    {
        ArgumentNullException.ThrowIfNull(normalGroups);

        var trainingFeatures = normalGroups
            .SelectMany(group => group.Records)
            .Select(record => record.Features)
            .Where(features => features is not null)
            .Select(features => features!.Clone())
            .ToArray();

        var minimumSamples = FeatureVector.StatisticalFeatureCount + 1;
        if (trainingFeatures.Length < minimumSamples)
            throw new InvalidOperationException(
                $"At least {minimumSamples} normal subgroup samples are required for the {FeatureVector.StatisticalFeatureNames.Count}-feature covariance model.");

        return _inspectionService.Train(trainingFeatures, confidence, normalModeCount: 2);
    }

    public GroupInspectionResult Inspect(
        GroupData group,
        InspectionModel model,
        GroupDecisionPolicy? decisionPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(model);

        var features = GetGroupFeatures(group);
        var meanResult = _inspectionService.Inspect(features, model);
        var subgroupResults = _subgroupInspectionService.Inspect(group, model);

        return _decisionService.CreateResult(group, meanResult, subgroupResults, decisionPolicy);
    }

    private static FeatureVector GetGroupFeatures(GroupData group)
    {
        if (group.Records.Count == 0)
            throw new InvalidOperationException($"Group '{group.Id}' contains no records.");

        var mean = group.MeanFeatures();
        return mean ?? throw new InvalidOperationException($"Group '{group.Id}' contains no valid features.");
    }
}
