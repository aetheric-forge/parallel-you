using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Plan;

namespace ParallelYou.Models.Plan;

public abstract class PlanBase : IPlan
{
    public Guid Id { get; init; }

    protected PlanBase(Guid id)
    {
        Id = id;
    }
}
