using System.Text;
using System.Text.Json;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Artifacts;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Authorities;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Representations;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using Moq;
using ParallelYou.Abstractions;
using ParallelYou.Models.Reflection;
using ParallelYou.Services.Reflection;
using ParallelYou.Tests;
using ReflectionService = ParallelYou.Services.Reflection.ReflectionService;

namespace ParallelYou.Tests.Services.Reflection;

public class ReflectionServiceTests : TestBase
{
    private class ConcreteKnowledgeRepresentation : IKnowledgeRepresentation
    {
        public string ContentType { get; set; } = "application/json";
        public string? Encoding { get; set; }
        public string? Language { get; set; }
        public long ContentLength { get; set; }
        public string? ContentHash { get; set; }
        public Func<CancellationToken, Task<Stream>> StreamFactory { get; set; }

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
    public async Task StartReflectionAsync_ShouldCreateReflection()
    {
        var librarianMock = new Mock<ILibrarian>();
        var artificerMock = new Mock<IArtificer>();
        var service = new ReflectionService(librarianMock.Object, artificerMock.Object);
        var subject = new ReflectionSubject { Id = Guid.NewGuid(), Type = "Experience", Description = "Test" };
        var questions = new List<string> { "Question 1" };

        var artifact = new ConcreteKnowledgeArtifact();
        
        librarianMock.SetupSequence(l => l.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(), 
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(), 
            It.IsAny<IEnumerable<IKnowledgeReference>>(), 
            It.IsAny<AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Authorities.IKnowledgeAuthority>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact)
            .ReturnsAsync(artifact);

        artificerMock.Setup(a => a.PutAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<Stream>(), 
            It.IsAny<IStagingMetadata>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IStagingReference>().Object);

        artificerMock.Setup(a => a.ExistsAsync(It.IsAny<IStagingReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var referenceMock = new KnowledgeReference("Scheme", "Kind", "Name", "1");
        artificerMock.Setup(a => a.OpenReadAsync(It.IsAny<IStagingReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(referenceMock))));

        var reflection = await service.StartReflectionAsync(subject, questions);

        Assert.NotNull(reflection);
        Assert.Equal(subject.Id, reflection.Subject.Id);
        Assert.Single(reflection.Questions);
        Assert.Empty(reflection.Evidence);
        Assert.Empty(reflection.Insights);
    }

    private ConcreteKnowledgeArtifact? _currentArtifact;

    private void SetupMocks(Mock<ILibrarian> librarianMock, Mock<IArtificer> artificerMock)
    {
        _currentArtifact = new ConcreteKnowledgeArtifact();
        // We will need to set the representations dynamically when we know what the reflection looks like.
        // Actually, let's just make it store the reflection that is published.
        
        librarianMock.Setup(l => l.PublishArtifactAsync(It.IsAny<IKnowledgeDescriptor>(), It.IsAny<IEnumerable<IKnowledgeRepresentation>>(), It.IsAny<IEnumerable<IKnowledgeReference>>(), It.IsAny<IKnowledgeAuthority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IKnowledgeDescriptor descriptor, IEnumerable<IKnowledgeRepresentation> reps, IEnumerable<IKnowledgeReference> lineage, IKnowledgeAuthority authority, CancellationToken ct) => {
                _currentArtifact.Representations = reps.ToList();
                return _currentArtifact;
            });

        librarianMock.Setup(l => l.GetArtifactAsync(It.IsAny<IKnowledgeReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_currentArtifact);

        artificerMock.Setup(a => a.PutAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<IStagingMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IStagingReference>().Object);

        artificerMock.Setup(a => a.ExistsAsync(It.IsAny<IStagingReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var referenceMock = new KnowledgeReference("Scheme", "Kind", "Name", "1");
        artificerMock.Setup(a => a.OpenReadAsync(It.IsAny<IStagingReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(referenceMock))));
    }

    private async Task<ParallelYou.Models.Reflection.ReflectionService> GetReflectionFromArtifact()
    {
        var representation = _currentArtifact!.Representations.FirstOrDefault(r => r.ContentType == "application/json");
        if (representation == null) throw new InvalidOperationException("Representation not found");
        using var stream = await representation.OpenStreamAsync(CancellationToken.None);
        var reflection = await JsonSerializer.DeserializeAsync<ParallelYou.Models.Reflection.ReflectionService>(stream);
        return reflection ?? throw new InvalidOperationException("Failed to deserialize reflection");
    }

    [Fact]
    public async Task AddEvidenceAsync_ShouldAddEvidence()
    {
        var librarianMock = new Mock<ILibrarian>();
        var artificerMock = new Mock<IArtificer>();
        SetupMocks(librarianMock, artificerMock);
        
        var service = new ReflectionService(librarianMock.Object, artificerMock.Object);
        var subject = new ReflectionSubject { Id = Guid.NewGuid(), Type = "Experience", Description = "Test" };
        
        var reflection = await service.StartReflectionAsync(subject, new List<string>());
        var evidence = new ReflectionEvidence { Id = Guid.NewGuid(), Content = "Test Evidence", Provenance = new Provenance(ProvenanceKind.Declared, "Test Source", DateTimeOffset.UtcNow) };

        await service.AddEvidenceAsync(reflection.Id, evidence);

        var updatedReflection = await GetReflectionFromArtifact();
        Assert.Single(updatedReflection.Evidence);
        Assert.Equal(evidence.Id, updatedReflection.Evidence.First().Id);
    }

    [Fact]
    public async Task AddInsightAsync_ShouldAddInsight()
    {
        var librarianMock = new Mock<ILibrarian>();
        var artificerMock = new Mock<IArtificer>();
        SetupMocks(librarianMock, artificerMock);
        
        var service = new ReflectionService(librarianMock.Object, artificerMock.Object);
        var subject = new ReflectionSubject { Id = Guid.NewGuid(), Type = "Experience", Description = "Test" };
        
        var reflection = await service.StartReflectionAsync(subject, new List<string>());
        var insight = new ReflectionInsightSubmission { Id = Guid.NewGuid(), Content = "Test Insight" };

        await service.AddInsightAsync(reflection.Id, insight);

        var updatedReflection = await GetReflectionFromArtifact();
        Assert.Single(updatedReflection.Insights);
        Assert.Equal(insight.Id, updatedReflection.Insights.First().Id);
        Assert.False(updatedReflection.Insights.First().IsAdopted);
    }

    [Fact]
    public async Task AddEvidenceAsync_WhenReflectionNotFound_ShouldNotThrow()
    {
        var librarianMock = new Mock<ILibrarian>();
        var artificerMock = new Mock<IArtificer>();
        var service = new ReflectionService(librarianMock.Object, artificerMock.Object);
        var evidence = new ReflectionEvidence { Id = Guid.NewGuid(), Content = "Test Evidence", Provenance = new Provenance(ProvenanceKind.Declared, "Test Source", DateTimeOffset.UtcNow) };

        await service.AddEvidenceAsync(Guid.NewGuid(), evidence);
    }

    [Fact]
    public async Task AddInsightAsync_WhenReflectionNotFound_ShouldNotThrow()
    {
        var librarianMock = new Mock<ILibrarian>();
        var artificerMock = new Mock<IArtificer>();
        var service = new ReflectionService(librarianMock.Object, artificerMock.Object);
        var insight = new ReflectionInsightSubmission { Id = Guid.NewGuid(), Content = "Test Insight" };

        var success = await service.AddInsightAsync(Guid.NewGuid(), insight);
        
        Assert.False(success);
    }
}
