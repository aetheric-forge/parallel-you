using Moq;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Tracking;
using ParallelYou.Services.Tracking;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Representations;

namespace ParallelYou.Tests.Services.Tracking;

public class TrackingServiceTests
{
    private readonly Mock<ILibrarian> _mockLibrarian = new();

    private record TestTrackedState(Guid SubjectId, string Value, Provenance Provenance, decimal Confidence) : ITrackedState;

    [Fact]
    public async Task TrackAsync_ShouldSucceed()
    {
        var service = new TrackingService(_mockLibrarian.Object);
        var state = new TestTrackedState(Guid.NewGuid(), "test-value", new Provenance(ProvenanceKind.Declared, "test-source", DateTimeOffset.Now), 0.9m);

        await service.TrackAsync(state);
        
        _mockLibrarian.Verify(l => l.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(), 
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(), 
            null, 
            null, 
            default), Times.Once);
    }

    [Fact]
    public async Task GetCurrentStateAsync_ShouldReturnNullInitially()
    {
        var service = new TrackingService(_mockLibrarian.Object);
        var subjectId = Guid.NewGuid();

        var result = await service.GetCurrentStateAsync(subjectId);

        Assert.Null(result);
    }
}
