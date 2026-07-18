namespace ParallelYou.Abstractions.Plan;

public interface IPlan
{
    Guid Id { get; }
    string Subject { get; }
}
