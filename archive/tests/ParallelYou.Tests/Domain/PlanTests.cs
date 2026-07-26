using Moq;
using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Plan;

namespace ParallelYou.Tests.Domain;

public class PlanTests
{
    [Fact]
    public void IPlan_ShouldHaveCorrectId()
    {
        var id = Guid.NewGuid();
        var mockPlan = new Mock<IPlan>();
        mockPlan.Setup(p => p.Id).Returns(id);

        Assert.Equal(id, mockPlan.Object.Id);
    }

    [Fact]
    public void IPlanRepresentation_ShouldHaveCorrectProperties()
    {
        var planId = Guid.NewGuid();
        var provenance = new Provenance(ProvenanceKind.Declared, "Source", DateTimeOffset.UtcNow);
        var displayName = "Test Plan";

        var mockRepresentation = new Mock<IPlanRepresentation>();
        mockRepresentation.Setup(r => r.PlanId).Returns(planId);
        mockRepresentation.Setup(r => r.Provenance).Returns(provenance);
        mockRepresentation.Setup(r => r.DisplayName).Returns(displayName);

        Assert.Equal(planId, mockRepresentation.Object.PlanId);
        Assert.Equal(provenance, mockRepresentation.Object.Provenance);
        Assert.Equal(displayName, mockRepresentation.Object.DisplayName);
    }
}
