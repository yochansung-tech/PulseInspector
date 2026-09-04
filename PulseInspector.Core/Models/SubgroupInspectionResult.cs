namespace PulseInspector.Models;

public sealed class SubgroupInspectionResult
{
    public required int Index { get; init; }
    public required string SourceName { get; init; }
    public required FeatureVector Features { get; init; }
    public double MahalanobisDistance { get; init; }
    public double Threshold { get; init; }
    public bool IsDefect { get; init; }
    public string Reason => IsDefect
        ? $"Mahalanobis distance {MahalanobisDistance:F6} exceeds threshold {Threshold:F6}."
        : "Within the statistical threshold.";
}
