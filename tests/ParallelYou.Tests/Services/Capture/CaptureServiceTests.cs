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
        var authority = new Mock<IKnowledgeAuthority>();
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
        var result = await service.CaptureAsync(evidence.Object, authority.Object);

        // Assert
        Assert.NotNull(result);
        MockLibrarian.Verify(l => l.PublishArtifactAsync(
            It.IsAny<IKnowledgeDescriptor>(),
            It.IsAny<IEnumerable<IKnowledgeRepresentation>>(),
            It.IsAny<IEnumerable<IKnowledgeReference>?>(),
            It.Is<IKnowledgeAuthority?>(value => ReferenceEquals(value, authority.Object)),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task CaptureAsync_WithNullEvidence_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new CaptureService(MockLibrarian.Object);

        // Act & Assert
        var authority = new Mock<IKnowledgeAuthority>();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.CaptureAsync(null!, authority.Object));
    }

    [Fact]
    public async Task CaptureAsync_WithNullAuthority_ShouldThrowArgumentNullException()
    {
        var service = new CaptureService(MockLibrarian.Object);
        var evidence = new Mock<ICaptureEvidence>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.CaptureAsync(evidence.Object, null!));
    }

    [Fact]
    public async Task GetCapturesAsync_DelegatesAuthorityToLibrarian()
    {
        var service = new CaptureService(MockLibrarian.Object);
        var authority = new Mock<IKnowledgeAuthority>();
        IReadOnlyCollection<IKnowledgeArtifact> expected =
            [new Mock<IKnowledgeArtifact>().Object];

        MockLibrarian
            .Setup(librarian => librarian.FindArtifactsAsync(
                authority.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await service.GetCapturesAsync(authority.Object);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetCapturesAsync_WithNullAuthority_ShouldThrowArgumentNullException()
    {
        var service = new CaptureService(MockLibrarian.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.GetCapturesAsync(null!));
    }
}
