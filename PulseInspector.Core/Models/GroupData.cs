namespace PulseInspector.Models;

public sealed class GroupData
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public List<WaveformRecord> Records { get; } = new();
    public bool IsDefective { get; set; }
    public int RecordCount => Records.Count;
    public int WaveformSampleCount => Records.Count == 0 ? 0 : Records[0].SampleCount;
    [Obsolete("Use RecordCount instead. SampleCount now means waveform sample count.")]
    public int SampleCount => RecordCount;
    public IReadOnlyList<double[]> Waveforms => Records.Select(r => r.Samples).ToArray();
    public IReadOnlyList<FeatureVector> Features => Records.Select(r => r.Features).ToArray();

    public void AddWaveform(double[] waveform, FeatureVector? features = null, string? sourceName = null,
        double sampleIntervalSeconds = 0, bool hasExplicitTimeAxis = false)
    {
        ArgumentNullException.ThrowIfNull(waveform);
        if (waveform.Length < 2) throw new ArgumentException("Waveform must contain at least two samples.", nameof(waveform));
        if (Records.Count > 0 && waveform.Length != WaveformSampleCount)
            throw new ArgumentException("All waveforms in a group must have the same sample count.", nameof(waveform));
        if (sampleIntervalSeconds <= 0 || !double.IsFinite(sampleIntervalSeconds))
            throw new ArgumentOutOfRangeException(nameof(sampleIntervalSeconds));
        if (Records.Count > 0 && Math.Abs(sampleIntervalSeconds - Records[0].SampleIntervalSeconds) >
            Math.Max(sampleIntervalSeconds, Records[0].SampleIntervalSeconds) * 1e-9)
            throw new ArgumentException("All waveforms in a group must have the same sample interval.", nameof(sampleIntervalSeconds));
        Records.Add(new WaveformRecord { SourceName = sourceName ?? string.Empty, Samples = (double[])waveform.Clone(),
            Features = features?.Clone() ?? new FeatureVector(), SampleIntervalSeconds = sampleIntervalSeconds,
            HasExplicitTimeAxis = hasExplicitTimeAxis });
    }

    public double[]? MeanWaveform()
    {
        if (Records.Count == 0) return null;
        var result = new double[WaveformSampleCount];
        foreach (var record in Records) for (var i = 0; i < result.Length; i++) result[i] += record.Samples[i];
        for (var i = 0; i < result.Length; i++) result[i] /= Records.Count;
        return result;
    }

    public FeatureVector? MeanFeatures()
    {
        if (Records.Count == 0) return null;
        var result = new FeatureVector();
        foreach (var name in FeatureVector.StatisticalFeatureNames) result[name] = Records.Average(r => r.Features[name]);
        return result;
    }
}
