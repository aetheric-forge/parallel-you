namespace ParallelYou.Models.Recommendation;

public record Recommendation : ParallelYou.Abstractions.Recommendation.IRecommendation
{
    public required string Id { get; init; }
    public required string CandidateDescription { get; init; }
}
