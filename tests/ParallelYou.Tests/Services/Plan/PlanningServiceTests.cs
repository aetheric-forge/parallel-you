using Moq;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Authorities;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Artifacts;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Representations;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using AethericForge.Runtime.Models.Knowledge.Representations;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Primitives;
using AethericForge.Runtime.Models.Staging;
using ParallelYou.Abstractions.Plan;
using ParallelYou.Services.Plan;
using ParallelYou.Tests;
using ParallelYou.Models.Plan;

namespace ParallelYou.Tests.Services.Plan;

public class PlanningServiceTests : TestBase
{
    private class ConcreteKnowledgeRepresentation : IKnowledgeRepresentation
    {
        public string ContentType { get; set; } = "application/json";
        public string? Encoding { get; set; }
        public string? Language { get; set; }
        public long ContentLength { get; set; }
        public string? ContentHash { get; set; }
        public Func<CancellationToken, Task<Stream>> StreamFactory { get; set; } = _ => Task.FromResult<Stream>(new MemoryStream());

        public Task<Stream> OpenStreamAsync(CancellationToken cancellationToken) => StreamFactory(cancellationToken);
    }

    private class ConcreteKnowledgeArtifact : IKnowledgeArtifact, AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives.IKnowledgeObject
    {
        public IKnowledgeReference Reference { get; set; } = new KnowledgeReference("Scheme", "Kind", "Name", "1");
        public IKnowledgeDescriptor Descriptor { get; set; } = new KnowledgeDescriptor("Test");
        public KnowledgeLifecycle Lifecycle { get; set; }
        public KnowledgeState State { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public IKnowledgeAuthority? Authority { get; set; }
        public IReadOnlyCollection<IKnowledgeRepresentation> Representations { get; set; } = new List<IKnowledgeRepresentation>();
        public IReadOnlyCollection<IKnowledgeReference> Lineage { get; set; } = new List<IKnowledgeReference>();
    }

    [Fact]
    public async Task PlanAsync_ShouldCreateAndPublishPlan()
    {
        // Arrange
        var service = new PlanningService(MockLibrarian.Object, MockArtificer.Object);
        var subject = "Test Subject";
        var artifact = new ConcreteKnowledgeArtifact();

        MockLibrarian.Setup(l => l.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(), 
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(), 
            It.IsAny<IEnumerable<IKnowledgeReference>>(), 
            It.IsAny<IKnowledgeAuthority>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        MockArtificer.Setup(a => a.PutAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<Stream>(), 
            It.IsAny<IStagingMetadata>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IStagingReference>().Object);

        // Act
        var plan = await service.PlanAsync(subject);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal(subject, plan.Subject);
        
        MockLibrarian.Verify(l => l.PublishArtifactAsync(It.IsAny<IKnowledgeDescriptor>(), It.IsAny<IEnumerable<IKnowledgeRepresentation>>(), It.IsAny<IEnumerable<IKnowledgeReference>>(), It.IsAny<IKnowledgeAuthority>(), It.IsAny<CancellationToken>()), Times.Once);
        MockArtificer.Verify(a => a.PutAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<IStagingMetadata>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanAsync_ShouldThrowException_WhenLibrarianFails()
    {
        // Arrange
        var service = new PlanningService(MockLibrarian.Object, MockArtificer.Object);
        var subject = "Test Subject";

        MockLibrarian.Setup(l => l.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(), 
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(), 
            It.IsAny<IEnumerable<IKnowledgeReference>>(), 
            It.IsAny<IKnowledgeAuthority>(), 
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Librarian failure"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.PlanAsync(subject));
    }

    [Fact]
    public async Task PlanAsync_ShouldThrowException_WhenArtificerFails()
    {
        // Arrange
        var service = new PlanningService(MockLibrarian.Object, MockArtificer.Object);
        var subject = "Test Subject";
        var artifact = new ConcreteKnowledgeArtifact();

        MockLibrarian.Setup(l => l.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(), 
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(), 
            It.IsAny<IEnumerable<IKnowledgeReference>>(), 
            It.IsAny<IKnowledgeAuthority>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        MockArtificer.Setup(a => a.PutAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<Stream>(), 
            It.IsAny<IStagingMetadata>(), 
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Artificer failure"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.PlanAsync(subject));
    }
}
