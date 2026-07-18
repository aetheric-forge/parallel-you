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
using ParallelYou.Abstractions.Intention;
using ParallelYou.Models.Intention;
using ParallelYou.Services.Intention;

namespace ParallelYou.Tests.Services.Intention;

public class IntentionServiceTests
{
    private static IKnowledgeAuthority CreateAuthority(string subjectId = "account")
        => new KnowledgeAuthority(
            new IdentitySubject(subjectId, IdentityScheme.OpenIdConnect),
            IntentionAuthority.Context);

    private static IntentionSubmission CreateSubmission(string description)
        => new(
            description,
            new Provenance(
                ProvenanceKind.Declared,
                "Human declaration",
                DateTimeOffset.UtcNow),
            true);

    [Fact]
    public async Task CreateIntentionAsync_PublishesOwnedIntentionAndCurrentPointer()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        TestKnowledgeArtifact? publishedArtifact = null;
        librarian.Setup(candidate => candidate.PublishArtifactAsync(
                It.IsAny<IKnowledgeDescriptor>(),
                It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
                null,
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                IKnowledgeDescriptor descriptor,
                IEnumerable<IKnowledgeRepresentation> representations,
                IEnumerable<IKnowledgeReference>? lineage,
                IKnowledgeAuthority artifactAuthority,
                CancellationToken _) =>
            {
                publishedArtifact = new TestKnowledgeArtifact
                {
                    Reference = new KnowledgeReference(
                        "test",
                        "intention",
                        Guid.NewGuid().ToString("N"),
                        "1"),
                    Descriptor = descriptor,
                    Authority = artifactAuthority,
                    Representations = representations.ToArray(),
                    Lineage = lineage?.ToArray() ?? []
                };
                return publishedArtifact;
            });
        artificer.Setup(candidate => candidate.PutAsync(
                "IntentionCurrent",
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<IStagingMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IStagingReference>().Object);

        var service = new IntentionService(librarian.Object, artificer.Object);
        var intention = await service.CreateIntentionAsync(
            personId,
            CreateSubmission("Keep building the Forge"),
            authority);

        Assert.Equal(personId, intention.PersonId);
        Assert.Equal("Keep building the Forge", intention.Representation.Description);
        Assert.Equal(intention.Id, intention.Representation.IntentionId);
        Assert.NotEqual(Guid.Empty, intention.Representation.Id);
        Assert.True(intention.Representation.IsAdopted);
        Assert.NotNull(publishedArtifact);
        artificer.Verify(candidate => candidate.PutAsync(
            "IntentionCurrent",
            It.Is<string>(key =>
                key.Contains(personId.ToString("N")) &&
                key.Contains(intention.Id.ToString("N"))),
            It.IsAny<Stream>(),
            It.IsAny<IStagingMetadata>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviseIntentionAsync_PreservesIdentityAndLinksPreviousRepresentation()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var intentionId = Guid.NewGuid();
        var previousIntention = CreateIntention(
            personId,
            intentionId,
            "Explore the Forge");
        var previousArtifact = CreateArtifact(
            previousIntention,
            authority,
            DateTimeOffset.UtcNow.AddMinutes(-1));
        SetupCurrentArtifact(librarian, artificer, previousArtifact);
        librarian.Setup(candidate => candidate.PublishArtifactAsync(
                It.IsAny<IKnowledgeDescriptor>(),
                It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
                It.Is<IEnumerable<IKnowledgeReference>>(lineage =>
                    lineage.Single().Equals(previousArtifact.Reference)),
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                IKnowledgeDescriptor descriptor,
                IEnumerable<IKnowledgeRepresentation> representations,
                IEnumerable<IKnowledgeReference> lineage,
                IKnowledgeAuthority artifactAuthority,
                CancellationToken _) => new TestKnowledgeArtifact
            {
                Reference = new KnowledgeReference(
                    "test",
                    "intention",
                    Guid.NewGuid().ToString("N"),
                    "2"),
                Descriptor = descriptor,
                Authority = artifactAuthority,
                Representations = representations.ToArray(),
                Lineage = lineage.ToArray()
            });
        artificer.Setup(candidate => candidate.PutAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<IStagingMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IStagingReference>().Object);

        var service = new IntentionService(librarian.Object, artificer.Object);
        var revision = await service.ReviseIntentionAsync(
            personId,
            intentionId,
            CreateSubmission("Build the Forge deliberately"),
            authority);

        Assert.NotNull(revision);
        Assert.Equal(intentionId, revision!.Id);
        Assert.NotEqual(
            previousIntention.Representation.Id,
            revision.Representation.Id);
        Assert.Equal(
            "Build the Forge deliberately",
            revision.Representation.Description);
        librarian.Verify(candidate => candidate.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(),
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
            It.Is<IEnumerable<IKnowledgeReference>>(lineage =>
                lineage.Single().Equals(previousArtifact.Reference)),
            authority,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviseIntentionAsync_RejectsCurrentArtifactFromAnotherAccount()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var personId = Guid.NewGuid();
        var intentionId = Guid.NewGuid();
        var artifact = CreateArtifact(
            CreateIntention(personId, intentionId, "Other account"),
            CreateAuthority("account-b"),
            DateTimeOffset.UtcNow);
        SetupCurrentArtifact(librarian, artificer, artifact);

        var service = new IntentionService(librarian.Object, artificer.Object);
        var revision = await service.ReviseIntentionAsync(
            personId,
            intentionId,
            CreateSubmission("Attempted revision"),
            CreateAuthority("account-a"));

        Assert.Null(revision);
        librarian.Verify(candidate => candidate.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(),
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
            It.IsAny<IEnumerable<IKnowledgeReference>>(),
            It.IsAny<IKnowledgeAuthority>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetIntentionsAsync_ReturnsLatestRepresentationPerIntention()
    {
        var librarian = new Mock<ILibrarian>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var intentionId = Guid.NewGuid();
        librarian.Setup(candidate => candidate.FindArtifactsAsync(
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateArtifact(
                    CreateIntention(personId, intentionId, "Earlier meaning"),
                    authority,
                    DateTimeOffset.UtcNow.AddMinutes(-1)),
                CreateArtifact(
                    CreateIntention(personId, intentionId, "Current meaning"),
                    authority,
                    DateTimeOffset.UtcNow)
            ]);

        var service = new IntentionService(
            librarian.Object,
            Mock.Of<IArtificer>());
        var intentions = await service.GetIntentionsAsync(personId, authority);

        var intention = Assert.Single(intentions);
        Assert.Equal("Current meaning", intention.Representation.Description);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEveryRepresentationForIntention()
    {
        var librarian = new Mock<ILibrarian>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var intentionId = Guid.NewGuid();
        librarian.Setup(candidate => candidate.FindArtifactsAsync(
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateArtifact(
                    CreateIntention(personId, intentionId, "Earlier meaning"),
                    authority,
                    DateTimeOffset.UtcNow.AddMinutes(-1)),
                CreateArtifact(
                    CreateIntention(personId, intentionId, "Current meaning"),
                    authority,
                    DateTimeOffset.UtcNow),
                CreateArtifact(
                    CreateIntention(personId, Guid.NewGuid(), "Unrelated"),
                    authority,
                    DateTimeOffset.UtcNow)
            ]);

        var service = new IntentionService(
            librarian.Object,
            Mock.Of<IArtificer>());
        var history = await service.GetHistoryAsync(
            personId,
            intentionId,
            authority);

        Assert.Equal(2, history.Count);
        Assert.Equal("Current meaning", history.First().Description);
        Assert.Equal("Earlier meaning", history.Last().Description);
    }

    [Fact]
    public async Task CreateIntentionAsync_RejectsWrongAuthorityContext()
    {
        var authority = new KnowledgeAuthority(
            new IdentitySubject("account", IdentityScheme.OpenIdConnect),
            "ParallelYou.Plan");
        var service = new IntentionService(
            Mock.Of<ILibrarian>(),
            Mock.Of<IArtificer>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateIntentionAsync(
                Guid.NewGuid(),
                CreateSubmission("An intention"),
                authority));
    }

    private static ParallelYou.Models.Intention.Intention CreateIntention(
        Guid personId,
        Guid intentionId,
        string description)
        => new(
            intentionId,
            personId,
            new IntentionRepresentation(
                Guid.NewGuid(),
                intentionId,
                new Provenance(
                    ProvenanceKind.Declared,
                    "Human declaration",
                    DateTimeOffset.UtcNow),
                description,
                true));

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
        ParallelYou.Models.Intention.Intention intention,
        IKnowledgeAuthority authority,
        DateTimeOffset updatedAtUtc)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(intention);
        return new TestKnowledgeArtifact
        {
            Reference = new KnowledgeReference(
                "test",
                "intention",
                intention.Representation.Id.ToString("N"),
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
            new KnowledgeReference("test", "intention", "representation", "1");
        public IKnowledgeDescriptor Descriptor { get; set; } =
            new KnowledgeDescriptor("Intention");
        public KnowledgeLifecycle Lifecycle { get; set; }
        public KnowledgeState State { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public IKnowledgeAuthority? Authority { get; set; }
        public IReadOnlyCollection<IKnowledgeRepresentation> Representations { get; set; } = [];
        public IReadOnlyCollection<IKnowledgeReference> Lineage { get; set; } = [];
    }
}
