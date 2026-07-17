namespace ParallelYou.Abstractions.Energy;

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
