using ParallelYou.Abstractions.Plan;

namespace ParallelYou.Abstractions.Plan;

public interface IPlanningService
{
    Task<IPlan> PlanAsync(string subject, CancellationToken cancellationToken = default);
}
