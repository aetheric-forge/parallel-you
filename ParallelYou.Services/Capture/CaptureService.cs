using System.Text;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Artifacts;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using AethericForge.Runtime.Models.Knowledge.Representations;
using ParallelYou.Abstractions.Capture;

namespace ParallelYou.Services.Capture;

public class CaptureService(ILibrarian librarian) : ServiceBase, ICaptureService
{
    public async Task<IKnowledgeArtifact> CaptureAsync(ICaptureEvidence evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        
        var descriptor = new KnowledgeDescriptor(evidence.Title);
        var representation = new KnowledgeRepresentation("application/octet-stream",
            evidence.Content.Length, 
            async _ => await Task.FromResult(new MemoryStream(Encoding.UTF8.GetBytes(evidence.Content)))
        );
        var artifact = await librarian.PublishArtifactAsync(descriptor, [representation], ct: cancellationToken);
        
        return artifact;
    }
}
