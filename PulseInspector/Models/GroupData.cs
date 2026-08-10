namespace PulseInspector.Models;

public sealed class GroupData
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public List<double[]> Waveforms { get; } = new();
    public List<FeatureVector> Features { get; } = new();
    public bool IsDefective { get; set; }

    public int SampleCount => Waveforms.Count;

    public void AddWaveform(double[] waveform, FeatureVector? features = null)
    {
        ArgumentNullException.ThrowIfNull(waveform);
        if (waveform.Length == 0)
            throw new ArgumentException("Waveform cannot be empty.", nameof(waveform));

        if (Waveforms.Count > 0 && waveform.Length != Waveforms[0].Length)
            throw new ArgumentException("All waveforms in a group must have the same sample count.", nameof(waveform));

        Waveforms.Add((double[])waveform.Clone());
        if (features is not null)
            Features.Add(features.Clone());
    }

    public double[]? MeanWaveform()
    {
        if (Waveforms.Count == 0) return null;
        var length = Waveforms[0].Length;
        var result = new double[length];
        foreach (var waveform in Waveforms)
            for (var i = 0; i < length; i++) result[i] += waveform[i];
        for (var i = 0; i < length; i++) result[i] /= Waveforms.Count;
        return result;
    }

    public FeatureVector? MeanFeatures()
    {
        if (Features.Count == 0) return null;

        var result = new FeatureVector();
        foreach (var name in FeatureVector.StatisticalFeatureNames)
            result[name] = Features.Average(v => v[name]);

        return result;
    }
}
