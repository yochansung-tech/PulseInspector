using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class GroupInspectionService
{
    private readonly InspectionService _inspectionService;

    public GroupInspectionService(InspectionService? inspectionService = null)
    {
        _inspectionService = inspectionService ?? new InspectionService();
    }

    public InspectionModel Train(IEnumerable<GroupData> normalGroups, double confidence = 0.999)
    {
        var groupFeatures = normalGroups
            .Select(GetGroupFeatures)
            .ToArray();

        if (groupFeatures.Length < 2)
            throw new InvalidOperationException("At least two normal groups are required for training.");

        return _inspectionService.Train(groupFeatures, confidence);
    }

    public GroupInspectionResult Inspect(GroupData group, InspectionModel model)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(model);

        var features = GetGroupFeatures(group);
        var result = _inspectionService.Inspect(features, model);

        return new GroupInspectionResult(
            group.Id,
            result.IsDefect,
            group.SampleCount,
            result.MahalanobisDistance,
            result.Threshold,
            result.Features,
            result.Message);
    }

    private static FeatureVector GetGroupFeatures(GroupData group)
    {
        if (group.Features.Count == 0)
            throw new InvalidOperationException($"Group '{group.Id}' contains no extracted features.");

        var mean = group.MeanFeatures();
        return mean ?? throw new InvalidOperationException($"Group '{group.Id}' contains no valid features.");
    }
}
