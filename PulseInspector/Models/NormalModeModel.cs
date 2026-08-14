namespace PulseInspector.Models;

/// <summary>
/// Statistical model for one known-normal waveform population (mode).
/// Mean and covariance are stored in the shared standardized feature space
/// defined by InspectionModel.FeatureMeans/FeatureScales.
/// </summary>
public sealed class NormalModeModel
{
    public int ModeIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SampleCount { get; set; }
    public double[] Mean { get; set; } = Array.Empty<double>();
    public double[,] Covariance { get; set; } = new double[0, 0];
    public double[,] InverseCovariance { get; set; } = new double[0, 0];
    public double[] StandardDeviations { get; set; } = Array.Empty<double>();
    public double Confidence { get; set; } = 0.999;
    public double Threshold { get; set; }

    public bool IsTrained =>
        SampleCount >= FeatureVector.StatisticalFeatureCount + 1 &&
        Mean.Length == FeatureVector.StatisticalFeatureCount &&
        Covariance.GetLength(0) == Mean.Length &&
        Covariance.GetLength(1) == Mean.Length &&
        InverseCovariance.GetLength(0) == Mean.Length &&
        InverseCovariance.GetLength(1) == Mean.Length &&
        StandardDeviations.Length == Mean.Length &&
        double.IsFinite(Threshold) && Threshold > 0;

    public void Validate()
    {
        if (!IsTrained)
            throw new InvalidOperationException($"Normal mode '{Name}' is incomplete.");
    }
}
