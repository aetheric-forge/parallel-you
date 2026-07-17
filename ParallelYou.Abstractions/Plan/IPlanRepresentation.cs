namespace ParallelYou.Abstractions.Plan;

public interface IPlanRepresentation
{
    Guid PlanId { get; }
    Provenance Provenance { get; }
    string? DisplayName { get; }
}
