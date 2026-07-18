using Moq;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;

namespace ParallelYou.Tests;

public abstract class TestBase
{
    protected readonly Mock<ILibrarian> MockLibrarian = new();
    protected readonly Mock<IArtificer> MockArtificer = new();
}
