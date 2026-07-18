using Microsoft.Extensions.DependencyInjection;
using ParallelYou.Abstractions.Capture;
using ParallelYou.Web.Hosting;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication;
using AethericForge.Runtime.Models.Identity.Primitives;
using AethericForge.Runtime.Models.Knowledge.Authorities;
using Xunit;

namespace ParallelYou.Tests.Services.Capture;

public record CaptureEvidence(string Title, string Content) : ICaptureEvidence;

public class CaptureWorkflowTests
{
    [Fact]
    public async Task CaptureAndRetrieve_ReturnsCorrectArtifact()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddForgeCampus();
        var serviceProvider = services.BuildServiceProvider();
        
        var captureService = serviceProvider.GetRequiredService<ICaptureService>();
        var librarian = serviceProvider.GetRequiredService<ILibrarian>();
        
        var evidence = new CaptureEvidence("Test Title", "Test Content");
        var authority = new KnowledgeAuthority(
            new IdentitySubject("test-person", IdentityScheme.OpenIdConnect),
            "ParallelYou.Capture");
        
        // Act
        var artifact = await captureService.CaptureAsync(evidence, authority);
        
        // Assert
        Assert.NotNull(artifact);
        Assert.Equal("Test Title", artifact.Descriptor.Title);
        Assert.Equal("test-person", artifact.Authority?.Identity.SubjectId);
        
        // Retrieve
        var retrievedArtifact = await librarian.GetArtifactAsync(artifact.Reference);
        
        // Verify
        Assert.NotNull(retrievedArtifact);
        // Compare references, they should be the same object or have same content
        Assert.Equal(artifact.Reference, retrievedArtifact!.Reference);
    }
}
