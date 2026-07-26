using Moq;
using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Time;

namespace ParallelYou.Tests.Domain;

public class TimeTests
{
    [Fact]
    public void ITime_ShouldHaveCorrectId()
    {
        var id = Guid.NewGuid();
        var mockTime = new Mock<ITime>();
        mockTime.Setup(t => t.Id).Returns(id);

        Assert.Equal(id, mockTime.Object.Id);
    }

    [Fact]
    public void ITimeRepresentation_ShouldHaveCorrectProperties()
    {
        var timeId = Guid.NewGuid();
        var provenance = new Provenance(ProvenanceKind.Declared, "Source", DateTimeOffset.UtcNow);
        var displayName = "Test Time";

        var mockRepresentation = new Mock<ITimeRepresentation>();
        mockRepresentation.Setup(r => r.TimeId).Returns(timeId);
        mockRepresentation.Setup(r => r.Provenance).Returns(provenance);
        mockRepresentation.Setup(r => r.DisplayName).Returns(displayName);

        Assert.Equal(timeId, mockRepresentation.Object.TimeId);
        Assert.Equal(provenance, mockRepresentation.Object.Provenance);
        Assert.Equal(displayName, mockRepresentation.Object.DisplayName);
    }
}
