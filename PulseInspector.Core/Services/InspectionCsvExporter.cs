using System.Globalization;
using System.Text;
using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class InspectionCsvExporter
{
    public void ExportGroupResult(string filePath, GroupInspectionResult result, IReadOnlyList<SubgroupInspectionResult>? subgroupResults = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath); ArgumentNullException.ThrowIfNull(result); var directory = Path.GetDirectoryName(Path.GetFullPath(filePath)); if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory); using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true)); writer.WriteLine(BuildGroupHeader()); writer.WriteLine(BuildGroupRow(result)); if (subgroupResults is null || subgroupResults.Count == 0) return; writer.WriteLine(); writer.WriteLine(BuildSubgroupHeader()); foreach (var subgroup in subgroupResults) writer.WriteLine(BuildSubgroupRow(subgroup));
    }
    public string ExportGroupResultToString(GroupInspectionResult result, IReadOnlyList<SubgroupInspectionResult>? subgroupResults = null)
    {
        ArgumentNullException.ThrowIfNull(result); var builder = new StringBuilder(); builder.AppendLine(BuildGroupHeader()); builder.AppendLine(BuildGroupRow(result)); if (subgroupResults is null || subgroupResults.Count == 0) return builder.ToString(); builder.AppendLine(); builder.AppendLine(BuildSubgroupHeader()); foreach (var subgroup in subgroupResults) builder.AppendLine(BuildSubgroupRow(subgroup)); return builder.ToString();
    }
    private static string BuildGroupHeader() => string.Join(',', new[] { "RecordType", "GroupId", "IsDefect", "RecordCount", "MahalanobisDistance", "Threshold", "DefectiveSubgroupCount", "DefectiveSubgroupRate", "MaximumSubgroupMahalanobisDistance", "DecisionRule", "Message", "Charge", "FWHM", "Noise", "Peak", "RiseTime", "ZScore" });
    private static string BuildGroupRow(GroupInspectionResult result) { var fields = new List<string> { "Group", result.GroupId, result.IsDefect.ToString(), result.SampleCount.ToString(CultureInfo.InvariantCulture), Format(result.MahalanobisDistance), Format(result.Threshold), result.DefectiveSubgroupCount.ToString(CultureInfo.InvariantCulture), Format(result.DefectiveSubgroupRate), Format(result.MaximumSubgroupMahalanobisDistance), result.DecisionRule.ToString(), result.Message }; fields.AddRange(FeatureValues(result.Features)); return string.Join(',', fields.Select(Csv)); }
    private static string BuildSubgroupHeader() => string.Join(',', new[] { "RecordType", "Index", "SourceName", "IsDefect", "MahalanobisDistance", "Threshold", "Reason", "Charge", "FWHM", "Noise", "Peak", "RiseTime", "ZScore" });
    private static string BuildSubgroupRow(SubgroupInspectionResult result) { var fields = new List<string> { "Subgroup", result.Index.ToString(CultureInfo.InvariantCulture), result.SourceName, result.IsDefect.ToString(), Format(result.MahalanobisDistance), Format(result.Threshold), result.Reason }; fields.AddRange(FeatureValues(result.Features)); return string.Join(',', fields.Select(Csv)); }
    private static IEnumerable<string> FeatureValues(FeatureVector features) { foreach (var name in FeatureVector.StatisticalFeatureNames) yield return Format(features[name]); yield return Format(features["ZScore"]); }
    private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string Csv(string value) { if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value; return '"' + value.Replace("\"", "\"\"") + '"'; }
}
