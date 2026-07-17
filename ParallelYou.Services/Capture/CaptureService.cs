using ParallelYou.Abstractions.Capture;

namespace ParallelYou.Services.Capture;

public class CaptureService : ServiceBase, ICaptureService
{
    public async Task CaptureAsync(ICaptureEvidence evidence, CancellationToken cancellationToken = default)
    {
        // Implementation logic for Capture capability
        await Task.CompletedTask;
    }
}
