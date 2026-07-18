using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using AethericForge.Runtime.Models.Knowledge.Representations;
using ParallelYou.Abstractions.Tracking;

namespace ParallelYou.Services.Tracking;

public class TrackingService(ILibrarian librarian): ServiceBase, ITrackingService
{
    private static readonly ConcurrentDictionary<Guid, ITrackedState> _states = new();

    public async Task TrackAsync(ITrackedState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        
        _states[state.SubjectId] = state;
        
        var descriptor = new KnowledgeDescriptor($"Tracking_{state.SubjectId}");
        var content = JsonSerializer.Serialize(state);
        var representation = new KnowledgeRepresentation("application/json",
            content.Length, 
            async _ => await Task.FromResult(new MemoryStream(Encoding.UTF8.GetBytes(content)))
        );
        await librarian.PublishArtifactAsync(descriptor, [representation], ct: cancellationToken);
    }

    public async Task<ITrackedState?> GetCurrentStateAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_states.GetValueOrDefault(subjectId));
    }
}
