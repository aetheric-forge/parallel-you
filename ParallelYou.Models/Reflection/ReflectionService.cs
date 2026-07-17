using System;
using System.Collections.Generic;
using ParallelYou.Abstractions.Reflection;

namespace ParallelYou.Models.Reflection;

public class ReflectionService : IReflection
{
    public Guid Id { get; set; }
    public IReflectionSubject Subject { get; set; }
    public IEnumerable<string> Questions { get; set; }
    public List<IReflectionEvidence> Evidence { get; set; } = new();
    public List<IReflectionInsight> Insights { get; set; } = new();

    IEnumerable<IReflectionEvidence> IReflection.Evidence => Evidence;
    IEnumerable<IReflectionInsight> IReflection.Insights => Insights;
}
