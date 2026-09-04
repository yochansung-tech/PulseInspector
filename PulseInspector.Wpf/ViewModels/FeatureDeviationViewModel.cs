namespace PulseInspector.Wpf.ViewModels;

public sealed record FeatureDeviationViewModel(
    string FeatureName,
    double Value,
    double Mean,
    double Scale,
    double ZScore,
    double AbsoluteZScore,
    double Contribution);
