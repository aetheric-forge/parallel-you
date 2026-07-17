namespace ParallelYou.Abstractions.Tracking;

public interface ITrackedState
{
    Guid SubjectId { get; }
    string Value { get; }
    DateTime EffectiveTime { get; }
    string? Context { get; }
    decimal Confidence { get; }
}
