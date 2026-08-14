using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Tests;

/// <summary>
/// Regression/scaffold for the production T1-T8 validation set.
/// The application test runner invokes this only when the real CSV fixtures
/// are present under TestData/RealValidation; CI remains deterministic without
/// requiring production measurement files in the repository.
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

        var loader = new CsvDataLoader();
        var groups = new List<GroupData>();
        foreach (var file in trainingFiles)
            groups.AddRange(loader.LoadGroups(file));

        var model = new GroupInspectionService().Train(groups, 0.999);
        if (!model.IsMultiModal)
            throw new InvalidOperationException("Real validation training did not produce two normal modes.");

        var total = model.NormalModes.Sum(mode => mode.SampleCount);
        if (total <= 0)
            throw new InvalidOperationException("Real validation model contains no training samples.");

        // The production dataset is expected to contain two known-normal
        // populations. Do not assert exact percentages here because the fixture
        // may be expanded over time; report the learned proportions instead.
        var proportions = model.NormalModes
            .Select(mode => (double)mode.SampleCount / total)
            .ToArray();

        if (proportions.Any(p => p <= 0 || p >= 1))
            throw new InvalidOperationException("Real validation model contains an empty normal population.");
    }
}
