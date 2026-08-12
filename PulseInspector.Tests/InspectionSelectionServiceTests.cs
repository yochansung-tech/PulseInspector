using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Tests;

internal static class InspectionSelectionServiceTests
{
    public static void Run()
    {
        var trainingGroups = new List<GroupData>();
        for (var i = 0; i < 10; i++)
        {
            var group = new GroupData();
            group.AddWaveform(new[] { 0d, 1d, 0d }, CreateFeatures(i), $"train-{i}-1", 1e-6);
            group.AddWaveform(new[] { 0d, 1d, 0d }, CreateFeatures(i + 0.1), $"train-{i}-2", 1e-6);
            trainingGroups.Add(group);
        }

        var model = new GroupInspectionService().Train(trainingGroups, 0.999);
        var inspectedGroup = new GroupData();
        inspectedGroup.AddWaveform(new[] { 0d, 2d, 0d }, CreateFeatures(4), "normal-row", 1e-6);
        inspectedGroup.AddWaveform(new[] { 0d, 8d, 0d }, CreateDefectiveFeatures(), "defect-row", 1e-6);

        var subgroupResults = new SubgroupInspectionService().Inspect(inspectedGroup, model);
        var service = new InspectionSelectionService();
        var rows = service.CreateRows(subgroupResults);

        Assert(rows.Count == 2, "Subgroup display row count is incorrect.");
        Assert(rows[0].Index == 1 && rows[1].Index == 2, "Subgroup display indices are incorrect.");
        Assert(rows[0].SourceName == "normal-row", "Normal subgroup source name was not preserved.");
        Assert(rows[1].SourceName == "defect-row", "Defective subgroup source name was not preserved.");
        Assert(rows[1].IsDefect, "Defective subgroup display state was not preserved.");

        var selected = service.Select(inspectedGroup, 1, subgroupResults, model);
        Assert(selected.Record.SourceName == "defect-row", "Selected subgroup record does not match the selected row.");
        Assert(selected.Record.Samples.SequenceEqual(new[] { 0d, 8d, 0d }), "Selected waveform does not match the subgroup row.");
        Assert(selected.Result is not null && selected.Result.IsDefect, "Selected subgroup result was not preserved.");
        Assert(selected.Deviations.Count == FeatureVector.StatisticalFeatureCount, "Selected subgroup deviations do not contain all statistical features.");
        Assert(selected.Deviations.Any(d => d.FeatureName == "Peak"), "Peak deviation is missing from the selected subgroup.");
        Assert(selected.Deviations.Any(d => d.FeatureName == "Charge"), "Charge deviation is missing from the selected subgroup.");

        var expected = new FeatureDeviationService().Analyze(selected.Record.Features, model);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert(selected.Deviations[i].FeatureName == expected[i].FeatureName, "Selected subgroup deviation ordering changed.");
            AssertNear(selected.Deviations[i].MahalanobisContribution, expected[i].MahalanobisContribution, 1e-12, "Mahalanobis contribution");
        }

        ExpectFailure(() => service.Select(inspectedGroup, -1, subgroupResults, model), "Negative subgroup index was accepted.");
        ExpectFailure(() => service.Select(inspectedGroup, inspectedGroup.RecordCount, subgroupResults, model), "Out-of-range subgroup index was accepted.");
    }

    private static FeatureVector CreateFeatures(double i)
    {
        var vector = new FeatureVector();
        vector["Charge"] = 10 + 0.10 * i;
        vector["FWHM"] = 20 + 0.05 * i;
        vector["Noise"] = 0.50 + 0.005 * i;
        vector["Peak"] = 100 + 0.20 * i;
        vector["RiseTime"] = 2 + 0.01 * i;
        vector["ZScore"] = 0.01 * i;
        return vector;
    }

    private static FeatureVector CreateDefectiveFeatures()
    {
        var vector = CreateFeatures(4);
        vector["Peak"] = 180;
        vector["Charge"] = 25;
        vector["FWHM"] = 35;
        vector["Noise"] = 2;
        vector["RiseTime"] = 5;
        vector["ZScore"] = 4;
        return vector;
    }

    private static void AssertNear(double actual, double expected, double tolerance, string name)
    {
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{name} mismatch. Expected {expected:G17}, actual {actual:G17}.");
    }

    private static void ExpectFailure(Action action, string message)
    {
        try
        {
            action();
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
