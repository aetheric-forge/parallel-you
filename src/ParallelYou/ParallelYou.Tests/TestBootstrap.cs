using ParallelYou.Repo.Abstractions;
using ParallelYou.Repo.Backends;

namespace ParallelYou.Tests;

public static class TestBootstrap
{
    public static IRepo NewRepo() => new InMemoryRepo();
}