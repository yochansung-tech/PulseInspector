namespace PulseInspector.Models;

public sealed record GroupInspectionResult(
    string GroupId,
    bool IsDefect,
    int SampleCount,
    double MahalanobisDistance,
    double Threshold,
    FeatureVector Features,
    string Message);
