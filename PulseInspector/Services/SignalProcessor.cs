namespace PulseInspector.Services;

public static class SignalProcessor
{
    public static double EstimateBaseline(IReadOnlyList<double> data, int count = 0)
    {
        if (data.Count == 0) return 0;
        count = count <= 0 ? Math.Max(1, data.Count / 10) : Math.Min(count, data.Count);
        var values = data.Take(count).OrderBy(x => x).ToArray();
        var mid = values.Length / 2;
        return values.Length % 2 == 0 ? (values[mid - 1] + values[mid]) / 2.0 : values[mid];
    }

    public static double[] RemoveBaseline(IReadOnlyList<double> data, double baseline) => data.Select(x => x - baseline).ToArray();

    public static double TrapezoidalIntegration(IReadOnlyList<double> data, double dt)
    {
        double q = 0;
        for (var i = 0; i < data.Count - 1; i++)
        {
            var a = Math.Max(0, data[i]);
            var b = Math.Max(0, data[i + 1]);
            q += (a + b) * 0.5 * dt;
        }
        return q;
    }

    public static double EstimateNoise(IReadOnlyList<double> data, int count = 0)
    {
        if (data.Count < 2) return 0;
        count = count <= 0 ? Math.Max(2, data.Count / 10) : Math.Min(count, data.Count);
        var segment = data.Take(count).ToArray();
        var mean = segment.Average();
        return Math.Sqrt(segment.Sum(x => (x - mean) * (x - mean)) / (segment.Length - 1));
    }

    public static (int Index, double Value) Peak(IReadOnlyList<double> data)
    {
        if (data.Count == 0) return (-1, 0);
        var index = 0;
        for (var i = 1; i < data.Count; i++) if (data[i] > data[index]) index = i;
        return (index, data[index]);
    }

    public static double RiseTime(IReadOnlyList<double> data, double dt, double low = 0.1, double high = 0.9)
    {
        if (data.Count == 0) return 0;
        var peak = Peak(data).Value;
        if (peak <= 0) return 0;
        var lo = peak * low; var hi = peak * high;
        var iLo = FirstCrossing(data, lo); var iHi = FirstCrossing(data, hi);
        return iLo >= 0 && iHi >= iLo ? (iHi - iLo) * dt : 0;
    }

    public static double Fwhm(IReadOnlyList<double> data, double dt)
    {
        if (data.Count == 0) return 0;
        var half = Peak(data).Value * 0.5;
        var left = FirstCrossing(data, half);
        var right = LastCrossing(data, half);
        return left >= 0 && right >= left ? (right - left) * dt : 0;
    }

    private static int FirstCrossing(IReadOnlyList<double> data, double threshold)
    {
        for (var i = 0; i < data.Count; i++) if (data[i] >= threshold) return i;
        return -1;
    }
    private static int LastCrossing(IReadOnlyList<double> data, double threshold)
    {
        for (var i = data.Count - 1; i >= 0; i--) if (data[i] >= threshold) return i;
        return -1;
    }
}
