using Moq;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Tracking;
using ParallelYou.Services.Tracking;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Representations;
using ParallelYou.Tests;

namespace ParallelYou.Tests.Services.Tracking;

public class TrackingServiceTests : TestBase
{
    private record TestTrackedState(Guid SubjectId, string Value, Provenance Provenance, decimal Confidence) : ITrackedState;

    [Fact]
    public async Task TrackAsync_ShouldSucceed()
    {
        var service = new TrackingService(MockLibrarian.Object);
        var state = new TestTrackedState(Guid.NewGuid(), "test-value", new Provenance(ProvenanceKind.Declared, "test-source", DateTimeOffset.Now), 0.9m);

        await service.TrackAsync(state);
        
        MockLibrarian.Verify(l => l.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(), 
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(), 
            null, 
            null, 
            default), Times.Once);
    }

    [Fact]
    public async Task GetCurrentStateAsync_ShouldReturnNullInitially()
    {
        var service = new TrackingService(MockLibrarian.Object);
        var subjectId = Guid.NewGuid();

        var result = await service.GetCurrentStateAsync(subjectId);

        Assert.Null(result);
    }

    [Fact]
    public async Task TrackAsync_WithNullState_ShouldThrowArgumentNullException()
    {
        var service = new TrackingService(MockLibrarian.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.TrackAsync(null!));
    }

    [Fact]
    public async Task TrackAsync_ThenGetCurrentStateAsync_ShouldReturnTrackedState()
    {
        var service = new TrackingService(MockLibrarian.Object);
        var subjectId = Guid.NewGuid();
        var state = new TestTrackedState(subjectId, "value1", new Provenance(ProvenanceKind.Declared, "test-source", DateTimeOffset.Now), 0.9m);

        await service.TrackAsync(state);
        
        // This test will fail until GetCurrentStateAsync is implemented.
        var result = await service.GetCurrentStateAsync(subjectId);
        
        Assert.NotNull(result);
        Assert.Equal("value1", result!.Value);
    }

    [Fact]
    public async Task TrackAsync_WithNewerState_ShouldReplaceCurrentState()
    {
        var service = new TrackingService(MockLibrarian.Object);
        var subjectId = Guid.NewGuid();
        var state1 = new TestTrackedState(subjectId, "value1", new Provenance(ProvenanceKind.Declared, "test-source", DateTimeOffset.Now), 0.9m);
        var state2 = new TestTrackedState(subjectId, "value2", new Provenance(ProvenanceKind.Declared, "test-source", DateTimeOffset.Now), 0.9m);

        await service.TrackAsync(state1);
        await service.TrackAsync(state2);
        
        var result = await service.GetCurrentStateAsync(subjectId);
        
        Assert.NotNull(result);
        Assert.Equal("value2", result!.Value);
    }
}
