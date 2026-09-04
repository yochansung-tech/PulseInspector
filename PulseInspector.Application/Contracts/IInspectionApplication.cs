using PulseInspector.Models;

namespace PulseInspector.Application.Contracts;

public interface IInspectionApplication
{
    FeatureVector ExtractFeatures(IReadOnlyList<double> samples, double sampleIntervalSeconds);
}
