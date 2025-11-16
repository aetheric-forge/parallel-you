using ParallelYou.Model.Bus;

namespace ParallelYou.Model.Threads.Messages;

public sealed class SagaCreated(Saga saga) : Message<Saga>(saga) { }
