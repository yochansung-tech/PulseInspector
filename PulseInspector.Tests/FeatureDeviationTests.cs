using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Tests;

internal static class FeatureDeviationTests
{
    public static void Run()
    {
        var trainingGroups = new List<GroupData>();
        for (var i = 0; i < 20; i++)
        {
            var group = new GroupData();
            group.AddWaveform(new[] { 0d, 1d }, CreateFeatures(i), $"train-{i}-1", 1e-6);
            group.AddWaveform(new[] { 0d, 1d }, CreateFeatures(i + 0.2), $"train-{i}-2", 1e-6);
            trainingGroups.Add(group);
        }
        var inspection = new GroupInspectionService();
        var model = inspection.Train(trainingGroups, 0.999);
        var defective = CreateFeatures(10.1);
        defective["Peak"] = 180;
        defective["Charge"] = 25;
        defective["FWHM"] = 35;
        defective["Noise"] = 2.0;
        defective["RiseTime"] = 5.0;
        defective["ZScore"] = 4.0;

        var deviations = new FeatureDeviationService().Analyze(defective, model);
        Assert(deviations.Count == FeatureVector.StatisticalFeatureCount, "Feature deviation count is incorrect.");
        Assert(deviations.Select(d => d.FeatureName).Distinct(StringComparer.Ordinal).Count() == FeatureVector.StatisticalFeatureCount, "Feature deviation names are not unique.");
        Assert(deviations.All(d => double.IsFinite(d.ZScore) && double.IsFinite(d.AbsoluteZScore)), "Feature Z-score contains a non-finite value.");
        Assert(deviations.All(d => double.IsFinite(d.MahalanobisContribution)), "Mahalanobis contribution contains a non-finite value.");
        Assert(deviations.All(d => d.AbsoluteZScore >= 0), "Absolute Z-score cannot be negative.");
        Assert(deviations.Zip(deviations.Skip(1), (a, b) => a.AbsoluteZScore >= b.AbsoluteZScore).All(x => x), "Feature deviations are not sorted by absolute Z-score.");

        var contributionSum = deviations.Sum(d => d.MahalanobisContribution);
        var distanceSquared = ComputeMahalanobisSquared(defective, model);
        AssertNear(contributionSum, distanceSquared, Math.Max(1e-9, Math.Abs(distanceSquared) * 1e-8), "Mahalanobis contribution sum");
        Assert(distanceSquared > model.Threshold, "Defective vector should exceed the configured Mahalanobis threshold.");
        Assert(deviations[0].AbsoluteZScore > 0, "Top feature deviation should have a non-zero Z-score.");
        Assert(deviations.Any(d => d.FeatureName == "Peak" && d.AbsoluteZScore > 1), "Peak deviation was not reflected in the feature explanation.");
        Assert(deviations.Any(d => d.FeatureName == "Charge" && d.AbsoluteZScore > 1), "Charge deviation was not reflected in the feature explanation.");

        FeatureDeviationGridTests.Run();
        InspectionSelectionServiceTests.Run();
        InspectionCsvExporterTests.Run();
        WinFormsSmokeTest.Run();
    }

    private static double ComputeMahalanobisSquared(FeatureVector vector, InspectionModel model)
    {
        var x = vector.ToStatisticalArray();
        var centered = new double[x.Length];
        for (var i = 0; i < x.Length; i++) centered[i] = x[i] - model.Mean[i];
        var weighted = new double[x.Length];
        for (var i = 0; i < x.Length; i++)
            for (var j = 0; j < x.Length; j++) weighted[i] += model.InverseCovariance[i, j] * centered[j];
        var sum = 0d;
        for (var i = 0; i < centered.Length; i++) sum += centered[i] * weighted[i];
        return sum;
    }

    private static FeatureVector CreateFeatures(double i)
    {
        var vector = new FeatureVector();
        vector["Charge"] = 10 + 0.10 * i;
        vector["FWHM"] = 20 + 0.05 * i;
        vector["Noise"] = 0.50 + 0.005 * i;
        vector["Peak"] = 100 + 0.20 * i;
        vector["RiseTime"] = 2.0 + 0.01 * i;
        vector["ZScore"] = 0.01 * i;
        return vector;
    }

    private static void AssertNear(double actual, double expected, double tolerance, string name)
    {
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{name} mismatch. Expected {expected:G17}, actual {actual:G17}, tolerance {tolerance:G17}.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
