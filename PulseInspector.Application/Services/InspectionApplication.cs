using PulseInspector.Application.Contracts;
using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Application.Services;

public sealed class InspectionApplication : IInspectionApplication
{
    private readonly FeatureExtractor _featureExtractor;

    public InspectionApplication(FeatureExtractor? featureExtractor = null)
    {
        _featureExtractor = featureExtractor ?? new FeatureExtractor();
    }

    public FeatureVector ExtractFeatures(IReadOnlyList<double> samples, double sampleIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return _featureExtractor.Extract(samples, sampleIntervalSeconds);
    }
}
