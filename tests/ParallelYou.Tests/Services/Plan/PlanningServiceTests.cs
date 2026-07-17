using ParallelYou.Abstractions.Plan;
using ParallelYou.Services.Plan;

namespace ParallelYou.Tests.Services.Plan;

public class PlanningServiceTests
{
    [Fact]
    public async Task PlanAsync_ShouldReturnValidPlan()
    {
        // Arrange
        var service = new PlanningService();
        var subject = "Test Subject";

        // Act
        var plan = await service.PlanAsync(subject);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEqual(Guid.Empty, plan.Id);
    }
}
