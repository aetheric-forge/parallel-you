using Moq;
using ParallelYou.Abstractions.Activity;

namespace ParallelYou.Tests.Domain;

public class ActivityTests
{
    [Fact]
    public void IActivity_ShouldHaveCorrectId()
    {
        var id = Guid.NewGuid();
        var mockActivity = new Mock<IActivity>();
        mockActivity.Setup(a => a.Id).Returns(id);

        Assert.Equal(id, mockActivity.Object.Id);
    }

    [Fact]
    public void IActivityRepresentation_ShouldHaveCorrectProperties()
    {
        var activityId = Guid.NewGuid();
        var provenance = new Provenance(ProvenanceKind.Declared, "Source", DateTimeOffset.UtcNow);
        var displayName = "Test Activity";

        var mockRepresentation = new Mock<IActivityRepresentation>();
        mockRepresentation.Setup(r => r.ActivityId).Returns(activityId);
        mockRepresentation.Setup(r => r.Provenance).Returns(provenance);
        mockRepresentation.Setup(r => r.DisplayName).Returns(displayName);

        Assert.Equal(activityId, mockRepresentation.Object.ActivityId);
        Assert.Equal(provenance, mockRepresentation.Object.Provenance);
        Assert.Equal(displayName, mockRepresentation.Object.DisplayName);
    }
}
