namespace PulseInspector.Models;

public sealed class InspectionModel
{
    public double[] Mean { get; set; } = Array.Empty<double>();
    public double[,] InverseCovariance { get; set; } = new double[0, 0];
    public double Confidence { get; set; } = 0.999;
    public double Threshold { get; set; } = 22.4577;
    public int FeatureCount => Mean.Length;
    public bool IsTrained => Mean.Length > 0 && InverseCovariance.GetLength(0) == Mean.Length;
}
