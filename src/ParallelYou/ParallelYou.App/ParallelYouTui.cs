using ParallelYou.Bus.Abstractions;
using ParallelYou.Repo.Abstractions;

namespace ParallelYou.App;

public class ParallelYouTui
{
    private readonly IRepo _repo;
    private readonly IBroker _broker;

    public ParallelYouTui(IRepo repo, IBroker broker)
    {
        _repo = repo;
        _broker = broker;
    }

    // Placeholder TUI loop so the app compiles for tests; real implementation can replace this.
    public Task RunAsync(CancellationToken token)
    {
        // No-op interactive loop; exit immediately.
        return Task.CompletedTask;
    }
}