using Moq;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;

namespace ParallelYou.Tests;

public abstract class TestBase
{
    protected readonly Mock<ILibrarian> MockLibrarian = new();
}
