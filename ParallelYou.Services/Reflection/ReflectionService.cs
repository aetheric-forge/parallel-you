using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using AethericForge.Runtime.Models.Knowledge.Representations;
using ParallelYou.Abstractions.Reflection;
using ParallelYou.Models.Reflection;

namespace ParallelYou.Services.Reflection;

public class ReflectionService(ILibrarian librarian) : ServiceBase, IReflectionService
{
    private readonly ConcurrentDictionary<Guid, ParallelYou.Models.Reflection.ReflectionService> _reflections = new();

    private async Task PublishReflectionAsync(ParallelYou.Models.Reflection.ReflectionService reflectionService, CancellationToken cancellationToken)
    {
        var descriptor = new KnowledgeDescriptor($"Reflection_{reflectionService.Id}");
        var content = JsonSerializer.Serialize(reflectionService);
        var representation = new KnowledgeRepresentation("application/json",
            content.Length, 
            async _ => await Task.FromResult(new MemoryStream(Encoding.UTF8.GetBytes(content)))
        );
        await librarian.PublishArtifactAsync(descriptor, [representation], ct: cancellationToken);
    }

    public async Task<IReflection> StartReflectionAsync(IReflectionSubject subject, IEnumerable<string> questions, CancellationToken cancellationToken = default)
    {
        var reflection = new ParallelYou.Models.Reflection.ReflectionService
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Questions = questions.ToList()
        };

        _reflections[reflection.Id] = reflection;
        
        await PublishReflectionAsync(reflection, cancellationToken);
        
        return reflection;
    }

    public async Task<bool> AddEvidenceAsync(Guid reflectionId, IReflectionEvidence evidence, CancellationToken cancellationToken = default)
    {
        if (_reflections.TryGetValue(reflectionId, out var reflection))
        {
            reflection.Evidence.Add(evidence);
            await PublishReflectionAsync(reflection, cancellationToken);
        }
        await Task.CompletedTask;

        return true;
    }

    public async Task<bool> AddInsightAsync(Guid reflectionId, IReflectionInsightSubmission insight,
        CancellationToken cancellationToken = default)
    {
        if (_reflections.TryGetValue(reflectionId, out var reflection))
        {
            reflection.Insights.Add(new ReflectionInsight
            {
                Id = insight.Id,
                Content = insight.Content,
                IsAdopted = false
            });
            await PublishReflectionAsync(reflection, cancellationToken);
        }
        await Task.CompletedTask;

        return true;
    }
}
