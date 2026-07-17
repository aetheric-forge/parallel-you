using Moq;
using ParallelYou.Abstractions.Attention;

namespace ParallelYou.Tests.Domain;

public class AttentionTests
{
    [Fact]
    public void IAttention_ShouldHaveCorrectId()
    {
        var id = Guid.NewGuid();
        var mockAttention = new Mock<IAttention>();
        mockAttention.Setup(a => a.Id).Returns(id);

        Assert.Equal(id, mockAttention.Object.Id);
    }

    [Fact]
    public void IAttentionRepresentation_ShouldHaveCorrectProperties()
    {
        var attentionId = Guid.NewGuid();
        var provenance = new Provenance(ProvenanceKind.Declared, "Source", DateTimeOffset.UtcNow);
        var displayName = "Test Attention";

        var mockRepresentation = new Mock<IAttentionRepresentation>();
        mockRepresentation.Setup(r => r.AttentionId).Returns(attentionId);
        mockRepresentation.Setup(r => r.Provenance).Returns(provenance);
        mockRepresentation.Setup(r => r.DisplayName).Returns(displayName);

        Assert.Equal(attentionId, mockRepresentation.Object.AttentionId);
        Assert.Equal(provenance, mockRepresentation.Object.Provenance);
        Assert.Equal(displayName, mockRepresentation.Object.DisplayName);
    }
}
