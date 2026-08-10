namespace PulseInspector.Models;

public sealed class GroupData
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public List<WaveformRecord> Records { get; } = new();
    public bool IsDefective { get; set; }

    public int SampleCount => Records.Count;

    public IReadOnlyList<double[]> Waveforms => Records.Select(r => r.Samples).ToArray();
    public IReadOnlyList<FeatureVector> Features => Records.Select(r => r.Features).ToArray();

    public void AddWaveform(double[] waveform, FeatureVector? features = null, string? sourceName = null)
    {
        ArgumentNullException.ThrowIfNull(waveform);
        if (waveform.Length == 0)
            throw new ArgumentException("Waveform cannot be empty.", nameof(waveform));

        if (Records.Count > 0 && waveform.Length != Records[0].SampleCount)
            throw new ArgumentException("All waveforms in a group must have the same sample count.", nameof(waveform));

        Records.Add(new WaveformRecord
        {
            SourceName = sourceName ?? string.Empty,
            Samples = (double[])waveform.Clone(),
            Features = features?.Clone() ?? new FeatureVector()
        });
    }

    public double[]? MeanWaveform()
    {
        if (Records.Count == 0) return null;
        var length = Records[0].SampleCount;
        var result = new double[length];
        foreach (var record in Records)
            for (var i = 0; i < length; i++) result[i] += record.Samples[i];
        for (var i = 0; i < length; i++) result[i] /= Records.Count;
        return result;
    }

    public FeatureVector? MeanFeatures()
    {
        if (Records.Count == 0) return null;

        var result = new FeatureVector();
        foreach (var name in FeatureVector.StatisticalFeatureNames)
            result[name] = Records.Average(r => r.Features[name]);

        return result;
    }
}
