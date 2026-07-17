using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelYou.Abstractions.Reflection;
using ParallelYou.Models.Reflection;

namespace ParallelYou.Services.Reflection;

public class ReflectionService : ServiceBase, IReflectionService
{
    private readonly ConcurrentDictionary<Guid, ParallelYou.Models.Reflection.Reflection> _reflections = new();

    public async Task<IReflection> StartReflectionAsync(IReflectionSubject subject, IEnumerable<string> questions, CancellationToken cancellationToken = default)
    {
        var reflection = new ParallelYou.Models.Reflection.Reflection
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Questions = questions.ToList()
        };

        _reflections[reflection.Id] = reflection;
        return await Task.FromResult<IReflection>(reflection);
    }

    public async Task AddEvidenceAsync(Guid reflectionId, IReflectionEvidence evidence, CancellationToken cancellationToken = default)
    {
        if (_reflections.TryGetValue(reflectionId, out var reflection))
        {
            reflection.Evidence.Add(evidence);
        }
        await Task.CompletedTask;
    }

    public async Task AddInsightAsync(Guid reflectionId, IReflectionInsight insight, CancellationToken cancellationToken = default)
    {
        if (_reflections.TryGetValue(reflectionId, out var reflection))
        {
            reflection.Insights.Add(insight);
        }
        await Task.CompletedTask;
    }
}
