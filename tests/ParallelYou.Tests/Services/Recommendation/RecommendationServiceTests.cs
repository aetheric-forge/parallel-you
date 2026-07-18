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
using ParallelYou.Abstractions.Plan;
using ParallelYou.Abstractions.Recommendation;
using ParallelYou.Models.Plan;
using ParallelYou.Models.Recommendation;
using ParallelYou.Services.Recommendation;

namespace ParallelYou.Tests.Services.Recommendation;

public class RecommendationServiceTests
{
    private static IKnowledgeAuthority CreateAuthority(string subjectId = "account")
        => new KnowledgeAuthority(
            new IdentitySubject(subjectId, IdentityScheme.OpenIdConnect),
            RecommendationAuthority.Context);

    private static RecommendationSubmission CreateSubmission(
        Guid planId,
        string rationale = "This may preserve momentum without hiding uncertainty")
        => new(
            new RecommendationSubject(planId, "How to proceed with the Forge Plan"),
            [
                new RecommendationCandidate(
                    Guid.NewGuid(),
                    planId,
                    RecommendationCandidateKind.ConsiderPlan,
                    "Consider the current Plan without treating it as inevitable")
            ],
            rationale,
            ["Available energy may differ from what is presently known"],
            0.7m,
            new Provenance(
                ProvenanceKind.Inferred,
                "Recommendation process",
                DateTimeOffset.UtcNow));

    [Fact]
    public async Task PresentRecommendationAsync_PublishesAwaitingRecommendationAndCurrentPointer()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var planning = new Mock<IPlanningService>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        SetupPlans(planning, personId, planId);
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
                "RecommendationCurrent",
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<IStagingMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IStagingReference>().Object);

        var service = new RecommendationService(
            librarian.Object,
            artificer.Object,
            planning.Object);
        var recommendation = await service.PresentRecommendationAsync(
            personId,
            CreateSubmission(planId),
            authority);

        Assert.Equal(personId, recommendation.PersonId);
        Assert.Equal(planId, recommendation.Representation.Subject.PlanId);
        Assert.Equal(
            RecommendationResponse.AwaitingResponse,
            recommendation.Representation.Response);
        Assert.Empty(recommendation.Representation.SelectedCandidateIds);
        Assert.Null(recommendation.Representation.ResponseProvenance);
        Assert.Single(recommendation.Representation.Candidates);
        artificer.Verify(candidate => candidate.PutAsync(
            "RecommendationCurrent",
            It.Is<string>(key =>
                key.Contains(personId.ToString("N")) &&
                key.Contains(recommendation.Id.ToString("N"))),
            It.IsAny<Stream>(),
            It.IsAny<IStagingMetadata>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PresentRecommendationAsync_RejectsPlanOutsidePersonsAccount()
    {
        var planning = new Mock<IPlanningService>();
        var personId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        planning.Setup(candidate => candidate.GetPlansAsync(
                personId,
                It.IsAny<IKnowledgeAuthority>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var librarian = new Mock<ILibrarian>();
        var service = new RecommendationService(
            librarian.Object,
            Mock.Of<IArtificer>(),
            planning.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PresentRecommendationAsync(
                personId,
                CreateSubmission(planId),
                CreateAuthority()));
        librarian.Verify(candidate => candidate.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(),
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
            It.IsAny<IEnumerable<IKnowledgeReference>>(),
            It.IsAny<IKnowledgeAuthority>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordResponseAsync_RecordsSelectedCandidateAndHumanProvenance()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var recommendationId = Guid.NewGuid();
        var current = CreateRecommendation(personId, recommendationId, planId);
        var previousArtifact = CreateArtifact(
            current,
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
        var responseProvenance = new Provenance(
            ProvenanceKind.Declared,
            "Human response",
            DateTimeOffset.UtcNow);

        var service = new RecommendationService(
            librarian.Object,
            artificer.Object,
            Mock.Of<IPlanningService>());
        var responded = await service.RecordResponseAsync(
            personId,
            recommendationId,
            RecommendationResponse.Accepted,
            [current.Representation.Candidates.Single().Id],
            responseProvenance,
            authority);

        Assert.NotNull(responded);
        Assert.Equal(RecommendationResponse.Accepted, responded!.Representation.Response);
        Assert.Equal(responseProvenance, responded.Representation.ResponseProvenance);
        Assert.Equal(
            current.Representation.Candidates.Single().Id,
            responded.Representation.Candidates.Single().Id);
        Assert.Equal(
            current.Representation.Candidates.Single().Id,
            Assert.Single(responded.Representation.SelectedCandidateIds));
        Assert.NotEqual(
            current.Representation.Id,
            responded.Representation.Id);
    }

    [Fact]
    public async Task ReviseRecommendationAsync_ResetsResponseWithoutErasingPriorVersion()
    {
        var librarian = new Mock<ILibrarian>();
        var artificer = new Mock<IArtificer>();
        var planning = new Mock<IPlanningService>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var recommendationId = Guid.NewGuid();
        SetupPlans(planning, personId, planId);
        var responded = CreateRecommendation(
            personId,
            recommendationId,
            planId,
            RecommendationResponse.Rejected);
        var previousArtifact = CreateArtifact(
            responded,
            authority,
            DateTimeOffset.UtcNow.AddMinutes(-1));
        SetupCurrentArtifact(librarian, artificer, previousArtifact);
        SetupRevisionPublish(librarian, artificer, authority);

        var service = new RecommendationService(
            librarian.Object,
            artificer.Object,
            planning.Object);
        var revision = await service.ReviseRecommendationAsync(
            personId,
            recommendationId,
            CreateSubmission(planId, "Changed circumstances support another look"),
            authority);

        Assert.NotNull(revision);
        Assert.Equal(recommendationId, revision!.Id);
        Assert.Equal(
            RecommendationResponse.AwaitingResponse,
            revision.Representation.Response);
        Assert.Null(revision.Representation.ResponseProvenance);
        Assert.Equal(
            "Changed circumstances support another look",
            revision.Representation.Rationale);
    }

    [Fact]
    public async Task GetRecommendationsAndHistory_ReturnCurrentAndPreserveResponses()
    {
        var librarian = new Mock<ILibrarian>();
        var authority = CreateAuthority();
        var personId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var recommendationId = Guid.NewGuid();
        librarian.Setup(candidate => candidate.FindArtifactsAsync(
                authority,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateArtifact(
                    CreateRecommendation(personId, recommendationId, planId),
                    authority,
                    DateTimeOffset.UtcNow.AddMinutes(-1)),
                CreateArtifact(
                    CreateRecommendation(
                        personId,
                        recommendationId,
                        planId,
                        RecommendationResponse.Accepted),
                    authority,
                    DateTimeOffset.UtcNow)
            ]);

        var service = new RecommendationService(
            librarian.Object,
            Mock.Of<IArtificer>(),
            Mock.Of<IPlanningService>());
        var recommendations = await service.GetRecommendationsAsync(
            personId,
            authority);
        var history = await service.GetHistoryAsync(
            personId,
            recommendationId,
            authority);

        var recommendation = Assert.Single(recommendations);
        Assert.Equal(
            RecommendationResponse.Accepted,
            recommendation.Representation.Response);
        Assert.Equal(2, history.Count);
        Assert.Equal(RecommendationResponse.Accepted, history.First().Response);
        Assert.Equal(RecommendationResponse.AwaitingResponse, history.Last().Response);
    }

    [Fact]
    public async Task PresentRecommendationAsync_RejectsWrongAuthorityContext()
    {
        var authority = new KnowledgeAuthority(
            new IdentitySubject("account", IdentityScheme.OpenIdConnect),
            PlanAuthority.Context);
        var service = new RecommendationService(
            Mock.Of<ILibrarian>(),
            Mock.Of<IArtificer>(),
            Mock.Of<IPlanningService>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PresentRecommendationAsync(
                Guid.NewGuid(),
                CreateSubmission(Guid.NewGuid()),
                authority));
    }

    private static void SetupPlans(
        Mock<IPlanningService> planning,
        Guid personId,
        Guid planId)
    {
        planning.Setup(candidate => candidate.GetPlansAsync(
                personId,
                It.Is<IKnowledgeAuthority>(authority =>
                    authority.Context == PlanAuthority.Context),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePlan(personId, planId)]);
    }

    private static ParallelYou.Models.Plan.Plan CreatePlan(
        Guid personId,
        Guid planId)
        => new(
            planId,
            personId,
            new PlanRepresentation(
                Guid.NewGuid(),
                planId,
                new Provenance(
                    ProvenanceKind.Declared,
                    "Human-authored Plan",
                    DateTimeOffset.UtcNow),
                "Forge Plan",
                "Continue deliberately",
                [Guid.NewGuid()],
                true));

    private static ParallelYou.Models.Recommendation.Recommendation
        CreateRecommendation(
            Guid personId,
            Guid recommendationId,
            Guid planId,
            RecommendationResponse response = RecommendationResponse.AwaitingResponse)
    {
        var candidateId = Guid.NewGuid();
        return new(
            recommendationId,
            personId,
            new RecommendationRepresentation(
                Guid.NewGuid(),
                recommendationId,
                new RecommendationSubject(planId, "How to proceed"),
                [
                    new RecommendationCandidate(
                        candidateId,
                        planId,
                        RecommendationCandidateKind.ConsiderPlan,
                        "Consider the Plan")
                ],
                "This may be useful",
                ["Conditions may change"],
                0.7m,
                new Provenance(
                    ProvenanceKind.Inferred,
                    "Recommendation process",
                    DateTimeOffset.UtcNow),
                response,
                response == RecommendationResponse.Accepted
                    ? [candidateId]
                    : [],
                response == RecommendationResponse.AwaitingResponse
                    ? null
                    : new Provenance(
                        ProvenanceKind.Declared,
                        "Human response",
                        DateTimeOffset.UtcNow)));
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

    private static void SetupRevisionPublish(
        Mock<ILibrarian> librarian,
        Mock<IArtificer> artificer,
        IKnowledgeAuthority authority)
    {
        librarian.Setup(candidate => candidate.PublishArtifactAsync(
                It.IsAny<IKnowledgeDescriptor>(),
                It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
                It.IsAny<IEnumerable<IKnowledgeReference>>(),
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
    }

    private static TestKnowledgeArtifact CreateArtifact(
        ParallelYou.Models.Recommendation.Recommendation recommendation,
        IKnowledgeAuthority authority,
        DateTimeOffset updatedAtUtc)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(recommendation);
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
            "recommendation",
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
            new KnowledgeDescriptor("Recommendation");
        public KnowledgeLifecycle Lifecycle { get; set; }
        public KnowledgeState State { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public IKnowledgeAuthority? Authority { get; set; }
        public IReadOnlyCollection<IKnowledgeRepresentation> Representations { get; set; } = [];
        public IReadOnlyCollection<IKnowledgeReference> Lineage { get; set; } = [];
    }
}
