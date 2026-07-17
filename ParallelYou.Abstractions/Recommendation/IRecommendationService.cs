using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ParallelYou.Abstractions.Recommendation;

public interface IRecommendationService
{
    Task<IEnumerable<IRecommendation>> RecommendAsync(string subject, CancellationToken cancellationToken = default);
}
