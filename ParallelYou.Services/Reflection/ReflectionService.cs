using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using AethericForge.Runtime.Models.Knowledge.Representations;
using AethericForge.Runtime.Models.Staging;
using ParallelYou.Abstractions.Reflection;
using ParallelYou.Models.Reflection;

namespace ParallelYou.Services.Reflection;

public class ReflectionService(ILibrarian librarian, IArtificer artificer) : ServiceBase, IReflectionService
{
    private async Task<ParallelYou.Models.Reflection.ReflectionService?> GetReflectionAsync(Guid id, CancellationToken cancellationToken)
    {
        var referenceReference = new StagingReference("ReflectionMapping", id.ToString());
        if (!await artificer.ExistsAsync(referenceReference, cancellationToken))
        {
            return null;
        }

        using var referenceStream = await artificer.OpenReadAsync(referenceReference, cancellationToken);
        var reference = await JsonSerializer.DeserializeAsync<KnowledgeReference>(referenceStream, cancellationToken: cancellationToken);
        if (reference == null)
        {
            return null;
        }

        var artifact = await librarian.GetArtifactAsync(reference, cancellationToken);
        if (artifact == null)
        {
            return null;
        }

        // Assuming JSON representation
        var representation = artifact.Representations.FirstOrDefault(r => r.ContentType == "application/json");
        if (representation == null)
        {
            return null;
        }

        using var stream = await representation.OpenStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<ParallelYou.Models.Reflection.ReflectionService>(stream, cancellationToken: cancellationToken);
    }

    private async Task PublishReflectionAsync(ParallelYou.Models.Reflection.ReflectionService reflectionService, CancellationToken cancellationToken)
    {
        var content = JsonSerializer.Serialize(reflectionService);
        
        var descriptor = new KnowledgeDescriptor($"Reflection_{reflectionService.Id}");
        var representation = new KnowledgeRepresentation("application/json",
            content.Length, 
            async _ => await Task.FromResult(new MemoryStream(Encoding.UTF8.GetBytes(content)))
        );
        var artifact = await librarian.PublishArtifactAsync(descriptor, [representation], ct: cancellationToken);
        
        // Store reference in Artificer
        var referenceContent = JsonSerializer.Serialize(artifact.Reference);
        await artificer.PutAsync("ReflectionMapping", reflectionService.Id.ToString(), new MemoryStream(Encoding.UTF8.GetBytes(referenceContent)), ct: cancellationToken);
    }

    public async Task<IReflection> StartReflectionAsync(IReflectionSubject subject, IEnumerable<string> questions, CancellationToken cancellationToken = default)
    {
        var reflection = new ParallelYou.Models.Reflection.ReflectionService
        {
            Id = Guid.NewGuid(),
            Subject = new ReflectionSubject { Id = subject.Id, Type = subject.Type, Description = subject.Description },
            Questions = questions.ToList()
        };
        
        await PublishReflectionAsync(reflection, cancellationToken);
        
        return reflection;
    }

    public async Task<bool> AddEvidenceAsync(Guid reflectionId, IReflectionEvidence evidence, CancellationToken cancellationToken = default)
    {
        var reflection = await GetReflectionAsync(reflectionId, cancellationToken);
        if (reflection == null)
        {
            return false;
        }

        reflection.Evidence.Add(new ReflectionEvidence { Id = evidence.Id, Content = evidence.Content, Provenance = evidence.Provenance });
        await PublishReflectionAsync(reflection, cancellationToken);

        return true;
    }

    public async Task<bool> AddInsightAsync(Guid reflectionId, IReflectionInsightSubmission insight,
        CancellationToken cancellationToken = default)
    {
        var reflection = await GetReflectionAsync(reflectionId, cancellationToken);
        if (reflection == null)
        {
            return false;
        }

        reflection.Insights.Add(new ReflectionInsight
        {
            Id = insight.Id,
            Content = insight.Content,
            IsAdopted = false
        });
        await PublishReflectionAsync(reflection, cancellationToken);
        
        return true;
    }
}
