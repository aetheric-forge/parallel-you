using ParallelYou.Model.Threads;

namespace ParallelYou.Model.Messages.Threads;


public sealed class DomainCreated(Domain domain) :  Message<Domain>(domain) { }
