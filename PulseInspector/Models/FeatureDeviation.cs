namespace PulseInspector.Models;

public sealed record FeatureDeviation(
    string FeatureName,
    double Value,
    double ReferenceMean,
    double ReferenceStandardDeviation,
    double ZScore,
    double AbsoluteZScore,
    double Contribution);
