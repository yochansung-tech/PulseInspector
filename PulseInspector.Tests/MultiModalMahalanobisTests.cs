using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Tests;

internal static class MultiModalMahalanobisTests
{
    public static void Run()
    {
        var trainingGroups = new List<GroupData>();

        // Two known-normal waveform populations. Mode 2 is intentionally a
        // minority population, matching the validated ~83% / ~17% production
        // training distribution.
        for (var i = 0; i < 60; i++)
        {
            var group = new GroupData();
            var mode = i < 50 ? 0 : 1;
            for (var j = 0; j < 4; j++)
                group.AddWaveform(new[] { 0d, 1d }, CreateFeatures(mode, i, j), $"train-{i}-{j}", 1e-6);
            trainingGroups.Add(group);
        }

        var service = new GroupInspectionService();
        var model = service.Train(trainingGroups, 0.999);

        Assert(model.IsTrained, "Multimodal model should be trained.");
        Assert(model.IsMultiModal, "Training should create two normal modes.");
        Assert(model.NormalModes.Count == 2, "Expected exactly two normal modes.");
        Assert(model.NormalModes.All(m => m.IsTrained), "Every normal mode should be complete.");
        Assert(model.NormalModes.Sum(m => m.SampleCount) == 240, "All subgroup training samples must participate in mode training.");
        Assert(model.NormalModes[0].Mean[FeatureVector.GetStatisticalIndex("FWHM")] <
               model.NormalModes[1].Mean[FeatureVector.GetStatisticalIndex("FWHM")],
               "Normal modes should have deterministic lower-FWHM/higher-FWHM ordering.");

        var normalMode1 = CreateFeatures(0, 61, 0);
        var normalMode2 = CreateFeatures(1, 61, 0);
        var defect = CreateFeatures(0, 61, 0);
        defect["Peak"] = 500;
        defect["Charge"] = 500;
        defect["FWHM"] = 500;
        defect["Noise"] = 100;
        defect["RiseTime"] = 100;

        var result1 = new InspectionService().Inspect(normalMode1, model);
        var result2 = new InspectionService().Inspect(normalMode2, model);
        var defectResult = new InspectionService().Inspect(defect, model);

        Assert(!result1.IsDefect, "Known-normal minority/majority mode sample must remain normal.");
        Assert(!result2.IsDefect, "Known-normal second mode sample must remain normal.");
        Assert(defectResult.IsDefect, "Clearly abnormal sample must remain defective.");
        Assert(defectResult.MahalanobisDistance > defectResult.Threshold, "Defect distance must exceed selected-mode threshold.");

        var deviations = new FeatureDeviationService().Analyze(normalMode2, model);
        Assert(deviations.Count == FeatureVector.StatisticalFeatureCount, "Multimodal deviation analysis must return all statistical features.");
        Assert(deviations.All(d => double.IsFinite(d.MahalanobisContribution)), "Multimodal deviation contribution must remain finite.");
    }

    private static FeatureVector CreateFeatures(int mode, int i, int j)
    {
        var v = new FeatureVector();
        var offset = (i % 5) * 0.02 + j * 0.005;
        if (mode == 0)
        {
            v["Charge"] = 10 + offset;
            v["FWHM"] = 0.60 + offset * 0.02;
            v["Noise"] = 0.50 + offset * 0.01;
            v["Peak"] = 100 + offset;
            v["RiseTime"] = 0.70 + offset * 0.02;
        }
        else
        {
            v["Charge"] = 20 + offset;
            v["FWHM"] = 1.20 + offset * 0.02;
            v["Noise"] = 0.80 + offset * 0.01;
            v["Peak"] = 160 + offset;
            v["RiseTime"] = 0.90 + offset * 0.02;
        }
        return v;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
