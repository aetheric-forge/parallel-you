namespace ParallelYou.Model.Threads;

public sealed class Saga
(
    string? id,
    string domainId,
    string title,
    int priority,
    string quantum,
    bool archived = false,
    DateTime? createdAt = null,
    DateTime? updatedAt = null,
    DateTime? archivedAt = null
) : Thread(id, title, priority, quantum, archived, createdAt, updatedAt, archivedAt)
{
    public string DomainId { get; set; } = domainId;
}
