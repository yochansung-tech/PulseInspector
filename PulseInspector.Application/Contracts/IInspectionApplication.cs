using PulseInspector.Models;

namespace PulseInspector.Application.Contracts;

public interface IInspectionApplication
{
    FeatureVector ExtractFeatures(IReadOnlyList<double> samples, double sampleIntervalSeconds);
    GroupData LoadGroup(IEnumerable<string> filePaths, double sampleIntervalSeconds);
    IReadOnlyList<GroupData> LoadGroupsFromRows(IEnumerable<string> filePaths, double sampleIntervalSeconds);
    InspectionModel Train(IEnumerable<GroupData> normalGroups, double confidence);
    GroupInspectionResult Inspect(GroupData group, InspectionModel model, GroupDecisionPolicy decisionPolicy);
    IReadOnlyList<SubgroupInspectionResult> InspectSubgroups(GroupData group, InspectionModel model);
    IReadOnlyList<FeatureDeviation> AnalyzeDeviations(FeatureVector vector, InspectionModel model);
}
