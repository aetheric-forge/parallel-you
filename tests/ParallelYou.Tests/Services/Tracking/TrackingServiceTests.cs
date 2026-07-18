using System.Text.Json;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Artifacts;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Authorities;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Representations;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using AethericForge.Runtime.Models.Identity.Primitives;
using AethericForge.Runtime.Models.Knowledge.Authorities;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using Moq;
using ParallelYou.Abstractions;
using ParallelYou.Abstractions.Tracking;
using ParallelYou.Models.Tracking;
using ParallelYou.Services.Tracking;

namespace ParallelYou.Tests.Services.Tracking;

public class TrackingServiceTests
{
    private static IKnowledgeAuthority CreateAuthority(string subjectId = "account")
        => new KnowledgeAuthority(
            new IdentitySubject(subjectId, IdentityScheme.OpenIdConnect),
            TrackingAuthority.Context);

    private static TrackedState CreateState(
        Guid personId,
        Guid subjectId,
        string value = "Steady")
        => new(
            Guid.NewGuid(),
            personId,
            new TrackedSubject(subjectId, "SelfObservation", "Energy"),
            value,
            new Provenance(
                ProvenanceKind.Declared,
                "Human report",
                DateTimeOffset.UtcNow),
            1m);

    [Fact]
    public async Task TrackAsync_PublishesAuthorityScopedObservationAndCurrentPointer()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var authority = CreateAuthority();
        var state = CreateState(Guid.NewGuid(), Guid.NewGuid());
        var artifact = CreateArtifact(state, authority, DateTimeOffset.UtcNow);
        librarian.Setup(candidate => candidate.PublishArtifactAsync(
                It.IsAny<IKnowledgeDescriptor>(),
                It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
                null,
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);
        artificer.Setup(candidate => candidate.ExistsAsync(
                It.IsAny<IStagingReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        artificer.Setup(candidate => candidate.PutAsync(
                "TrackingCurrent",
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<IStagingMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IStagingReference>().Object);

        var service = new TrackingService(librarian.Object, artificer.Object);
        var result = await service.TrackAsync(state, authority);

        Assert.Equal(state.Id, result.Id);
        librarian.Verify(candidate => candidate.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(),
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
            null,
            authority,
            It.IsAny<CancellationToken>()), Times.Once);
        artificer.Verify(candidate => candidate.PutAsync(
            "TrackingCurrent",
            It.Is<string>(key =>
                key.Contains(state.PersonId.ToString("N")) &&
                key.Contains(state.Subject.Id.ToString("N"))),
            It.IsAny<Stream>(),
            It.IsAny<IStagingMetadata>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackAsync_LinksNewObservationToPreviousObservation()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var authority = CreateAuthority();
        var state = CreateState(Guid.NewGuid(), Guid.NewGuid(), "Changed");
        var previous = CreateArtifact(
            CreateState(state.PersonId, state.Subject.Id, "Earlier"),
            authority,
            DateTimeOffset.UtcNow.AddHours(-1));
        var published = CreateArtifact(state, authority, DateTimeOffset.UtcNow);
        SetupCurrentArtifact(librarian, artificer, previous);
        librarian.Setup(candidate => candidate.PublishArtifactAsync(
                It.IsAny<IKnowledgeDescriptor>(),
                It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
                It.Is<IEnumerable<IKnowledgeReference>>(lineage =>
                    lineage.Single().Equals(previous.Reference)),
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(published);
        artificer.Setup(candidate => candidate.PutAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<IStagingMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IStagingReference>().Object);

        var service = new TrackingService(librarian.Object, artificer.Object);
        await service.TrackAsync(state, authority);

        librarian.Verify(candidate => candidate.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(),
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
            It.Is<IEnumerable<IKnowledgeReference>>(lineage =>
                lineage.Single().Equals(previous.Reference)),
            authority,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentStateAsync_RejectsArtifactFromAnotherAuthority()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var personId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var requestedAuthority = CreateAuthority("account-a");
        var otherArtifact = CreateArtifact(
            CreateState(personId, subjectId),
            CreateAuthority("account-b"),
            DateTimeOffset.UtcNow);
        SetupCurrentArtifact(librarian, artificer, otherArtifact);

        var service = new TrackingService(librarian.Object, artificer.Object);
        var result = await service.GetCurrentStateAsync(
            personId,
            subjectId,
            requestedAuthority);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEveryObservationForRequestedSubject()
    {
        var librarian = new Mock<ILibrarian>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var older = CreateState(personId, subjectId, "Earlier");
        var newer = CreateState(personId, subjectId, "Now");
        var unrelated = CreateState(personId, Guid.NewGuid(), "Other");
        librarian.Setup(candidate => candidate.FindArtifactsAsync(
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateArtifact(older, authority, DateTimeOffset.UtcNow.AddHours(-1)),
                CreateArtifact(newer, authority, DateTimeOffset.UtcNow),
                CreateArtifact(unrelated, authority, DateTimeOffset.UtcNow)
            ]);

        var service = new TrackingService(
            librarian.Object,
            Mock.Of<IArtificer>());
        var history = await service.GetHistoryAsync(
            personId,
            subjectId,
            authority);

        Assert.Equal(2, history.Count);
        Assert.Equal("Now", history.First().Value);
        Assert.Equal("Earlier", history.Last().Value);
    }

    [Fact]
    public async Task GetCurrentStatesAsync_ReturnsLatestObservationPerSubject()
    {
        var librarian = new Mock<ILibrarian>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        librarian.Setup(candidate => candidate.FindArtifactsAsync(
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateArtifact(
                    CreateState(personId, subjectId, "Earlier"),
                    authority,
                    DateTimeOffset.UtcNow.AddHours(-1)),
                CreateArtifact(
                    CreateState(personId, subjectId, "Now"),
                    authority,
                    DateTimeOffset.UtcNow)
            ]);

        var service = new TrackingService(
            librarian.Object,
            Mock.Of<IArtificer>());
        var states = await service.GetCurrentStatesAsync(personId, authority);

        var current = Assert.Single(states);
        Assert.Equal("Now", current.Value);
    }

    [Fact]
    public async Task TrackAsync_RejectsWrongAuthorityContext()
    {
        var state = CreateState(Guid.NewGuid(), Guid.NewGuid());
        var wrongAuthority = new KnowledgeAuthority(
            new IdentitySubject("account", IdentityScheme.OpenIdConnect),
            "ParallelYou.Capture");
        var service = new TrackingService(
            Mock.Of<ILibrarian>(),
            Mock.Of<IArtificer>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.TrackAsync(state, wrongAuthority));
    }

    private static void SetupCurrentArtifact(
        Mock<ILibrarian> librarian,
        Mock<IArtificer> artificer,
        IKnowledgeArtifact artifact)
    {
        artificer.Setup(candidate => candidate.ExistsAsync(
                It.IsAny<IStagingReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        artificer.Setup(candidate => candidate.OpenReadAsync(
                It.IsAny<IStagingReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(
                JsonSerializer.SerializeToUtf8Bytes(artifact.Reference)));
        librarian.Setup(candidate => candidate.GetArtifactAsync(
                It.IsAny<IKnowledgeReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);
    }

    private static TestKnowledgeArtifact CreateArtifact(
        TrackedState state,
        IKnowledgeAuthority authority,
        DateTimeOffset updatedAtUtc)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(state);
        return new TestKnowledgeArtifact
        {
            Reference = new KnowledgeReference(
                "test",
                "tracking",
                state.Id.ToString("N"),
                "1"),
            Authority = authority,
            UpdatedAtUtc = updatedAtUtc,
            Representations =
            [
                new TestKnowledgeRepresentation
                {
                    ContentLength = content.LongLength,
                    StreamFactory = _ => Task.FromResult<Stream>(
                        new MemoryStream(content))
                }
            ]
        };
    }

    private sealed class TestKnowledgeRepresentation : IKnowledgeRepresentation
    {
        public string ContentType { get; set; } = "application/json";
        public string? Encoding { get; set; } = "utf-8";
        public string? Language { get; set; }
        public long ContentLength { get; set; }
        public string? ContentHash { get; set; }
        public required Func<CancellationToken, Task<Stream>> StreamFactory { get; set; }

        public Task<Stream> OpenStreamAsync(CancellationToken cancellationToken)
            => StreamFactory(cancellationToken);
    }

    private sealed class TestKnowledgeArtifact : IKnowledgeArtifact
    {
        public IKnowledgeReference Reference { get; set; } =
            new KnowledgeReference("test", "tracking", "state", "1");
        public IKnowledgeDescriptor Descriptor { get; set; } =
            new KnowledgeDescriptor("Tracking");
        public KnowledgeLifecycle Lifecycle { get; set; }
        public KnowledgeState State { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public IKnowledgeAuthority? Authority { get; set; }
        public IReadOnlyCollection<IKnowledgeRepresentation> Representations { get; set; } = [];
        public IReadOnlyCollection<IKnowledgeReference> Lineage { get; set; } = [];
    }
}
