using ParallelYou.Model.Threads;

namespace ParallelYou.Model.Messages.Threads;

public sealed class SagaUpdated(Saga saga) : Message<Saga>(saga) { }
