namespace ParallelYou.Abstractions.Recommendation;

public interface IRecommendation
{
    string Id { get; }
    string CandidateDescription { get; }
}
