using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Intention;

namespace ParallelYou.Models.Intention;

public abstract class IntentionRepresentationBase : IIntentionRepresentation
{
    public Guid IntentionId { get; init; }
    public Provenance Provenance { get; init; }
    public string Description { get; init; }

    protected IntentionRepresentationBase(Guid intentionId, Provenance provenance, string description)
    {
        IntentionId = intentionId;
        Provenance = provenance;
        Description = description;
    }
}
