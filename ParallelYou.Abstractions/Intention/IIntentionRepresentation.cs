namespace ParallelYou.Abstractions.Intention;

public interface IIntentionRepresentation
{
    Guid IntentionId { get; }
    Provenance Provenance { get; }
    string Description { get; }
}
