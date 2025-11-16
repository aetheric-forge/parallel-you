namespace ParallelYou.Model.Messages;

public abstract class Message
(
    Guid? id,                                   // construct with `null` to have default Guid.NewGuid() assignment at construction
    string? type,                               // routing key (null → derived from runtime type)
    DateTime? timestamp = null,                 // leave null to get current timestamp
    Guid? causationId = null,                   // currently unused 
    Guid? correlationId = null                 // ditto
)
{
    private readonly string? _providedType = type;

    public Guid Id { get; } = id ?? Guid.NewGuid();
    // If not provided, compute from the actual runtime type (e.g., DomainCreated → "domain.created")
    public string Type => _providedType ?? RoutingKeyFor(GetType());
    public DateTime Timestamp { get; } = timestamp ?? DateTime.UtcNow;
    public Guid? CausationId { get; } = causationId;
    public Guid? CorrelationId { get; } = correlationId;

    protected static string RoutingKeyFor<TEvent>() => RoutingKeyFor(typeof(TEvent));

    private static string RoutingKeyFor(Type t)
    {
        var name = t.Name; // e.g. "DomainCreated"
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
                sb.Append('.');

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString(); // "domain.created"
    }
}

public abstract class Message<TPayload>
(
    TPayload payload,
    string? type = null,
    Guid? id = null,
    DateTime? timestamp = null,
    Guid? causationId = null,
    Guid? correlationId = null
) : Message(id, type, timestamp, causationId, correlationId)
{
    public TPayload Payload { get; } = payload;
}
