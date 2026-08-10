namespace PulseInspector.Models;

public sealed class FeatureVector
{
    private static readonly string[] OrderedNames =
    {
        "Charge", "FWHM", "MahalanobisDistance", "Noise", "Peak", "RiseTime", "Threshold", "ZScore"
    };

    // The six statistical features used by the Mahalanobis model.
    private static readonly string[] StatisticalNames =
    {
        "Charge", "FWHM", "Noise", "Peak", "RiseTime", "ZScore"
    };

    private readonly Dictionary<string, double> _values = new(StringComparer.Ordinal);

    public static IReadOnlyList<string> FeatureNames => OrderedNames;
    public static IReadOnlyList<string> StatisticalFeatureNames => StatisticalNames;

    public double this[string name]
    {
        get => _values.TryGetValue(name, out var value) ? value : 0.0;
        set => _values[name] = value;
    }

    public IReadOnlyDictionary<string, double> Values => _values;

    public double[] ToArray() => OrderedNames.Select(n => this[n]).ToArray();

    public double[] ToStatisticalArray() => StatisticalNames.Select(n => this[n]).ToArray();

    public FeatureVector Clone()
    {
        var copy = new FeatureVector();
        foreach (var name in OrderedNames) copy[name] = this[name];
        return copy;
    }
}
