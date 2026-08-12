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
        var raw = vector.ToStatisticalArray();
        var standardized = new double[raw.Length];

        for (var i = 0; i < raw.Length; i++)
        {
            var scale = model.FeatureScales[i] > 0 ? model.FeatureScales[i] : 1e-12;
            standardized[i] = (raw[i] - model.FeatureMeans[i]) / scale;
        }

        // Model.Mean is in standardized space.
        var centered = new double[raw.Length];
        for (var i = 0; i < raw.Length; i++)
            centered[i] = standardized[i] - model.Mean[i];

        var weighted = new double[raw.Length];
        for (var i = 0; i < raw.Length; i++)
            for (var j = 0; j < raw.Length; j++)
                weighted[i] += model.InverseCovariance[i, j] * centered[j];

        var result = new List<FeatureDeviation>(names.Count);
        for (var i = 0; i < names.Count; i++)
        {
            var rawStd = model.FeatureScales[i] > 0 ? model.FeatureScales[i] : 1e-12;
            var z = centered[i];
            var contribution = centered[i] * weighted[i];

            result.Add(new FeatureDeviation(
                names[i],
                raw[i],
                model.FeatureMeans[i],
                rawStd,
                z,
                Math.Abs(z),
                contribution));
        }

        return result
            .OrderByDescending(x => x.AbsoluteZScore)
            .ThenBy(x => x.FeatureName, StringComparer.Ordinal)
            .ToArray();
    }
}
