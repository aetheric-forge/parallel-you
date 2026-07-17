using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using ParallelYou.Abstractions.Plan;
using ParallelYou.Models.Plan;
using ParallelYou.Services;

namespace ParallelYou.Services.Plan;

public class PlanningService(ILibrarian librarian) : ServiceBase, IPlanningService
{
    public async Task<IPlan> PlanAsync(string subject, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return new ParallelYou.Models.Plan.Plan(Guid.NewGuid());
    }
}
