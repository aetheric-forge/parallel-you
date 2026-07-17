using Moq;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using ParallelYou.Abstractions.Plan;
using ParallelYou.Services.Plan;
using ParallelYou.Tests;

namespace ParallelYou.Tests.Services.Plan;

public class PlanningServiceTests : TestBase
{
    [Fact]
    public async Task PlanAsync_ShouldReturnValidPlan()
    {
        // Arrange
        var service = new PlanningService(MockLibrarian.Object);
        var subject = "Test Subject";

        // Act
        var plan = await service.PlanAsync(subject);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEqual(Guid.Empty, plan.Id);
    }
}
