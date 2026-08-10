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
        ArgumentNullException.ThrowIfNull(normalGroups);

        var groupFeatures = normalGroups
            .Select(GetGroupFeatures)
            .ToArray();

        // Six statistical dimensions require at least seven independent
        // group observations for a full-rank sample covariance matrix.
        var minimumGroups = FeatureVector.StatisticalFeatureNames.Count + 1;
        if (groupFeatures.Length < minimumGroups)
            throw new InvalidOperationException(
                $"At least {minimumGroups} normal groups are required for the six-feature covariance model.");

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

        if (group.Features.Count != group.Waveforms.Count)
            throw new InvalidOperationException(
                $"Group '{group.Id}' has {group.Waveforms.Count} waveforms but {group.Features.Count} feature vectors.");

        var mean = group.MeanFeatures();
        return mean ?? throw new InvalidOperationException($"Group '{group.Id}' contains no valid features.");
    }
}
