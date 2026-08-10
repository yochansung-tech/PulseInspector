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

        // Z-score is defined from the training distribution of Peak.
        var peakValues = samples.Select(v => v["Peak"]).ToArray();
        var peakMean = peakValues.Average();
        var peakStd = StandardDeviation(peakValues, peakMean);
        if (peakStd <= 0) peakStd = 1e-12;

        foreach (var sample in samples)
            sample["ZScore"] = (sample["Peak"] - peakMean) / peakStd;

        var rows = samples.Select(v => v.ToStatisticalArray()).ToArray();
        var mean = StatisticsService.Mean(rows);
        var inverse = StatisticsService.Invert(StatisticsService.Covariance(rows));
        var threshold = StatisticsService.ChiSquareQuantile(FeatureVector.StatisticalFeatureNames.Count, confidence);

        return new InspectionModel
        {
            Mean = mean,
            InverseCovariance = inverse,
            PeakMean = peakMean,
            PeakStandardDeviation = peakStd,
            Confidence = confidence,
            Threshold = threshold
        };
    }

    public InspectionResult Inspect(FeatureVector vector, InspectionModel model)
    {
        if (!model.IsTrained)
            throw new InvalidOperationException("The inspection model has not been trained.");

        var peakStd = model.PeakStandardDeviation <= 0 ? 1e-12 : model.PeakStandardDeviation;
        vector["ZScore"] = (vector["Peak"] - model.PeakMean) / peakStd;

        var distance = StatisticsService.Mahalanobis(
            vector.ToStatisticalArray(),
            model.Mean,
            model.InverseCovariance);

        var defect = distance > model.Threshold;
        vector["MahalanobisDistance"] = distance;
        vector["Threshold"] = model.Threshold;

        return new InspectionResult(
            defect,
            distance,
            model.Threshold,
            vector,
            defect ? "Abnormal group" : "Normal group");
    }

    private static double StandardDeviation(IReadOnlyList<double> values, double mean)
    {
        if (values.Count < 2) return 0;
        var sum = values.Sum(x => (x - mean) * (x - mean));
        return Math.Sqrt(sum / (values.Count - 1));
    }
}
