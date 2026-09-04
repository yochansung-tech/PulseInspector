using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class InspectionService
{
    private readonly TrainingValidationService _trainingValidation = new();
    private const double CovarianceRegularization = 1e-6;
    public InspectionService() { }
    public TrainingValidationResult ValidateTraining(IEnumerable<FeatureVector> vectors) => _trainingValidation.Validate(vectors);
    public InspectionModel Train(IEnumerable<FeatureVector> vectors, double confidence = 0.999)
    {
        if (confidence <= 0 || confidence >= 1) throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        var samples = vectors.Select(v => v.Clone()).ToArray(); if (samples.Length == 0) throw new InvalidOperationException("At least one training feature vector is required.");
        var validation = _trainingValidation.Validate(samples); var errors = validation.Issues.Where(i => i.Code.StartsWith("ERROR_", StringComparison.Ordinal)).ToArray();
        if (errors.Length > 0) throw new InvalidOperationException("Training data validation failed: " + string.Join("; ", errors.Select(e => e.Message)));
        var rawRows = samples.Select(v => v.ToStatisticalArray()).ToArray(); var featureCount = FeatureVector.StatisticalFeatureCount;
        var featureMeans = Enumerable.Range(0, featureCount).Select(i => rawRows.Average(row => row[i])).ToArray();
        var featureScales = Enumerable.Range(0, featureCount).Select(i => SampleStandardDeviation(rawRows.Select(row => row[i]).ToArray(), featureMeans[i])).Select(scale => scale > 0 && double.IsFinite(scale) ? scale : 1e-12).ToArray();
        var standardizedRows = rawRows.Select(row => Standardize(row, featureMeans, featureScales)).ToArray(); var mean = StatisticsService.Mean(standardizedRows); var covariance = StatisticsService.Covariance(standardizedRows);
        for (var i = 0; i < featureCount; i++) covariance[i, i] += CovarianceRegularization;
        var inverse = StatisticsService.Invert(covariance); var standardDeviations = Enumerable.Range(0, featureCount).Select(i => Math.Sqrt(Math.Max(covariance[i, i], 0.0))).Select(x => x > 0 ? x : 1e-12).ToArray();
        var threshold = StatisticsService.ChiSquareQuantile(featureCount, confidence); var peakIndex = FeatureVector.GetStatisticalIndex("Peak"); var peakValues = rawRows.Select(row => row[peakIndex]).ToArray();
        var model = new InspectionModel { Mean = mean, Covariance = covariance, InverseCovariance = inverse, StandardDeviations = standardDeviations, FeatureMeans = featureMeans, FeatureScales = featureScales, PeakMean = featureMeans[peakIndex], PeakStandardDeviation = featureScales[peakIndex], Confidence = confidence, Threshold = threshold };
        model.ValidateFeatureOrder(); return model;
    }
    public InspectionResult Inspect(FeatureVector vector, InspectionModel model)
    {
        model.ValidateFeatureOrder(); var peakStd = model.PeakStandardDeviation > 0 ? model.PeakStandardDeviation : 1e-12; vector["ZScore"] = (vector["Peak"] - model.PeakMean) / peakStd;
        var rawValues = vector.ToStatisticalArray(); if (rawValues.Length != model.Mean.Length) throw new InvalidOperationException("FeatureVector and InspectionModel dimensions do not match.");
        var standardizedValues = Standardize(rawValues, model.FeatureMeans, model.FeatureScales); var distance = StatisticsService.Mahalanobis(standardizedValues, model.Mean, model.InverseCovariance); var defect = distance > model.Threshold;
        vector["MahalanobisDistance"] = distance; vector["Threshold"] = model.Threshold; return new InspectionResult(defect, distance, model.Threshold, vector, defect ? "Abnormal group" : "Normal group");
    }
    private static double[] Standardize(IReadOnlyList<double> values, IReadOnlyList<double> means, IReadOnlyList<double> scales)
    {
        if (values.Count != means.Count || values.Count != scales.Count) throw new ArgumentException("Feature standardization dimensions do not match."); var result = new double[values.Count];
        for (var i = 0; i < values.Count; i++) { var scale = scales[i] > 0 ? scales[i] : 1e-12; result[i] = (values[i] - means[i]) / scale; } return result;
    }
    private static double SampleStandardDeviation(IReadOnlyList<double> values, double mean)
    { if (values.Count < 2) return 0; var sum = values.Sum(x => (x - mean) * (x - mean)); return Math.Sqrt(sum / (values.Count - 1)); }
}
