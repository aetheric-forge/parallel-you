using ParallelYou.Model.Threads;

namespace ParallelYou.Model.Messages.Threads;

public sealed class SagaCreated(Saga saga) : Message<Saga>(saga) { }
