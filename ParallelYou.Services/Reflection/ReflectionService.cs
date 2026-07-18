using System.Text.Json;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Artifacts;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Authorities;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using AethericForge.Runtime.Models.Knowledge.Representations;
using AethericForge.Runtime.Models.Staging;
using ParallelYou.Abstractions.Reflection;
using ParallelYou.Models.Reflection;

namespace ParallelYou.Services.Reflection;

public sealed class ReflectionService(
    ILibrarian librarian,
    IArtificer artificer) : ServiceBase, IReflectionService
{
    public async Task<IReflection> StartReflectionAsync(
        Guid personId,
        IReflectionSubject subject,
        IEnumerable<string> questions,
        IKnowledgeAuthority authority,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(questions);
        ValidateAuthority(authority);

        var reflection = new ParallelYou.Models.Reflection.ReflectionService
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Subject = new ReflectionSubject
            {
                Id = subject.Id,
                Type = subject.Type,
                Description = subject.Description
            },
            Questions = questions.ToList()
        };

        await PublishReflectionAsync(
            reflection,
            authority,
            lineage: null,
            cancellationToken);

        return reflection;
    }

    public async Task<bool> AddEvidenceAsync(
        Guid reflectionId,
        IReflectionEvidence evidence,
        IKnowledgeAuthority authority,
        IKnowledgeReference? evidenceReference = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ValidateAuthority(authority);

        var loaded = await GetReflectionAsync(
            reflectionId,
            authority,
            cancellationToken);
        if (loaded is null)
        {
            return false;
        }

        loaded.Reflection.Evidence.Add(new ReflectionEvidence
        {
            Id = evidence.Id,
            Content = evidence.Content,
            Provenance = evidence.Provenance
        });

        var lineage = evidenceReference is null
            ? new[] { loaded.Reference }
            : new[] { loaded.Reference, evidenceReference };
        await PublishReflectionAsync(
            loaded.Reflection,
            authority,
            lineage,
            cancellationToken);

        return true;
    }

    public async Task<bool> AddInsightAsync(
        Guid reflectionId,
        IReflectionInsightSubmission insight,
        IKnowledgeAuthority authority,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(insight);
        ValidateAuthority(authority);

        var loaded = await GetReflectionAsync(
            reflectionId,
            authority,
            cancellationToken);
        if (loaded is null)
        {
            return false;
        }

        loaded.Reflection.Insights.Add(new ReflectionInsight
        {
            Id = insight.Id,
            Content = insight.Content,
            IsAdopted = true
        });

        await PublishReflectionAsync(
            loaded.Reflection,
            authority,
            [loaded.Reference],
            cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<IReflection>> GetReflectionsAsync(
        IKnowledgeAuthority authority,
        CancellationToken cancellationToken = default)
    {
        ValidateAuthority(authority);

        var artifacts = await librarian.FindArtifactsAsync(
            authority,
            cancellationToken);
        var reflections = new List<(IReflection Reflection, DateTimeOffset UpdatedAtUtc)>();

        foreach (var artifact in artifacts)
        {
            var reflection = await DeserializeReflectionAsync(
                artifact,
                cancellationToken);
            if (reflection is not null)
            {
                reflections.Add((reflection, artifact.UpdatedAtUtc));
            }
        }

        return reflections
            .GroupBy(item => item.Reflection.Id)
            .Select(group => group
                .OrderByDescending(item => item.UpdatedAtUtc)
                .First())
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Select(item => item.Reflection)
            .ToArray();
    }

    private async Task<LoadedReflection?> GetReflectionAsync(
        Guid id,
        IKnowledgeAuthority authority,
        CancellationToken cancellationToken)
    {
        var mapping = new StagingReference(
            "ReflectionMapping",
            GetMappingKey(authority, id));
        if (!await artificer.ExistsAsync(mapping, cancellationToken))
        {
            return null;
        }

        await using var referenceStream = await artificer.OpenReadAsync(
            mapping,
            cancellationToken);
        var reference = await JsonSerializer.DeserializeAsync<KnowledgeReference>(
            referenceStream,
            cancellationToken: cancellationToken);
        if (reference is null)
        {
            return null;
        }

        var artifact = await librarian.GetArtifactAsync(
            reference,
            cancellationToken);
        if (artifact is null || !HasAuthority(artifact, authority))
        {
            return null;
        }

        var reflection = await DeserializeReflectionAsync(
            artifact,
            cancellationToken);
        return reflection is null
            ? null
            : new LoadedReflection(reflection, artifact.Reference);
    }

    private async Task<IKnowledgeArtifact> PublishReflectionAsync(
        ParallelYou.Models.Reflection.ReflectionService reflection,
        IKnowledgeAuthority authority,
        IEnumerable<IKnowledgeReference>? lineage,
        CancellationToken cancellationToken)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(reflection);
        var artifact = await librarian.PublishArtifactAsync(
            new KnowledgeDescriptor($"Reflection_{reflection.Id}"),
            [new KnowledgeRepresentation(
                "application/json",
                content.LongLength,
                _ => Task.FromResult<Stream>(new MemoryStream(content)),
                encoding: "utf-8")],
            lineage,
            authority,
            cancellationToken);

        var referenceContent = JsonSerializer.SerializeToUtf8Bytes(
            artifact.Reference);
        await artificer.PutAsync(
            "ReflectionMapping",
            GetMappingKey(authority, reflection.Id),
            new MemoryStream(referenceContent),
            ct: cancellationToken);

        return artifact;
    }

    private static async Task<ParallelYou.Models.Reflection.ReflectionService?>
        DeserializeReflectionAsync(
            IKnowledgeArtifact artifact,
            CancellationToken cancellationToken)
    {
        var representation = artifact.Representations.FirstOrDefault(candidate =>
            string.Equals(
                candidate.ContentType,
                "application/json",
                StringComparison.OrdinalIgnoreCase));
        if (representation is null)
        {
            return null;
        }

        await using var stream = await representation.OpenStreamAsync(
            cancellationToken);
        return await JsonSerializer
            .DeserializeAsync<ParallelYou.Models.Reflection.ReflectionService>(
                stream,
                cancellationToken: cancellationToken);
    }

    private static void ValidateAuthority(IKnowledgeAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(authority);
        if (!string.Equals(
                authority.Context,
                ReflectionAuthority.Context,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Reflection authority context must be '{ReflectionAuthority.Context}'.",
                nameof(authority));
        }
    }

    private static bool HasAuthority(
        IKnowledgeArtifact artifact,
        IKnowledgeAuthority authority)
    {
        var candidate = artifact.Authority;
        return candidate is not null &&
               candidate.Identity.Scheme == authority.Identity.Scheme &&
               string.Equals(
                   candidate.Identity.SubjectId,
                   authority.Identity.SubjectId,
                   StringComparison.Ordinal) &&
               string.Equals(
                   candidate.Context,
                   authority.Context,
                   StringComparison.Ordinal);
    }

    private static string GetMappingKey(
        IKnowledgeAuthority authority,
        Guid reflectionId)
        => $"{authority.Identity.Scheme}:{authority.Identity.SubjectId}:{reflectionId:N}";

    private sealed record LoadedReflection(
        ParallelYou.Models.Reflection.ReflectionService Reflection,
        IKnowledgeReference Reference);
}
