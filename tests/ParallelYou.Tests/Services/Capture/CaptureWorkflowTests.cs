using Microsoft.Extensions.DependencyInjection;
using ParallelYou.Abstractions.Capture;
using ParallelYou.Web.Hosting;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
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
        
        // Act
        var artifact = await captureService.CaptureAsync(evidence);
        
        // Assert
        Assert.NotNull(artifact);
        Assert.Equal("Test Title", artifact.Descriptor.Title);
        
        // Retrieve
        var retrievedArtifact = await librarian.GetArtifactAsync(artifact.Reference);
        
        // Verify
        Assert.NotNull(retrievedArtifact);
        // Compare references, they should be the same object or have same content
        Assert.Equal(artifact.Reference, retrievedArtifact!.Reference);
    }
}
