using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Tracking;

namespace ParallelYou.Models.Tracking;

public record TrackedState(
    Guid SubjectId,
    string Value,
    Provenance Provenance,
    decimal Confidence
) : ITrackedState;
