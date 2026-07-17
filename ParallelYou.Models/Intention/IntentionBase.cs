using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Intention;

namespace ParallelYou.Models.Intention;

public abstract class IntentionBase : IIntention
{
    public Guid Id { get; init; }

    protected IntentionBase(Guid id)
    {
        Id = id;
    }
}
