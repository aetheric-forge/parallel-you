using ParallelYou.Model.Bus;

namespace ParallelYou.Model.Threads.Messages;

public sealed class DomainUpdated(Domain domain) : Message<Domain>(domain) { }
