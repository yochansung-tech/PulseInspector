namespace PulseInspector.Models;

public sealed class InspectionModel
{
    // Mean/covariance are defined only for the six statistical features,
    // always in FeatureVector.StatisticalFeatureNames order.
    public double[] Mean { get; set; } = Array.Empty<double>();
    public double[,] Covariance { get; set; } = new double[0, 0];
    public double[,] InverseCovariance { get; set; } = new double[0, 0];

    public double PeakMean { get; set; }
    public double PeakStandardDeviation { get; set; }
    public double[] StandardDeviations { get; set; } = Array.Empty<double>();
    public double Confidence { get; set; } = 0.999;
    public double Threshold { get; set; } = 22.457744;

    public int FeatureCount => Mean.Length;

    public bool IsTrained => Mean.Length == FeatureVector.StatisticalFeatureCount
        && Covariance.GetLength(0) == Mean.Length
        && Covariance.GetLength(1) == Mean.Length
        && InverseCovariance.GetLength(0) == Mean.Length
        && InverseCovariance.GetLength(1) == Mean.Length
        && StandardDeviations.Length == Mean.Length;

    public void ValidateFeatureOrder()
    {
        if (!IsTrained)
            throw new InvalidOperationException("The inspection model is incomplete.");

        if (FeatureCount != FeatureVector.StatisticalFeatureCount)
            throw new InvalidOperationException("Inspection model feature count does not match FeatureVector.");
    }
}
