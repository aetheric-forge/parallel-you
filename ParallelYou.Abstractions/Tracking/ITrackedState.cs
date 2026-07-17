namespace ParallelYou.Abstractions.Tracking;

using ParallelYou.Abstractions;

public interface ITrackedState
{
    Guid SubjectId { get; }
    string Value { get; }
    Provenance Provenance { get; }
    decimal Confidence { get; }
}
