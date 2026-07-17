using Moq;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Artifacts;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Representations;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Authorities;
using ParallelYou.Abstractions.Capture;
using ParallelYou.Services.Capture;
using ParallelYou.Tests;

namespace ParallelYou.Tests.Services.Capture;

public class CaptureServiceTests : TestBase
{
    [Fact]
    public async Task CaptureAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new CaptureService(MockLibrarian.Object);
        
        var evidence = new Mock<ICaptureEvidence>();
        evidence.Setup(e => e.Title).Returns("Test Title");
        evidence.Setup(e => e.Content).Returns("Test Content");

        MockLibrarian.Setup(l => l.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(),
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
            It.IsAny<IEnumerable<IKnowledgeReference>?>(),
            It.IsAny<IKnowledgeAuthority?>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new Mock<IKnowledgeArtifact>().Object);

        // Act
        var result = await service.CaptureAsync(evidence.Object);

        // Assert
        Assert.NotNull(result);
        MockLibrarian.Verify(l => l.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(),
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
            It.IsAny<IEnumerable<IKnowledgeReference>?>(),
            It.IsAny<IKnowledgeAuthority?>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
