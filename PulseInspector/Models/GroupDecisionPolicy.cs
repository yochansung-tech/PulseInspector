namespace PulseInspector.Models;

public enum GroupDecisionRule
{
    AnyDefectiveSubgroup,
    DefectiveSubgroupRate,
    MaximumMahalanobis
}

public sealed record GroupDecisionPolicy(
    GroupDecisionRule Rule = GroupDecisionRule.AnyDefectiveSubgroup,
    double DefectiveSubgroupRateThreshold = 0.0)
{
    public void Validate()
    {
        if (!double.IsFinite(DefectiveSubgroupRateThreshold) ||
            DefectiveSubgroupRateThreshold < 0.0 ||
            DefectiveSubgroupRateThreshold > 1.0)
            throw new ArgumentOutOfRangeException(nameof(DefectiveSubgroupRateThreshold), "Defect rate threshold must be between 0 and 1.");
    }
}
