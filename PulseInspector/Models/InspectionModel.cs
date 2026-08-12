namespace PulseInspector.Models;

public sealed class InspectionModel
{
    // Mean/covariance are defined for the five independent statistical
    // features in FeatureVector.StatisticalFeatureNames order. Values stored
    // in Mean/Covariance are in standardized feature space.
    public double[] Mean { get; set; } = Array.Empty<double>();
    public double[,] Covariance { get; set; } = new double[0, 0];
    public double[,] InverseCovariance { get; set; } = new double[0, 0];

    // Raw-space statistics retained for diagnostics and display.
    public double PeakMean { get; set; }
    public double PeakStandardDeviation { get; set; }

    // Per-feature raw-space center/scale used to standardize inspection data.
    public double[] FeatureMeans { get; set; } = Array.Empty<double>();
    public double[] FeatureScales { get; set; } = Array.Empty<double>();

    public double[] StandardDeviations { get; set; } = Array.Empty<double>();
    public double Confidence { get; set; } = 0.999;
    public double Threshold { get; set; } = 20.515005; // chi-square(df=5, 0.999)

    public int FeatureCount => Mean.Length;

    public bool IsTrained => Mean.Length == FeatureVector.StatisticalFeatureCount
        && Covariance.GetLength(0) == Mean.Length
        && Covariance.GetLength(1) == Mean.Length
        && InverseCovariance.GetLength(0) == Mean.Length
        && InverseCovariance.GetLength(1) == Mean.Length
        && StandardDeviations.Length == Mean.Length
        && FeatureMeans.Length == Mean.Length
        && FeatureScales.Length == Mean.Length;

    public void ValidateFeatureOrder()
    {
        if (!IsTrained)
            throw new InvalidOperationException("The inspection model is incomplete.");

        if (FeatureCount != FeatureVector.StatisticalFeatureCount)
            throw new InvalidOperationException("Inspection model feature count does not match FeatureVector.");
    }
}
