using PulseInspector.Models;

namespace PulseInspector.Wpf.ViewModels;

public sealed class GroupViewModel
{
    public GroupViewModel(GroupData model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public GroupData Model { get; }
    public string Id => Model.Id;
    public int RecordCount => Model.RecordCount;
    public int SampleCount => Model.WaveformSampleCount;
    public bool IsDefective
    {
        get => Model.IsDefective;
        set => Model.IsDefective = value;
    }

    public string DisplayName => $"Group {Id[..Math.Min(8, Id.Length)]}  ({RecordCount} waveform{(RecordCount == 1 ? string.Empty : "s")})";
}

public sealed record FeatureValueViewModel(string Name, double Value);

public sealed record SubgroupResultViewModel(
    int Index,
    string SourceName,
    double MahalanobisDistance,
    double Threshold,
    bool IsDefect);
