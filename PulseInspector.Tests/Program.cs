using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Tests;

internal static class Program
{
    private static int Main()
    {
        try
        {
            TestFeatureOrder();
            TestGroupMeanFeatures();
            TestRowBasedCsvLoading();
            TestGroupDecisionRules();
            TestMahalanobisTrainingAndInspection();
            Console.WriteLine("ALL TESTS PASSED");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("TEST FAILURE");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void TestFeatureOrder()
    {
        var expected = new[] { "Charge", "FWHM", "Noise", "Peak", "RiseTime", "ZScore" };
        Assert(FeatureVector.StatisticalFeatureNames.SequenceEqual(expected), "Statistical feature order changed.");

        var vector = FeatureVector.FromStatisticalArray(new[] { 1d, 2d, 3d, 4d, 5d, 6d });
        Assert(vector.ToStatisticalArray().SequenceEqual(new[] { 1d, 2d, 3d, 4d, 5d, 6d }), "Statistical array round-trip failed.");
    }

    private static void TestGroupMeanFeatures()
    {
        var group = new GroupData();
        group.AddWaveform(new[] { 0d, 1d }, CreateFeatures(1), "row1", 1e-6);
        group.AddWaveform(new[] { 2d, 3d }, CreateFeatures(3), "row2", 1e-6);
        var mean = group.MeanFeatures()!;

        foreach (var name in FeatureVector.StatisticalFeatureNames)
            Assert(Math.Abs(mean[name] - CreateFeatures(2)[name]) < 1e-12, $"Group mean mismatch for {name}.");
    }

    private static void TestRowBasedCsvLoading()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pulseinspector-test-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "1,2,3,4\n5,6,7,8\n");
            var loader = new CsvRowWaveformLoader();
            var rows = loader.LoadRows(path, new CsvImportOptions { SampleIntervalSeconds = 0.001 });
            Assert(rows.Count == 2, "Row-based CSV did not create two subgroups.");
            Assert(rows.All(r => r.Samples.Length == 4), "Row-based CSV sample count is incorrect.");
            Assert(rows.All(r => Math.Abs(r.SampleIntervalSeconds - 0.001) < 1e-15), "Configured sample interval was not preserved.");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void TestGroupDecisionRules()
    {
        var results = new[]
        {
            new SubgroupInspectionResult { Index = 1, SourceName = "1", MahalanobisDistance = 1, Threshold = 10, IsDefect = false },
            new SubgroupInspectionResult { Index = 2, SourceName = "2", MahalanobisDistance = 20, Threshold = 10, IsDefect = true }
        };
        var service = new GroupDecisionService();
        Assert(service.IsDefect(results, new GroupDecisionPolicy { Rule = GroupDecisionRule.AnyDefectiveSubgroup }), "Any-defective rule failed.");
        Assert(!service.IsDefect(results, new GroupDecisionPolicy { Rule = GroupDecisionRule.DefectiveSubgroupRate, DefectiveSubgroupRateThreshold = 0.75 }), "Defective-rate rule failed.");
        Assert(service.IsDefect(results, new GroupDecisionPolicy { Rule = GroupDecisionRule.MaximumMahalanobis }), "Maximum Mahalanobis rule failed.");
    }

    private static void TestMahalanobisTrainingAndInspection()
    {
        var groups = new List<GroupData>();
        for (var i = 0; i < 10; i++)
        {
            var group = new GroupData();
            group.AddWaveform(new[] { 0d, 1d }, CreateIndependentFeatures(i), $"g{i}-1", 1e-6);
            group.AddWaveform(new[] { 0d, 1d }, CreateIndependentFeatures(i), $"g{i}-2", 1e-6);
            groups.Add(group);
        }

        var service = new GroupInspectionService();
        var model = service.Train(groups, 0.999);
        Assert(model.Mean.Length == FeatureVector.StatisticalFeatureCount, "Model dimension is incorrect.");
        Assert(model.Threshold > 0, "Chi-square threshold was not calculated.");

        var normal = groups[0];
        var result = service.Inspect(normal, model);
        Assert(result.SubgroupResults.Count == 2, "Subgroup inspection count is incorrect.");
        Assert(result.SubgroupResults.All(r => double.IsFinite(r.MahalanobisDistance)), "Subgroup Mahalanobis distance is not finite.");
    }

    private static FeatureVector CreateFeatures(double value)
    {
        var vector = new FeatureVector();
        foreach (var name in FeatureVector.StatisticalFeatureNames)
            vector[name] = value;
        return vector;
    }

    private static FeatureVector CreateIndependentFeatures(double i)
    {
        var vector = new FeatureVector();
        vector["Charge"] = 10 + i;
        vector["FWHM"] = 20 + 2 * i + (i % 2) * 0.25;
        vector["Noise"] = 0.5 + 0.1 * i + (i % 3) * 0.03;
        vector["Peak"] = 100 + 3 * i + (i % 4) * 0.2;
        vector["RiseTime"] = 2 + 0.15 * i + (i % 2) * 0.04;
        vector["ZScore"] = 0;
        return vector;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
