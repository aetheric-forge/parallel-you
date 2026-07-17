using ParallelYou.Abstractions.Tracking;

namespace ParallelYou.Services.Tracking;

public class TrackingService : ServiceBase, ITrackingService
{
    public async Task TrackAsync(ITrackedState state, CancellationToken cancellationToken = default)
    {
        // Implementation
        await Task.CompletedTask;
    }

    public async Task<ITrackedState?> GetCurrentStateAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        // Implementation
        return await Task.FromResult<ITrackedState?>(null);
    }
}
