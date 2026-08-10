namespace PulseInspector.Models;

public sealed class WaveformData
{
    public string SourceName { get; init; } = string.Empty;
    public double[] Samples { get; init; } = Array.Empty<double>();
    public double SampleIntervalSeconds { get; init; }
    public double MeasurementPeriodSeconds => SampleIntervalSeconds * Samples.Length;
    public bool HasExplicitTimeAxis { get; init; }

    public int SampleCount => Samples.Length;

    public void Validate()
    {
        if (Samples.Length < 2)
            throw new InvalidOperationException("A waveform must contain at least two samples.");
        if (!double.IsFinite(SampleIntervalSeconds) || SampleIntervalSeconds <= 0)
            throw new InvalidOperationException("Sample interval must be a positive finite value.");
        if (Samples.Any(x => !double.IsFinite(x)))
            throw new InvalidOperationException("Waveform contains a non-finite sample.");
    }
}
