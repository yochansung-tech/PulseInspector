namespace PulseInspector.Models;

public sealed record GroupInspectionResult(
    string GroupId,
    bool IsDefect,
    int SampleCount,
    double MahalanobisDistance,
    double Threshold,
    FeatureVector Features,
    string Message,
    int DefectiveSubgroupCount = 0,
    double DefectiveSubgroupRate = 0.0,
    double MaximumSubgroupMahalanobisDistance = 0.0,
    GroupDecisionRule DecisionRule = GroupDecisionRule.AnyDefectiveSubgroup);
