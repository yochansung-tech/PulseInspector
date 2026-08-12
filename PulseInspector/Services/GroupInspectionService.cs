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

    public InspectionModel Train(IEnumerable<GroupData> normalGroups, double confidence = 0.999)
    {
        ArgumentNullException.ThrowIfNull(normalGroups);

        var groupFeatures = normalGroups
            .Select(GetGroupFeatures)
            .ToArray();

        var minimumGroups = FeatureVector.StatisticalFeatureNames.Count + 1;
        if (groupFeatures.Length < minimumGroups)
            throw new InvalidOperationException(
                $"At least {minimumGroups} normal groups are required for the {FeatureVector.StatisticalFeatureNames.Count}-feature covariance model.");

        return _inspectionService.Train(groupFeatures, confidence);
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
