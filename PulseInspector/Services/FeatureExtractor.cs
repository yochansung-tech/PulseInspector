using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class FeatureExtractor
{
    public double SampleInterval { get; set; } = 2.56e-6 / 64.0;

    public FeatureVector Extract(IReadOnlyList<double> waveform)
    {
        var baseline = SignalProcessor.EstimateBaseline(waveform);
        var corrected = SignalProcessor.RemoveBaseline(waveform, baseline);
        var peak = SignalProcessor.Peak(corrected).Value;
        var noise = SignalProcessor.EstimateNoise(corrected);
        var features = new FeatureVector();
        features["Peak"] = peak;
        features["Charge"] = SignalProcessor.TrapezoidalIntegration(corrected, SampleInterval);
        features["RiseTime"] = SignalProcessor.RiseTime(corrected, SampleInterval);
        features["FWHM"] = SignalProcessor.Fwhm(corrected, SampleInterval);
        features["Noise"] = noise;
        return features;
    }
}
