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

        // Explain deviations against the closest normal mode when the model is
        // multimodal. Legacy single-mode models continue to use the top-level
        // Mean/InverseCovariance fields.
        var mode = model.NormalModes.Count > 1
            ? model.NormalModes
                .Select(candidate => new
                {
                    Candidate = candidate,
                    Distance = StatisticsService.Mahalanobis(
                        standardized, candidate.Mean, candidate.InverseCovariance)
                })
                .OrderBy(x => x.Distance)
                .First().Candidate
            : null;

        var mean = mode?.Mean ?? model.Mean;
        var inverseCovariance = mode?.InverseCovariance ?? model.InverseCovariance;

        var centered = new double[raw.Length];
        for (var i = 0; i < raw.Length; i++)
            centered[i] = standardized[i] - mean[i];

        var weighted = new double[raw.Length];
        for (var i = 0; i < raw.Length; i++)
            for (var j = 0; j < raw.Length; j++)
                weighted[i] += inverseCovariance[i, j] * centered[j];

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
