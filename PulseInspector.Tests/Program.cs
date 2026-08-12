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
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void TestRowBasedCsvEndToEnd()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pulseinspector-e2e-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "0,1,2,3,4,3,2,1\n0,2,4,6,8,6,4,2\n0,1.5,3,4.5,6,4.5,3,1.5\n");
            const double dt = 40e-9;
            var loader = new CsvRowWaveformLoader();
            var extractor = new FeatureExtractor();
            var rows = loader.LoadRows(path, new CsvImportOptions { SampleIntervalSeconds = dt });
            var group = new GroupData();
            foreach (var row in rows)
            {
                var features = extractor.Extract(row.Samples, row.SampleIntervalSeconds);
                group.AddWaveform(row.Samples, features, row.SourceName, row.SampleIntervalSeconds, row.HasExplicitTimeAxis);
            }
            Assert(rows.Count == 3, "End-to-end row CSV did not produce three rows.");
            Assert(group.RecordCount == 3, "End-to-end CSV rows were not preserved as three records.");
            Assert(group.WaveformSampleCount == 8, "End-to-end waveform sample count is incorrect.");
            Assert(group.Records.All(r => r.Samples.Length == 8), "A CSV row was altered during loading.");
            Assert(group.Records.All(r => Math.Abs(r.SampleIntervalSeconds - dt) < 1e-15), "Sample interval was not propagated to every subgroup.");
            Assert(group.Records.Select(r => r.SourceName).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 3, "Subgroup source identities were not preserved.");
            Assert(group.Records.All(r => double.IsFinite(r.Features["Peak"])), "Feature extraction produced an invalid Peak.");
            Assert(group.Records.All(r => double.IsFinite(r.Features["Charge"])), "Feature extraction produced an invalid Charge.");
            Assert(group.MeanFeatures() is not null, "Group mean feature generation failed after CSV import.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void TestFeatureExtractionExpectedValues()
    {
        var extractor = new FeatureExtractor();
        const double dt = 1e-6;
        var samples = new[] { 0d, 1d, 2d, 1d, 0d };
        var features = extractor.Extract(samples, dt);
        AssertNear(features["Peak"], 2d, 1e-12, "Peak");
        AssertNear(features["Charge"], 4e-6, 1e-18, "Charge");
        AssertNear(features["RiseTime"], 1e-6, 1e-18, "RiseTime");
        AssertNear(features["FWHM"], 2e-6, 1e-18, "FWHM");
        AssertNear(features["Noise"], 0d, 1e-12, "Noise");
    }

    private static void TestRealisticPulseCsvFeatures()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "RealisticPulse_01.csv");
        Assert(File.Exists(path), "Realistic pulse test fixture was not copied to the test output.");
        const double dt = 40e-9;
        var loader = new CsvRowWaveformLoader();
        var extractor = new FeatureExtractor();
        var rows = loader.LoadRows(path, new CsvImportOptions { SampleIntervalSeconds = dt });
        Assert(rows.Count == 5, "Realistic pulse fixture must contain five subgroups.");
        Assert(rows.All(r => r.Samples.Length == 22), "Realistic pulse fixture must contain 22 samples per subgroup.");
        var features = rows.Select(r => extractor.Extract(r.Samples, r.SampleIntervalSeconds)).ToList();
        var peaks = features.Select(f => f["Peak"]).ToArray();
        var charges = features.Select(f => f["Charge"]).ToArray();
        var riseTimes = features.Select(f => f["RiseTime"]).ToArray();
        var fwhms = features.Select(f => f["FWHM"]).ToArray();
        var noises = features.Select(f => f["Noise"]).ToArray();
        Assert(peaks.All(double.IsFinite), "Realistic pulse Peak contains a non-finite value.");
        Assert(charges.All(double.IsFinite), "Realistic pulse Charge contains a non-finite value.");
        Assert(riseTimes.All(double.IsFinite), "Realistic pulse RiseTime contains a non-finite value.");
        Assert(fwhms.All(double.IsFinite), "Realistic pulse FWHM contains a non-finite value.");
        Assert(noises.All(double.IsFinite), "Realistic pulse Noise contains a non-finite value.");
        Assert(peaks[4] > peaks[0] && peaks[4] > peaks[1] && peaks[4] > peaks[2] && peaks[4] > peaks[3], "Highest-amplitude row was not identified by Peak.");
        Assert(charges[4] > charges[0] && charges[4] > charges[1] && charges[4] > charges[2] && charges[4] > charges[3], "Highest-amplitude row was not identified by Charge.");
        var fwhmMin = fwhms.Min();
        var fwhmMax = fwhms.Max();
        Assert(fwhmMin > 0 && fwhmMax / fwhmMin < 1.5, "Realistic pulse FWHM spread is unexpectedly large.");
        Assert(noises.Max() < peaks.Min() * 0.2, "Realistic pulse noise is unexpectedly large relative to Peak.");
        Assert(charges.All(q => q > 0), "Realistic pulse Charge must be positive.");
    }

    private static void TestNormalVsDefectivePulseDetection()
    {
        var trainingGroups = new List<GroupData>();
        for (var i = 0; i < 20; i++)
        {
            var group = new GroupData();
            var f1 = CreateTrainingFeatures(i);
            var f2 = CreateTrainingFeatures(i + 0.2);
            group.AddWaveform(new[] { 0d, 1d }, f1, $"train-{i}-1", 1e-6);
            group.AddWaveform(new[] { 0d, 1d }, f2, $"train-{i}-2", 1e-6);
            trainingGroups.Add(group);
        }
        var inspection = new GroupInspectionService();
        var model = inspection.Train(trainingGroups, 0.999);
        Assert(model.Threshold > 0, "Training did not produce a positive Mahalanobis threshold.");

        var normalGroup = new GroupData();
        normalGroup.AddWaveform(new[] { 0d, 1d }, CreateTrainingFeatures(10.1), "normal-1", 1e-6);
        normalGroup.AddWaveform(new[] { 0d, 1d }, CreateTrainingFeatures(10.2), "normal-2", 1e-6);
        var normalResult = inspection.Inspect(normalGroup, model);
        Assert(normalResult.DefectiveSubgroupCount == 0, "A normal group was classified as defective.");
        Assert(normalResult.DefectiveSubgroupRate == 0, "Normal group defect rate should be zero.");

        var defectiveGroup = new GroupData();
        defectiveGroup.AddWaveform(new[] { 0d, 1d }, CreateTrainingFeatures(10.1), "defect-normal", 1e-6);
        defectiveGroup.AddWaveform(new[] { 0d, 1d }, CreateDefectiveFeatures(), "defect-outlier", 1e-6);
        var defectiveResult = inspection.Inspect(defectiveGroup, model);
        Assert(defectiveResult.DefectiveSubgroupCount >= 1, "The deliberately defective subgroup was not detected.");
        Assert(defectiveResult.DefectiveSubgroupRate > 0, "Defective group did not receive a positive defect rate.");
        Assert(defectiveResult.MaximumSubgroupMahalanobisDistance > model.Threshold, "Defective subgroup did not exceed the Mahalanobis threshold.");

        var subgroupResults = new SubgroupInspectionService().Inspect(defectiveGroup, model);
        Assert(subgroupResults.Count == 2, "Defective group subgroup count is incorrect.");
        Assert(subgroupResults.Any(r => r.IsDefect), "No subgroup was marked defective.");
    }

    private static void TestGroupDecisionRules()
    {
        var results = new[]
        {
            new SubgroupInspectionResult { Index = 1, SourceName = "1", Features = CreateFeatures(1), MahalanobisDistance = 1, Threshold = 10, IsDefect = false },
            new SubgroupInspectionResult { Index = 2, SourceName = "2", Features = CreateFeatures(2), MahalanobisDistance = 20, Threshold = 10, IsDefect = true }
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
        Assert(result.DefectiveSubgroupCount >= 0, "Group inspection returned an invalid subgroup defect count.");
        Assert(result.DefectiveSubgroupRate >= 0 && result.DefectiveSubgroupRate <= 1, "Group inspection returned an invalid subgroup defect rate.");
        Assert(double.IsFinite(result.MaximumSubgroupMahalanobisDistance), "Maximum subgroup Mahalanobis distance is not finite.");
        var subgroupService = new SubgroupInspectionService();
        var subgroupResults = subgroupService.Inspect(normal, model);
        Assert(subgroupResults.Count == 2, "Subgroup inspection count is incorrect.");
        Assert(subgroupResults.All(r => double.IsFinite(r.MahalanobisDistance)), "Subgroup Mahalanobis distance is not finite.");
        Assert(subgroupResults.All(r => r.Features is not null), "Subgroup inspection did not preserve FeatureVector.");
    }

    private static void TestIndividualFeatureDefects()
    {
        var trainingGroups = new List<GroupData>();
        for (var i = 0; i < 20; i++)
        {
            var group = new GroupData();
            group.AddWaveform(new[] { 0d, 1d }, CreateTrainingFeatures(i), $"train-{i}", 1e-6);
            trainingGroups.Add(group);
        }
        var inspection = new GroupInspectionService();
        var model = inspection.Train(trainingGroups, 0.999);
        foreach (var featureName in new[] { "Peak", "Charge", "FWHM", "RiseTime", "Noise" })
        {
            var vector = CreateTrainingFeatures(10);
            vector[featureName] *= 2.5;
            var group = new GroupData();
            group.AddWaveform(new[] { 0d, 1d }, vector, $"defect-{featureName}", 1e-6);
            var result = inspection.Inspect(group, model);
            Assert(result.DefectiveSubgroupCount >= 1, $"{featureName} defect was not detected.");
        }
    }

    private static FeatureVector CreateTrainingFeatures(double i)
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

    private static FeatureVector CreateDefectiveFeatures()
    {
        var vector = CreateTrainingFeatures(10.1);
        vector["Peak"] = 180;
        vector["Charge"] = 25;
        vector["FWHM"] = 35;
        vector["Noise"] = 2.0;
        vector["RiseTime"] = 5.0;
        vector["ZScore"] = 4.0;
        return vector;
    }

    private static FeatureVector CreateFeatures(double value)
    {
        var vector = new FeatureVector();
        foreach (var name in FeatureVector.StatisticalFeatureNames) vector[name] = value;
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
