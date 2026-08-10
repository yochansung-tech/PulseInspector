namespace PulseInspector.Models;

public sealed class GroupData
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public List<double[]> Waveforms { get; } = new();
    public List<FeatureVector> Features { get; } = new();
    public bool IsDefective { get; set; }

    public int SampleCount => Waveforms.Count;
    public double[]? MeanWaveform()
    {
        if (Waveforms.Count == 0) return null;
        var length = Waveforms.Min(x => x.Length);
        var result = new double[length];
        foreach (var waveform in Waveforms)
            for (var i = 0; i < length; i++) result[i] += waveform[i];
        for (var i = 0; i < length; i++) result[i] /= Waveforms.Count;
        return result;
    }
}
