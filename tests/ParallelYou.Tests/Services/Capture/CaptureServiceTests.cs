using ParallelYou.Abstractions.Capture;
using ParallelYou.Services.Capture;

namespace ParallelYou.Tests.Services.Capture;

public sealed record CaptureEvidence(object evidence) : ICaptureEvidence;

public class CaptureServiceTests
{
    [Fact]
    public async Task CaptureAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new CaptureService();
        var evidence = new object();

        // Act
        var exception = await Record.ExceptionAsync(() => service.CaptureAsync(new CaptureEvidence(evidence)));

        // Assert
        Assert.Null(exception);
    }
}
