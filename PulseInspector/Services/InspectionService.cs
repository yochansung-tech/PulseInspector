using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class InspectionService
{
    public InspectionModel Train(IEnumerable<FeatureVector> vectors, double confidence = 0.999)
    {
        var rows = vectors.Select(v => v.ToArray()).ToArray();
        if (rows.Length == 0) throw new InvalidOperationException("Training data is empty.");
        var mean = StatisticsService.Mean(rows);
        var inverse = StatisticsService.Invert(StatisticsService.Covariance(rows));
        return new InspectionModel { Mean = mean, InverseCovariance = inverse, Confidence = confidence, Threshold = StatisticsService.ChiSquare99_9(mean.Length) };
    }

    public InspectionResult Inspect(FeatureVector vector, InspectionModel model)
    {
        if (!model.IsTrained) throw new InvalidOperationException("The inspection model has not been trained.");
        var distance = StatisticsService.Mahalanobis(vector.ToArray(), model.Mean, model.InverseCovariance);
        var defect = distance > model.Threshold;
        vector["MahalanobisDistance"] = distance;
        vector["Threshold"] = model.Threshold;
        var z = vector["Peak"] - model.Mean[Array.IndexOf(FeatureVector.FeatureNames.ToArray(), "Peak")];
        vector["ZScore"] = z;
        return new InspectionResult(defect, distance, model.Threshold, vector, defect ? "Abnormal group" : "Normal group");
    }
}
