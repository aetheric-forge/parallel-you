using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Person;

namespace ParallelYou.Tests.Domain;

public class ProvenanceTests
{
    [Fact]
    public void Provenance_Equality_ShouldBeValueBased()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var p1 = new Provenance(ProvenanceKind.Declared, "Source", timestamp);
        var p2 = new Provenance(ProvenanceKind.Declared, "Source", timestamp);
        var p3 = new Provenance(ProvenanceKind.Observed, "Source", timestamp);

        Assert.Equal(p1, p2);
        Assert.NotEqual(p1, p3);
    }
}
