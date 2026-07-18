using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Linq;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using AethericForge.Runtime.Models.Knowledge.Primitives;
using AethericForge.Runtime.Models.Staging;
using ParallelYou.Abstractions.Recommendation;
using ParallelYou.Models.Recommendation;

namespace ParallelYou.Services.Recommendation;

public class RecommendationService(ILibrarian librarian, IArtificer artificer) : ServiceBase, IRecommendationService
{
    public async Task<IEnumerable<IRecommendation>> RecommendAsync(string subject, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return Enumerable.Empty<IRecommendation>();

        // 1. Try to get a recommendation reference from Artificer
        var referenceReference = new StagingReference("RecommendationMapping", subject); // Assume subject as key
        if (!await artificer.ExistsAsync(referenceReference, cancellationToken))
        {
            return Enumerable.Empty<IRecommendation>();
        }

        // 2. Open and read the reference
        using var referenceStream = await artificer.OpenReadAsync(referenceReference, cancellationToken);
        var reference = await JsonSerializer.DeserializeAsync<KnowledgeReference>(referenceStream, cancellationToken: cancellationToken);
        if (reference == null)
            return Enumerable.Empty<IRecommendation>();

        // 3. Get artifact from Librarian
        var artifact = await librarian.GetArtifactAsync(reference, cancellationToken);
        if (artifact == null)
            return Enumerable.Empty<IRecommendation>();

        // 4. Read representation (assuming JSON)
        var representation = artifact.Representations.FirstOrDefault(r => r.ContentType == "application/json");
        if (representation == null)
            return Enumerable.Empty<IRecommendation>();

        using var stream = await representation.OpenStreamAsync(cancellationToken);
        var recommendation = await JsonSerializer.DeserializeAsync<ParallelYou.Models.Recommendation.Recommendation>(stream, cancellationToken: cancellationToken);

        if (recommendation == null)
            return Enumerable.Empty<IRecommendation>();

        return new List<IRecommendation> { recommendation };
    }
}
