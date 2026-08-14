using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Tests;

internal static class MultiModalPerformanceValidationTests
{
    public static void Run()
    {
        var groups = BuildKnownNormalGroups();
        var service = new GroupInspectionService();
        var model = service.Train(groups, 0.999);

        if (!model.IsMultiModal || model.NormalModes.Count != 2)
            throw new InvalidOperationException("Expected two normal modes for performance validation.");

        var normalA = BuildModeA(5000);
        var normalB = BuildModeB(5000);
        var defects = BuildDefects(1000);

        var normalResults = normalA.Concat(normalB)
            .Select(v => new InspectionService().Inspect(v, model))
            .ToArray();
        var defectResults = defects
            .Select(v => new InspectionService().Inspect(v, model))
            .ToArray();

        var falsePositiveRate = normalResults.Count(r => r.IsDefect) / (double)normalResults.Length;
        var truePositiveRate = defectResults.Count(r => r.IsDefect) / (double)defectResults.Length;

        // The validation set is intentionally well separated. These bounds are
        // regression guards against accidentally reverting to a single-mode model.
        if (falsePositiveRate > 0.005)
            throw new InvalidOperationException($"False-positive rate unexpectedly high: {falsePositiveRate:P4}.");
        if (truePositiveRate < 0.995)
            throw new InvalidOperationException($"True-positive rate unexpectedly low: {truePositiveRate:P4}.");

        var normalDistances = normalResults.Select(r => r.MahalanobisDistance).OrderBy(x => x).ToArray();
        var defectDistances = defectResults.Select(r => r.MahalanobisDistance).OrderBy(x => x).ToArray();
        if (defectDistances[0] <= normalDistances[^1])
            throw new InvalidOperationException("Defect/normal Mahalanobis distributions overlap in the regression validation set.");
    }

    private static List<GroupData> BuildKnownNormalGroups()
    {
        var groups = new List<GroupData>();
        for (var i = 0; i < 200; i++)
        {
            var group = new GroupData();
            var mode = i < 166 ? 0 : 1;
            for (var j = 0; j < 10; j++)
                group.AddWaveform(new[] { 0d, 1d }, Create(mode, i, j), $"normal-{i}-{j}", 1e-6);
            groups.Add(group);
        }
        return groups;
    }

    private static List<FeatureVector> BuildModeA(int count) => Enumerable.Range(0, count).Select(i => Create(0, i, 0)).ToList();
    private static List<FeatureVector> BuildModeB(int count) => Enumerable.Range(0, count).Select(i => Create(1, i, 0)).ToList();

    private static List<FeatureVector> BuildDefects(int count)
    {
        return Enumerable.Range(0, count).Select(i =>
        {
            var v = Create(i % 2, i, 0);
            v["Peak"] *= 1.8;
            v["Charge"] *= 1.7;
            v["Noise"] *= 2.5;
            v["RiseTime"] *= 1.5;
            return v;
        }).ToList();
    }

    private static FeatureVector Create(int mode, int i, int j)
    {
        var v = new FeatureVector();
        var noise = ((i * 17 + j * 7) % 100) / 10000.0;
        if (mode == 0)
        {
            v["Charge"] = 2.00 + noise;
            v["FWHM"] = 1.00 + noise;
            v["Noise"] = 0.020 + noise / 5;
            v["Peak"] = 0.40 + noise;
            v["RiseTime"] = 0.80 + noise;
        }
        else
        {
            v["Charge"] = 2.70 + noise;
            v["FWHM"] = 1.40 + noise;
            v["Noise"] = 0.030 + noise / 5;
            v["Peak"] = 0.48 + noise;
            v["RiseTime"] = 1.00 + noise;
        }
        return v;
    }
}
