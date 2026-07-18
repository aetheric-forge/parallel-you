using System.Text;
using System.Text.Json;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using AethericForge.Runtime.Models.Knowledge.Representations;
using AethericForge.Runtime.Models.Staging;
using ParallelYou.Abstractions.Tracking;
using ParallelYou.Models.Tracking;

namespace ParallelYou.Services.Tracking;

public class TrackingService(ILibrarian librarian, IArtificer artificer): ServiceBase, ITrackingService
{
    public async Task TrackAsync(ITrackedState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        
        var content = JsonSerializer.Serialize(state);
        await artificer.PutAsync("Tracking", state.SubjectId.ToString(), new MemoryStream(Encoding.UTF8.GetBytes(content)), ct: cancellationToken);
        
        var descriptor = new KnowledgeDescriptor($"Tracking_{state.SubjectId}");
        var representation = new KnowledgeRepresentation("application/json",
            content.Length, 
            async _ => await Task.FromResult(new MemoryStream(Encoding.UTF8.GetBytes(content)))
        );
        await librarian.PublishArtifactAsync(descriptor, [representation], ct: cancellationToken);
    }

    public async Task<ITrackedState?> GetCurrentStateAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        var reference = new StagingReference("Tracking", subjectId.ToString());
        if (!await artificer.ExistsAsync(reference, cancellationToken))
        {
            return null;
        }

        using var stream = await artificer.OpenReadAsync(reference, cancellationToken);
        return await JsonSerializer.DeserializeAsync<TrackedState>(stream, cancellationToken: cancellationToken);
    }
}
