using ParallelYou.Model.Bus;

namespace ParallelYou.Model.Threads.Messages;


public sealed class DomainCreated(Domain domain) :  Message<Domain>(domain) { }
