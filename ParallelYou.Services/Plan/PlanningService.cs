using System.Text;
using System.Text.Json;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using AethericForge.Runtime.Models.Knowledge.Representations;
using AethericForge.Runtime.Models.Staging;
using ParallelYou.Abstractions.Plan;
using ParallelYou.Services;

namespace ParallelYou.Services.Plan;

public class PlanningService(ILibrarian librarian, IArtificer artificer) : ServiceBase, IPlanningService
{
    private async Task PublishPlanAsync(ParallelYou.Models.Plan.Plan plan, CancellationToken cancellationToken)
    {
        var content = JsonSerializer.Serialize(plan);
        
        var descriptor = new KnowledgeDescriptor($"Plan_{plan.Id}");
        var representation = new KnowledgeRepresentation("application/json",
            content.Length, 
            async _ => await Task.FromResult(new MemoryStream(Encoding.UTF8.GetBytes(content)))
        );
        var artifact = await librarian.PublishArtifactAsync(descriptor, [representation], ct: cancellationToken);
        
        // Store reference in Artificer
        var referenceContent = JsonSerializer.Serialize(artifact.Reference);
        await artificer.PutAsync("PlanMapping", plan.Id.ToString(), new MemoryStream(Encoding.UTF8.GetBytes(referenceContent)), ct: cancellationToken);
    }

    public async Task<IPlan> PlanAsync(string subject, CancellationToken cancellationToken = default)
    {
        var plan = new ParallelYou.Models.Plan.Plan(Guid.NewGuid(), subject);
        
        await PublishPlanAsync(plan, cancellationToken);
        
        return plan;
    }
}
