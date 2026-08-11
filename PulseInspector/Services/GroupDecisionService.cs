using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class GroupDecisionService
{
    public bool IsDefect(
        IReadOnlyList<SubgroupInspectionResult> subgroupResults,
        GroupDecisionPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(subgroupResults);
        if (subgroupResults.Count == 0)
            throw new InvalidOperationException("A group must contain at least one subgroup.");

        policy ??= new GroupDecisionPolicy();
        policy.Validate();

        return policy.Rule switch
        {
            GroupDecisionRule.AnyDefectiveSubgroup => subgroupResults.Any(r => r.IsDefect),
            GroupDecisionRule.DefectiveSubgroupRate =>
                subgroupResults.Count(r => r.IsDefect) / (double)subgroupResults.Count >= policy.DefectiveSubgroupRateThreshold,
            GroupDecisionRule.MaximumMahalanobis =>
                subgroupResults.Max(r => r.MahalanobisDistance) > subgroupResults[0].Threshold,
            _ => throw new ArgumentOutOfRangeException(nameof(policy.Rule))
        };
    }

    public GroupInspectionResult CreateResult(
        GroupData group,
        InspectionResult meanResult,
        IReadOnlyList<SubgroupInspectionResult> subgroupResults,
        GroupDecisionPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(meanResult);
        ArgumentNullException.ThrowIfNull(subgroupResults);
        if (subgroupResults.Count == 0)
            throw new InvalidOperationException("A group must contain at least one subgroup.");

        policy ??= new GroupDecisionPolicy();
        policy.Validate();

        var defectCount = subgroupResults.Count(r => r.IsDefect);
        var defectRate = defectCount / (double)subgroupResults.Count;
        var maximumDistance = subgroupResults.Max(r => r.MahalanobisDistance);
        var finalDefect = IsDefect(subgroupResults, policy);
        var ruleText = policy.Rule switch
        {
            GroupDecisionRule.AnyDefectiveSubgroup => "any defective subgroup",
            GroupDecisionRule.DefectiveSubgroupRate => $"defective subgroup rate >= {policy.DefectiveSubgroupRateThreshold:P2}",
            GroupDecisionRule.MaximumMahalanobis => "maximum subgroup Mahalanobis distance exceeds threshold",
            _ => policy.Rule.ToString()
        };

        var message = finalDefect
            ? $"DEFECT by {ruleText}. {defectCount}/{subgroupResults.Count} subgroup(s) defective."
            : $"NORMAL by {ruleText}. {defectCount}/{subgroupResults.Count} subgroup(s) defective.";

        return new GroupInspectionResult(
            group.Id,
            finalDefect,
            group.SampleCount,
            meanResult.MahalanobisDistance,
            meanResult.Threshold,
            meanResult.Features,
            message,
            defectCount,
            defectRate,
            maximumDistance,
            policy.Rule);
    }
}
