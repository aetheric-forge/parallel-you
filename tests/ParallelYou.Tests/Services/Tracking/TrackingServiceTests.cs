using Moq;
using ParallelYou.Abstractions.Tracking;
using ParallelYou.Services.Tracking;

namespace ParallelYou.Tests.Services.Tracking;

public class TrackingServiceTests
{
    [Fact]
    public async Task TrackAsync_ShouldSucceed()
    {
        var service = new TrackingService();
        var mockSubject = new Mock<ITrackedSubject>();
        var mockState = new Mock<ITrackedState>();

        await service.TrackAsync(mockState.Object);
        
        // No exceptions should be thrown
    }

    [Fact]
    public async Task GetCurrentStateAsync_ShouldReturnNullInitially()
    {
        var service = new TrackingService();
        var mockSubject = new Mock<ITrackedSubject>();

        var result = await service.GetCurrentStateAsync(mockSubject.Object.Id);

        Assert.Null(result);
    }
}
