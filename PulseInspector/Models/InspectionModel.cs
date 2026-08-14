namespace PulseInspector.Models;

public sealed class InspectionModel
{
    // Mean/covariance are retained for backward compatibility and mirror
    // the first normal mode. New inspections use NormalModes when available.
    public double[] Mean { get; set; } = Array.Empty<double>();
    public double[,] Covariance { get; set; } = new double[0, 0];
    public double[,] InverseCovariance { get; set; } = new double[0, 0];

    // Raw-space statistics retained for diagnostics and display.
    public double PeakMean { get; set; }
    public double PeakStandardDeviation { get; set; }

    // Shared raw-space center/scale used to standardize every inspection
    // vector before it is evaluated against the individual normal modes.
    public double[] FeatureMeans { get; set; } = Array.Empty<double>();
    public double[] FeatureScales { get; set; } = Array.Empty<double>();

    public double[] StandardDeviations { get; set; } = Array.Empty<double>();
    public double Confidence { get; set; } = 0.999;
    public double Threshold { get; set; } = 20.515005;

    // Known-normal waveform populations. Current PulseInspector training
    // creates two modes because the validated training data contains two
    // recurring normal waveform populations (~83% / ~17%).
    public List<NormalModeModel> NormalModes { get; set; } = new();

    public int FeatureCount => Mean.Length;
    public bool IsMultiModal => NormalModes.Count > 1 && NormalModes.All(m => m.IsTrained);

    public bool IsTrained =>
        Mean.Length == FeatureVector.StatisticalFeatureCount
        && Covariance.GetLength(0) == Mean.Length
        && Covariance.GetLength(1) == Mean.Length
        && InverseCovariance.GetLength(0) == Mean.Length
        && InverseCovariance.GetLength(1) == Mean.Length
        && StandardDeviations.Length == Mean.Length
        && FeatureMeans.Length == Mean.Length
        && FeatureScales.Length == Mean.Length
        && (NormalModes.Count == 0 || NormalModes.All(m => m.IsTrained));

    public void ValidateFeatureOrder()
    {
        if (!IsTrained)
            throw new InvalidOperationException("The inspection model is incomplete.");

        if (FeatureCount != FeatureVector.StatisticalFeatureCount)
            throw new InvalidOperationException("Inspection model feature count does not match FeatureVector.");

        foreach (var mode in NormalModes)
            mode.Validate();
    }
}
