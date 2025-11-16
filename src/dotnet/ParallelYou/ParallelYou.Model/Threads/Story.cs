namespace ParallelYou.Model.Threads;

public sealed class Story
(
    string? id,
    string sagaId,
    string title,
    int priority,
    string quantum,
    EnergyBand energy = EnergyBand.Moderate,
    StoryState state = StoryState.Ready,
    bool archived = false,
    DateTime? dueAt = null,
    DateTime? createdAt = null,
    DateTime? updatedAt = null,
    DateTime? archivedAt = null
) : Thread(id, title, priority, quantum, archived, createdAt, updatedAt, archivedAt)
{
    public string SagaId { get; set; } = sagaId;
    public DateTime? DueAt { get; set; } = dueAt;
    public EnergyBand Energy = energy;
    public StoryState StoryState = state;
}
