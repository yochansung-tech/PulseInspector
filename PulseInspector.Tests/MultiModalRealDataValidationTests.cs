using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Tests;

/// <summary>
/// Regression/scaffold for the production T1-T8 validation set.
/// The test runs only when real CSV fixtures are present under TestData/RealValidation.
/// </summary>
internal static class MultiModalRealDataValidationTests
{
    public static void RunIfFixturesPresent()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "TestData", "RealValidation");
        if (!Directory.Exists(root))
            return;

        var trainingFiles = Directory
            .GetFiles(root, "*.csv", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileName(path).StartsWith("Training_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (trainingFiles.Length == 0)
            return;

        var loader = new CsvRowWaveformLoader();
        var extractor = new FeatureExtractor();
        var groups = new List<GroupData>();

        foreach (var file in trainingFiles)
        {
            var rows = loader.LoadRows(file, new CsvImportOptions
            {
                MeasurementPeriodSeconds = 2.56e-6
            });

            var group = new GroupData { Id = Path.GetFileNameWithoutExtension(file) };
            foreach (var waveform in rows)
            {
                var features = extractor.Extract(waveform.Samples, waveform.SampleIntervalSeconds);
                group.AddWaveform(
                    waveform.Samples,
                    features,
                    waveform.SourceName,
                    waveform.SampleIntervalSeconds,
                    waveform.HasExplicitTimeAxis);
            }

            groups.Add(group);
        }

        var model = new GroupInspectionService().Train(groups, 0.999);
        if (!model.IsMultiModal)
            throw new InvalidOperationException("Real validation training did not produce two normal modes.");

        var total = model.NormalModes.Sum(mode => mode.SampleCount);
        if (total <= 0)
            throw new InvalidOperationException("Real validation model contains no training samples.");

        var proportions = model.NormalModes
            .Select(mode => (double)mode.SampleCount / total)
            .ToArray();

        if (proportions.Any(p => p <= 0 || p >= 1))
            throw new InvalidOperationException("Real validation model contains an empty normal population.");
    }
}
