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
        var x = vector.ToStatisticalArray();
        var centered = new double[x.Length];
        for (var i = 0; i < x.Length; i++)
            centered[i] = x[i] - model.Mean[i];

        var weighted = new double[x.Length];
        for (var i = 0; i < x.Length; i++)
            for (var j = 0; j < x.Length; j++)
                weighted[i] += model.InverseCovariance[i, j] * centered[j];

        var result = new List<FeatureDeviation>(names.Count);
        for (var i = 0; i < names.Count; i++)
        {
            var std = model.StandardDeviations[i];
            var z = std > 0 ? centered[i] / std : 0.0;
            var contribution = centered[i] * weighted[i];

            result.Add(new FeatureDeviation(
                names[i], x[i], model.Mean[i], std, z, Math.Abs(z), contribution));
        }

        return result
            .OrderByDescending(x => x.AbsoluteZScore)
            .ThenBy(x => x.FeatureName, StringComparer.Ordinal)
            .ToArray();
    }
}
