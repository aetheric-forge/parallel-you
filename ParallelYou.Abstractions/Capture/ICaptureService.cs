using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Artifacts;

namespace ParallelYou.Abstractions.Capture;

public interface ICaptureService
{
    Task<IKnowledgeArtifact> CaptureAsync(ICaptureEvidence evidence, CancellationToken cancellationToken = default);
}
