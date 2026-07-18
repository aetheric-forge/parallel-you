using Moq;
using System.Text;
using System.Text.Json;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Tracking;
using ParallelYou.Services.Tracking;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Representations;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Primitives;
using ParallelYou.Tests;

namespace ParallelYou.Tests.Services.Tracking;

public class TrackingServiceTests : TestBase
{
    private record TestTrackedState(Guid SubjectId, string Value, Provenance Provenance, decimal Confidence) : ITrackedState;
    private readonly Mock<IArtificer> _mockArtificer = new();
    
    [Fact]
    public async Task TrackAsync_ShouldSucceed()
    {
        var service = new TrackingService(MockLibrarian.Object, _mockArtificer.Object);
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
        var service = new TrackingService(MockLibrarian.Object, _mockArtificer.Object);
        var subjectId = Guid.NewGuid();
        
        _mockArtificer.Setup(a => a.ExistsAsync(It.IsAny<IStagingReference>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await service.GetCurrentStateAsync(subjectId);

        Assert.Null(result);
    }

    [Fact]
    public async Task TrackAsync_WithNullState_ShouldThrowArgumentNullException()
    {
        var service = new TrackingService(MockLibrarian.Object, _mockArtificer.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.TrackAsync(null!));
    }

    [Fact]
    public async Task TrackAsync_ThenGetCurrentStateAsync_ShouldReturnTrackedState()
    {
        var service = new TrackingService(MockLibrarian.Object, _mockArtificer.Object);
        var subjectId = Guid.NewGuid();
        var state = new TestTrackedState(subjectId, "value1", new Provenance(ProvenanceKind.Declared, "test-source", DateTimeOffset.Now), 0.9m);

        _mockArtificer.Setup(a => a.ExistsAsync(It.IsAny<IStagingReference>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockArtificer.Setup(a => a.OpenReadAsync(It.IsAny<IStagingReference>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(state))));

        await service.TrackAsync(state);
        
        var result = await service.GetCurrentStateAsync(subjectId);
        
        Assert.NotNull(result);
        Assert.Equal("value1", result!.Value);
    }

    [Fact]
    public async Task TrackAsync_WithNewerState_ShouldReplaceCurrentState()
    {
        var service = new TrackingService(MockLibrarian.Object, _mockArtificer.Object);
        var subjectId = Guid.NewGuid();
        var state1 = new TestTrackedState(subjectId, "value1", new Provenance(ProvenanceKind.Declared, "test-source", DateTimeOffset.Now), 0.9m);
        var state2 = new TestTrackedState(subjectId, "value2", new Provenance(ProvenanceKind.Declared, "test-source", DateTimeOffset.Now), 0.9m);

        _mockArtificer.Setup(a => a.ExistsAsync(It.IsAny<IStagingReference>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockArtificer.Setup(a => a.OpenReadAsync(It.IsAny<IStagingReference>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(state2))));

        await service.TrackAsync(state1);
        await service.TrackAsync(state2);
        
        var result = await service.GetCurrentStateAsync(subjectId);
        
        Assert.NotNull(result);
        Assert.Equal("value2", result!.Value);
    }
}
