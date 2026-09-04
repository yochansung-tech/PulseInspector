namespace PulseInspector.Models;

public sealed record InspectionResult(
    bool IsDefect,
    double MahalanobisDistance,
    double Threshold,
    FeatureVector Features,
    string Message);
