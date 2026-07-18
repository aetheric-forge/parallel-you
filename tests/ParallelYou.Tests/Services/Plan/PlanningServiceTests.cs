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
using ParallelYou.Abstractions.Plan;
using ParallelYou.Models.Intention;
using ParallelYou.Models.Plan;
using ParallelYou.Services.Plan;

namespace ParallelYou.Tests.Services.Plan;

public class PlanningServiceTests
{
    private static IKnowledgeAuthority CreateAuthority(string subjectId = "account")
        => new KnowledgeAuthority(
            new IdentitySubject(subjectId, IdentityScheme.OpenIdConnect),
            PlanAuthority.Context);

    private static PlanSubmission CreateSubmission(
        Guid intentionId,
        string course = "Continue deliberately",
        bool isAccepted = false)
        => new(
            "Forge plan",
            course,
            [intentionId],
            new Provenance(
                ProvenanceKind.Declared,
                "Human proposal",
                DateTimeOffset.UtcNow),
            isAccepted);

    [Fact]
    public async Task CreatePlanAsync_PublishesProvisionalPlanAndCurrentPointer()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var intentions = new Mock<IIntentionService>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var intentionId = Guid.NewGuid();
        SetupAdoptedIntentions(intentions, personId, intentionId);
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
                CancellationToken _) => new TestKnowledgeArtifact
            {
                Reference = NewReference(),
                Descriptor = descriptor,
                Authority = artifactAuthority,
                Representations = representations.ToArray(),
                Lineage = lineage?.ToArray() ?? []
            });
        artificer.Setup(candidate => candidate.PutAsync(
                "PlanCurrent",
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<IStagingMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IStagingReference>().Object);

        var service = new PlanningService(
            librarian.Object,
            artificer.Object,
            intentions.Object);
        var plan = await service.CreatePlanAsync(
            personId,
            CreateSubmission(intentionId),
            authority);

        Assert.Equal(personId, plan.PersonId);
        Assert.Equal("Forge plan", plan.Representation.DisplayName);
        Assert.Equal("Continue deliberately", plan.Representation.Course);
        Assert.Contains(intentionId, plan.Representation.IntentionIds);
        Assert.False(plan.Representation.IsAccepted);
        artificer.Verify(candidate => candidate.PutAsync(
            "PlanCurrent",
            It.Is<string>(key =>
                key.Contains(personId.ToString("N")) &&
                key.Contains(plan.Id.ToString("N"))),
            It.IsAny<Stream>(),
            It.IsAny<IStagingMetadata>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePlanAsync_RejectsUnadoptedIntention()
    {
        var intentions = new Mock<IIntentionService>();
        var personId = Guid.NewGuid();
        var intentionId = Guid.NewGuid();
        intentions.Setup(candidate => candidate.GetIntentionsAsync(
                personId,
                It.IsAny<IKnowledgeAuthority>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateIntention(personId, intentionId, isAdopted: false)
            ]);
        var librarian = new Mock<ILibrarian>();
        var service = new PlanningService(
            librarian.Object,
            Mock.Of<IArtificer>(),
            intentions.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreatePlanAsync(
                personId,
                CreateSubmission(intentionId),
                CreateAuthority()));
        librarian.Verify(candidate => candidate.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(),
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
            It.IsAny<IEnumerable<IKnowledgeReference>>(),
            It.IsAny<IKnowledgeAuthority>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevisePlanAsync_PreservesIdentityAndLinksPreviousRepresentation()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var intentions = new Mock<IIntentionService>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var intentionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        SetupAdoptedIntentions(intentions, personId, intentionId);
        var previousPlan = CreatePlan(
            personId,
            planId,
            intentionId,
            "Earlier course");
        var previousArtifact = CreateArtifact(
            previousPlan,
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
                Reference = NewReference(),
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

        var service = new PlanningService(
            librarian.Object,
            artificer.Object,
            intentions.Object);
        var revision = await service.RevisePlanAsync(
            personId,
            planId,
            CreateSubmission(
                intentionId,
                "Revised course",
                isAccepted: true),
            authority);

        Assert.NotNull(revision);
        Assert.Equal(planId, revision!.Id);
        Assert.NotEqual(
            previousPlan.Representation.Id,
            revision.Representation.Id);
        Assert.Equal("Revised course", revision.Representation.Course);
        Assert.True(revision.Representation.IsAccepted);
    }

    [Fact]
    public async Task RevisePlanAsync_RejectsCurrentArtifactFromAnotherAccount()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var intentions = new Mock<IIntentionService>();
        var personId = Guid.NewGuid();
        var intentionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        SetupAdoptedIntentions(intentions, personId, intentionId);
        var artifact = CreateArtifact(
            CreatePlan(personId, planId, intentionId, "Other account"),
            CreateAuthority("account-b"),
            DateTimeOffset.UtcNow);
        SetupCurrentArtifact(librarian, artificer, artifact);

        var service = new PlanningService(
            librarian.Object,
            artificer.Object,
            intentions.Object);
        var revision = await service.RevisePlanAsync(
            personId,
            planId,
            CreateSubmission(intentionId),
            CreateAuthority("account-a"));

        Assert.Null(revision);
    }

    [Fact]
    public async Task GetPlansAndHistory_ReturnCurrentAndPreserveEarlierRepresentations()
    {
        var librarian = new Mock<ILibrarian>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var intentionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        librarian.Setup(candidate => candidate.FindArtifactsAsync(
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateArtifact(
                    CreatePlan(personId, planId, intentionId, "Earlier course"),
                    authority,
                    DateTimeOffset.UtcNow.AddMinutes(-1)),
                CreateArtifact(
                    CreatePlan(personId, planId, intentionId, "Current course"),
                    authority,
                    DateTimeOffset.UtcNow)
            ]);

        var service = new PlanningService(
            librarian.Object,
            Mock.Of<IArtificer>(),
            Mock.Of<IIntentionService>());
        var plans = await service.GetPlansAsync(personId, authority);
        var history = await service.GetHistoryAsync(personId, planId, authority);

        var plan = Assert.Single(plans);
        Assert.Equal("Current course", plan.Representation.Course);
        Assert.Equal(2, history.Count);
        Assert.Equal("Current course", history.First().Course);
        Assert.Equal("Earlier course", history.Last().Course);
    }

    [Fact]
    public async Task CreatePlanAsync_RejectsWrongAuthorityContext()
    {
        var authority = new KnowledgeAuthority(
            new IdentitySubject("account", IdentityScheme.OpenIdConnect),
            "ParallelYou.Intention");
        var service = new PlanningService(
            Mock.Of<ILibrarian>(),
            Mock.Of<IArtificer>(),
            Mock.Of<IIntentionService>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreatePlanAsync(
                Guid.NewGuid(),
                CreateSubmission(Guid.NewGuid()),
                authority));
    }

    private static void SetupAdoptedIntentions(
        Mock<IIntentionService> intentions,
        Guid personId,
        Guid intentionId)
    {
        intentions.Setup(candidate => candidate.GetIntentionsAsync(
                personId,
                It.Is<IKnowledgeAuthority>(authority =>
                    authority.Context == IntentionAuthority.Context),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateIntention(personId, intentionId, isAdopted: true)
            ]);
    }

    private static ParallelYou.Models.Intention.Intention CreateIntention(
        Guid personId,
        Guid intentionId,
        bool isAdopted)
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
                "Build the Forge",
                isAdopted));

    private static ParallelYou.Models.Plan.Plan CreatePlan(
        Guid personId,
        Guid planId,
        Guid intentionId,
        string course)
        => new(
            planId,
            personId,
            new PlanRepresentation(
                Guid.NewGuid(),
                planId,
                new Provenance(
                    ProvenanceKind.Declared,
                    "Human proposal",
                    DateTimeOffset.UtcNow),
                "Forge plan",
                course,
                [intentionId],
                false));

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
        ParallelYou.Models.Plan.Plan plan,
        IKnowledgeAuthority authority,
        DateTimeOffset updatedAtUtc)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(plan);
        return new TestKnowledgeArtifact
        {
            Reference = NewReference(),
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

    private static KnowledgeReference NewReference()
        => new(
            "test",
            "plan",
            Guid.NewGuid().ToString("N"),
            "1");

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
        public IKnowledgeReference Reference { get; set; } = NewReference();
        public IKnowledgeDescriptor Descriptor { get; set; } =
            new KnowledgeDescriptor("Plan");
        public KnowledgeLifecycle Lifecycle { get; set; }
        public KnowledgeState State { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public IKnowledgeAuthority? Authority { get; set; }
        public IReadOnlyCollection<IKnowledgeRepresentation> Representations { get; set; } = [];
        public IReadOnlyCollection<IKnowledgeReference> Lineage { get; set; } = [];
    }
}
