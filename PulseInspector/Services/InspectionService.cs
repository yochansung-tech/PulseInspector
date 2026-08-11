using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class InspectionService
{
    public InspectionModel Train(IEnumerable<FeatureVector> vectors, double confidence = 0.999)
    {
        if (confidence <= 0 || confidence >= 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");

        var samples = vectors.Select(v => v.Clone()).ToArray();
        if (samples.Length < 2)
            throw new InvalidOperationException("At least two training samples are required.");

        var peakValues = samples.Select(v => v["Peak"]).ToArray();
        var peakMean = peakValues.Average();
        var peakStd = StandardDeviation(peakValues, peakMean);
        if (peakStd <= 0) peakStd = 1e-12;

        foreach (var sample in samples)
            sample["ZScore"] = (sample["Peak"] - peakMean) / peakStd;

        var rows = samples.Select(v => v.ToStatisticalArray()).ToArray();
        var mean = StatisticsService.Mean(rows);
        var covariance = StatisticsService.Covariance(rows);
        var inverse = StatisticsService.Invert(covariance);
        var standardDeviations = Enumerable.Range(0, FeatureVector.StatisticalFeatureCount)
            .Select(i => Math.Sqrt(Math.Max(covariance[i, i], 0.0)))
            .Select(x => x > 0 ? x : 1e-12)
            .ToArray();
        var threshold = StatisticsService.ChiSquareQuantile(FeatureVector.StatisticalFeatureCount, confidence);

        var model = new InspectionModel
        {
            Mean = mean,
            Covariance = covariance,
            InverseCovariance = inverse,
            StandardDeviations = standardDeviations,
            PeakMean = peakMean,
            PeakStandardDeviation = peakStd,
            Confidence = confidence,
            Threshold = threshold
        };
        model.ValidateFeatureOrder();
        return model;
    }

    public InspectionResult Inspect(FeatureVector vector, InspectionModel model)
    {
        model.ValidateFeatureOrder();

        var peakStd = model.PeakStandardDeviation <= 0 ? 1e-12 : model.PeakStandardDeviation;
        vector["ZScore"] = (vector["Peak"] - model.PeakMean) / peakStd;

        var statisticalValues = vector.ToStatisticalArray();
        if (statisticalValues.Length != model.Mean.Length)
            throw new InvalidOperationException("FeatureVector and InspectionModel dimensions do not match.");

        var distance = StatisticsService.Mahalanobis(
            statisticalValues, model.Mean, model.InverseCovariance);

        var defect = distance > model.Threshold;
        vector["MahalanobisDistance"] = distance;
        vector["Threshold"] = model.Threshold;

        return new InspectionResult(defect, distance, model.Threshold, vector,
            defect ? "Abnormal group" : "Normal group");
    }

    private static double StandardDeviation(IReadOnlyList<double> values, double mean)
    {
        if (values.Count < 2) return 0;
        var sum = values.Sum(x => (x - mean) * (x - mean));
        return Math.Sqrt(sum / (values.Count - 1));
    }
}
