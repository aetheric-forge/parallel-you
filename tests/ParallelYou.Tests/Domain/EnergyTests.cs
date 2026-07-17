using Moq;
using ParallelYou.Abstractions.Energy;

namespace ParallelYou.Tests.Domain;

public class EnergyTests
{
    [Fact]
    public void IEnergy_ShouldHaveCorrectId()
    {
        var id = Guid.NewGuid();
        var mockEnergy = new Mock<IEnergy>();
        mockEnergy.Setup(e => e.Id).Returns(id);

        Assert.Equal(id, mockEnergy.Object.Id);
    }

    [Fact]
    public void IEnergyRepresentation_ShouldHaveCorrectProperties()
    {
        var energyId = Guid.NewGuid();
        var provenance = new Provenance(ProvenanceKind.Declared, "Source", DateTimeOffset.UtcNow);
        var displayName = "Test Energy";

        var mockRepresentation = new Mock<IEnergyRepresentation>();
        mockRepresentation.Setup(r => r.EnergyId).Returns(energyId);
        mockRepresentation.Setup(r => r.Provenance).Returns(provenance);
        mockRepresentation.Setup(r => r.DisplayName).Returns(displayName);

        Assert.Equal(energyId, mockRepresentation.Object.EnergyId);
        Assert.Equal(provenance, mockRepresentation.Object.Provenance);
        Assert.Equal(displayName, mockRepresentation.Object.DisplayName);
    }
}
