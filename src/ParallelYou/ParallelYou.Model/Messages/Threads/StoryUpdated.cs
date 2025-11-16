using ParallelYou.Model.Threads;

namespace ParallelYou.Model.Messages.Threads;

public sealed class StoryUpdated(Story story): Message<Story>(story) { }
