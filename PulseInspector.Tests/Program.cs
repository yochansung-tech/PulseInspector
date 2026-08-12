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
            TestRowBasedCsvEndToEnd();
            TestFeatureExtractionExpectedValues();
            TestRealisticPulseCsvFeatures();
            TestGroupDecisionRules();
            TestMahalanobisTrainingAndInspection();
            TestNormalVsDefectivePulseDetection();
            TestIndividualFeatureDefects();
            FeatureDeviationTests.Run();
            WinFormsSmokeTest.Run();
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
        var expectedStatistical = new[] { "Charge", "FWHM", "Noise", "Peak", "RiseTime" };
        Assert(FeatureVector.StatisticalFeatureNames.SequenceEqual(expectedStatistical), "Statistical feature order changed.");
        Assert(FeatureVector.StatisticalFeatureCount == expectedStatistical.Length, "Statistical feature count changed.");

        var vector = FeatureVector.FromStatisticalArray(new[] { 1d, 2d, 3d, 4d, 5d });
        Assert(vector.ToStatisticalArray().SequenceEqual(new[] { 1d, 2d, 3d, 4d, 5d }), "Statistical array round-trip failed.");

        // ZScore remains part of the complete/display feature set, but is intentionally
        // excluded from the statistical Mahalanobis feature vector because it is derived from Peak.
        Assert(FeatureVector.FeatureNames.Contains("ZScore"), "Diagnostic ZScore feature is missing.");
        Assert(!FeatureVector.StatisticalFeatureNames.Contains("ZScore"), "ZScore must not be part of the statistical feature vector.");
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
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void TestRowBasedCsvEndToEnd()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pulseinspector-e2e-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "0,1,2,1,0\n0,2,4,2,0\n");
            var loader = new CsvRowWaveformLoader();
            var rows = loader.LoadRows(path, new CsvImportOptions { SampleIntervalSeconds = 1e-6 });
            Assert(rows.Count == 2, "End-to-end row loading failed.");
            var group = new GroupData();
            foreach (var row in rows)
                group.AddWaveform(row.Samples, CreateFeatures(row.Samples.Max()), row.Source, row.SampleIntervalSeconds);
            Assert(group.RecordCount == 2, "End-to-end GroupData record count is incorrect.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static FeatureVector CreateFeatures(double value)
    {
        var f = new FeatureVector();
        foreach (var name in FeatureVector.StatisticalFeatureNames)
            f[name] = value;
        f["ZScore"] = value;
        return f;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
