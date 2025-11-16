using ParallelYou.Model.Bus;

namespace ParallelYou.Model.Threads.Messages;

public sealed class SagaUpdated(Saga saga) : Message<Saga>(saga) { }
