using ParallelYou.Abstractions.Plan;

namespace ParallelYou.Models.Plan;

public abstract class PlanRepresentationBase : IPlanRepresentation
{
    public Guid PlanId { get; init; }
    public Provenance Provenance { get; init; }
    public string? DisplayName { get; init; }

    protected PlanRepresentationBase(Guid planId, Provenance provenance, string? displayName)
    {
        PlanId = planId;
        Provenance = provenance;
        DisplayName = displayName;
    }
}
