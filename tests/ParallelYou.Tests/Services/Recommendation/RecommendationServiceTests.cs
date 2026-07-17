using System.Threading.Tasks;
using ParallelYou.Abstractions.Recommendation;
using ParallelYou.Services.Recommendation;
using Xunit;

namespace ParallelYou.Tests.Services.Recommendation;

public sealed record Recommendation(string Id, string CandidateDescription) : IRecommendation;

public class RecommendationServiceTests : TestBase
{
    [Fact]
    public async Task RecommendAsync_ShouldReturnEmptyListInitially()
    {
        // Arrange
        var service = new RecommendationService(MockLibrarian.Object);
        var subject = "Test Subject";

        // Act
        var result = await service.RecommendAsync(subject);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task RecommendAsync_WithEmptySubject_ShouldReturnEmptyList()
    {
        // Arrange
        var service = new RecommendationService(MockLibrarian.Object);
        var subject = string.Empty;

        // Act
        var result = await service.RecommendAsync(subject);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task RecommendAsync_WithNullSubject_ShouldReturnEmptyList()
    {
        // Arrange
        var service = new RecommendationService(MockLibrarian.Object);
        string? subject = null;

        // Act
        var result = await service.RecommendAsync(subject!);

        // Assert
        Assert.Empty(result);
    }
}
