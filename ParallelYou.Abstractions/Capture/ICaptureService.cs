namespace ParallelYou.Abstractions.Capture;

public interface ICaptureService
{
    Task CaptureAsync(ICaptureEvidence evidence, CancellationToken cancellationToken = default);
}
