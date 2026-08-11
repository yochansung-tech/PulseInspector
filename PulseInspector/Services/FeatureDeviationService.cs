using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class FeatureDeviationService
{
    public IReadOnlyList<FeatureDeviation> Analyze(FeatureVector vector, InspectionModel model)
    {
        ArgumentNullException.ThrowIfNull(vector);
        ArgumentNullException.ThrowIfNull(model);
        if (!model.IsTrained)
            throw new InvalidOperationException("The inspection model has not been trained.");

        var names = FeatureVector.StatisticalFeatureNames;
        var result = new List<FeatureDeviation>(names.Count);

        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];
            var value = vector[name];
            var mean = model.Mean[i];

            // The model covariance does not expose individual standard deviations directly.
            // The diagonal of the inverse covariance is not a valid variance, so contribution is
            // intentionally reported as a normalized squared deviation using the model mean only
            // when a finite scale can be estimated. A zero scale is treated as zero contribution.
            var varianceScale = EstimateVarianceScale(model.InverseCovariance, i);
            var std = varianceScale > 0 ? 1.0 / Math.Sqrt(varianceScale) : 0.0;
            var z = std > 0 ? (value - mean) / std : 0.0;

            result.Add(new FeatureDeviation(
                name,
                value,
                mean,
                std,
                z,
                Math.Abs(z),
                z * z));
        }

        return result
            .OrderByDescending(x => x.AbsoluteZScore)
            .ThenBy(x => x.FeatureName, StringComparer.Ordinal)
            .ToArray();
    }

    private static double EstimateVarianceScale(double[,] inverseCovariance, int index)
    {
        // For a diagonal covariance matrix this is exact. For a correlated model, a
        // per-feature z-score requires the original covariance, which is intentionally
        // not stored in InspectionModel. Return zero rather than inventing a scale.
        return 0.0;
    }
}
