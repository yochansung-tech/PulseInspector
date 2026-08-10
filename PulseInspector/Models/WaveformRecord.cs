namespace PulseInspector.Models;

public sealed class WaveformRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string SourceName { get; init; } = string.Empty;
    public double[] Samples { get; init; } = Array.Empty<double>();
    public FeatureVector Features { get; init; } = new();
    public double SampleIntervalSeconds { get; init; }
    public double MeasurementPeriodSeconds => SampleIntervalSeconds * Samples.Length;
    public bool HasExplicitTimeAxis { get; init; }

    public int SampleCount => Samples.Length;
}
