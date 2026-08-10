namespace PulseInspector.Models;

public sealed class InspectionModel
{
    // Mean/covariance are defined only for the six statistical features.
    public double[] Mean { get; set; } = Array.Empty<double>();
    public double[,] InverseCovariance { get; set; } = new double[0, 0];

    // Peak statistics are retained to calculate the Z-score consistently at inspection time.
    public double PeakMean { get; set; }
    public double PeakStandardDeviation { get; set; }

    public double Confidence { get; set; } = 0.999;
    public double Threshold { get; set; } = 22.457744;

    public int FeatureCount => Mean.Length;
    public bool IsTrained => Mean.Length == FeatureVector.StatisticalFeatureNames.Count
        && InverseCovariance.GetLength(0) == Mean.Length
        && InverseCovariance.GetLength(1) == Mean.Length;
}
