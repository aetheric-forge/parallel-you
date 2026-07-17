using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelYou.Abstractions.Reflection;

public interface IReflectionService
{
    Task<IReflection> StartReflectionAsync(IReflectionSubject subject, IEnumerable<string> questions, CancellationToken cancellationToken = default);
    Task AddEvidenceAsync(Guid reflectionId, IReflectionEvidence evidence, CancellationToken cancellationToken = default);
    Task AddInsightAsync(Guid reflectionId, IReflectionInsight insight, CancellationToken cancellationToken = default);
}
