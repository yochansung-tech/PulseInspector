namespace PulseInspector.Services;

public sealed class SubgroupDetectionOptions
{
    public double StartThreshold { get; init; }
    public double EndThreshold { get; init; }
    public int MinimumSamples { get; init; } = 3;
    public int EndConfirmationSamples { get; init; } = 2;
    public int MinimumGapSamples { get; init; }
    public double? Baseline { get; init; }
}

public sealed class DetectedSubgroup
{
    public int StartIndex { get; init; }
    public int EndIndex { get; init; }
    public double Baseline { get; init; }
    public int SampleCount => EndIndex - StartIndex + 1;

    public double[] Extract(double[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (StartIndex < 0 || EndIndex >= source.Length || EndIndex < StartIndex)
            throw new ArgumentOutOfRangeException(nameof(source));

        var result = new double[SampleCount];
        Array.Copy(source, StartIndex, result, 0, SampleCount);
        return result;
    }
}

public sealed class SubgroupDetector
{
    public IReadOnlyList<DetectedSubgroup> Detect(
        IReadOnlyList<double> samples,
        SubgroupDetectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(options);

        if (samples.Count < 2)
            throw new ArgumentException("At least two samples are required.", nameof(samples));
        if (!double.IsFinite(options.StartThreshold) || options.StartThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(options.StartThreshold));
        if (!double.IsFinite(options.EndThreshold) || options.EndThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(options.EndThreshold));
        if (options.EndThreshold > options.StartThreshold)
            throw new ArgumentException("EndThreshold must not exceed StartThreshold.", nameof(options));
        if (options.MinimumSamples < 1)
            throw new ArgumentOutOfRangeException(nameof(options.MinimumSamples));
        if (options.EndConfirmationSamples < 1)
            throw new ArgumentOutOfRangeException(nameof(options.EndConfirmationSamples));
        if (options.MinimumGapSamples < 0)
            throw new ArgumentOutOfRangeException(nameof(options.MinimumGapSamples));

        var baseline = options.Baseline ?? EstimateBaseline(samples);
        var result = new List<DetectedSubgroup>();
        var start = -1;
        var belowCount = 0;
        var lastEnd = -1;

        for (var i = 0; i < samples.Count; i++)
        {
            var excursion = Math.Abs(samples[i] - baseline);
            if (!double.IsFinite(excursion))
                throw new InvalidDataException("Waveform contains a non-finite sample.");

            if (start < 0)
            {
                if (excursion >= options.StartThreshold && i - lastEnd - 1 >= options.MinimumGapSamples)
                {
                    start = i;
                    belowCount = 0;
                }
                continue;
            }

            if (excursion <= options.EndThreshold)
            {
                belowCount++;
                if (belowCount >= options.EndConfirmationSamples)
                {
                    var end = i - belowCount;
                    AddIfValid(result, start, end, baseline, options.MinimumSamples);
                    lastEnd = end;
                    start = -1;
                    belowCount = 0;
                }
            }
            else
            {
                belowCount = 0;
            }
        }

        if (start >= 0)
            AddIfValid(result, start, samples.Count - 1, baseline, options.MinimumSamples);

        return result;
    }

    private static void AddIfValid(ICollection<DetectedSubgroup> result, int start, int end, double baseline, int minimumSamples)
    {
        if (end < start || end - start + 1 < minimumSamples)
            return;

        result.Add(new DetectedSubgroup
        {
            StartIndex = start,
            EndIndex = end,
            Baseline = baseline
        });
    }

    private static double EstimateBaseline(IReadOnlyList<double> samples)
    {
        var finite = samples.Where(double.IsFinite).ToArray();
        if (finite.Length == 0)
            throw new InvalidDataException("Waveform contains no finite samples.");

        // Estimate the baseline from the samples with the smallest absolute excursion,
        // while retaining their signed values. This avoids turning a negative baseline
        // into a positive number.
        var count = Math.Max(1, (int)Math.Ceiling(finite.Length * 0.10));
        var low = finite
            .OrderBy(x => Math.Abs(x))
            .Take(count)
            .OrderBy(x => x)
            .ToArray();

        return low[low.Length / 2];
    }
}
