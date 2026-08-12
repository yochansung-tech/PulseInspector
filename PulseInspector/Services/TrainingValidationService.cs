using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed record TrainingValidationIssue(string FeatureName, string Code, string Message, double Value = double.NaN);

public sealed class TrainingValidationResult
{
    public int SampleCount { get; init; }
    public int FeatureCount { get; init; }
    public IReadOnlyList<TrainingValidationIssue> Issues { get; init; } = Array.Empty<TrainingValidationIssue>();
    public bool IsValid => Issues.Count == 0;
    public bool HasWarnings => Issues.Any(i => i.Code.StartsWith("WARN_", StringComparison.Ordinal));
}

public sealed class TrainingValidationService
{
    public TrainingValidationResult Validate(IEnumerable<FeatureVector> vectors)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        var samples = vectors.Select(v => v.Clone()).ToArray();
        var issues = new List<TrainingValidationIssue>();
        var required = FeatureVector.StatisticalFeatureCount;

        if (samples.Length < required + 1)
        {
            issues.Add(new TrainingValidationIssue(
                "", "ERROR_INSUFFICIENT_SAMPLES",
                $"At least {required + 1} training samples are recommended for a {required}-dimensional covariance model. Current count: {samples.Length}.", samples.Length));
        }

        foreach (var feature in FeatureVector.StatisticalFeatureNames)
        {
            var values = samples.Select(v => v[feature]).ToArray();
            if (values.Any(x => !double.IsFinite(x)))
            {
                issues.Add(new TrainingValidationIssue(feature, "ERROR_NONFINITE", "Training data contains NaN or Infinity."));
                continue;
            }

            if (values.Length == 0) continue;

            var mean = values.Average();
            var variance = values.Length > 1
                ? values.Sum(x => (x - mean) * (x - mean)) / (values.Length - 1)
                : 0.0;
            var std = Math.Sqrt(Math.Max(variance, 0));

            // Features have very different physical scales. For example, Charge
            // is an integral (A*s) and can legitimately be much smaller than 1,
            // while Peak is expressed in uA. Using max(abs(mean), 1.0) made all
            // small-valued physical features look artificially low-variance.
            // Scale must therefore be derived from the feature itself.
            var maxAbs = values.Max(x => Math.Abs(x));
            var scale = Math.Max(Math.Abs(mean), maxAbs);
            scale = Math.Max(scale, 1e-30);

            var relativeStd = std / scale;

            if (relativeStd <= 1e-12)
            {
                issues.Add(new TrainingValidationIssue(
                    feature,
                    "ERROR_ZERO_VARIANCE",
                    "Feature variance is effectively zero relative to its own scale; covariance model may be singular.",
                    std));
            }
            else if (relativeStd <= 1e-6)
            {
                issues.Add(new TrainingValidationIssue(
                    feature,
                    "WARN_LOW_VARIANCE",
                    "Feature variance is very small relative to its own scale.",
                    std));
            }
        }

        var duplicateCount = CountDuplicateRows(samples);
        if (duplicateCount > 0)
        {
            issues.Add(new TrainingValidationIssue(
                "", "WARN_DUPLICATE_SAMPLES",
                $"Detected {duplicateCount} duplicate statistical feature row(s). Duplicates reduce effective training diversity.", duplicateCount));
        }

        return new TrainingValidationResult
        {
            SampleCount = samples.Length,
            FeatureCount = required,
            Issues = issues
        };
    }

    private static int CountDuplicateRows(IReadOnlyList<FeatureVector> samples)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = 0;
        foreach (var sample in samples)
        {
            var key = string.Join("|", sample.ToStatisticalArray().Select(v => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
            if (!seen.Add(key)) duplicates++;
        }
        return duplicates;
    }
}
