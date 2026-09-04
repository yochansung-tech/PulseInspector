using PulseInspector.Application.Services;
using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Tests;

internal static class Program
{
    private static int Main()
    {
        try
        {
            TestFeatureOrder(); TestGroupMeanFeatures(); TestRowBasedCsvLoading(); TestRowBasedCsvEndToEnd();
            TestFeatureExtraction(); TestTrainingValidation(); TestMahalanobisTrainingAndInspection();
            TestNormalVsDefectivePulseDetection(); TestApplicationFacade(); FeatureDeviationTests.Run(); WinFormsSmokeTest.Run();
            Console.WriteLine("ALL TESTS PASSED"); return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("TEST FAILURE"); Console.Error.WriteLine(ex); return 1;
        }
    }

    private static void TestApplicationFacade()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pulseinspector-app-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "0\n1\n2\n1\n0\n");
            var application = new InspectionApplication();
            var group = application.LoadGroup(new[] { path }, 1e-6);
            Assert(group.RecordCount == 1, "Application facade did not load one waveform.");
            Assert(group.Records[0].Features["Peak"] > 0, "Application facade did not extract features.");
            var validation = application.ValidateTraining(new[] { group });
            Assert(!validation.IsValid, "A single group should not pass covariance training validation.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void TestFeatureOrder()
    {
        var expectedStatistical = new[] { "Charge", "FWHM", "Noise", "Peak", "RiseTime" };
        Assert(FeatureVector.StatisticalFeatureNames.SequenceEqual(expectedStatistical), "Statistical feature order changed.");
        Assert(FeatureVector.StatisticalFeatureCount == expectedStatistical.Length, "Statistical feature count changed.");
        var vector = FeatureVector.FromStatisticalArray(new[] { 1d, 2d, 3d, 4d, 5d });
        Assert(vector.ToStatisticalArray().SequenceEqual(new[] { 1d, 2d, 3d, 4d, 5d }), "Statistical array round-trip failed.");
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
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                group.AddWaveform(row.Samples, CreateFeatures(row.Samples.Max()), $"row-{index + 1}", row.SampleIntervalSeconds);
            }
            Assert(group.RecordCount == 2, "End-to-end GroupData record count is incorrect.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void TestFeatureExtraction()
    {
        var extractor = new FeatureExtractor();
        var waveform = new[] { 0d, 1d, 3d, 6d, 3d, 1d, 0d };
        var features = extractor.Extract(waveform, 1e-6);
        foreach (var name in FeatureVector.StatisticalFeatureNames) Assert(double.IsFinite(features[name]), $"Feature '{name}' is not finite.");
        Assert(features["Peak"] > 0, "Peak extraction failed."); Assert(features["Charge"] > 0, "Charge extraction failed.");
    }

    private static void TestTrainingValidation()
    {
        var vectors = Enumerable.Range(0, 7).Select(CreateTrainingFeatures).ToArray();
        var validation = new InspectionService().ValidateTraining(vectors);
        Assert(!validation.Issues.Any(i => i.Code.StartsWith("ERROR_", StringComparison.Ordinal)), "Valid training vectors were rejected by validation.");
    }

    private static void TestMahalanobisTrainingAndInspection()
    {
        var vectors = Enumerable.Range(0, 10).Select(CreateTrainingFeatures).ToArray();
        var service = new InspectionService(); var model = service.Train(vectors, 0.999);
        Assert(model.Mean.Length == FeatureVector.StatisticalFeatureCount, "Model feature dimension is incorrect.");
        Assert(model.InverseCovariance.GetLength(0) == FeatureVector.StatisticalFeatureCount, "Inverse covariance dimension is incorrect.");
        Assert(model.InverseCovariance.GetLength(1) == FeatureVector.StatisticalFeatureCount, "Inverse covariance dimension is incorrect.");
        Assert(model.Threshold > 0, "Mahalanobis threshold is invalid.");
        var result = service.Inspect(CreateTrainingFeatures(5), model);
        Assert(double.IsFinite(result.MahalanobisDistance), "Normal Mahalanobis distance is not finite.");
        Assert(result.MahalanobisDistance < model.Threshold, "Training-like sample was classified as defect.");
    }

    private static void TestNormalVsDefectivePulseDetection()
    {
        var training = Enumerable.Range(0, 12).Select(CreateTrainingFeatures).ToArray();
        var service = new InspectionService(); var model = service.Train(training, 0.99);
        var normal = CreateTrainingFeatures(6); var defect = CreateTrainingFeatures(100);
        defect["Peak"] *= 8; defect["Charge"] *= 8; defect["Noise"] *= 5;
        Assert(!service.Inspect(normal, model).IsDefect, "Normal synthetic sample was classified as defect.");
        Assert(service.Inspect(defect, model).IsDefect, "Strong synthetic defect was not detected.");
    }

    private static FeatureVector CreateTrainingFeatures(int index)
    {
        var x = index - 5.5; var f = new FeatureVector();
        f["Peak"] = 1.0 + 0.03 * x + 0.002 * x * x; f["Charge"] = 2.0e-6 + 0.12e-6 * x + 0.01e-6 * x * x;
        f["RiseTime"] = 4.0e-6 + 0.08e-6 * x; f["FWHM"] = 6.0e-6 + 0.12e-6 * x + 0.01e-6 * x * x;
        f["Noise"] = 0.03 + 0.003 * x; f["ZScore"] = 0; return f;
    }

    private static FeatureVector CreateFeatures(double value)
    {
        var f = new FeatureVector(); foreach (var name in FeatureVector.StatisticalFeatureNames) f[name] = value; f["ZScore"] = value; return f;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
