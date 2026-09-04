namespace PulseInspector.Models;

public sealed record FeatureDeviation(
    string FeatureName,
    double Value,
    double Mean,
    double StandardDeviation,
    double ZScore,
    double AbsoluteZScore,
    double MahalanobisContribution);
