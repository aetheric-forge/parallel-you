using Moq;
using ParallelYou.Abstractions.Person;

namespace ParallelYou.Tests.Domain;

public class PersonTests
{
    [Fact]
    public void IPerson_ShouldHaveCorrectId()
    {
        var id = Guid.NewGuid();
        var mockPerson = new Mock<IPerson>();
        mockPerson.Setup(p => p.Id).Returns(id);

        Assert.Equal(id, mockPerson.Object.Id);
    }

    [Fact]
    public void IPersonRepresentation_ShouldHaveCorrectProperties()
    {
        var personId = Guid.NewGuid();
        var provenance = new Provenance(ProvenanceKind.Declared, "Source", DateTimeOffset.UtcNow);
        var displayName = "Test Name";

        var mockRepresentation = new Mock<IPersonRepresentation>();
        mockRepresentation.Setup(r => r.PersonId).Returns(personId);
        mockRepresentation.Setup(r => r.Provenance).Returns(provenance);
        mockRepresentation.Setup(r => r.DisplayName).Returns(displayName);

        Assert.Equal(personId, mockRepresentation.Object.PersonId);
        Assert.Equal(provenance, mockRepresentation.Object.Provenance);
        Assert.Equal(displayName, mockRepresentation.Object.DisplayName);
    }
}
