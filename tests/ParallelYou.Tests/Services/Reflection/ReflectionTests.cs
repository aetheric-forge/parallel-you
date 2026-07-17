using ParallelYou.Abstractions;
using ParallelYou.Models.Reflection;
using ParallelYou.Services.Reflection;

namespace ParallelYou.Tests.Services.Reflection;

public class ReflectionTests
{
    [Fact]
    public async Task StartReflectionAsync_ShouldCreateReflection()
    {
        var service = new ReflectionService();
        var subject = new ReflectionSubject { Id = Guid.NewGuid(), Type = "Experience", Description = "Test" };
        var questions = new List<string> { "Question 1" };

        var reflection = await service.StartReflectionAsync(subject, questions);

        Assert.NotNull(reflection);
        Assert.Equal(subject.Id, reflection.Subject.Id);
        Assert.Single(reflection.Questions);
        Assert.Empty(reflection.Evidence);
        Assert.Empty(reflection.Insights);
    }

    [Fact]
    public async Task AddEvidenceAsync_ShouldAddEvidence()
    {
        var service = new ReflectionService();
        var subject = new ReflectionSubject { Id = Guid.NewGuid(), Type = "Experience", Description = "Test" };
        var reflection = await service.StartReflectionAsync(subject, new List<string>());
        var evidence = new ReflectionEvidence { Id = Guid.NewGuid(), Content = "Test Evidence", Provenance = new Provenance(ProvenanceKind.Declared, "Test Source", DateTimeOffset.UtcNow) };

        await service.AddEvidenceAsync(reflection.Id, evidence);

        Assert.Single(reflection.Evidence);
        Assert.Equal(evidence.Id, reflection.Evidence.First().Id);
    }

    [Fact]
    public async Task AddInsightAsync_ShouldAddInsight()
    {
        var service = new ReflectionService();
        var subject = new ReflectionSubject { Id = Guid.NewGuid(), Type = "Experience", Description = "Test" };
        var reflection = await service.StartReflectionAsync(subject, new List<string>());
        var insight = new ReflectionInsightSubmission { Id = Guid.NewGuid(), Content = "Test Insight" };

        await service.AddInsightAsync(reflection.Id, insight);

        Assert.Single(reflection.Insights);
        Assert.Equal(insight.Id, reflection.Insights.First().Id);
        Assert.False(reflection.Insights.First().IsAdopted);
    }

    [Fact]
    public async Task AddEvidenceAsync_WhenReflectionNotFound_ShouldNotThrow()
    {
        var service = new ReflectionService();
        var evidence = new ReflectionEvidence { Id = Guid.NewGuid(), Content = "Test Evidence", Provenance = new Provenance(ProvenanceKind.Declared, "Test Source", DateTimeOffset.UtcNow) };

        await service.AddEvidenceAsync(Guid.NewGuid(), evidence);
    }

    [Fact]
    public async Task AddInsightAsync_WhenReflectionNotFound_ShouldNotThrow()
    {
        var service = new ReflectionService();
        var insight = new ReflectionInsightSubmission { Id = Guid.NewGuid(), Content = "Test Insight" };

        var success = await service.AddInsightAsync(Guid.NewGuid(), insight);
        
        Assert.True(success);
    }
}
