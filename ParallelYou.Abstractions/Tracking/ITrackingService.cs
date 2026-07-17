namespace ParallelYou.Abstractions.Tracking;

public interface ITrackingService
{
    Task TrackAsync(ITrackedState state, CancellationToken cancellationToken = default);
    Task<ITrackedState?> GetCurrentStateAsync(Guid subjectId, CancellationToken cancellationToken = default);
}
