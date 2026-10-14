using System.Collections.Immutable;

namespace ParallelYou.Web.Provisioning;

// Private consumer artifact, delivered independently of the public institution catalog.
public sealed record ConsumerConfiguration(int Version, string Module, string DeploymentId,
    string InstitutionId, string InstitutionVersion, string Environment, string PlanId,
    ImmutableDictionary<string, string> Settings)
{
    public string InstitutionName { get; init; } = "Parallel You";
    public string Description { get; init; } = "A private practice for preserving observations, reflection, and chosen direction.";
    public override string ToString() => $"ConsumerConfiguration {{ DeploymentId = {DeploymentId}, Settings = [redacted] }}";
}
