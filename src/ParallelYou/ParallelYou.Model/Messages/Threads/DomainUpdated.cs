using ParallelYou.Model.Threads;

namespace ParallelYou.Model.Messages.Threads;

public sealed class DomainUpdated(Domain domain) : Message<Domain>(domain) { }
