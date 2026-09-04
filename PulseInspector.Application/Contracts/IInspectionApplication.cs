using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Application.Contracts;

public interface IInspectionApplication
{
    FeatureVector ExtractFeatures(IReadOnlyList<double> samples, double sampleIntervalSeconds);
    GroupData LoadGroup(IEnumerable<string> filePaths, double sampleIntervalSeconds);
    IReadOnlyList<GroupData> LoadGroupsFromRows(IEnumerable<string> filePaths, double sampleIntervalSeconds);
    TrainingValidationResult ValidateTraining(IEnumerable<GroupData> normalGroups);
    InspectionModel Train(IEnumerable<GroupData> normalGroups, double confidence);
    GroupInspectionResult Inspect(GroupData group, InspectionModel model, GroupDecisionPolicy decisionPolicy);
    IReadOnlyList<SubgroupInspectionResult> InspectSubgroups(GroupData group, InspectionModel model);
    IReadOnlyList<FeatureDeviation> AnalyzeDeviations(FeatureVector vector, InspectionModel model);
    void ExportInspectionResult(string filePath, GroupInspectionResult result, IReadOnlyList<SubgroupInspectionResult>? subgroupResults = null);
}
