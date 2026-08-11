using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class SubgroupInspectionService
{
    private readonly InspectionService _inspectionService;

    public SubgroupInspectionService(InspectionService? inspectionService = null)
    {
        _inspectionService = inspectionService ?? new InspectionService();
    }

    public IReadOnlyList<SubgroupInspectionResult> Inspect(
        GroupData group,
        InspectionModel model)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(model);

        var results = new List<SubgroupInspectionResult>(group.RecordCount);
        for (var i = 0; i < group.Records.Count; i++)
        {
            var record = group.Records[i];
            var features = record.Features
                ?? throw new InvalidOperationException($"Subgroup {i + 1} has no feature vector.");

            var result = _inspectionService.Inspect(features.Clone(), model);
            results.Add(new SubgroupInspectionResult
            {
                Index = i + 1,
                SourceName = record.SourceName,
                Features = result.Features,
                MahalanobisDistance = result.MahalanobisDistance,
                Threshold = result.Threshold,
                IsDefect = result.IsDefect
            });
        }

        return results;
    }
}
