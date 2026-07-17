namespace ParallelYou.Abstractions.Plan;

public enum ProvenanceKind
{
    Declared,
    Observed,
    Inferred,
    Assumed,
}

public record Provenance(
    ProvenanceKind Kind,
    string? Source,
    DateTimeOffset Timestamp
);
