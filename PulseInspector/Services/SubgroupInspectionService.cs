using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class SubgroupInspectionService
{
    private readonly GroupInspectionService _groupService = new();

    public IReadOnlyList<SubgroupInspectionResult> Inspect(
        GroupData group,
        InspectionModel model)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(model);

        var results = new List<SubgroupInspectionResult>(group.SampleCount);
        for (var i = 0; i < group.Records.Count; i++)
        {
            var record = group.Records[i];
            var features = record.Features
                ?? throw new InvalidOperationException($"Subgroup {i + 1} has no feature vector.");

            var distance = _groupService.CalculateMahalanobisDistance(features, model);
            results.Add(new SubgroupInspectionResult
            {
                Index = i + 1,
                SourceName = record.SourceName,
                Features = features,
                MahalanobisDistance = distance,
                Threshold = model.Threshold,
                IsDefect = distance > model.Threshold
            });
        }

        return results;
    }
}
