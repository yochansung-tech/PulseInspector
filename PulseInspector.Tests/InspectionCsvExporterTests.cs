using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Tests;

internal static class InspectionCsvExporterTests
{
    public static void Run()
    {
        var normal = CreateFeatures(10);
        var group = new GroupInspectionResult(
            "G-01",
            true,
            3,
            25.5,
            22.457744,
            normal,
            "Group contains a defective subgroup.",
            1,
            1.0 / 3.0,
            25.5,
            GroupDecisionRule.AnyDefectiveSubgroup);

        var subgroups = new[]
        {
            new SubgroupInspectionResult
            {
                Index = 1,
                SourceName = "row,1",
                Features = normal,
                MahalanobisDistance = 25.5,
                Threshold = 22.457744,
                IsDefect = true
            }
        };

        var exporter = new InspectionCsvExporter();
        var csv = exporter.ExportGroupResultToString(group, subgroups);
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert(lines.Length == 4, "CSV export should contain group header, group row, subgroup header and subgroup row.");
        Assert(lines[0].StartsWith("RecordType,GroupId,IsDefect,RecordCount,"), "Group CSV header is incorrect.");
        Assert(lines[0].EndsWith("Charge,FWHM,Noise,Peak,RiseTime,ZScore"), "Feature order in group CSV is incorrect.");
        Assert(lines[1].StartsWith("Group,G-01,True,3,"), "Group CSV row is incorrect.");
        Assert(lines[2].StartsWith("RecordType,Index,SourceName,IsDefect,"), "Subgroup CSV header is incorrect.");
        Assert(lines[3].Contains("\"row,1\""), "CSV quoting for comma-containing source name failed.");
        Assert(lines[3].EndsWith(",10,10,10,10,10,10"), "Subgroup feature values are not exported in stable order.");

        var path = Path.Combine(Path.GetTempPath(), $"pulseinspector-export-{Guid.NewGuid():N}.csv");
        try
        {
            exporter.ExportGroupResult(path, group, subgroups);
            Assert(File.Exists(path), "CSV export file was not created.");
            var fileText = File.ReadAllText(path);
            Assert(fileText == csv, "File export and string export differ.");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static FeatureVector CreateFeatures(double value)
    {
        var vector = new FeatureVector();
        foreach (var name in FeatureVector.StatisticalFeatureNames)
            vector[name] = value;

        // ZScore is diagnostic-only, but it is intentionally exported after
        // the five statistical features. Give the test fixture a deterministic
        // value so the complete exported feature sequence is validated.
        vector["ZScore"] = value;
        return vector;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
