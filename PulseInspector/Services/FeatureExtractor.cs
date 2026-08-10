using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class FeatureExtractor
{
    public FeatureVector Extract(IReadOnlyList<double> waveform, double sampleIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(waveform);
        if (sampleIntervalSeconds <= 0 || !double.IsFinite(sampleIntervalSeconds))
            throw new ArgumentOutOfRangeException(nameof(sampleIntervalSeconds));

        var baseline = SignalProcessor.EstimateBaseline(waveform);
        var corrected = SignalProcessor.RemoveBaseline(waveform, baseline);
        var peak = SignalProcessor.Peak(corrected).Value;
        var noise = SignalProcessor.EstimateNoise(corrected);
        var features = new FeatureVector();
        features["Peak"] = peak;
        features["Charge"] = SignalProcessor.TrapezoidalIntegration(corrected, sampleIntervalSeconds);
        features["RiseTime"] = SignalProcessor.RiseTime(corrected, sampleIntervalSeconds);
        features["FWHM"] = SignalProcessor.Fwhm(corrected, sampleIntervalSeconds);
        features["Noise"] = noise;
        return features;
    }
}
