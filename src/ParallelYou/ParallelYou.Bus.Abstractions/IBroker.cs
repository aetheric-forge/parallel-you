using ParallelYou.Model.Messages;

namespace ParallelYou.Bus.Abstractions;

public interface IBroker
{
    Task Publish(Message msg);
    Task Emit(string routingKey, IReadOnlyDictionary<string, object> payload, IReadOnlyDictionary<string, object>? meta = null);
    void Route(string routingKey, MessageHandler handler);
    ITransport Transport { get; }
}
