using Moq;
using ParallelYou.Abstractions.Intention;

namespace ParallelYou.Tests.Domain;

public class IntentionTests
{
    [Fact]
    public void IIntention_ShouldHaveCorrectId()
    {
        var id = Guid.NewGuid();
        var mockIntention = new Mock<IIntention>();
        mockIntention.Setup(i => i.Id).Returns(id);

        Assert.Equal(id, mockIntention.Object.Id);
    }

    [Fact]
    public void IIntentionRepresentation_ShouldHaveCorrectProperties()
    {
        var intentionId = Guid.NewGuid();
        var provenance = new Provenance(ProvenanceKind.Declared, "Source", DateTimeOffset.UtcNow);
        var description = "Test Description";

        var mockRepresentation = new Mock<IIntentionRepresentation>();
        mockRepresentation.Setup(r => r.IntentionId).Returns(intentionId);
        mockRepresentation.Setup(r => r.Provenance).Returns(provenance);
        mockRepresentation.Setup(r => r.Description).Returns(description);

        Assert.Equal(intentionId, mockRepresentation.Object.IntentionId);
        Assert.Equal(provenance, mockRepresentation.Object.Provenance);
        Assert.Equal(description, mockRepresentation.Object.Description);
    }
}
