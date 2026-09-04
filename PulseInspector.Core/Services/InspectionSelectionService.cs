using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed record SubgroupDisplayRow(int Index, string SourceName, double MahalanobisDistance, double Threshold, bool IsDefect);
public sealed record SelectedSubgroupInspection(WaveformRecord Record, SubgroupInspectionResult? Result, IReadOnlyList<FeatureDeviation> Deviations);
public sealed class InspectionSelectionService
{
    private readonly FeatureDeviationService _deviationService;
    public InspectionSelectionService(FeatureDeviationService? deviationService = null) { _deviationService = deviationService ?? new FeatureDeviationService(); }
    public IReadOnlyList<SubgroupDisplayRow> CreateRows(IReadOnlyList<SubgroupInspectionResult> results) => results.Select(r => new SubgroupDisplayRow(r.Index, r.SourceName, r.MahalanobisDistance, r.Threshold, r.IsDefect)).ToArray();
    public SelectedSubgroupInspection Select(GroupData group, int recordIndex, IReadOnlyList<SubgroupInspectionResult> results, InspectionModel model)
    {
        ArgumentNullException.ThrowIfNull(group); ArgumentNullException.ThrowIfNull(model); if (recordIndex < 0 || recordIndex >= group.Records.Count) throw new ArgumentOutOfRangeException(nameof(recordIndex)); var record = group.Records[recordIndex]; var result = results.FirstOrDefault(r => r.Index == recordIndex + 1); var deviations = _deviationService.Analyze(record.Features, model); return new SelectedSubgroupInspection(record, result, deviations);
    }
}
