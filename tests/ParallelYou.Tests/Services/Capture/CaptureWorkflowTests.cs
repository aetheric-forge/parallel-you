using AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Models.Authorities;
using AethericForge.Runtime.Models.Identity.Primitives;
using AethericForge.Runtime.Models.Knowledge.Authorities;
using AethericForge.Runtime.Providers.Knowledge.InMemory;
using AethericForge.Runtime.Services.Knowledge;
using AethericForge.Runtime.Services.Library;
using ParallelYou.Abstractions.Capture;
using ParallelYou.Services.Capture;

namespace ParallelYou.Tests.Services.Capture;

public record CaptureEvidence(string Title, string Content) : ICaptureEvidence;

public sealed class CaptureWorkflowTests
{
    [Fact]
    public async Task CaptureAndRetrieve_RoundTripsThroughLibraryAndKnowledgeProvider()
    {
        var provider = new InMemoryKnowledgeProvider("parallel-you-test");
        IKnowledgeService knowledgeService = new KnowledgeService(
            [provider],
            new Team<ICuratorClerk>([]));
        ILibrarian librarian = new Librarian(
            knowledgeService,
            new Team<ILibraryClerk>([]));
        ICaptureService captureService = new CaptureService(librarian);
        var evidence = new CaptureEvidence("Test Title", "Test Content");
        var authority = new KnowledgeAuthority(
            new IdentitySubject("test-person", IdentityScheme.OpenIdConnect),
            CaptureAuthority.Context);

        var artifact = await captureService.CaptureAsync(evidence, authority);

        Assert.Equal("Test Title", artifact.Descriptor.Title);
        Assert.Equal("test-person", artifact.Authority?.Identity.SubjectId);

        var captures = await captureService.GetCapturesAsync(authority);
        Assert.Contains(captures, capture => capture.Reference.Equals(artifact.Reference));

        var retrievedArtifact = await librarian.GetArtifactAsync(artifact.Reference);

        Assert.NotNull(retrievedArtifact);
        Assert.Equal(artifact.Reference, retrievedArtifact!.Reference);

        var representation = Assert.Single(retrievedArtifact.Representations);
        await using var content = await representation.OpenStreamAsync();
        using var reader = new StreamReader(content);
        Assert.Equal("Test Content", await reader.ReadToEndAsync());
    }
}
