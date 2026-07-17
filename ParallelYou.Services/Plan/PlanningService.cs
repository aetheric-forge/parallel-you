using ParallelYou.Abstractions.Plan;

namespace ParallelYou.Services.Plan;

public class PlanningService : IPlanningService
{
    public Task<IPlan> PlanAsync(string subject, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IPlan>(new ParallelYou.Models.Plan.Plan(Guid.NewGuid()));
    }
}
