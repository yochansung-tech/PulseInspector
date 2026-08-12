namespace PulseInspector.Models;

public sealed class FeatureVector
{
    // Single source of truth for the complete feature ordering.
    private static readonly string[] OrderedNames =
    {
        "Charge", "FWHM", "MahalanobisDistance", "Noise", "Peak", "RiseTime", "Threshold", "ZScore"
    };

    // ZScore is retained as a display/diagnostic feature, but is NOT included
    // in the Mahalanobis model because it is derived directly from Peak.
    private static readonly string[] StatisticalNames =
    {
        "Charge", "FWHM", "Noise", "Peak", "RiseTime"
    };

    private readonly Dictionary<string, double> _values = new(StringComparer.Ordinal);

    public static IReadOnlyList<string> FeatureNames => OrderedNames;
    public static IReadOnlyList<string> StatisticalFeatureNames => StatisticalNames;
    public static int StatisticalFeatureCount => StatisticalNames.Length;

    public double this[string name]
    {
        get => _values.TryGetValue(name, out var value) ? value : 0.0;
        set => _values[name] = value;
    }

    public IReadOnlyDictionary<string, double> Values => _values;

    public double[] ToArray() => OrderedNames.Select(n => this[n]).ToArray();

    public double[] ToStatisticalArray() => StatisticalNames.Select(n => this[n]).ToArray();

    public static FeatureVector FromStatisticalArray(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count != StatisticalNames.Length)
            throw new ArgumentException($"Expected {StatisticalNames.Length} statistical values, received {values.Count}.", nameof(values));

        var result = new FeatureVector();
        for (var i = 0; i < StatisticalNames.Length; i++)
            result[StatisticalNames[i]] = values[i];
        return result;
    }

    public static int GetStatisticalIndex(string featureName)
    {
        var index = Array.IndexOf(StatisticalNames, featureName);
        if (index < 0)
            throw new ArgumentException($"'{featureName}' is not a statistical feature.", nameof(featureName));
        return index;
    }

    public FeatureVector Clone()
    {
        var copy = new FeatureVector();
        foreach (var name in OrderedNames) copy[name] = this[name];
        return copy;
    }
}
